from hydrus.client import ClientConstants as CC
from hydrus.client import ClientGlobals as CG
from hydrus.client import ClientLocation
from hydrus.client.gui.pages import ClientGUIPageManager
from hydrus.client.gui.pages import ClientGUIPages
from hydrus.client.gui.pages import ClientGUIPagesCore
from hydrus.client.media import ClientMediaCollect
from hydrus.client.media import ClientMediaSort
from hydrus.client.metadata import ClientTags
from hydrus.client.search import ClientSearchFileSearchContext
from hydrus.client.search import ClientSearchTagContext
from hydrus.core import HydrusExceptions

def MakeNewPageFromAPI( page_type, page_name, page_of_pages_key, focus_page, predicates, file_service_key, tag_service_key, hashes, service_key, paths, delete_after_success, file_sort_type, file_sort_asc, file_sort_namespaces, collect_namespaces, system_hash_locked, urls, url ):
    
    root_notebook = CG.client_controller.gui.GetTopLevelNotebook()
    
    if page_of_pages_key is not None:
        
        target_notebook = root_notebook.GetPageFromPageKey( page_of_pages_key )
        
        if target_notebook is None or not isinstance( target_notebook, ClientGUIPages.PagesNotebook ):
            
            raise HydrusExceptions.NotFoundException( 'Could not find a page of pages with that key!' )
            
    else:
        
        target_notebook = root_notebook
        
    
    if page_type == ClientGUIPagesCore.PAGE_TYPE_QUERY:
        
        if file_service_key is not None:
            
            location_context = ClientLocation.LocationContext.STATICCreateSimple( file_service_key )
            
        else:
            
            location_context = ClientLocation.LocationContext.STATICCreateSimple( CC.COMBINED_LOCAL_FILE_DOMAINS_SERVICE_KEY )
            
        
        if tag_service_key is None:
            
            tag_service_key = CC.COMBINED_TAG_SERVICE_KEY
            
        
        tag_context = ClientSearchTagContext.TagContext( service_key = tag_service_key )
        
        file_search_context = ClientSearchFileSearchContext.FileSearchContext( location_context = location_context, tag_context = tag_context, predicates = predicates )
        
        page_manager = ClientGUIPageManager.CreatePageManagerQuery( page_name or 'files', file_search_context )
        
        if file_sort_namespaces is not None:
            
            sort_order = CC.SORT_ASC if file_sort_asc else CC.SORT_DESC
            
            media_sort = ClientMediaSort.MediaSort(
                sort_type = ( 'namespaces', ( tuple( file_sort_namespaces ), ClientTags.TAG_DISPLAY_DISPLAY_ACTUAL ) ),
                sort_order = sort_order
            )
            
            page_manager.SetVariable( 'media_sort', media_sort )
            
        elif file_sort_type is not None:
            
            sort_order = CC.SORT_ASC if file_sort_asc else CC.SORT_DESC
            
            media_sort = ClientMediaSort.MediaSort(
                sort_type = ( 'system', file_sort_type ),
                sort_order = sort_order
            )
            
            page_manager.SetVariable( 'media_sort', media_sort )
            
        
        if collect_namespaces is not None:
            
            media_collect = ClientMediaCollect.MediaCollect(
                namespaces = collect_namespaces,
                collect_unmatched = True
            )
            
            page_manager.SetVariable( 'media_collect', media_collect )
            
        
        if system_hash_locked is not None and system_hash_locked:
            
            page_manager.SetVariable( 'system_hash_locked', True )
            page_manager.SetVariable( 'system_hash_locked_syncs_new', True )
            page_manager.SetVariable( 'system_hash_locked_syncs_removes', True )
            
        
        page = target_notebook.NewPage( page_manager, initial_hashes = hashes or [], select_page = False )
        
    elif page_type == ClientGUIPagesCore.PAGE_TYPE_PAGE_OF_PAGES:
        
        page = target_notebook.NewPagesNotebook( name = page_name or 'pages', give_it_a_blank_page = False, select_page = False )
        
    elif page_type == ClientGUIPagesCore.PAGE_TYPE_IMPORT_MULTIPLE_GALLERY:
        
        page_manager = ClientGUIPageManager.CreatePageManagerImportGallery( page_name = page_name )
        
        page = target_notebook.NewPage( page_manager, select_page = False )
        
    elif page_type == ClientGUIPagesCore.PAGE_TYPE_IMPORT_SIMPLE_DOWNLOADER:
        
        page_manager = ClientGUIPageManager.CreatePageManagerImportSimpleDownloader()
        
        page = target_notebook.NewPage( page_manager, select_page = False )
        
    elif page_type == ClientGUIPagesCore.PAGE_TYPE_IMPORT_URLS:
        
        page_manager = ClientGUIPageManager.CreatePageManagerImportURLs( page_name = page_name )
        
        if len( urls ) > 0:
            
            from hydrus.client.importing import ClientImportSimpleURLs
            
            urls_import: ClientImportSimpleURLs.URLsImport = page_manager.GetVariable( 'urls_import' )
            urls_import.PendURLs( urls )
            
        
        page = target_notebook.NewPage( page_manager, select_page = False )
        
    elif page_type == ClientGUIPagesCore.PAGE_TYPE_IMPORT_MULTIPLE_WATCHER:
        
        page_manager = ClientGUIPageManager.CreatePageManagerImportMultipleWatcher( page_name = page_name, url = url )
        
        page = target_notebook.NewPage( page_manager, select_page = False )
        
    elif page_type == ClientGUIPagesCore.PAGE_TYPE_DUPLICATE_FILTER:
        
        if file_service_key is not None:
            
            location_context = ClientLocation.LocationContext.STATICCreateSimple( file_service_key )
            
        else:
            
            location_context = None
            
        
        page_manager = ClientGUIPageManager.CreatePageManagerDuplicateFilter( page_name = page_name, location_context = location_context )
        
        page = target_notebook.NewPage( page_manager, select_page = False )
        
    elif page_type == ClientGUIPagesCore.PAGE_TYPE_PETITIONS:
        
        if service_key is None:
            
            raise HydrusExceptions.BadRequestException( 'Petitions page requires a service_key!' )
            
        
        page_manager = ClientGUIPageManager.CreatePageManagerPetitions( service_key )
        
        page = target_notebook.NewPage( page_manager, select_page = False )
        
    elif page_type == ClientGUIPagesCore.PAGE_TYPE_IMPORT_HDD:
        
        from hydrus.client.importing.options import ImportOptionsContainer
        
        import_options_container = ImportOptionsContainer.ImportOptionsContainer()
        
        page_manager = ClientGUIPageManager.CreatePageManagerImportHDD(
            paths = paths,
            import_options_container = import_options_container,
            metadata_routers = [],
            paths_to_additional_service_keys_to_tags = {},
            delete_after_success = delete_after_success
        )
        
        page = target_notebook.NewPage( page_manager, select_page = False )
        
    else:
        
        raise HydrusExceptions.BadRequestException( 'Unrecognised or unsupported page type!' )
        
    
    if focus_page:
        
        CG.client_controller.gui.ShowPage( page.GetPageKey() )
        
        if hasattr( page, 'SetMediaFocus' ):
            
            page.SetMediaFocus()
            
        
    
    return ( page.GetPageKey(), page.GetPageManager().GetType(), page.GetName() )
    
