import collections
import collections.abc
import time
import weakref

# ok, what's going on here?
# we used to have a 'tag_ids_to_tags = {}' style cache for some tag and hash calls. it was pretty basic but saved SQLite time on fetching expensive strings. it uses a funky 'populate' pre-call
# every time the current population request caused it to overflow 100,000 items or whatever, it would cull itself back to a copy of a dict of the current request
# this caused memory bloat and fragmentation due to discarding the old dict so frequently _combined_ with the fact that SQLite was producing new, non-interned strings/hashes/whatever
# we ended up with fifteen copies of a tag after cycles of heavy maintenance or file re-search work that would churn the cache
# we are making a new cache with these goals:
    # we now try to track every item of type x currently used in the program
    # every item of that type has exactly one copy
    # it'd be nice if it has a grace period, too, so if the user refreshes a big media page, we don't just trigger weakref in the reload gap and clear out/repopulate wastefully

# We've achieved this with a weakref and an LRU with optional TTL

# of course, recall, that strs and bytes can't be weakreffed. this is not such a big deal for hashes, which tend to be de-duplicated via the media results weakref cache, but it is for tags
# thus, this is something we'll want to revisit when we eventually make a richer 'Tag' object, and importantly so, since we'll want one canonical Tag with siblings and such hung off it

# btw performance matters here and it may be faster to give this guy an object rather than a tuple of ( timeout, value ), so consider that
class IdToObjectCacheTTL( object ):
    
    def __init__( self, name: str, ttl: int, max_strong_size: int ):
        
        self._name = name
        self._ttl = ttl
        self._max_strong_size = max_strong_size
        
        self._ids_to_values_weak = weakref.WeakValueDictionary()
        self._ids_to_values_strong = collections.OrderedDict()
        
    
    def __contains__( self, key ):
        
        return key in self._ids_to_values_weak
        
    
    def __getitem__( self, key: int ):
        
        obj = self._ids_to_values_weak.get( key )
        
        if obj is None:
            
            raise KeyError( f'An Id Cache ({self._name}) was asked for key "{key}", but it does not hold an entry for that! This should never happen, please let hydev know.' )
            
        
        self._touch( key, obj )
        
        return obj
        
    
    def __setitem__( self, key: int, obj: object ):
        
        self._ids_to_values_weak[ key ] = obj
        
        self._touch( key, obj )
        
    
    def _touch( self, key, obj ):
        
        timeout = time.monotonic() + self._ttl
        
        if key in self._ids_to_values_strong:
            
            self._ids_to_values_strong[ key ] = ( timeout, obj )
            self._ids_to_values_strong.move_to_end( key )
            
        else:
            
            self._ids_to_values_strong[ key ] = ( timeout, obj )
            
        
    
    def _touch_many( self, keys_to_objs: dict[ int, object ] ):
        
        for ( key, obj ) in keys_to_objs.items():
            
            timeout = time.monotonic() + self._ttl
            
            if key in self._ids_to_values_strong:
                
                self._ids_to_values_strong[ key ] = ( timeout, obj )
                self._ids_to_values_strong.move_to_end( key )
                
            else:
                
                self._ids_to_values_strong[ key ] = ( timeout, obj )
                
            
        
    
    def clear( self ):
        
        self._ids_to_values_weak = weakref.WeakValueDictionary()
        self._ids_to_values_strong = collections.OrderedDict()
        
    
    def maintain_touch_record( self ):
        
        num_removed = 0
        
        max_deletes_this_call = max( 1, int( len( self._ids_to_values_weak ) / 16 ) )
        
        now = time.monotonic()
        
        num_to_remove_for_timeout = 0
        
        for ( current_timeout, _ ) in self._ids_to_values_strong.values():
            
            if current_timeout > now:
                
                break
                
            
            num_to_remove_for_timeout += 1
            
            if num_to_remove_for_timeout >= max_deletes_this_call:
                
                break
                
            
        
        if num_to_remove_for_timeout > 0:
            
            for i in range( num_to_remove_for_timeout ):
                
                self._ids_to_values_strong.popitem( last = False )
                
            
            num_removed += num_to_remove_for_timeout
            
        
        oversize_quantity = len( self._ids_to_values_strong ) - self._max_strong_size
        
        num_to_remove_for_oversize = min( oversize_quantity, max_deletes_this_call - num_removed )
        
        if num_to_remove_for_oversize > 0:
            
            for i in range( num_to_remove_for_oversize ):
                
                self._ids_to_values_strong.popitem( last = False )
                
            
        
    
    def touch_many_if_have( self, keys: collections.abc.Iterable[ int ] ):
        
        # we are being careful here. the caller is saying 'hey I'm about to populate you with these guys, so if you have any of them, touch them so they stay in memory for a bit
        
        for key in keys:
            
            # we have to be careful here. if a guy asks this guy if he has something, we immediately hold on to a reference and then touch it strongly so it stays in memory!
            
            obj = self._ids_to_values_weak.get( key )
            
            if obj is not None:
                
                self._touch( key, obj )
                
            
        
    
    
    def update( self, keys_to_objs: dict[ int, object ] ):
        
        self._ids_to_values_weak.update( keys_to_objs )
        
        self._touch_many( keys_to_objs )
        
    
class IdToPrimitiveCacheTTL( object ):
    
    def __init__( self, name: str, ttl: int, min_size: int, max_size: int ):
        
        self._name = name
        self._ttl = ttl
        self._min_size = min_size
        self._max_size = max_size
        
        self._ids_to_values = collections.OrderedDict()
        
    
    def __contains__( self, key ):
        
        return key in self._ids_to_values
        
    
    def __getitem__( self, key: int ):
        
        ( _, obj ) = self._ids_to_values.get( key )
        
        if obj is None:
            
            raise KeyError( f'An Id Cache ({self._name}) was asked for key "{key}", but it does not hold an entry for that! This should never happen, please let hydev know.' )
            
        
        self._touch( key, obj )
        
        return obj
        
    
    def __setitem__( self, key: int, obj: object ):
        
        self._touch( key, obj )
        
    
    def _touch( self, key, obj ):
        
        timeout = time.monotonic() + self._ttl
        
        if key in self._ids_to_values:
            
            self._ids_to_values[ key ] = ( timeout, obj )
            self._ids_to_values.move_to_end( key )
            
        else:
            
            self._ids_to_values[ key ] = ( timeout, obj )
            
        
    
    def _touch_many( self, keys_to_objs: dict[ int, object ] ):
        
        for ( key, obj ) in keys_to_objs.items():
            
            timeout = time.monotonic() + self._ttl
            
            if key in self._ids_to_values:
                
                self._ids_to_values[ key ] = ( timeout, obj )
                self._ids_to_values.move_to_end( key )
                
            else:
                
                self._ids_to_values[ key ] = ( timeout, obj )
                
            
        
    
    def clear( self ):
        
        self._ids_to_values = collections.OrderedDict()
        
    
    def maintain_touch_record( self ):
        
        if len( self._ids_to_values ) <= self._min_size:
            
            return
            
        
        num_removed = 0
        max_deletes_this_call = max( 1, int( len( self._ids_to_values ) / 16 ) )
        
        now = time.monotonic()
        
        num_to_remove_for_timeout = 0
        
        for ( current_timeout, _ ) in self._ids_to_values.values():
            
            if current_timeout > now:
                
                break
                
            
            num_to_remove_for_timeout += 1
            
            if num_to_remove_for_timeout >= max_deletes_this_call:
                
                break
                
            
        
        if num_to_remove_for_timeout > 0:
            
            for i in range( num_to_remove_for_timeout ):
                
                self._ids_to_values.popitem( last = False )
                
            
            num_removed += num_to_remove_for_timeout
            
        
        oversize_quantity = len( self._ids_to_values ) - self._max_size
        
        num_to_remove_for_oversize = min( oversize_quantity, max_deletes_this_call - num_removed )
        
        if num_to_remove_for_oversize > 0:
            
            for i in range( num_to_remove_for_oversize ):
                
                self._ids_to_values.popitem( last = False )
                
            
        
    
    def update( self, keys_to_objs: dict[ int, object ] ):
        
        self._touch_many( keys_to_objs )
        
    

# roughly 16MB per 100k integers, sans the string or whatever 'value' that is held
class IdToPrimitiveCache( object ):
    
    def __init__( self, name: str, max_size: int ):
        
        self._name = name
        self._max_size = max_size
        
        self._ids_to_values = collections.OrderedDict()
        
    
    def __contains__( self, key ):
        
        return key in self._ids_to_values
        
    
    def __delitem__( self, key: int ):
        
        del self._ids_to_values[ key ]
        
    
    def __getitem__( self, key: int ):
        
        obj = self._ids_to_values.get( key )
        
        if obj is None:
            
            raise KeyError( f'An Id Cache ({self._name}) was asked for key "{key}", but it does not hold an entry for that! This should never happen, please let hydev know.' )
            
        
        self._ids_to_values.move_to_end( key )
        
        return obj
        
    
    def __setitem__( self, key: int, obj: object ):
        
        if key in self._ids_to_values:
            
            self._ids_to_values.move_to_end( key )
            
        else:
            
            self._ids_to_values[ key ] = obj
            
        
    
    def clear( self ):
        
        self._ids_to_values.clear()
        
    
    def maintain_touch_record( self ):
        
        current_size = len( self._ids_to_values )
        
        if current_size > self._max_size:
            
            max_deletes_this_call = max( 1, int( current_size / 16 ) )
            
            num_to_remove = min( current_size - self._max_size, max_deletes_this_call )
            
            for i in range( num_to_remove ):
                
                self._ids_to_values.popitem( last = False )
                
            
        
    
    def update( self, keys_to_objs: dict[ int, object ] ):
        
        for ( key, obj ) in keys_to_objs.items():
            
            if key in self._ids_to_values:
                
                self._ids_to_values.move_to_end( key )
                
            else:
                
                self._ids_to_values[ key ] = obj
                
            
        
    
