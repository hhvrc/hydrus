from qtpy import QtCore as QC
from qtpy import QtWidgets as QW

from hydrus.client import ClientConstants as CC
from hydrus.client.gui import QtPorting as QP
from hydrus.client.gui.panels import ClientGUIScrolledPanels
from hydrus.client.gui.widgets import ClientGUICommon

class EditTextSpinboxPanel( ClientGUIScrolledPanels.EditPanel ):
    
    def __init__( self, parent: QW.QWidget, message: str, default = 0, min_value = None, max_value = None ):
        
        super().__init__( parent )
        
        self._spinbox = QW.QSpinBox( self )
        
        if min_value is not None:
            
            self._spinbox.setMinimum( min_value )
            
        
        if max_value is not None:
            
            self._spinbox.setMaximum( max_value )
            
        
        self._spinbox.setValue( default )
        
        #
        
        st_message = ClientGUICommon.BetterStaticText( self, message )
        st_message.setWordWrap( True )
        
        vbox = QP.VBoxLayout()
        
        QP.AddToLayout( vbox, st_message, CC.FLAGS_EXPAND_PERPENDICULAR )
        QP.AddToLayout( vbox, self._spinbox, CC.FLAGS_EXPAND_PERPENDICULAR )
        
        vbox.addStretch( 0 )
        
        self.widget().setLayout( vbox )
        
        self._spinbox.setFocus( QC.Qt.FocusReason.OtherFocusReason )
        
    
    def GetValue( self ):
        
        return self._spinbox.value()
        
    
