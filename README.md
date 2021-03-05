# Star Sector Resource Spreadsheet Generator for Dyson Sphere Program

**DSP Star Sector Resource Spreadsheet Generator** is a mod for the Unity game Dyson Sphere Program developed by Youthcat Studio and published by Gamera Game.  The game is available on [here](https://store.steampowered.com/app/1366540/Dyson_Sphere_Program/).

This mod generates a comma-seperated-values, csv, file containing all the resources on all the planets in all the star systems in the whole star sector.  This mod is intended to be used as a planning tool.

What to know what tidally locked planets are around high-luminosity stars?  Want to know what system has spinform stalagmite crystals, fractal silicon and crude oil to make particle broadband?  This mod is for you.

If you like this mod, please click the thumbs up at the [top of the page](https://dsp.thunderstore.io/package/GreyHak/DSP_Star_Sector_Resource_Spreadsheet_Generator/) (next to the Total rating).  That would be a nice thank you for me, and help other people to find a mod you enjoy.

If you have issues with this mod, please report them on [GitHub](https://github.com/GreyHak/dsp-csv-gen/issues).  I try to respond within 12 hours.

## Usage
Generation of the resource spreadsheet is triggered with a button click, by loading a save or by pausing the game.
![TriggerButton.jpg](https://raw.githubusercontent.com/GreyHak/dsp-csv-gen/master/TriggerButton.jpg)

The game does not keep all resource information loaded.  So once spreadsheet generation is requested there is a delay until the .csv file is created.  You can see the progress shown on the HUD button.  It's been timed at 32 seconds for a 64-star system.

## Exported Details
The generated spreadsheet will contain columns for the following data.
 - Planet Name
 - Star Information
   - _ Star Name
   - _ Star Dyson Luminosity
   - _ Star Type
   - _ Star Position X, Y, Z
   - _ Plus more. See the config file.
 - Planet Properties
   - _ Wind Strength
   - _ Luminosity
   - _ Planet Type
   - _ Land Percent
   - _ Singularity (e.g. Tidally Locked)
   - _ Plus more. See the config file.
 - Rocky Planet/Moon Resources
   - _ Ocean Type
   - _ Iron quantity
   - _ Copper quantity
   - _ Silicon quantity
   - _ Titanium quantity
   - _ Stone quantity
   - _ Coal quantity
   - _ Crude oil rate
   - _ Fire ice quantity
   - _ Kimberlite quantity
   - _ Fractal silicon quantity
   - _ Organic crystal quantity
   - _ Optical grating crystal quantity
   - _ Spiniform stalagmite crystal quantity
   - _ Unipolar magnet quantity
 - Gas Planet Resource
   - _ Hydrogen rate
   - _ Deuterium rate
   - _ Fire ice rate

## Config File
The configuration file is called greyhak.dysonsphereprogram.resourcespreadsheetgen.cfg.  It is generated the first time you run the game with this mod installed.  On Windows 10 it is located at
"%PROGRAMFILES(X86)%\Steam\steamapps\common\Dyson Sphere Program\BepInEx\config\greyhak.dysonsphereprogram.resourcespreadsheetgen.cfg".  

 - **Output Path:**  By default the spreadsheet is saved on Windows to DSP_Star_Sector_Resources_${seed}-${starCount}.csv to your Documents folder and on Linux to just DSP_Star_Sector_Resources.csv.  The spreadsheet output path can be changed by specifying a different path in this config file.
 - **Planet Loading and Unloading:**  By default this mod loads all planets.  This is the only way to get all the planet resource data.  When disabling this flag the spreadsheet will indicate "Unloaded" for all fields that are unavailable because planet loading was skipped.  When planet loading is enabled, you can also enable planet unloading to save on memory.
 - **Spreadsheet Formatting:**  By default columns are separated with a comma, but that can be changed.  By default floats are in your local format, but that can be changed.  By default floats are output with their full precision, but that change be changed.
 - **More Data:**  16 parameters, 6 for the star and 10 for the planet, can be configurably enabled.  Less clutter for most people.  Extra data for those that want it.

## Installation
This mod uses the BepInEx mod plugin framework.  So BepInEx must be installed to use this mod.  Find details for installing BepInEx [in their user guide](https://bepinex.github.io/bepinex_docs/master/articles/user_guide/installation/index.html#installing-bepinex-1).  This mod was tested with BepInEx x64 5.4.5.0 and Dyson Sphere Program 0.6.16.5827 on Windows 10.

To manually install this mod, add the DSPResourceSpreadsheetGenMod.dll to your %PROGRAMFILES(X86)%\Steam\steamapps\common\Dyson Sphere Program\BepInEx\plugins\ folder.

This mod can also be installed using ebkr's [r2modman dsp](https://dsp.thunderstore.io/package/ebkr/r2modman_dsp/) mod manager by clicking "Install with Mod Manager" on the [DSP Modding](https://dsp.thunderstore.io/package/GreyHak/DSP_Star_Sector_Resource_Spreadsheet_Generator/) site.

## Open Source
The source code for this mod is available for download, review and forking on GitHub [here](https://github.com/GreyHak/dsp-csv-gen) under the BSD 3 clause license.

## Change Log
### Next
 - Another speed optimization from [Therzok](https://github.com/Therzok).
### v3.0.2
 - Rebuild required for the recent Dyson Sphere Program [0.6.16.5827 update](https://store.steampowered.com/news/app/1366540?updates=true&emclan=103582791468239772&emgid=3064108587879145729).
### v3.0.1
 - Further speed optimizations.  Thanks to [Therzok](https://github.com/Therzok) for the suggestions.
 - Additional output data which can be enabled in the configuration file.
### v3.0.0-beta
 - Fast new vein generation algorithm which also minimizes memory usage.  No more 2-hour exports for 1024 stars.  Now exporting 1024 stars (8000 planets) has been timed under 15 minutes.
 - Fixed a bug added in v2.0.2's update for localization. If the singularity value had multiple flags set, it would extend beyond its column.
 - Removed start triggers on game start and pause now that there is a button on the HUD.
 - Thanks to Re=L and JackPS9 on Discord for testing and feedback on the beta.
### v2.0.2
 - Added configurability for number format localization, column separators, and floating point precision. (Contributed by [ctcDNightmare](https://github.com/ctcDNightmare).)
 - README corrections.
### v2.0.1
 - ${seed} and ${starCount} can now be optionally included in the output filename generation.  (Contributed by [ctcDNightmare](https://github.com/ctcDNightmare).)
 - Small improvement in progress bar accuracy.
### v2.0.0
 - New planet loading algorithm to minimize memory usage.  This should greatly decrease the amount of memory needed for this mod, especially when it is used in conjunction with Touhma's Galactic Scale mod.
 - Fixed a bug which would cause planets to not show up for a while while the planets being loaded for the spreadsheet were generating.  Sorry, but fixing this problem is going to slow down the process.  I think you'll agree the slower speed is worth the game stability.
 - Fixed a bug which would cause the spreadsheet not to generate if the last planet on the last star was an ocean world.
 - New configuration setting to enable planet unloading.
### v1.3.0
 - Added two patches to correct for bugs in the original game, bugs which cause problems when exiting a game to the load menu while planets/factories are being loaded.  One of these bugs results in an exception being thrown.  Another results in all local planets being colored magenta.  There's a third bug in the original game which occurs in the vegetable generation, but results in an exception which doesn't appear to be causing issues, so has been left alone.
 - Cancel spreadsheet generation when a game ends.
 - Reset progress circle when a new game is loaded while generation was in progress for the old game.
 - Added bug reporting link to this README.
 - Improvement in identifying load menu game.
### v1.2.0
 - Added button to HUD to trigger spreadsheet generation.  Button will show the progress of loading planets.  So the two prior triggers (game load and pause) are now disabled by default.
 - Added column for star dyson luminosity.  Removed star luminosity to avoid confusion.
 - Adjusted the description in the manifest
 - Minor README compatibility enhancement
### v1.1.4
 - Added config option to disable planet loading.
### v1.1.3
 - Added ability to enable and disable save triggers in config file.
 - Improved compatability of default save file location.
### v1.1.2
 - README-only update.  thunderstore needs to update their markdown interpreter for list subitems.
### v1.1.1
 - Incorporated change from [operinko](https://github.com/operinko) to create the output directory if needed.
 - Enhancement to README, most notably to include a note about memory usage when this mod is used with Touhma's Galactic Scale mod.
### v1.1.0
 - Added a config file for specifying the path of the output spreadsheet.
 - Exception text is improved.
### v1.0.1
 - Added missing BepInEx dependency to manifest file.
### v1.0.0
 - Initial release.
