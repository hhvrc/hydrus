from qtpy import QtCore as QC
from qtpy import QtWidgets as QW

from hydrus.client import ClientConstants as CC
from hydrus.client.gui import ClientGUIFunctions
from hydrus.client.gui import QtPorting as QP
from hydrus.client.gui.panels.options import ClientGUIOptionsPanelBase
from hydrus.client.gui.widgets import ClientGUICommon

class ImportingPanel( ClientGUIOptionsPanelBase.OptionsPagePanel ):
    
    def __init__( self, parent, new_options ):
        
        super().__init__( parent )
        
        self._new_options = new_options
        
        #
        
        work_slots_panel = ClientGUICommon.StaticBox( self, 'work slots' )
        
        self._thread_slots_misc = ClientGUICommon.BetterSpinBox( work_slots_panel, 10, 1, 500 )
        self._thread_slots_gallery_files = ClientGUICommon.BetterSpinBox( work_slots_panel, 5, 1, 500 )
        self._thread_slots_gallery_search = ClientGUICommon.BetterSpinBox( work_slots_panel, 15, 1, 500 )
        self._thread_slots_watcher_files = ClientGUICommon.BetterSpinBox( work_slots_panel, 5, 1, 500 )
        self._thread_slots_watcher_check = ClientGUICommon.BetterSpinBox( work_slots_panel, 15, 1, 500 )
        
        #
        
        drag_and_drop = ClientGUICommon.StaticBox( self, 'drag and drop' )
        
        self._show_destination_page_when_dnd_url = QW.QCheckBox( drag_and_drop )
        self._show_destination_page_when_dnd_url.setToolTip( ClientGUIFunctions.WrapToolTip( 'When dropping a URL on the program, should we switch to the destination page?' ) )
        
        #
        
        self._thread_slots_misc.setValue( self._new_options.GetInteger( 'thread_slots_misc' ) )
        self._thread_slots_gallery_files.setValue( self._new_options.GetInteger( 'thread_slots_gallery_files' ) )
        self._thread_slots_gallery_search.setValue( self._new_options.GetInteger( 'thread_slots_gallery_search' ) )
        self._thread_slots_watcher_files.setValue( self._new_options.GetInteger( 'thread_slots_watcher_files' ) )
        self._thread_slots_watcher_check.setValue( self._new_options.GetInteger( 'thread_slots_watcher_check' ) )
        
        #
        
        self._show_destination_page_when_dnd_url.setChecked( self._new_options.GetBoolean( 'show_destination_page_when_dnd_url' ) )
        
        #
        
        rows = []
        
        rows.append( ( 'Number of gallery downloader file queues that can import at the same time:', self._thread_slots_gallery_files ) )
        rows.append( ( 'Number of gallery downloader searches that can run at the same time:', self._thread_slots_gallery_search ) )
        rows.append( ( 'Number of watcher page file queues that can run at the same time:', self._thread_slots_watcher_files ) )
        rows.append( ( 'Number of watcher page checkers that can run at the same time:', self._thread_slots_watcher_check ) )
        rows.append( ( 'Number of other paged importer jobs that can run at the same time:', self._thread_slots_misc ) )
        
        gridbox = ClientGUICommon.WrapInGrid( work_slots_panel, rows )
        
        label = 'ADVANCED! DO NOT CHANGE THIS UNLESS YOU UNDERSTAND IT.'
        
        st_warning = ClientGUICommon.BetterStaticText( work_slots_panel, label = label )
        st_warning.setObjectName( 'HydrusWarning' )
        st_warning.setAlignment( QC.Qt.AlignmentFlag.AlignCenter )
        
        label = 'Due to current technical limitations, the hydrus import pages are throttled so that only so many can run in parallel. In UI, this is the difference between "pending" and "working" job status. Unfortunately, a page that is "working" but blocked on bandwidth or similar will nonetheless consume a slot.'
        
        st = ClientGUICommon.BetterStaticText( work_slots_panel, label = label )
        st.setWordWrap( True )
        
        work_slots_panel.Add( st_warning, CC.FLAGS_EXPAND_PERPENDICULAR )
        work_slots_panel.Add( st, CC.FLAGS_EXPAND_PERPENDICULAR )
        work_slots_panel.Add( gridbox, CC.FLAGS_EXPAND_SIZER_PERPENDICULAR )
        
        #
        
        rows = []
        
        rows.append( ( 'When DnDing a URL onto the program, switch to the download page:', self._show_destination_page_when_dnd_url ) )
        
        gridbox = ClientGUICommon.WrapInGrid( drag_and_drop, rows )
        
        drag_and_drop.Add( gridbox, CC.FLAGS_EXPAND_SIZER_PERPENDICULAR )
        
        #
        
        vbox = QP.VBoxLayout()
        
        QP.AddToLayout( vbox, work_slots_panel, CC.FLAGS_EXPAND_PERPENDICULAR )
        QP.AddToLayout( vbox, drag_and_drop, CC.FLAGS_EXPAND_PERPENDICULAR )
        vbox.addStretch( 0 )
        
        self.setLayout( vbox )
        
    
    def UpdateOptions( self ):
        
        self._new_options.SetBoolean( 'show_destination_page_when_dnd_url', self._show_destination_page_when_dnd_url.isChecked() )
        
        self._new_options.SetInteger( 'thread_slots_misc', self._thread_slots_misc.value() )
        self._new_options.SetInteger( 'thread_slots_gallery_files', self._thread_slots_gallery_files.value() )
        self._new_options.SetInteger( 'thread_slots_gallery_search', self._thread_slots_gallery_search.value() )
        self._new_options.SetInteger( 'thread_slots_watcher_files', self._thread_slots_watcher_files.value() )
        self._new_options.SetInteger( 'thread_slots_watcher_check', self._thread_slots_watcher_check.value() )
        
    
