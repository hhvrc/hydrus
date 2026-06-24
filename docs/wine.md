---
title: Running In Wine/Bottles
---

# running a client or server in wine

Several Linux and macOS users have found success running hydrus with Wine. As hydrus has updated over the years, the exact config needed has changed.

In my experience (most recently 2026-06), using Bottles was the trick, and choosing the latest wine-11 as the runner. Wine 9.0 did not run v675.

You may need 'dotnet48' and 'vcrun2022', but I am not sure. Let me know what you figure out!

The general process:

- Install Bottles, get it all set up
- Add a new 'Application' profile, wait for it to set itself up.
- Under dependencies, install `dotnet48` and `vcrun2022`.
- Open up the profile in your file manager, and in the `drive_c` dir, extract your `Hydrus Network` install dir.
- Add the virtual `C:\Hydrus Network\hydrus_client.exe` as a new shortcut for your profile.
- Run the shortcut!

Some UI elements may be a bit wonky. Notebook tabs, fonts, text input fields, etc.. but the ffmpeg bridge works, so it detects video and audio filetypes, and mpv seems to work, with audio!
