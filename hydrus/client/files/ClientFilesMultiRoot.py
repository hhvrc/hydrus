"""
Multi-root file storage support for dual/multi-boot setups.

Activated by placing an `override_paths.json` in the hydrus install root
(next to hydrus_client.py). Completely reversible: delete the config to disable.

Example override_paths.json:

{
    "db_dir": "/mnt/uwu/hydrus",
    "files_dir": "/mnt/nas/hydrus/files",
    "thumbnails_dir": "/mnt/uwu/hydrus/thumbs"
}

All fields are optional. When set, they override the default path resolution
for that component. The db_dir override replaces the --db_dir argument or
default db directory. files_dir and thumbnails_dir override where file/thumbnail
base locations resolve to, regardless of what the database stores.
"""

import json
import os

from hydrus.core import HydrusConstants as HC
from hydrus.core import HydrusData

_instance = None

def GetConfig():

    global _instance

    if _instance is None:

        _instance = OverridePathsConfig()


    return _instance


def ResetConfig():

    global _instance

    _instance = None


class OverridePathsConfig( object ):

    def __init__( self ):

        self._db_dir = None
        self._files_dir = None
        self._thumbnails_dir = None
        self._active = False

        self._Load()


    def _Load( self ):

        config_path = os.path.join( HC.BASE_DIR, 'override_paths.json' )

        if not os.path.exists( config_path ):

            return


        try:

            with open( config_path, 'r' ) as f:

                data = json.load( f )


            if not isinstance( data, dict ):

                HydrusData.Print( 'override_paths.json: root should be a dict, ignoring.' )

                return


            if 'db_dir' in data and data[ 'db_dir' ] is not None:

                self._db_dir = os.path.normpath( data[ 'db_dir' ] )


            if 'files_dir' in data and data[ 'files_dir' ] is not None:

                self._files_dir = os.path.normpath( data[ 'files_dir' ] )


            if 'thumbnails_dir' in data and data[ 'thumbnails_dir' ] is not None:

                self._thumbnails_dir = os.path.normpath( data[ 'thumbnails_dir' ] )


            self._active = any( x is not None for x in ( self._db_dir, self._files_dir, self._thumbnails_dir ) )

            if self._active:

                parts = []

                if self._db_dir is not None: parts.append( f'db_dir={self._db_dir}' )
                if self._files_dir is not None: parts.append( f'files_dir={self._files_dir}' )
                if self._thumbnails_dir is not None: parts.append( f'thumbnails_dir={self._thumbnails_dir}' )

                HydrusData.Print( f'Override paths config loaded: {", ".join( parts )}' )


        except Exception as e:

            HydrusData.Print( f'Failed to load override_paths.json: {e}' )



    def IsActive( self ):

        return self._active


    def GetDBDirOverride( self ):

        return self._db_dir


    def GetFilesDirOverride( self ):

        return self._files_dir


    def GetThumbnailsDirOverride( self ):

        return self._thumbnails_dir


    def ResolveBaseLocationPath( self, original_path, db_dir ):
        """
        Given a base location's resolved absolute path, check if it should
        be overridden based on whether it's a files or thumbnails location.

        Hydrus stores base locations as portable paths like "files" or "thumbs"
        which resolve to db_dir/files or db_dir/thumbs. This method intercepts
        that resolved path and substitutes the override if configured.
        """

        if not self._active:

            return original_path


        try:

            relative = os.path.relpath( original_path, db_dir )

        except ValueError:

            return original_path


        relative_parts = os.path.normpath( relative ).replace( '\\', '/' ).split( '/' )

        if len( relative_parts ) == 0:

            return original_path


        first_component = relative_parts[ 0 ]

        if first_component == 'files' and self._files_dir is not None:

            if len( relative_parts ) > 1:

                return os.path.join( self._files_dir, *relative_parts[ 1: ] )

            else:

                return self._files_dir



        if first_component == 'thumbs' and self._thumbnails_dir is not None:

            if len( relative_parts ) > 1:

                return os.path.join( self._thumbnails_dir, *relative_parts[ 1: ] )

            else:

                return self._thumbnails_dir



        return original_path


    def GetAlternativePaths( self, original_path, base_location_path, db_dir ):
        """
        Given a full file path and its base location path, return alternative
        paths by substituting with the override directory.
        """

        if not self._active:

            return []


        override_base = self.ResolveBaseLocationPath( base_location_path, db_dir )

        if override_base == base_location_path:

            return []


        try:

            relative = os.path.relpath( original_path, base_location_path )

        except ValueError:

            return []


        return [ os.path.join( override_base, relative ) ]


    def SubfolderExistsAnywhere( self, subfolder_path, base_location_path, db_dir ):
        """
        Check if a subfolder path (or its equivalent under an override root)
        actually exists on disk.
        """

        if os.path.exists( subfolder_path ) and os.path.isdir( subfolder_path ):

            return True


        if not self._active:

            return False


        for alt_path in self.GetAlternativePaths( subfolder_path, base_location_path, db_dir ):

            if os.path.exists( alt_path ) and os.path.isdir( alt_path ):

                return True



        return False


