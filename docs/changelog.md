---
title: Changelog
---

# changelog

!!! note
    This is the new changelog, only the most recent builds. For all versions, see the [old changelog](old_changelog.html).

## [Version 674](https://github.com/hydrusnetwork/hydrus/releases/tag/v674)

### misc

* you can now customise how mouse wheel events propagate out of the hover taglist in the media viewer. this has had a variety of hacky/patchy behaviour before; now you can hit up `options->media viewer hovers` and tell it to: never propagate; propagate only if no vertical scrollbar; propagate only if vertical scrollbar hasn't been used recently (the new default behaviour); and propagate immediately after scrollbar hits an end (which is what Qt _wants_ to do). this new 'has been used recently' tech locks you in the outer or outer context and uses a little voodoo, but I quite like it (issue #2024)
* thanks to a user, the human-readable embedded text section in the media viewer little button up top will now decode and show an embedded `Character Card V2` spec. previously, it would just dump the json string under 'chara', but now it looks a good bit better
* the help docs built into the Windows and Linux builds and the one built by the 'build_help.py' script by users running from source are now built in a more strict offline mode that caches the javascript for search tech locally. a user noticed they were previously fetching something from unpkg.com. now they should work properly even on a completely offline machine (iissue #2023)
* a variety of file existence checks and merge functions now check for 'hey this seems to be a remote storage that is disconnected/timing out' error states. previously these guys were just doing 'file does not exist' catching. this means booting the client with your NAS defined in some way but not mounted has nicer error handling. you'll get the repair locations dialog with an updated message rather than 'oh god unhandled boot I/O error aieeeee'
* added 'shutdown report mode' to the `help->debug->report modes` menu. this will report the shutdown calls, shutdown exception catches, and actual mainloop shutdown of all the program's thread workers and other mainloop daemons, with the intention of helping figure out some situations where the client will exit seemingly fine but with a silent low-resource process lingering (we think it is a thread orphaned from the signalling system or otherwise stuck in some deadlock)

### cookies.txt and expiration fixes

* when importing cookies.txt, 'session' cookies (i.e. those with no expiry) are now imported correctly. previously, they were being parsed as 'discard immediately' and were not being preserved
* fixed issues with sessions not saving new cookies after import via cookies.txt or clipboard if the user closed the client before that session was actually used in a request
* I hadn't realised, but hydrus was not being very aggressive about clearing 'session' cookies. after thinking about it, this is now intentional policy. I will add some buttons/options around this in future

### some repository account refresh cleanup

* the way repositories sync their accounts is a little cleaned up. clicking 'refresh account' is nicer and more reliable now
* the awkward and confusing 'network'/'hydrus account' panels in 'review services' for a repo are now merged into one expand/collapse box called 'hydrus service'. service status/errors usually appear on the top box while most people need the bottom; now you always see both at once. hope this makes some 'oh, everything is paused' situations a bit clearer
* the 'message' status text line in the 'account' panel now hides if there is no server message to show. this guy was just a weird UI gap for pretty much everyone
* 'refresh account' no longer disables itself when a repo is non-functional. this was another 'technically true, but not helpful' UI thing. if you click it, any blocker now gives a richer reason, with several generic 'account cannot sync right now' reasons replaced with the actual part of bandwidth tracking or whatever that is complaining
* if you try to hit 'refresh account', it now recognises if all network traffic is currently paused and breaks out early. previously, it would grey out and wait indefinitely until network traffic was unpaused
* a 'refresh account' call no longer sets a temporary 'unknown/unsynced' account to the service. if the fetch job fails, you keep the old account info
* errors from 'refresh account' are no longer put into toaster popups
* the 'tag filter' button for tag repositories is moved from 'network sync' to the new 'account' panel, beside the permissions button

### curl_cffi

* the recent test of `curl_cffi`, which adds http 2 and 3 support to hydrus, has proven successful. I am maturing the test and allowing a permanent on setting
* you now set the browser name under `options->connection`. http version selection is removed from the test--it seems it is doable and simpler to just let `curl_cffi` figure that out
* the setup_venv.py script now asks if you want `curl_cffi`
* `curl_cffi` is disabled for hydrus servers for now; we had some chunking issue when downloading from the PTR

### domain manager background work

* I moved forward my plans to launch a nicer unified 'here are the current statuses and settings for each network domain' UI and options system. this thing will eventually manage per-domain error timeouts, custom headers, perhaps some proxy settings, curl_cffi, and have some UI for recent errors. we'll migrate the stuff in `options->connection` to a 'global' entry and then allow more specific network context settings for particular domains; the usual deal
* I was thinking I'd launch a stub of this system to allow for a per-domain `curl_cffi` test, but I didn't want to rush it out, so I just kept to prep work and there's nothing launching here yet. I rounded out the objects I already had and verified the direction I'm going; I feel overall good about it

### boring stuff

* refactored some of the 'render human-readable data' method for KISS
* fixed some multi-line indenting in the human-readable rendering routine
* KISSed some inelegant 'clear expired cookies' calls and code
* added `help->debug->scan file storage folders`, which is just a test for a folder precache thing that I removed at the last minute last week when it performed terribly on an IRL spinning HDD. I rewrote it and will do some more testing
* cleaned up some error handling in 'server busy, try again later' parsing

## [Version 673](https://github.com/hydrusnetwork/hydrus/releases/tag/v673)

### misc

* the file history chart now has a custom y axis range. also, the chart now remembers if you have set either axis custom and new searches will auto-refit or maintain current dimensions as appropriate. hide/showing the lines will only recalculate the non-user-customised Y axis; let's see how that goes
* added a sanity check to the new fast 'give me the average character width' calculation, which is used for some scaling-agnostic UI sizing. one user (on a monospaced font, no less) had extremely wide average character width; I guess the font has funny kerning or extended characters or something. if the average character width is more than twice the reported height (which appears to be more reliable), I now fall back to a slower but more accurate calculation
* you can now edit the Access Key of a Client API permissions entry (a user mentioned they were migrating to a new client and updating every existing script to use new random keys was a pain). since you don't want to do this casually, it works through a button that gives a little spiel and tests the new key for validity and such, and the final ok will bail out if you paste something already in the system
* updated some system predicate parsing to support `<=` and `>=` operators, along with some variants like 'less than or equal to'. the types now supporting this are: width, height, duration, number of frames, number of words (issue #2019)

### new help docs for the recovery.txts

* added a 'Recovery' headline section to the help and migrated the .txt recovery docs to basic markdown
* the basedir 'help my client will not boot' is migrated to here
* all the .txts in the db dir like 'help my db is broke.txt' are migrated to here
* as planned, the `static/db_files` dir is removed. you no longer get a bunch of .txts in any new db folder. feel free to delete any old ones you have, but it isn't a big deal

### local file parsing optimisation

* when you drop a folder on the program, the main scan of that folder is a good bit faster than before and will scale a bit better
* when you drop a folder on the program, symlink loops are now recognised and broken out of
* when parsing import files from a folder, the main parse object now uses several fewer drive hits
* checking for 'file is in use' requires one less drive hit

### faster folder checking on startup

* when hydrus boots, it checks for the presence of all file storage folders. on a normal client, this is 512 directory presence checks; on an advanced granularity 3 system, this is 8192. this time adds up on boot, particularly on a cold HDD. I have improved the regular test here to do just one hard drive hit per folder instead of two. also, especially for the bootup phase, these locations are now scanned for _en masse_ with a carefully efficient/failsafe top-level scan on the main storage locations, massively reducing the number of hard drive hits required here

### optimised caching tech

* a user identified that a hacky id-to-value lookup cache used in tag and hash database modules was not working great. under certain types of strain, it would churn, leading to memory bloat and fragmentation
* I have tried several solutions and figured out a fairly decent replacement (LRU cache, nothing crazy) that will not churn so much and has less overhead. there's some additional long-term work that needs to be done to solve the bloat problem fully (full weakref tracking of tags/hashes), but I'm overall happy. tag and hash fetching when you load media or do various other heavy database jobs is now a little more optimised in several ways, and in most cases causes less memory duplication and fragmentation
* while I was poking around here, I also overhauled the general LRU cache used by a bunch of UI-level guys. thumbnail refetch and image zooming back and forth may be a shave faster

### source environment cleanup

* as planned a few months ago, v673 cleans up the 'running from source' setup significantly. you shouldn't have to do anything unless you run from source and use a custom script to automatically recreate your venv. I delete some old redundant scripts today, so if you happened to set an executable permission on something a long time ago, git may moan at you about being unable to pull because of your pending changes. deleting the files and then pulling again should work
* the pyproject.toml file no longer has any groups. there's one setup, nice and simple. the venue to test alternate library versions is now `setup_venv.py` exclusively
* the old basedir requirements.txt is now removed
* the manual 'running from source' help is updated. you now do just `pip install .` for a manual, pyproject.toml based pip install, with no groups needed
* the .bat/.command/.sh versions of `setup_help` and `setup_venv` and `git_pull` are removed--use the multiplat .py files from now on
* the `open_venv.bat/.ps1` scripts and `auto_update_installer.bat`, which were just fun experiments, are deleted. if you need some rinky-dink scripts to pull off a very custom thing like this, I recommend talking to an AI to get exactly what you need for your setup
* to improve hydrus package security, all dependency versions in the pyproject.toml and setup_venv.py and the build requirement.txts are now pinned/capped to recent latest versions. anything that was `>=` is now `<=` for the version as of the 672 build. all library version updates will now be considered manually by human eyes in future builds
* relatedly, the windows ffmpeg version is no longer latest but pinned at `8.1.1`
* deduped the basedir license files and renamed to `LICENSE`
* wrote a very basic `CONTRIBUTING.md` to mention that public pulls are closed right now
* for KISS, I'll switch the builds from their requirements.txts over to the pyproject.toml in the next future build test

### boring cleanup

* moved some file parsing code out of `ClientGUILocalFileimports` to `ClientImportFileParse`
* jiggled some 'make this panel x characters wide' numbers after last week's character-width update. this generally meant clearing out old +2 padding hacks and shaving some 64 to 60, that sort of thing, and I fixed a couple of things that were a little out of whack or sizing the wrong widget

## [Version 672](https://github.com/hydrusnetwork/hydrus/releases/tag/v672)

### misc

* fixed a stupid error where the new-ish `media playback->ffmpeg call timeout` setting was not hooked up correctly on options dialog ok and was not saving! sorry for the trouble, I don't know how this slipped through testing
* fixed an issue where edit-pasting a prefixed 'sha256:abcd...'-style hash into an existing 'system:hashes' would wipe out the existing hashes (issue #2015)
* fixed the 'is this video rotated 90 degrees?' test in my ffmpeg output parsing for ffmpeg 8.1.x (which the windows builds moved to recently) (issue #1377)
* improved the speed and precision of the core call in the 'hey roughly how wide is 16 characters for this widget?' size calculations used by stuff across the program. system:hash panel should fit better in different fonts and sizes now. this may make some tight multi-column lists (like the one in duplicate page, auto-resolution tab) go a bit wide, requiring you to shrink them a little manually to hide new horizontal scrollbars--forgive me
* if a PNG file has chromaticity data but not gamma data, I now sub in a gamma of 0.45455 (which works well as a best-case fallback), and continue with the new ICC Profile-based chromaticity correction. thanks to the user who noticed this in the duplicate filter and had example files to test that rendered with a slightly different glow but were inexplicably marked, in an older version of the client, as pixel dupes

### duplicates auto-resolution

* the cog icon of the potential duplicate par search panel (which you see on dupe pages and a couple other places in auto-resolution UI) has a new 'start new potential duplicate pair search panels paused' entry. if you use a bunch of these and wrestling with the pause status is annoying, try it out
* you can now use 'system:number of pixels' in the single-file 'test A or B using search terms' comparator
* you can now use 'system:ratio' in the single-file 'test A or B using search terms' comparator
* you can now use 'system:ratio' in the 'test A against B using file info' comparator, but the UI for it shows some default operator labels; `<` instead of `taller than`. the comparator summary label should work though
* the system pred dropdown in that edit panel (this is the Metadata Conditional edit panel) collapses system:width, height, num_pixels, and ratio down to 'system:dimensions', like in a normal search page
* added unit tests for the new comparators

### some import options follow-up

* added three simple examples to the top of the new import options help for 'setting up import previously deleted'; 'sending some tags elsewhere'; and 'forcing a tag redownload' as a stepping stone between 'ignore this whole system m8' and 'how to harmonically conjunct the polyhierarchic metalateral defaults'
* also added an example of how to customise and clone URL Class defaults
* added a 'help for this panel' button that links to the html help to the regular edit import options panel
* the regular edit import options panel's favourite button has a new 'save current value as new favourite' entry in its menu, under edit/add
* in the new import options system, the 'locations' import options now shows for post urls, watcher urls, and the subs defaults in simple mode

### client api

* added `/client_info`, accessible to all valid access keys, which provides a random hex `boot_id`, a float `boot_time`, and `currently_idle` bool, for tracking client restarts and throttling decisions
* Client API version is now 92

### boring cleanup

* the 'fetch service id' button in review services has nicer error handling and now disables the button while it works
* cleaned up how the 'set forced mimetype to these files' operation works behind the scenes
* reworked how the sidebar taglist broadcasts tag changes to the current search; moving from an old pubsub to a newer Qt signal
* fixed an issue with dissolving an OR predicate from the active predicates menu where the signal was being double-sent
* reworked the `NumberTest` rendering tech to better handle custom number and operator rendering
* the Number Tests across the program, which power a bunch of the newer system predicates where you can say 'width is approx 400 +/- 15%' are no longer coerced to integers behind the scenes. this doesn't affect much, but in the duplicates auto-resolution system, where you can do `A has height > 1.8x B`, that multiplier can now result in a float. in the trivial case of B height `1`, `1 * 1.8` is now less than `2`, rather than being rounded up

## [Version 671](https://github.com/hydrusnetwork/hydrus/releases/tag/v671)

### import options overhaul

* the frontend and object storage is moved to the new import options system! there's a bunch going on here, but in brief--
* the old file/tag/note import options are split into seven smaller types: prefetch, file filtering, tag filtering, locations, tags, notes, presentation
* your old options have been converted to the new system and it should all work as before, with only minor logical changes. you do not have to do anything
* all the defaults are now edited in `file->options->import options`. the system is richer and supports favourites/templates for quick-load
* all the 'import options' buttons across the program use some slightly newer UI and offer quick 'load from favourites'
* the 'edit subscriptions' dialog now has a column to show a summary for any custom import options you have set
* the 'edit subscription' dialog now has a column to show a summary for any queries that have 'additional tags' set up
* full help, now with screenshots, is here: https://hydrusnetwork.github.io/hydrus/getting_started_import_options.html

### misc

* the pretty media info lines that appear on a single media right-click flyout menu and some other places now includes a `approx bitrate: 1.4MB/s` line. it is pretty similar to the 'sort by approx bitrate' sort but only appears for stuff with an actual duration (the sort also puts big static images before small ones). it is just a naive `size / duration` and it'll do `1.4MB/s` with existing display tech rather than `7,200KBps` as you otherwise usually see, so I may revisit when I eventually get around to 1000/1024 byte-count presentation options and such
* renamed the shortcut labels 'close page' and 'restore the most recently closed page', to 'pages: close current' and 'pages: unclose the most recently closed'
* added a 'pages: rename current' shortcut to 'the main window' set
* fixed the setting of referral urls to child import objects in two cases: A) when a file import produced multiple child urls with the note `Found x new URLs in one post` and B) when a gallery url in the search phase produced an unexpected non-gallery url because of 3XX or api url class redirection with note `was redirected to a non-gallery url, which has been queued as a file import`.. the referral url was not being set correctly because of a regression in v664. thank you to the enterprising user who ran this through Codex to find the solution
* fixed a foolish typo that broke importing a cookies.txt (issue #2011)
* when loading a cookies.txt, if the file includes some complicated utf-8 encoding that the current default locale cannot decode, I now catch the decoding failure and try to recover with a newly encoded temp file (also issue #2011)
* fixed a bit of page counting logic that would turn up in some 'hey do you want to close the x pages?' labels and some 'do to current page' logic, where a page of pages was not counting child page of pages
* updated the base `requirements.txt` to match the future build stuff we folded in recently. missed this guy by accident; he'll be deleted in v673 in lieu of the cleaner `pyproject.toml` that's coming up
* did a quick hotfix patch to master for the curl_cffi test, which was frozen by accident at chrome/http2
* added a note to the installation help, Linux specifically: if you get a crash on any keypress after booting the client, you probably need to run from source (issue #2010)
* added some careful typedefs and imports to fix a deferred import that was causing certain recent database updates to fail out when trying to load the core options object

**normal users do not pass this line**

### import options detailed changes

* an import options manager with default settings is now created with a new db
* on update from v671, your existing quiet/loud file import options, post/watcher tag/note import options, and all the url class tag/note import options are converted into a new import options manager. the old settings are deleted from your options and domain manager objects once it is all done and clear
* a new button-with-a-dropdown-menu-arrow to handles the import options container of a specific import context. its arrow menu offers a detailed review of what it currently holds and supports quick 'favourites/template' loading
* the 'fetch page metadata even if hash/url recognised and file appears "already in db" stuff is moved from tag import options to the new 'prefetch import options'. this is the only movement of options; everything else has been separated into the sub-panels you've seen in recent weeks
* updated the new system's freeze routine to consult both the given url of the import object _and_ any referral url, _and_ if either is an API redirect, to look up every url class in the api redirect chain and select the first for which there is a custom import options entry. previously, referral url lookup was spotty, and it would only ever look at the final place in the api redirect chain. very sophisticated setups can now have separate per-url-class import options that nonetheless connect to the same final api url class
* import folders and subscription queries now add 'additional tags' even if the file ends up 'previously deleted' (all imports generally apply new content updates to a 'previously deleted' result, so this brings these supplementary tags into normal behaviour.) I may add an option around which import statuses quality for a post-import content update commit, now everything is on the same page
* if you hop skip and jump, you can now set 'additional tags' to a local file import that is entirely adjunct to the normal filename tagging system. there's probably some weird edge undesired behaviour now, because of odd new capabilities like this, so I'm interested in error reports or 'hey you should probably hide this by default here' reports
* made some svgs and screenshots for the new import options help, removed the old help, integrated it all into the 'gettting started - downloading' help, and added it to the main index

### import options detailed fixes

* fixed some import situations (usually when the import object had no other tags to add) where 'additional tags' in a tag import options might not be added
* if a file import results in 'ignored' but somehow gets an sha256 hash (local imports hit by file filtering rules would get this), no post-job content updates like urls or tags will be added to the file
* in edit subscriptions, the 'set file/tag/note import options' stuff is merged into one and there are copy/paste/favourite buttons like in `options->import options` with the same clever paste-merge tech. there is also a new column to see which subs have custom import options set. just a bit easier to mass-manage custom options here now
* fixed a bug with adding a _watchable_ URL with a custom import destination (via Client API); previously, it would just to the first watchable page. now it checks and creates a new one with the desired custom location import options as needed
* tag import options now present a single-line summary (they'd spill over to multiple lines when things got complicated), with some tighter syntax

### client api

* the `get_client_options` call no longer presents a `default_file_import_options` summary row
* Client API version is now 91

**non-hydev do not pass this line**

### some last-minute fixes and tweaks

* added some repair code for if your 'global' options container set is missing or missing a sub-option type. the holes are filled in and the fixed structure is saved back
* wrote some more safety code to ensure a non-full global options container cannot be set to the core options structure with the issue reported nicely to the user
* I didn't like how the favourite buttons were still loading up the complicated custom-merge dialog on 'load', so I made a quick-multi-load 'load' and slow-single-load 'custom load' menus. if you just want to set x to a big selection somewhere, no problem
* same deal with the new general 'import options button'--I added a submenu to pull from the favourites, and you can 'load' in one click or 'custom load' if you need something like 'hey I just need the prefetch options from this template'
* in the main edit panel, updated the options summary strings, which were in the form `file filtering: default/stuff` to `default file filtering` and `> file filtering: stuff`. this makes the non-default stuff stand out better, but maybe we can do more here. maybe this guy could evolve into a checklist or a multi-column list
* the new vertically stacked listbook now disables horizontal scrollbar too (the scrollbar was drawing over the bottom item, let's go)
* fixed an issue with my migration routine where a new container would not contain prefetch import options with the 'force refetch' stuff from tag import options when the original file import options was missing/default
* updated the migration routine to abstain from adding prefetch stuff if it was uninteresting
* updated all my importer migration code to safely attempt to determine the already-migrated defaults for this importer before migration so the 'if we don't disagree with our parent's defaults, we can set that sub-options to default' logic can kick in. this eases the transition and ensures we don't end up with a bunch of importers with default 'tag filter: allows everything' stubs because the user only changed the parsing rules
* fixed a little thing in the import options manager initialisation where the defaults set the 'quiet' presentation rules to the Client API setting--the Client API import does not publish files anywhere, so no need for that
* fixed an issue where the 'panel types to load for this import caller type' CONST reference was being unintentionally updated and thus poisoning later dialog opens
* ditched the 'set and check full' system as too brittle with bad error handling

### boring import options data migration log

* all importers' storage and final pipeline: hdd imports, import folders, simple downloaders, url downloader pages; a couple of misc 'download these urls' routines used by stuff like IPFS, hydrus file repository download, gallery imports, the multiple gallery import container, watcher imports, the multiple watcher import container, client api imports, and subscriptions
* subscription query 'additional tags'
* migrated some network report mode stuff over to the new system as a frozen import options container is assembled with potential URL data
* ancient legacy subscription update routine
* subscription serialisation unit test

### boring cleanup and refactoring

* the import options manager now tracks if it is dirty and needs saving, and the main client controller consults that in the normal maintenance and shutdown cycle
* removed the default file import options from `options->importing` and tag/note stuff from `network->downloaders->default import options`
* deleted the old system's legacy edit panels and widgets
* removed some hackery that made gallery/watcher list selection-inspection a little faster but was too beardy
* the old `show_downloader_options` display option is replaced by direct `import_options_caller_type` inspection in all situations
* wrote an `ImportOptionsMetatype` for our seven new sub-options to help some typing inference. all these guys now know their own `import_options_type` too, and some `Set` stuff is a bit simpler
* moved the subscription query 'additional tags' button to a simpler single-panel-dialog solution
* updated the UI for all importers to use the new import options container button
* figured out some default/display fixes for the newer container edit UI panels to show the correct 'this defers to default type x' whether editing a default, global, favourite, or a specific importer
* cleaned up some prefetch options juggling
* improved some edge cases in the process by which paged importers figure out their best-guess current location context
* moved some legacy presentation import options consultation in the gallery/watcher sidebar list menu is over to the new system
* when a new url import page call wants to queue up urls with a custom import destination or, now, prefetch import options, this is merged into an import options container. as an edge-case logical change here, such jobs will no longer be added to pre-existing pages that have the same import destination and prefetch settings but other options differences. this is really in the weeds, but I may change this behaviour and just merge them into existing pages but with different import options; we'll see
* a check that regularly goes 'hey are we still good to import stuff?' in all importers that reviews the validity of the destination import location (generally checking if we want to import to a local file domain that has since been deleted) has been cleaned up, refactored into one central location, and now consults the next-pending object for specific options. if someone sets all x url classes to import to 'cool places' and then deletes that, the queue now pauses ahead of time rather than spitting out a hundred errors. the actual exception raised here has an improved typedef, too, that the veto system catches easier
* moved the new system's constants to their own `ImportOptionsConstants.py`, `as IOC`, so stuff can see them better
* moved the new system's manager to its own `ImportOptionsManager.py`
* moved some 'filter these pending tags this way' from the now-defunct `ClientImportOptions` to `TagImportOptions`
* the various edit/custom-paste UI now obeys the 'simple mode :^)' set in `options->import options`
* renamed the old `ClientImportOptions` to `CheckerImportOptions`, since that is all it does now
* cleaned up some old rubbish `subscription.ToTuple` mess
* cleaned up some misc bad code in hdd importers
* deleted all the legacy UI code for the old system

## [Version 670](https://github.com/hydrusnetwork/hydrus/releases/tag/v670)

### misc

* when you paste queries into the edit subscription window, it now shows you the pending changes, i.e. which are new/already in/DEAD,  _before_ you say yes/no. if you have DEAD, it asks with a yesyes/no if you want to revive or not
* the 'rescue off-screen window' system now tries to slide windows down and/or right before falling back to 'topleft of primary screen'. for instance, if the off-screen dialog's originally proposed bottom-left corner would be in view on a particular screen, it now just slides the guy down a bit and adds any fuzzy padding. this makes the new 'put dialog over the mouse' mode nicer to work with near screen edges, although I need to do some more work to better handle particularly short dialogs and those that span across a multi-monitor border
* for the new 'put the dialog over the mouse' mode, if the dialog would appear off-screen, it no longer gives you a 'woah, that was offscreen' popup
* for the new 'put the dialog over the mouse' mode, if the current mouse cursor is so invalid it is seemingly not on any current screen, it falls back to 'topleft of parent mate'
* if you run from source with a normal-looking 'venv' directory, any update that includes new package versions is now going to say, 'hey, looks like you are running from source. this is a good time to update your venv' when you update. this will happen in this release
* thanks to a user, the F13-24 keys are now mappable in the shortcuts system

### tag suggestions

* in 'manage tags', on the 'related tags' panel, the 'do it just for my files/all known files' flipswitch is now visible on local tag services
* the 'related tags' panel now has a tag service selector, so you can say 'hey provide related suggests from the PTR' for your 'my tags' and so on
* the 'related tags' panel now has a 'searching through/not searching siblings and parents' button. this involves some black magic and I don't really know how well it works, but the tech seemed to be done db side so I just wired it up
* this is mostly experimental. if you use this a lot, give it a go and let me know if any of it is useful. if you find you are setting something one way or another over and over, it might be nice to have it remember the last settings
* the 'related tags' panel now displays sibling and parent relationships, like the other tag suggestion panels
* if you hit either of the 'related tags' search-type buttons, or the media changes, the 'related tags' search now refreshes using the same duration that was last searched (or 'quick' if none has been done yet)
* on tag menus, the 'favourites' sub-menu has been overhauled. for KISS, it now only appears when you have one tag selected, and it provides add/remove options for the general favourites _and_, now, all the 'most used' tag suggestion columns that appear in the manage tags dialog. ten points to anyone who can come up with a better names than 'most used tags' and 'favourite tags' that represents what they do while not confusing one for the other
* on tag menus, an empty 'search' sub-menu no longer appears in contexts without any search-page stuff (e.g. manage tags was doing this)

### curl_cffi test

* a user pointed me at the `curl_cffi` library this week, which provides http/2 and http/3 support with a `requests`-like interface. I have written a very small and basic shim to test this out with our network engine, and a new test mode is under `help->debug->network actions->curl_cffi test mode`. it asks for some test details and then switches the whole network engine over.
* very advanced source users can now add `curl_cffi` to their venvs and give it a whirl. if we discover it solves some problems that requests at http/1 can't handle, I'll formalise the test and plan out how to integrate this into a future domain manager so we can shape things to particular network contexts as needed
* I cleaned a bunch of session code and figured out some nicer session switching/reinitialising-tech and cookie-migration tech. I feel even more optimistic about also playing with `httpx`, which is a comprehensive `requests`-like replacement that offers http/2

### boring import options work

* wrote out a draft of a help document for import options at `https://hydrusnetwork.github.io/hydrus/getting_started_import_options.html`. I'll populate this with nice diagrams and screenshots and insert it properly as this becomes real. if you have been following along here, feel free to have a look and let me know where I'm making mistakes

### boring cleanup

* added a little note to the 'running from source' help regarding ffmpeg on LTS Linux flavours--you probably have an old system ffmpeg, for good reason, so I now say how to find a newer one for hydrus if you like
* added a small section to the backing up help on how to set up a wineprefix for ToDoList. if you try it, let me know if you have a different mfc experience
* cleaned up how mouse coordinates are fetched across the program; everything now goes through one place. Wayland has some issues with this (generally, in Wayland, I only get the last place the mouse was seen over my UI panels), so I may end up inserting some DEBUG recoveries for when this gives crazy answers
* decoupled a bunch of session and cookie code. the way cookies are fetched, inspected, and edited is a good bit cleaner and more centralised and abstract now
* 'session' cookies are now cleared faster and each session maintains its connection pool less redundantly
* misc refactoring to tidy up the initial import as the main controller boots. still lots to do
* pulled `ClientMedia` apart and cleaned up some inheritance mess. this whole thing is still awful, but this was a good step
* cleared out more linting issues with client media and friends, and in continuing a very long rewrite, a bunch of MediaSingle stuff is replaced with MediaResult, and a bunch of edge-case 'we wanted to show a collection, but it just lost its last member' bugs are fixed
* fixed 'copy thumbnail bmp' for collections, which wasn't falling back to a nice default behaviour before

### future build committed

* This release commits the changes tested with the recent future build. The test went well, and there are no special instructions for the update. Source users are encouraged to rebuild their venvs this week. Update as normal, and you will get--
* requests from `2.32.5` to `2.33.1` (security fix)
* chardet from `chardet>=3.0.4,<6` to `chardet>=3.0.4,<8` (requests dependency issue resolved in the new version)
* OpenCV from `4.11.0.86` to `4.12.0.88` (normal update)
* dateparser from `1.2.1` to `1.4.0` (had a build problem before)
* adds tldextract `5.3.1`, which provides for more sensible '.co.jp' top level domain recognition
* setuptools no longer pinned at `78.1.1` for the builds (an old hack removed), seems to now be ~82.x
* pyinstaller `6.14.1` to `6.16.0` for the builds (normal update, and by the hand of fate it worked)
* test version of OpenCV from `4.13.0.90` to `4.13.0.92` (build dependency fix that'll hit us later)
* test version of PySide6 (Qt) from `6.10.1` to `6.10.3` (normal update, but we discovered a fun bug yesterday in 6.10.1 that broke several lists and widgets)
* Windows mpv dll from `2023-08-20` to `2024-08-18` (cautious update; we've had many issues trying newer mpv dlls over the years)
* `action-gh-release` from v2 (Node 20) to v3 (Node 24) in the Linux and Windows build scripts
* thanks to a user, the Docker package build files are also now Node 24 compatible

## [Version 669](https://github.com/hydrusnetwork/hydrus/releases/tag/v669)

### misc

* thanks to a user, the Paper Dark QSSes now have colours to stand in for the stuff in `options->colours`
* thanks to a user, fixed some bad shortcut enums that probably broke 'media-next' and 'volume-up/down' as mappable keys
* the 'manage times' dialog now has copy/paste buttons beside the file modified time, archived time, and last viewed times. works the same as the copy/paste buttons inside the smaller edit dialogs. they'll copy a float like `1776805484.252` but will eat pretty much anything
* the rich copy button menu at the bottom of on 'edit times' now lets you copy just the: file modified time; archived time; last viewed times; and web domain times
* fixed a traceback when hitting certain 'move thumbnail' shortcuts on an empty page/selection
* added info to 'big updates' and 'running from source' help regarding how to checkout a particular tag with `git` and how to discard and stash changes when pulling
* added a `--no_qt_multimedia` debug launch argument to disallow any attempt to import `QtMultimedia`, which drives the QtMediaPlayer. in certain environments, this guy will cause a segfault, either on boot or when opening `file->options` and my newer audio device check hits it, let's go

### some window options

* the 'regular_center_dialog' entry in `options->gui` is now split into `quick_select_dialog`, `quick_yesno_dialog`, and `quick_entry_dialog`. all defaulting to 'center on parent window'
* more of the 'select from a list of buttons'-style dialogs now have definitions, usually the new `quick_select_dialog`
* `regular_center_dialog` is removed, and also, if you have it, a `deeply_nested_dialogs`, note the plural, stub that was never used
* in `options->gui` you can now set that a non-remember-position window spawn centered on the current mouse cursor position. I daresay this is kino for mouse-centric users on the new quick-dialog entries, but try it for yourself
* gave the list in `options->gui` a label brush-up. hope it is easier to pick out what is going on now

### boring import options overhaul

* I made it more pleasant to load and paste import options around the upcoming UI (which you can preview under `options->import options` while in `advanced mode`)--
* wrote a 'custom paste' dialog that shows you the source and the incoming paste as two checkbox lists and a clear preview of the pending result. this dialog sets up a merge-paste by default but has buttons to quickly set up fill-in-gaps-paste or replace-paste. it has special labels for when the incoming paste makes no changes and also a couple commentary labels if the paste makes no changes at all or if the source is 'global' and thus has special rules
* added 'custom paste' to the top of the paste button menus around here as the default way users should handle pastes while still allowing quicker mass-pastes with the old entries
* the old entries in the paste menus now have simple labels to reflect what the 'custom paste' dialog uses, pushing spurious explanation to tooltips. also, the safe and 'what users want to happen most of the time' `merge-paste` is now the top of the three
* hooked up a new 'load favourite' menu off the star button to the custom paste dialog, so you can now load without having to juggle via your clipboard and you get a nice clean preview of what is about to happen
* added a star button to the url class list too
* the individual import options panel now has a copy button, so you can just copy a single set of options nice and quick
* the rules around pasting to 'global' are tightened up
* when pasting an import options container into an 'edit import options container' panel that is in 'simple mode' and has a limited set of viewable options, the paste object is now filtered to that sub-selection of available import options types

### removal of UPnP

* as has been long-planned, hydrus no longer does anything UPnP related. this is old port-forwarding tech that could apply to the Client API or a Server service if you jumped through several hoops and put 'upnpc' in the `install_dir/bin` dir, but for most users it never fired. I removed the exe from regular installs years ago, removed a upnp mapping dialog two months ago, and am clearing out the last server port forwarding tech today. if you want to set up port forwarding, run your own solution particular to your hardware, not my old garbage!
* specifically--
* the client api service and serverside services no longer offer a 'upnp port' value to edit in their UI panels
* upnp readme is removed from `install_dir/bin`
* the primary controllers of the client and server no longer spin up a upnp manager/daemon to serve these jobs
* removed `HydrusNATPunch` and `TestHydrusNATPunch`

### other boring cleanup

* the way service names are fetched has a new safe variant that recovers from a missing (e.g. recently deleted) service properly, and every call for a service name is now safe across the program. this fixes an issue with deleting a tag service that is currently in focus in a search page (and likely several other similar issues, and some general skipped summaries and such for missing services. now it'll explicitly say "unknown service" universally when this happens)
* cleaned up the copy/paste logic in 'manage times' dialog. it was all good before, but a little ugly in how it decided whether to grab/push times wrt non-visible widgets
* decoupled some datetime copy/paste code so different datetime widgets can share the timestamp parsing
* KISSed a domain-umbrella checking trick that helps us migrate to `tldextract`
* did a touch of misc linting

## [Version 668](https://github.com/hydrusnetwork/hydrus/releases/tag/v668)

### mistakes

* HOTFIX: I made a v667a last week to fix a regression where subscription queries with outstanding file queues were downloading those files even if the individual query was paused. The 'finish jobs even if you are DEAD' check change was too lenient first time around. Sorry for the trouble!
* HOTFIX 2: I then made a v667b since the first one didn't get everything. individually paused queries that had a due-to-work 'do gallery search' check were 100% CPUing the subscription daemon. I fixed it in the same way as the file check and added additional guards for other 'cannot work' error states and calls. the run logic in individual subscription queries was not clean, and a little change revealed the faults. I regret it and apologise to those hit with inconvenience and frustration here
* I have added unit tests for query pause, DEAD, 'has outstanding file work', 'has a due sync', 'is checking now', and 'is expecting to work in the future' states. this specific error will not happen again

### new media viewer shortcuts

* thanks to a user, we have some more window and media viewer options, including some clever debug stuff for those who get annoying 'rescued from offscreen' behaviour--
* you can now map 'always on top: on, off, flip on/off' in the media viewer's shortcuts set
* you can now map 'frameless: flip on/off' in the media viewer's shortcuts set
* in `options->gui`, you can now control whether an offscreen-rescued window gets the (default 40, 40) top-left safety padding
* you can also only remove the safety padding only when doing the 'resize to media' job for the media viewer
* and you can set the fuzzy 40px amount to something else

### misc

* renamed a handful of shortcuts action labels from 'verb thing' to 'thing: verb' for better sorting
* ADVANCED: added settings to `options->importing` to expose the until-now hardcoded bottlenecks on how many particular import queues can run at once (e.g. 'gallery files'). this is the thing that handles a 'pending' vs 'working' state in import page UI. it has an 'ADVANCED' warning label

### client api

* I chickened out of removing/default-off-ing some deprecated calls in the Client API as I had planned. it is not the season for causing fresh headaches, and nothing will break today
* since we'll indefinitely live with some mess, I decided to make a quick update to `The Services Object`. I hate that I made the `service_key` the key of the Objects involved here since in some JSON libraries it is a real pain to navigate. thus, anywhere I add the old Services Object under `services`, I also now add `services_v2`, which has the services as a list. same format except they now have a `service_key` field. I will not remove `services` any time soon, so no need to change anything you already have
* the Client API docs now refer to the `services_v2` structure so that new guys and projects are steered towards something non-weird
* I concede this is spammy and inelegant, but it resolves and KISSes an early mistake. the cruft is intensifying to the point where we may not be able to contain it, but why contain it? let it spill over into the add-ons and the MCPs, let the scripts pile up in the streets. in the end, they will beg us for a Rust conversion
* Client API version is now 90

### db dir build stuff

* I reworked how the 'help my db is broke.txt' help files (and for Windows, 'sqlite3.exe') end up in a db dir. these files now primarily live in `static/db_files`. any .txt files in there are mirrored into a new database folder on db creation. I strongly considered updating these files on any update, but I went back on it, preferring not to auto-fiddle with the db dir overmuch--I'll make a manual command for it
* a `CONTENT_DB_PATH` variable that had caused some hassle for patched source installs like the AUR release is now gone
* I will be removing the 'db' dir from the main repo in the future. I'll do more planning and testing around .gitignore just to be certain doing that will be smooth. we obviously want this to be absolutely failsafe
* I also expect to eventually migrate these .txt files to proper .md help files, with formatting and everything, at which point this will scale back significantly, probably just to a single .txt that says 'if everything is busted, go here in the help'
* I am finally ready to plan moving the default location of the db outside of the install dir. we'll have a first-start wizard to select/find location and a small file in your user folder to track available profiles or whatever if the client is run without `-d`, or something along those lines. this transition will take careful planning, so your general thoughts are appreciated

### boring import options overhaul

* every single importer across the program now converts the legacy file, tag, and note import options to the new 'import options container' before passing it to the main file import object. the whole file import object pipeline below the importer level is now converted to the new options structure!
* cleared 'file import options' out of some of the parsing pipeline where it wasn't used any more. this seems to have cleared all import options out of the gallery pipeline
* wrote out a modern 'set up a default set of options' call following the existing loud/quiet dichotomy and 'post urls get tags; watchers don't' ruleset
* my in-testing 'import options' panel gets a 'reset to defaults' button that will reset the main defaults, all the custom url classes, or the favourites/templates
* fiddled with some of the wording in this UI. defaulting the defaults back to 'use defaults' etc.. can make for unclear tooltips
* fleshed out the container object to offer up different import options in a more convenient and nicely typed way in the real pipelines
* in the file import, the 'should fetch metadata even if url/hash recognised...' advanced setting is now strictly _any_ metadata. previously this checkbox would only kick in if you actually had some tags set up to parse and import somewhere. after the migration, these checkboxes will be moving to an independant 'prefetch import options' guy, away from tags completely
* certain non-already-in-db imports that have no tags or notes are now one db hit cheaper
* certain already-in-db imports are now one db hit cheaper

### other boring stuff

* fixed a possible count bug in the thread slot system where in certain late-initialisation systems it could undercount the number of active jobs in a slot
* the 'prefer system ffmpeg' setting no longer needs a client restart. the call that discerns the ffmpeg path to use is also a little more sensible and failsafe, and cycling the options dialog now resets some state so you can put an ffmpeg in your hydrus 'bin' folder, ok the options, and it'll redo the 'is there one there?' test
* updated `crash.log` to `/db/hydrus_crash.log` in `.gitignore` file

## [Version 667](https://github.com/hydrusnetwork/hydrus/releases/tag/v667)

### misc

* fixed an issue with subs where a sub with all DEAD queries was considered 'no work remaining', and thus subs in that situation with nonetheless outstanding file downloads would not clear those outstanding file downloads if the work spilled over to future sub runs
* all thumbnail pair lists (which are in the duplicates auto-resolution UI) now have a menu that says 'show x rows in a new page' so can you can review things in more detail. let me know if this swallows deleted thumbnails anywhere
* the file storage granularisation system can now handle cross-filesystem granularisation. this can happen on 3-to-2 when some of the 3-granular subfolders have been migrated to a second location and need to be collected in a central source that might be on a different partition. it seems to also occur in 2-to-3 when the filesystem has overlapping subfolder orphans from a previous borked migration or similar and things are tidied as part of the regranularisation. the previous very fast atomic 'file rename' was raising an error here; now I do some efficient device checking of subfolders and move to a 'copy2+unlink' combo instead when the device differs
* the granularisation panel now warns those with weird symlinked storage to stay away
* the routine that counts up the durations of a gif's frames and the hydrus native renderer now fall back to best-guess defaults if A) the file changes resolution mid-parse or B) any frame appears to have a width or height bigger than 16k pixels. we encountered a malformed gif this week that went from 640x480 to a fake 28579x93263 and caused a memory explosion. should handle this situation better now
* continuing the 'clean up PIL Images better' work, native animation renders that error out are now explicitly closed before reinitialisation
* thanks to a user, the 'Paper_Dark' QSS files have fixed, visible `▲/▼` button labels. relatedly, the objectNames I added for this guy last week are renamed to `HydrusCollapseArrowUp/Down`. they were `Expanded/Collapsed`, but since the arrows reverse depending on an upwards or downwards expand, we realised we'd want something more specific to the actual label being shown if we ever wanted custom icons/labels

### sidecars

* sidecars gets an easy one-click template that covers simple stuff. the 'edit metadata migration routers' dialog, which the main guy in sidecar editing, now has a star button. when exporting to sidecars, it offers an 'easy one-click JSON that covers the basics' template that spams out definitions for a single 'filename.jpg.json' JSON sidecar that gets your notes, urls, tags for each tag service by storage (no siblings/parents) and display (with siblings/parents), and timestamps for archived time, main import time, and media/preview viewer 'last viewed time'. when importing from sidecars, the buton offers the same 'easy one-click' option to eat from a JSON using the same format and will line up any tag services that have the same service name
* if you try to export notes to a .txt sidecar using `\n` as the separator, or conversely import notes from a `\n` .txt sidecar, the 'edit sidecar stuff' dialog now moans at you on ok, asking if you are totally sure that is correct. since notes often _include_ newlines, something like `||||` is more appropriate
* when dragging and dropping individual files onto the client, it can now give a pretty 'sidecar' result when you drop individual files. this only worked if you dropped directories previously, with individual file drops giving 'PROBLEM: filetype unsupported' for the sidecars. this is an aesthetic change only; it doesn't matter that sidecars are not parsed ok at this step, but it is nice to have the correct 'won't try to import' label
* in the 'add metadata before we import' sub-dialog of the manual file import, the 'sidecars' list now shows better metadata context around the sidecar content preview. the 'notes' now compresses multiple notes better, with a total count and elided preview; the 'tags' now says to which tag service they are going; the 'URLs' now says how many URLs and elides better; the 'timestamp' now converts to a nice datestring and says what the destination is for that time (previously it just said the UNIX timestamp flat wew)

### Client API and setting archived time

* for sidecars and the Client API, if you push an archived time on a file in the inbox, the file is now auto-archived! the UI and Client API docs now mention this
* previously, the data was (buggily) recorded but generally swallowed and not reported anywhere
* the Client API 'file metadata' call now gives a `time_archived` if the file is not in the inbox and there is a known timestamp
* sidecar routers, when harvesting timestamps to send to a sidecar file, will no longer suck an orphaned archive timestamp from a file that is in the inbox
* Client API version is now 89

### QtMediaPlayer transparency

* the QtMediaPlayer should now support transparency properly! previously, if you put up an animated gif with transparency, the transparent areas would be black with ugly fuzzy edges. now it does a checkerboard or greenscreen using the same settings as a static image. on some files there is still fuzz, but most of the time it looks great
* the QtMediaPlayer should also show the normal background colour while it waits for a non-transparent media to load. it probably flashed black before, so should cut down on some load flicker
* the normal static image checkerboard and greenscreen transparency uses the same, updated tech here. rather than drawing a bunch of boxes, we now have a textured pixmap brush, and in fact in all cases we just set a brush and do an eraseRect, nice and clean. Qt is great
* the 'show transparency and checkerboard in duplicate filter' setting in `duplicates` is now its own thing and applies independently to the one in `media playback`. previously it had some whack 'only applies if the media playback guy is unchecked', which is the kind of thing you can convince yourself is useful and technically correct but violates KISS

### ingesting serialised objects

* the 'import downloaders' panel now accepts drag-and-drop or pasted JSON files
* added a `help->debug->what is this object?` debug review panel to figure out mysterious bits of json or png or whatever

### boring stuff

* improved error handling for setting QtMediaPlayer null audio device on media load
* improved how 'show menu' events are handled, Qt-side, for the: colour picker widget; preview viewer; navigable media viewer; taglist
* cleaned up some ugly menu generation/show for the main thumbnail grid
* decoupled the 'import downloaders' serialised object ingestion routine and brushed it up a bit with nicer error reporting and so on
* all serialisable objects can now describe themselves with regards to the serialisation system, with special hooks for named objects and the special list, dict, and bytes dict
* fixed up several places that do a fast 'is it a jpeg/png' mime check, which was accidentally skipping the png side and doing the slower check
* fixed a bad layout flag in the 'select from a list of checkboxes now mate' dialog
* fixed some close-curly-brace typos in the example Client API Services Object JSON

## [☠⛤ Version 666 ⛤☠](https://github.com/hydrusnetwork/hydrus/releases/tag/v666)

### misc

* when you create a new subscription with 'add' button, it now asks you for the downloader up-front. this was easy to forget to set, previously; now you can't miss it
* the problem where media could sometimes not receive any content updates (rating changes, new tags, archive state, whatever) after a backup, lock, vacuum, or certain database transaction error recovery until a page was refreshed is fixed. a cache that the in-session pages refer to was held by a database module being reinitialised through these processes and thus, after cache reset, the connection to send the update signal on media changes wasn't getting through to UI
* added `^(?!...$).+$` to the regex input menu's quick selections for 'anything except this exact phrase'. I have to search stackexchange to figure this out every time I need it; now it is just there. also fixed a formatting typo in this menu for the 'any of these' entry

### media players

* improved the guard around the QtMultimedia module for those Qts where it is not available or otherwise fails to import
* if your options say to show something with QtMediaPlayer but the module isn't available, it now falls back to an 'open externally' button and does a little popup; the same handling for missing mpv
* improved the popup text when you boot `help->about` with an unusual module load error to report
* rejiggered the `help->about` 'Optional Libraries' list a little and moved mpv over there
* added another mpv 'null' audio error text, 'Could not open device', to our new cascade of 'calm down, but set the null audio device now' error handling
* added a `KDE and QtMediaPlayer` note to the 'installing - linux' help. installing `mit-krb5` may help

### qss

* the e621, e621_redux, and Paper_Dark qss stylesheets get 'built_release' alternates to handle the new 'lib' directory structure of the built release
* updated the guidance on external asset paths in `options->style`

### import options test panel

* booting the options dialog in 'advanced mode' now adds a 'PREVIEW: import options' panel. this is the new central place that we'll be customising import options after my rewrite here is done
* this panel saves nothing; it is only for playing around with. it takes your current settings and migrates them to the new system as the expected pending migration will
* I am happy with the technical side of things here, but this thing is complicated and I would like to throw advanced users in the deep end and get feedback on where the roughest parts are. I'll sand down the roughest corners with better UI and consider it all as I revise the help docs
* april fools this year is yet more hellish UI from hydev

### removing the default downloaders

* a fresh hydrus client database no longer starts with any downloader definitions! I am cleaning some legacy things and making the program more neutral from the start. users will now add whichever downloaders they want from third-party sources like the user-run downloader repo. not an april fools--sorry for the trouble!
* existing users keep what they have; this only affects new installs
* this also frees me from the maintenance burden, which I was failing at. several of the defaults were breaking and I was not keeping up with the issues
* so, the defaults are removed from the `static` dir and there are no 'add from defaults' buttons in the edit panels. the UI now supports and starts with a 'null' state of 'no downloader selected' everywhere
* users without any downloaders are pointed to the `network->downloaders` menu and the user-run repository
* I updated the help documentation all over regarding this change. it may need a revisit to make new-user onboarding smoother

### import options boring work

* added a 'paste-fill' paste type to the options panel, for filling in gaps without overwriting
* wrote paste buttons for individual options panels and the import options container panel
* wrote copy buttons for the container panel
* fleshed out the valueChanged signals for all the new options panels: prefetch; file filtering; tag filtering; locations; tags; notes; presentation
* wrote a favourites button and adjusted the UI to handle it in the appropriate locations
* wrote some basic default favourites
* polished the UI, swapping to icon buttons, bunch of stuff
* fixed some little logic and label bugs in the new import options stuff

### little stuff and cleanup

* the little `▲/▼` button you see across the program on static boxes and the popup toaster now uses a special shared class that uses nicer Qt tech to signal things and sets two new object names for QSS devs to hook into: `HydrusCollapseArrowCollapsed` and `HydrusCollapseArrowExpanded`. note that these guys use reversed labels depending on whether they expand up or down (I just realised this, working on it now), so if you want to put a new label or icon in, I think we might want to do some more here. let me know how it goes
* I think I fixed an issue where the 'hey your page is super mega tall, careful' message was showing more than once if the session includes multiple pages in this state
* updated some new async ui code, the stuff that does new unified callBack/errBack panel restoration after job reset, so that it better handles the error of its parent window being closed early, and a program shutdown call. this was causing some harmless error spam in some things like a delayed subscription popup files button press/dismiss
* fixed a stupid error in the updated Linux .desktop file deployment script that broke the Icon with quotes around the path
* made the various widgets, options, and importers ok with having no downloaders available. the first time a GUG comes in via the drag and drop system, it is set as the default
* amidst the downloader work, cleaned up how subscriptions do some edit panel `Set` stuff, removing some legacy jank
* fixed up an old hardcoded 'downloader updated' callable with a nicer Qt Signal
* improved the error messages around invalid/missing/null GUGs at different stages
* I also removed some legacy downloader db update code that would have fallen to bitrot here
* fixed a couple `finally/return` syntax issues
* 'manage login scripts' now has some red text up top warning that it is an ancient system that does not work well
* updated the 'adding new downloaders' help to be more clear about what is going on with the png files. it now has an example png for artbooru.com
* updated a bunch of ancient screenshots in the help, particularly the parsing system
* fixed duplicated 'index to fetch' label in html parsing formula panel
* replaced some whackadoodle ListBook fixed sizing with a proper `sizeHint` and some live rowheight checking and framewidth correction
* cleaned up some subscription unit tests
* the media result cache is now a singleton instance that persists through a database restart, and the unit tests now do some careful clearing on test database resetting here
* fixed a missing definition typo in the filetype labels

## [Version 665](https://github.com/hydrusnetwork/hydrus/releases/tag/v665)

### misc

* fixed an issue where a duplicates page would not re-enable the 'launch the filter' button sometimes when it previously had a count of 0 but got a pair-discovery update in the background that added new pairs (issue #1988)
* the 'help: random 403 errors' entry is now moved down in the 'retry' buttonlist. it will disappear in a few weeks, leaving just the `network->downloaders` menu item, once people hitting this menu have had more chance to see it
* fixed an issue where if you selected some files with a subset of trash and then said 'move/copy n files to blah local file domain', it would try and move the trashed files and throw an error. it now filters those files out of the actual operation as you'd expect
* fixed an issue in numerical rating rendering after deleting a numerical rating service (bad error handling on the missing service)
* added a section to the 'contact' help page about how to send broken files to hydev (zipping them up explanation etc..)
* updated the `io.github.hydrusnetwork.hydrus.desktop` file with easy mode help comments on how to edit things and added `StartupWMClass` to help taskbar grouping

### more audio device stuff

* applying the options dialog now updates all open mpv players to use the specified audio device
* applying the options dialog now updates all open QtMediaPlayers to use the specified audio device
* the `ao/xxxxxx: There are no playback devices available` mpv error, which until now has sparked the 'hey things are looking unstable with this file, so unloading it' response, now triggers a new 'hey, set all mpv windows to `null` audio device'. if you get this stuff when you unplug your headphones or something, let me know how it goes
* when a new QtMediaPlayer initialises, if the desired audio device is invalid, it falls back to auto. if the auto device is invalid, it resets to null
* new DEBUG checkboxes in `options->media playback` allow you to auto-set mpv or the QtMediaPlayer to 'null' audio device when playing silent media like animated gifs. I know we've had some issues around this over the years with mpv on Linux in particular, so let's see how it goes. this used to be default behaviour for the QtMediaPlayer btw; now it isn't

### some boring cleanup

* improved how media objects determine if they are in 'combined local file domains'
* a database routine used in repository sync and 'are we mostly caught up to this repo?' that fetches missing update hashes is now significantly faster
* the QtMediaPlayer no longer leaves the video output disconnected until the first non-audio file is loaded. things are more KISS
* the mpv and QtMediaPlayers recognise better their current audio device and can trigger update calls only on changes
* cleaned up some more QtMediaPlayer output setting generally
* updated some critical error text when trying to boot into a database that was created--and failed to initialise--on the last program boot
* removed some OpenRaster debug statements

### removed Qt5 gubbins

* Qt5 (which for us means PySide2 or PyQt5) is well behind us now, so any lingering support is just getting in the way. I removed it all this week. if you are struggling on a hyperpatched Win7 machine, forgive me but it is time to move to Linux
* removed Qt5 initialisation code
* removed debug code that tests for Qt5 support
* removed old thumbnail UI scaling Qt5 hack
* removed Qt5 stylesheet hacks
* removed Qt5 media panel swap hacks
* removed About Window Qt5 stuff
* removed an old Qt5 qtpy init hack
* removed Qt5 patches for mouse and drop events
* removed Qt5 QPDF check
* removed Qt5 QKeySequence conversion

### nicer PIL memory cleanup

* PIL images are now closed promptly (freeing up memory better and faster) in more locations: the visual tuning suite; jpeg quality estimation; icc profile inspection; embedded metadata inspection; exif inspection; decompression bomb testing; 'show file metadata' window; import metadata gen; icc profile inspection on load; on forced PIL loads that error out on conversion to numpy; on another standard method to load images; variable frame duration fetching, 'get number of times to play animation'; 'animation has valid duration'; serialisable object import; ugoira rendering; ugoira API rendering; ugoira property fetch; ugoira json property fetch; Ugoira thumb gen; PSD rendering; PSD thumb gen; the native PIL animation renderer; on EXIF rotation conversion; some weird dequantization; resolution fetch; when some thumbnail stuff errors out; some animation property fetch; OpenXML thumbnail gen; Paint.NET thumb gen; Krita thumb gen; Krita rendering; openraster thumb gen; openraster rendering
* I went overkill here and yet there are still some gaps. I got all the file loads though, I think, which is the main stuff here that I think was lagging. some of it is also a little ugly. we'll see if this improves some lazy memory cleanup during heavy import. if it helps, I may revisit to clean up

### import options overhaul

* wrote migration code that takes the old file/tag/note import options and produces a new populated `ImportOptionsContainer`
* updated the prefetch import options to track the 'fetch metadata even...' checkboxes, although they will do nothing until migration
* updated 'note import options' to a legacy object that can deliver a trivially shucked 'note import options' that has no 'is default' properly and works in the new system. the edit notes dialog, Client API add-notes call, duplicate content merge options, notes sidecar exporter, and unit tests now use the new object
* wrote an edit panel for the new note import options
* the legacy note import options now holds its shucked version inside it, now only taking responsibility for the 'is default' property otherwise, and all import contexts now consult the new object for work
* juggled around my options stack preference again for simplicity and added 'import folder' and 'client api' options import contexts
* brushed up the UI significantly with new labels, better options summaries, better help, a KISSer workflow that filters out overengineered options by default, a little description label for each import type, and another for each options type
* made the stack description and display clearer and added it to the url class section
* fixed some default options display
* brushed up all the import options summary statements and rearranged them all into single-line
* updated some tag filter label grammar. in some cases it was saying 'tags taken: allowing all tags', which comes from internal permissions language, when just a 'tags taken: all tags' was a better fit

## [Version 664](https://github.com/hydrusnetwork/hydrus/releases/tag/v664)

### misc

* the media 'delete' menu is no longer a flyout if there is only one deletion option (you should see 'delete from my files' more often)
* the preview viewer now has the same style of delete menu as the canvas and thumbnail
* the system tray options are no longer disabled on non-advanced non-Windows. this stuff works better these days

### audio devices and tracks

* you can now select the output audio device for QtMediaPlayers under `options->media playback`. default is 'default' (issue #1985)
* there's also a "DEBUG: null" choice to say 'never load any audio output device to QtMediaPlayer'. I know we've had some users who have had trouble with this
* I then did the same for mpv. it works a little different, so you hit a button to fetch the available options and then select from there, or type in manually if you know otherwise. similarly, you can select a 'DEBUG: null' option
* QtMediaPlayers now show their audio tracks in the player right-click menu. it says title, language, and codec, depending on what is available. you can select another track and it changes instantly!
* QtMediaPlayers also now show their video track(s)! I added the same 'switch video track' call as the audio stuff. if anyone has a multi-video-track example vid, I'd love to see it for my testing purposes, but the audio side was a dream so I assume it just werkz
* some this stuff is very slightly hacky, so let me know how this works for you

### file import object inheritance cleanup

* _tl;dr: some downloaders save modified time better_
* when a gallery parse or a file post parse creates multiple child file import objects, I have cleaned up how the parent gives the children data--
* source time is now only propagated if the parent source time is A) sensible and B) the child doesn't already have an older timestamp. all instances of file import object source-time-setting now follow this rule, which is how modified times are generally updated elsewhere in hydrus. 'older is generally more useful and trustworthy, unless it is new year's day 1970 etc..' also, file import objects now clip to thirty seconds ago when given a timestamp from the future (happens with timezone fun sometimes). thanks, particularly, to the user who identified and chased this down (issue #1984)
* referral url is similarly now propagated more softly; only inherited if it isn't set beforehand (was previously a forced overwrite in all cases. not sure it actually matters, but it might in future)

### some boring cleanup

* some critical image rendering sections now clean up their memory quickly and explicitly rather than waiting for the garbage collector to handle it later. more to come here in future
* cleaned up how `AsyncQtJob`s do UI restoration after an error, harmonising how the callback and errback restore the UI
* decoupled how some exception stuff is caught and processed and rendered for the user, and fixed some error-reporting pathways that were not rendering nicely

### boring import options overhaul

* I juggled some more pending import options stuff around, giving the wilder stuff a KISS pass
* wrote a panel to handle editing the new defaults. I simplified things a good bit and moved it all to the options dialog. it is hidden for now but I feel fairly good about it all
* filled in a bunch of holes and fixed some display bugs as I stitched it all together
* improved how import options and their containers present for network vs local imports
* got the import options editing dialog to remember the last selected options type
* I've now got to write some favourites UI, polish this all, write some migration tech, and then update the import pipeline to handle it all. feels doable
