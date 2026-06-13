import unittest

from unittest import mock

from hydrus.core import HydrusExceptions
from hydrus.core import HydrusTime

from hydrus.client.networking import ClientNetworkingConstants as CNC
from hydrus.client.networking import ClientNetworkingNetworkContextSettings

class TestSettings( unittest.TestCase ):
    
    def test_normal( self ):
        
        network_context_settings = ClientNetworkingNetworkContextSettings.NetworkContextSettings()
        
        self.assertRaises( HydrusExceptions.DataMissing, network_context_settings.GetSettingOrRaise, CNC.NETWORK_CONTEXT_SETTING_MAX_ACTIVE_NETWORK_JOBS )
        self.assertRaises( NotImplementedError, network_context_settings.GetSettingOrRaise, 'hello' )
        
        network_context_settings.SetSetting( CNC.NETWORK_CONTEXT_SETTING_MAX_ACTIVE_NETWORK_JOBS, 3 )
        
        self.assertEqual( network_context_settings.GetSettingOrRaise( CNC.NETWORK_CONTEXT_SETTING_MAX_ACTIVE_NETWORK_JOBS ), 3 )
        
        #
        
        network_context_settings.SetSetting( CNC.NETWORK_CONTEXT_SETTING_NETWORK_INFRASTRUCTURE_PROBLEMS_HALT_VELOCITY, [ 2, None ] )
        
        self.assertEqual( network_context_settings.Duplicate().GetSettingOrRaise( CNC.NETWORK_CONTEXT_SETTING_NETWORK_INFRASTRUCTURE_PROBLEMS_HALT_VELOCITY ), [ 2, None ] )
        
        #
        
        network_context_settings.SetSetting( CNC.NETWORK_CONTEXT_SETTING_CURL_CFFI_NAME, 'muh_browser' )
        
        self.assertEqual( network_context_settings.Duplicate().GetSettingOrRaise( CNC.NETWORK_CONTEXT_SETTING_CURL_CFFI_NAME ), 'muh_browser' )
        
    

class TestStatus( unittest.TestCase ):
    
    def test_normal( self ):
        
        domain_status = ClientNetworkingNetworkContextSettings.NetworkContextStatus()
        
        time_delta_s = 0.5
        time_delta_ms = int( time_delta_s * 1000 )
        now_ms = 100000
        older_than_limit_ms = now_ms - int( time_delta_ms * 1.2 )
        within_limit_ms = now_ms - int( time_delta_ms * 0.6 )
        much_later_ms = now_ms + 100000
        
        self.assertTrue( domain_status.IsStub() )
        
        with mock.patch.object( HydrusTime, 'GetNowMS', return_value = now_ms ):
            
            domain_status.CleanseOldRecords( time_delta_s )
            
        
        self.assertTrue( domain_status.IsStub() )
        
        with mock.patch.object( HydrusTime, 'GetNowMS', return_value = older_than_limit_ms ):
            
            domain_status.RegisterDomainEvent( CNC.NETWORK_CONTEXT_EVENT_NETWORK_INFRASTRUCTURE )
            
        
        with mock.patch.object( HydrusTime, 'GetNowMS', return_value = now_ms ):
            
            self.assertEqual( domain_status.NumberOfEvents( CNC.NETWORK_CONTEXT_EVENT_NETWORK_INFRASTRUCTURE, time_delta_s ), 0 )
            self.assertEqual( domain_status.NumberOfEvents( CNC.NETWORK_CONTEXT_EVENT_SERVERSIDE_BANDWIDTH, time_delta_s ), 0 )
            
        
        with mock.patch.object( HydrusTime, 'GetNowMS', return_value = within_limit_ms ):
            
            domain_status.RegisterDomainEvent( CNC.NETWORK_CONTEXT_EVENT_NETWORK_INFRASTRUCTURE )
            
        
        with mock.patch.object( HydrusTime, 'GetNowMS', return_value = now_ms ):
            
            self.assertEqual( domain_status.NumberOfEvents( CNC.NETWORK_CONTEXT_EVENT_NETWORK_INFRASTRUCTURE, time_delta_s ), 1 )
            self.assertEqual( domain_status.NumberOfEvents( CNC.NETWORK_CONTEXT_EVENT_SERVERSIDE_BANDWIDTH, time_delta_s ), 0 )
            
        
        with mock.patch.object( HydrusTime, 'GetNowMS', return_value = within_limit_ms + 5 ):
            
            domain_status.RegisterDomainEvent( CNC.NETWORK_CONTEXT_EVENT_NETWORK_INFRASTRUCTURE )
            
        
        with mock.patch.object( HydrusTime, 'GetNowMS', return_value = now_ms ):
            
            self.assertEqual( domain_status.NumberOfEvents( CNC.NETWORK_CONTEXT_EVENT_NETWORK_INFRASTRUCTURE, time_delta_s ), 2 )
            
            self.assertEqual( domain_status.NumberOfEvents( CNC.NETWORK_CONTEXT_EVENT_NETWORK_INFRASTRUCTURE, time_delta_s * 2 ), 3 )
            
            self.assertEqual( domain_status.NumberOfEvents( CNC.NETWORK_CONTEXT_EVENT_NETWORK_INFRASTRUCTURE, time_delta_s * 0.25 ), 0 )
            
            self.assertEqual( domain_status.NumberOfEvents( CNC.NETWORK_CONTEXT_EVENT_SERVERSIDE_BANDWIDTH, time_delta_s ), 0 )
            
            domain_status.CleanseOldRecords( time_delta_s )
            
            self.assertEqual( domain_status.NumberOfEvents( CNC.NETWORK_CONTEXT_EVENT_NETWORK_INFRASTRUCTURE, time_delta_s * 2 ), 2 )
            
        
        with mock.patch.object( HydrusTime, 'GetNowMS', return_value = much_later_ms ):
            
            self.assertEqual( domain_status.NumberOfEvents( CNC.NETWORK_CONTEXT_EVENT_SERVERSIDE_BANDWIDTH, time_delta_s ), 0 )
            
            self.assertEqual( domain_status.NumberOfEvents( CNC.NETWORK_CONTEXT_EVENT_NETWORK_INFRASTRUCTURE, time_delta_s ), 0 )
            
            domain_status.CleanseOldRecords( time_delta_s )
            
            self.assertEqual( domain_status.NumberOfEvents( CNC.NETWORK_CONTEXT_EVENT_NETWORK_INFRASTRUCTURE, time_delta_s ), 0 )
            
            self.assertTrue( domain_status.IsStub() )
            
        
    
