---
title: Help My Client Will Not Boot
---

# Help My Client Will Not Boot

## The purpose of this document

If you attempt to run the client and either nothing happens, you get a serious error and it halts, or the process starts but hangs forever, this document will guide you through what to do next.

## Instant Crash/Stop

If running the hydrus_client executable does nothing or gives you an odd error before dumping out, here are some common fixes to try:

1. Look for a `hydrus_crash.log` in your `install_dir/db` directory or user Desktop. Failing that, is there a `client - [date].log` file? Does it have an error at the bottom? If there is something, please send it in to me, hydrus_dev (see [contact](contact.md) for my contact details).

2. Some anti-virus program updates falsely detect that one of the dlls or other files in the client is bad and quietly quarantine them. Please check your anti-virus logs or compare your install directory with the 'extract only' release archive to see if there are missing files. Avast has done this several times. Instances of this are useful to know about as several users usually get hit by the same thing at the same time. Please feel free to also start a conversation if you just want to double-check it is a false-positive after all.

3. Extract a fresh 'extract only' client to your desktop and try running it. If an empty and new client boots but your existing client doesn't, that suggests there is either a problem with your database or a conflict with some older dlls from a previous install. A database problem will typically be reported in one of the log files, so you might like to try making a [clean install](getting_started_installing.md#clean_installs).

4. Check your permissions and hard drive health. For instance, sometimes when a hard drive has a fault, Windows sets it to 'dirty' mode, which causes all sorts of problems. Linux and OS X have presented their own permissions headaches. A default client works in 'portable' mode and needs read and write access to its 'db' folder.

5. Going forward, ensure you make [backup](getting_started_installing.md#backing_up) before updating the software. If a future update has a serious problem, it is then easy to rollback to the working version while we figure out a fix!

## Hanging Boot

If your client seems to hang on the database startup phase, especially after a previously bad shutdown (like a power cut), it could be that the db needs to 'heal' itself a bit. It can take several minutes, sometimes even more, to clean the bad shutdown up.

So, if your client puts up the splash screen but seems suddenly to take a very long time to start up, go make a coffee and give it time--it is probably just sorting itself out, not broken.

You can double-check this by looking at the hydrus_client executable in your OS's Task Manager (Ctrl+Shift+Esc on Windows). If it is doing some CPU/HDD, there is no need to kill the process.

Please contact hydrus_dev if it really does seem stuck (say, no progress after an hour, or if CPU/HDD activity completely drops to nothing for several minutes), or if every startup is delayed like this.

One possible cause of delayed startup every time (often a medium-length delay _before_ the splash screen appears) is overly paranoid virus scanners rechecking all of hydrus every time it starts. To relieve this, please check your anti-virus software's options.
