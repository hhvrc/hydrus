- What is this folder? -

The qss files in this place (and, if you create it, in a mirrored location under your db dir) appear as the UI stylesheet options under _options->style_.


- How to create custom stylesheets -

Don't edit any of the files in here--they'll just be overwritten the next time you update. Instead, create a new folder under "db_dir/static/qss" and copy one of these over for editing. Search the help files for 'custom assets' for more info on how this works.

The best thing is just to change some colour hex code, but if you feel brave, feel free to try inserting your own stuff. QSS is cool, and you can edit text size, change colours, make borders, add hover behaviour, all sorts.

Check out these existing QSS files for common names you might want to refer to in your own QSS. Hydrus uses some standard Qt widget names, custom class names, and some property loading.

The default_hydrus.qss is used by the client to draw some custom widget colours. It is prepended to any custom stylesheet that is loaded.


- More QSS -

A hydrus user created the Nereid styles! You can find the latest releases here:

https://github.com/6788-00/nereid-theme-hydrus

For more inspiration, there are more QSS files here:

https://wiki.qt.io/Gallery_of_Qt_CSS_Based_Styles

And a bunch of random projects have some too, such as:

https://github.com/ModOrganizer2/modorganizer/tree/master/src/stylesheets


- QSS Assets -

UNDER TESTING. HIT UP `help->debug->debug modes->qss absolute path test mode` TO TRY THIS OUT!

A QSS that has assets under a subfolder, like you see here for 'e621', 'Nereid', and 'Paper', can be tricky. The question is one of relative vs absolute paths. Relative paths are convenient, but deploying them is difficult.

For a while, because of some funny Qt path rules, we had to be careful about ensuring the CWD was the base install folder or manually replacing relative with absolute paths. Now, I fix it for you by replacing relative paths with the correct absolute path when I load the QSS up.

TO MAKE A PATH LOAD CORRECT, YOU MUST WRITE IT STARTING WITH `"static/qss/`, LIKE THIS:

url("static/qss/my_style/some_asset.svg")

I will replace that `"static/qss` with the respective absolute prefix when I load the file, no matter whether the QSS file is in a source's `install_dir/static/qss`, a built install's `install_dir/lib/static/qss`, or a user-custom `db_dir/static/qss`.

If it doesn't work, your log file will probably get some "Could not create pixmap from whatever/static/qss/blah/blah.svg" lines when the style tries to load, and you'll see the path it tried to use.
