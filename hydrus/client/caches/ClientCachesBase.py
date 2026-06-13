import collections
import collections.abc
import threading
import time
import typing

from hydrus.core import HydrusData
from hydrus.core import HydrusGlobals as HG

from hydrus.client import ClientGlobals as CG

class CacheableObject( object ):
    
    def GetEstimatedMemoryFootprint( self ) -> int:
        
        raise NotImplementedError()
        
    
    def IsFinishedLoading( self ):
        
        raise NotImplementedError()
        
    

class DataCacheEntry( object ):
    
    def __init__( self, data: CacheableObject ):
        
        self.data = data
        self.size_estimate = data.GetEstimatedMemoryFootprint()
        self.last_access_time = time.monotonic()
        
    
    def touch( self ):
        
        self.last_access_time = time.monotonic()
        
    

class DataCache( object ):
    
    def __init__( self, controller: "CG.ClientController.Controller", name, cache_size, timeout = 1200 ):
        
        self._controller = controller
        self._name = name
        self._cache_size = cache_size
        self._timeout = timeout
        
        self._keys_to_data: collections.OrderedDict[ typing.Any, DataCacheEntry ] = collections.OrderedDict()
        
        self._total_estimated_memory_footprint = 0
        
        self._lock = threading.Lock()
        
        self._controller.sub( self, 'MaintainCache', 'memory_maintenance_pulse' )
        
    
    def _Delete( self, key ):
        
        if key not in self._keys_to_data:
            
            return
            
        
        entry = self._keys_to_data[ key ]
        
        del self._keys_to_data[ key ]
        
        self._total_estimated_memory_footprint -= entry.size_estimate
        
        if HG.cache_report_mode:
            
            HydrusData.ShowText( 'Cache "{}" removing specific item "{}", size "{}". Current size {}.'.format( self._name, key, HydrusData.ToHumanBytes( entry.size_estimate ), HydrusData.ConvertValueRangeToBytes( self._total_estimated_memory_footprint, self._cache_size ) ) )
            
        
    
    def _DeleteLoadedItemsUntilFreeSpace( self, free_space_desired: int ) -> bool:
        
        current_free_space = self._cache_size - self._total_estimated_memory_footprint
        
        if current_free_space > free_space_desired:
            
            return True
            
        
        deletee_keys = []
        
        expected_free_space = self._cache_size - self._total_estimated_memory_footprint
        
        for ( key, entry ) in self._keys_to_data.items():
            
            if not entry.data.IsFinishedLoading():
                
                # this guy is still rendering, let's not push him out for a different prefetch
                continue
                
            
            deletee_keys.append( key )
            expected_free_space += entry.size_estimate
            
            if expected_free_space > free_space_desired:
                
                break
                
            
        
        if expected_free_space > free_space_desired:
            
            for key in deletee_keys:
                
                self._Delete( key )
                
            
            return True
            
        else:
            
            # we went through the whole cache and couldn't find enough easy freed-up space to fit this new guy in. not a good time to prefetch it!
            return False
            
        
    
    def _DeleteOldestItem( self ):
        
        ( key, entry ) = self._keys_to_data.popitem( last = False )
        
        self._total_estimated_memory_footprint -= entry.size_estimate
        
        if HG.cache_report_mode:
            
            HydrusData.ShowText( 'Cache "{}" removing oldest item "{}", size "{}". Current size {}.'.format( self._name, key, HydrusData.ToHumanBytes( entry.size_estimate ), HydrusData.ConvertValueRangeToBytes( self._total_estimated_memory_footprint, self._cache_size ) ) )
            
        
    
    def _GetData( self, key ) -> CacheableObject:
        
        if key not in self._keys_to_data:
            
            raise Exception( f'Cache error! Looking for "{key}", but it was missing.' )
            
        
        self._TouchKey( key )

        entry = self._keys_to_data[ key ]
        
        data = entry.data
        size_estimate = entry.size_estimate
        
        new_estimate = data.GetEstimatedMemoryFootprint()
        
        if new_estimate != size_estimate:
            
            self._total_estimated_memory_footprint += new_estimate - size_estimate
            
            entry.size_estimate = new_estimate
            
        
        return data
        
    
    def _TouchKey( self, key ):
        
        self._keys_to_data[ key ].touch()
        self._keys_to_data.move_to_end( key )
        
    
    def Clear( self ):
        
        with self._lock:
            
            self._keys_to_data.clear()
            
            self._total_estimated_memory_footprint = 0
            
        
    
    def AddData( self, key, data: CacheableObject ):
        
        with self._lock:
            
            if key not in self._keys_to_data:
                
                while self._total_estimated_memory_footprint > self._cache_size:
                    
                    self._DeleteOldestItem()
                    
                
                # an implicit touch is happening here
                
                entry = DataCacheEntry( data )
                
                self._keys_to_data[ key ] = entry
                
                self._total_estimated_memory_footprint += entry.size_estimate
                
                if HG.cache_report_mode:
                    
                    HydrusData.ShowText(
                        'Cache "{}" adding "{}" ({}). Current size {}.'.format(
                            self._name,
                            key,
                            HydrusData.ToHumanBytes( entry.size_estimate ),
                            HydrusData.ConvertValueRangeToBytes( self._total_estimated_memory_footprint, self._cache_size )
                        )
                    )
                    
                
            
        
    
    def DeleteData( self, key ):
        
        with self._lock:
            
            self._Delete( key )
            
        
    
    def GetAllKeys( self ) -> list[ object ]:
        
        with self._lock:
            
            return list( self._keys_to_data.keys() )
            
        
    
    def GetData( self, key ) -> CacheableObject:
        
        with self._lock:
            
            return self._GetData( key )
            
        
    
    def GetIfHasData( self, key ) -> CacheableObject | None:
        
        with self._lock:
            
            if key in self._keys_to_data:
                
                return self._GetData( key )
                
            else:
                
                return None
                
            
        
    
    def GetSizeLimit( self ) -> int:
        
        with self._lock:
            
            return self._cache_size
            
        
    
    def HasData( self, key ) -> bool:
        
        with self._lock:
            
            return key in self._keys_to_data
            
        
    
    def MaintainCache( self ) -> None:
        
        with self._lock:
            
            while self._total_estimated_memory_footprint > self._cache_size and len( self._keys_to_data ) > 0: # little sanity check haha
                
                self._DeleteOldestItem()
                
            
            if len( self._keys_to_data ) > 0:
                
                older_than_this_has_timed_out = time.monotonic() - self._timeout
                
                num_to_remove = 0
                
                for entry in self._keys_to_data.values():
                    
                    if entry.last_access_time < older_than_this_has_timed_out:
                        
                        num_to_remove += 1
                        
                    else:
                        
                        break
                        
                    
                
                if num_to_remove > 0:
                    
                    for i in range( num_to_remove ):
                        
                        self._DeleteOldestItem()
                        
                    
                
            
        
    
    def SetCacheSizeAndTimeout( self, cache_size, timeout ) -> None:
        
        with self._lock:
            
            self._cache_size = cache_size
            self._timeout = timeout
            
        
        self.MaintainCache()
        
    
    def TouchKey( self, key ):
        
        with self._lock:
            
            if key not in self._keys_to_data:
                
                return
                
            
            self._TouchKey( key )
            
        
    
    def TryToFlushEasySpaceForPrefetch( self, free_space_desired: int ):
        
        # ok, caller wants to do a prefetch. question is, is there enough soft space in the fifo queue to jam another guy in?
        # or, are we actually pretty busy now with image rendering and stuff and we don't really want to cut too hard?
        # let's see if we can free up some space and let the caller know
        
        with self._lock:
            
            successful = self._DeleteLoadedItemsUntilFreeSpace( free_space_desired )
            
            return successful
            
        
    
