from qtpy import QtCore as QC
from qtpy import QtWidgets as QW

from hydrus.core import HydrusConstants as HC

from hydrus.client import ClientConstants as CC
from hydrus.client import ClientGlobals as CG
from hydrus.client.gui import ClientGUIFunctions
from hydrus.client.gui import ClientGUIMenus
from hydrus.client.gui import QtPorting as QP

SystemTrayAvailable = QW.QSystemTrayIcon.isSystemTrayAvailable

class ClientSystemTrayIcon( QW.QSystemTrayIcon ):
    
    flip_show_ui = QC.Signal()
    flip_pause_network_jobs = QC.Signal()
    flip_pause_subscription_jobs = QC.Signal()
    activation = QC.Signal()
    exit_client = QC.Signal()
    
    def __init__( self, parent: QW.QWidget ):
        
        super().__init__( parent )
        
        self._parent_widget = parent
        
        self._ui_is_currently_shown = True
        self._should_always_show = False
        
        self._show_hide_menu_item = None
        self._network_traffic_menu_item = None
        self._subscriptions_paused_menu_item = None
        
        self._minimise_client_to_system_tray_menu_item = None
        self._close_client_to_system_tray_menu_item = None
        self._start_client_in_system_tray_menu_item = None
        
        icon = CC.global_icons().hydrus_system_tray
        
        self.setIcon( icon )
        
        self.activated.connect( self._ClickActivated )
        
        self._RegenerateMenu()
        
    
    def _ClickActivated( self, activation_reason ):
        
        # if we click immediately, some users get frozen ui, I assume a mix-up with the icon being destroyed during the same click event or similar
        
        CG.client_controller.CallAfterQtSafe( self, self._WasActivated, activation_reason )
        
    
    def _FlipSimpleBool( self, name ):
        
        CG.client_controller.new_options.FlipBoolean( name )
        
        self._UpdateSimpleBooleanMenuItemChecks()
        
    
    def _RegenerateMenu( self ):
        
        # I'm not a qwidget, but a qobject, so use my parent for this
        new_menu = ClientGUIMenus.GenerateMenu( self._parent_widget )
        
        self._show_hide_menu_item = ClientGUIMenus.AppendMenuItem( new_menu, 'show/hide', 'Hide or show the hydrus client', self.flip_show_ui.emit )
        
        self._UpdateShowHideMenuItemLabel()
        
        ClientGUIMenus.AppendSeparator( new_menu )
        
        self._network_traffic_menu_item = ClientGUIMenus.AppendMenuCheckItem( new_menu, 'pause network traffic', 'Pause/resume network traffic', False, self.flip_pause_network_jobs.emit )
        
        self._subscriptions_paused_menu_item = ClientGUIMenus.AppendMenuCheckItem( new_menu, 'pause subscriptions', 'Pause/resume subscriptions', False, self.flip_pause_subscription_jobs.emit )
        
        ClientGUIMenus.AppendSeparator( new_menu )
        
        options_menu = ClientGUIMenus.GenerateMenu( new_menu )
        
        self._minimise_client_to_system_tray_menu_item = ClientGUIMenus.AppendMenuCheckItem( options_menu, 'minimise client to system tray', 'Set whether the client should shrink to the taskbar on minimise or hide to system tray.', False, self._FlipSimpleBool, 'minimise_client_to_system_tray' )
        self._close_client_to_system_tray_menu_item = ClientGUIMenus.AppendMenuCheckItem( options_menu, 'close client to system tray', 'Set whether the client should exit the program on close or hide to system tray.', False, self._FlipSimpleBool, 'close_client_to_system_tray' )
        self._start_client_in_system_tray_menu_item = ClientGUIMenus.AppendMenuCheckItem( options_menu, 'start client in system tray', 'Set whether the client should boot up hidden to system tray.', False, self._FlipSimpleBool, 'start_client_in_system_tray' )
        
        ClientGUIMenus.AppendMenu( new_menu, options_menu, 'options' )
        
        ClientGUIMenus.AppendSeparator( new_menu )
        
        ClientGUIMenus.AppendMenuItem( new_menu, 'exit', 'Close the hydrus client', self.exit_client.emit )
        
        #
        
        old_menu = self.contextMenu()
        
        self.RegenOptionsCheckboxes()
        
        self.setContextMenu( new_menu )
        
        if old_menu is not None:
            
            ClientGUIMenus.DestroyMenu( old_menu )
            
        
    
    def _UpdateNetworkTrafficMenuItemCheck( self ):
        
        if self._network_traffic_menu_item is not None:
            
            self._network_traffic_menu_item.setChecked( CG.client_controller.new_options.GetBoolean( 'pause_all_new_network_traffic' ) )
            
        
    
    def _UpdateShowHideMenuItemLabel( self ):
        
        label = 'hide to system tray' if self._ui_is_currently_shown else 'show'
        
        self._show_hide_menu_item.setText( label )
        
    
    def _UpdateShowSelf( self ) -> bool:
        
        menu_regenerated = False
        
        should_show = self._should_always_show or not self._ui_is_currently_shown
        
        if should_show != self.isVisible():
            
            self.setVisible( should_show )
            
            if should_show:
                
                # apparently context menu needs to be regenerated on re-show
                
                self._RegenerateMenu()
                
                menu_regenerated = True
                
            
        
        return menu_regenerated
        
    
    def _UpdateSimpleBooleanMenuItemChecks( self ):
        
        if self._minimise_client_to_system_tray_menu_item is not None:
            
            self._minimise_client_to_system_tray_menu_item.setChecked( CG.client_controller.new_options.GetBoolean( 'minimise_client_to_system_tray' ) )
            
        
        if self._close_client_to_system_tray_menu_item is not None:
            
            self._close_client_to_system_tray_menu_item.setChecked( CG.client_controller.new_options.GetBoolean( 'close_client_to_system_tray' ) )
            
        
        if self._start_client_in_system_tray_menu_item is not None:
            
            self._start_client_in_system_tray_menu_item.setChecked( CG.client_controller.new_options.GetBoolean( 'start_client_in_system_tray' ) )
            
        
    
    def _UpdateSubscriptionsMenuItemCheck( self ):
        
        if self._subscriptions_paused_menu_item is not None:
            
            self._subscriptions_paused_menu_item.setChecked( CG.client_controller.new_options.GetBoolean( 'pause_subs_sync' ) )
            
        
    
    def _UpdateTooltip( self ):
        
        app_display_name = CG.client_controller.new_options.GetString( 'app_display_name' )
        
        tt = app_display_name
        
        if CG.client_controller.new_options.GetBoolean( 'pause_all_new_network_traffic' ):
            
            tt = '{} - network traffic paused'.format( tt )
            
        
        if CG.client_controller.new_options.GetBoolean( 'pause_subs_sync' ):
            
            tt = '{} - subscriptions paused'.format( tt )
            
        
        if self.toolTip != tt:
            
            self.setToolTip( ClientGUIFunctions.WrapToolTip( tt ) )
            
        
    
    def _WasActivated( self, activation_reason ):
        
        if not QP.isValid( self ) or HC.PLATFORM_MACOS:
            
            return
            
        
        if activation_reason in ( QW.QSystemTrayIcon.ActivationReason.Unknown, QW.QSystemTrayIcon.ActivationReason.Trigger ):
            
            self.activation.emit()
            
        elif activation_reason == QW.QSystemTrayIcon.ActivationReason.MiddleClick:
            
            self.flip_show_ui.emit()
            
        
    
    def RegenOptionsCheckboxes( self ):
        
        self._UpdateNetworkTrafficMenuItemCheck()
        self._UpdateSubscriptionsMenuItemCheck()
        self._UpdateSimpleBooleanMenuItemChecks()
        
        self._UpdateTooltip()
        
    
    def SetUIIsCurrentlyShown( self, ui_is_currently_shown: bool ):
        
        if ui_is_currently_shown != self._ui_is_currently_shown:
            
            self._ui_is_currently_shown = ui_is_currently_shown
            
            menu_regenerated = self._UpdateShowSelf()
            
            if not menu_regenerated:
                
                self._UpdateShowHideMenuItemLabel()
                
            
        
    
    def SetShouldAlwaysShow( self, should_always_show: bool ):
        
        if should_always_show != self._should_always_show:
            
            self._should_always_show = should_always_show
            
            self._UpdateShowSelf()
            
        
