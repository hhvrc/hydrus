import json
import os
import sqlite3

from hydrus.core import HydrusData

from hydrus.client.db import ClientDBModule

# Change-data-capture outbox for the postgres read model.
#
# Rows are written INSIDE hydrus's own transaction, alongside the real write, so
# a rollback discards them atomically and the drain can never see a change that
# hydrus did not commit. Nothing here does network IO -- that would stall the
# single db thread, which is the whole client's bottleneck.
#
# Payloads store hydrus's internal ids (hash_id, tag_id, service_id), NOT
# resolved hashes. Resolving in here would mean a lookup per row on the hot
# path; the drain reads the same SQLite file and joins against `hashes` and
# `tags` itself. hash_ids are never recycled by hydrus, so this is safe as long
# as the drain does not lag past a database rebuild.
#
# Wiring: one line per call site, via the module-level Record* functions below.
# They no-op when the module is not loaded, so an unpatched or disabled client
# costs an attribute lookup and nothing else.

OUTBOX_TABLE_VERSION = 681

# how many inserts between watermark checks; see _MaybePrune
PRUNE_CHECK_PERIOD = 4096

WATERMARK_FILENAME = 'pg_outbox_watermark'

OP_FILE_INFO      = 'file_info'
OP_FILE_STATUS    = 'file_status'
OP_MAPPINGS       = 'mappings'
OP_URL_ADD        = 'url_add'
OP_URL_DELETE     = 'url_del'
OP_NOTE_SET       = 'note_set'
OP_NOTE_DELETE    = 'note_del'
OP_PHASH_ADD      = 'phash_add'
OP_PHASH_DELETE   = 'phash_del'


class ClientDBPostgresOutbox( ClientDBModule.ClientDBModule ):

    # The outbox is a transient queue, not a store of record. Losing it costs a
    # backfill, nothing more -- so hydrus should show the gentle "recoverable"
    # missing-tables dialog rather than the alarming one.
    CAN_REPOPULATE_ALL_MISSING_DATA = True

    def __init__( self, cursor: sqlite3.Cursor, db_dir: str, enabled: bool ):

        super().__init__( 'client postgres outbox', cursor )

        self._db_dir = db_dir
        self._enabled = enabled
        self._inserts_since_prune_check = 0


    def _GetInitialTableGenerationDict( self ) -> dict:

        # Declaring nothing when disabled keeps an unconfigured client totally
        # inert: hydrus's boot-time _RepairDB (HydrusDB.py:437) never notices a
        # missing table, so there is no warning dialog and no schema change to
        # the user's database until they opt in.
        if not self._enabled:

            return {}


        return {
            # AUTOINCREMENT matters: it guarantees seq is monotonic and never
            # reused, which is what makes the drain's `WHERE seq > last_seq`
            # cursor correct. A plain INTEGER PRIMARY KEY can reuse the rowid of
            # a deleted row and would silently skip changes after a prune.
            'main.pg_outbox' : ( 'CREATE TABLE IF NOT EXISTS {} ( seq INTEGER PRIMARY KEY AUTOINCREMENT, op TEXT, payload TEXT );', OUTBOX_TABLE_VERSION )
        }


    def IsEnabled( self ) -> bool:

        return self._enabled


    def GetTablesAndColumnsThatUseDefinitions( self, content_type: int ) -> list[ tuple[ str, str ] ]:

        # Payloads are opaque JSON, so hydrus's definition-cleanup cannot and
        # should not rewrite them. Stale ids in undrained rows are the drain's
        # problem to skip, not something to rewrite here.

        return []


    def _Write( self, op: str, payload ):

        self._Execute(
            'INSERT INTO pg_outbox ( op, payload ) VALUES ( ?, ? );',
            ( op, json.dumps( payload, separators = ( ',', ':' ) ) )
        )

        self._MaybePrune()


    def _MaybePrune( self ):

        # The drain opens hydrus's SQLite read-only -- it must never write to a
        # database a running client owns. So it drops a watermark file instead
        # and we do the delete from in here, inside hydrus's own transaction.

        self._inserts_since_prune_check += 1

        if self._inserts_since_prune_check < PRUNE_CHECK_PERIOD:

            return


        self._inserts_since_prune_check = 0

        try:

            watermark_path = os.path.join( self._db_dir, WATERMARK_FILENAME )

            if not os.path.exists( watermark_path ):

                return


            with open( watermark_path, 'r', encoding = 'utf-8' ) as f:

                watermark = int( f.read().strip() )


            self._Execute( 'DELETE FROM pg_outbox WHERE seq <= ?;', ( watermark, ) )

        except Exception as e:

            # Never let bookkeeping break a real content update.
            HydrusData.Print( 'postgres outbox: could not prune: {}'.format( e ) )


    # -- record calls, one per hooked write path ----------------------------

    def RecordFilesInfo( self, rows ):

        # ( hash_id, size, mime, width, height, duration, num_frames, has_audio, num_words )
        rows = list( rows )

        if len( rows ) == 0:

            return


        self._Write( OP_FILE_INFO, { 'r' : rows } )


    def RecordFileStatus( self, service_id: int, hash_ids, status: int, timestamp_ms = None ):

        hash_ids = list( hash_ids )

        if len( hash_ids ) == 0:

            return


        self._Write( OP_FILE_STATUS, { 's' : service_id, 'h' : hash_ids, 'st' : status, 't' : timestamp_ms } )


    def RecordMappings( self, tag_service_id: int, status: int, mappings_ids, removing: bool ):

        # mappings_ids is an iterable of ( tag_id, hash_ids ).
        rows = [ ( tag_id, list( hash_ids ) ) for ( tag_id, hash_ids ) in mappings_ids ]

        rows = [ row for row in rows if len( row[1] ) > 0 ]

        if len( rows ) == 0:

            return


        self._Write( OP_MAPPINGS, { 's' : tag_service_id, 'st' : status, 'rm' : removing, 'r' : rows } )


    def RecordURL( self, hash_id: int, url: str, added: bool ):

        self._Write( OP_URL_ADD if added else OP_URL_DELETE, { 'h' : hash_id, 'u' : url } )


    def RecordNote( self, hash_id: int, name: str, note = None ):

        if note is None:

            self._Write( OP_NOTE_DELETE, { 'h' : hash_id, 'n' : name } )

        else:

            self._Write( OP_NOTE_SET, { 'h' : hash_id, 'n' : name, 'b' : note } )


    def RecordPerceptualHashes( self, hash_id: int, perceptual_hash_ids, added: bool ):

        perceptual_hash_ids = list( perceptual_hash_ids )

        if len( perceptual_hash_ids ) == 0:

            return


        self._Write( OP_PHASH_ADD if added else OP_PHASH_DELETE, { 'h' : hash_id, 'p' : perceptual_hash_ids } )


# ---------------------------------------------------------------------------
# Module-level shim.
#
# The hooks call these rather than holding a reference, so patching a call site
# is a single line and needs no constructor change in the module being hooked.
# That keeps the upstream diff small, which matters -- hydrus ships weekly.
# ---------------------------------------------------------------------------

_instance = None


def SetInstance( module ):

    global _instance

    _instance = module if ( module is not None and module.IsEnabled() ) else None


def RecordFilesInfo( rows ):

    if _instance is not None: _instance.RecordFilesInfo( rows )


def RecordFileStatus( service_id, hash_ids, status, timestamp_ms = None ):

    if _instance is not None: _instance.RecordFileStatus( service_id, hash_ids, status, timestamp_ms )


def RecordMappings( tag_service_id, status, mappings_ids, removing ):

    if _instance is not None: _instance.RecordMappings( tag_service_id, status, mappings_ids, removing )


def RecordURL( hash_id, url, added ):

    if _instance is not None: _instance.RecordURL( hash_id, url, added )


def RecordNote( hash_id, name, note = None ):

    if _instance is not None: _instance.RecordNote( hash_id, name, note )


def RecordPerceptualHashes( hash_id, perceptual_hash_ids, added ):

    if _instance is not None: _instance.RecordPerceptualHashes( hash_id, perceptual_hash_ids, added )
