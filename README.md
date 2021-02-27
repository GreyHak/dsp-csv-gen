# Star Sector Resource Spreadsheet Generator for Dyson Sphere Program

**DSP Star Sector Resource Spreadsheet Generator** is a mod for the Unity game Dyson Sphere Program developed by Youthcat Studio and published by Gamera Game.  The game is available on [here](https://store.steampowered.com/app/1366540/Dyson_Sphere_Program/).

This mod generates a comma-seperated-values, csv, file containing all the resources on all the planets in all the star systems in the whole star sector.  This mod is intended to be used as a planning tool.

What to know what tidally locked planets are around high-luminosity stars?  Want to know what system has spinform stalagmite crystals, fractal silicon and crude oil to make particle broadband?  This mod is for you.

If you like this mod, please click the thumbs up at the [top of the page](https://dsp.thunderstore.io/package/GreyHak/DSP_Star_Sector_Resource_Spreadsheet_Generator/) (next to the Total rating).  That would be a nice thank you for me, and help other people to find a mod you enjoy.

If you have issues with this mod, please report them on [GitHub](https://github.com/GreyHak/dsp-csv-gen/issues).  I try to respond within 12 hours.

## Exported Details
The generated spreadsheet will contain columns for the following data.
 - Planet Name
 - Star Information
   - _ Star Name
   - _ Star Dyson Luminosity
   - _ Star Type
   - _ Star Mass
   - _ Star Position X, Y, Z
 - Planet Properties
   - _ Wind Strength
   - _ Luminosity
   - _ Planet Type
   - _ Land Percent
   - _ Singularity (e.g. Tidally Locked)
   - _ Planet/Moon
   - _ Orbital Inclination
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

## Usage
Generation of the resource spreadsheet is triggered with a button click, by loading a save or by pausing the game.
![TriggerButton.jpg](https://raw.githubusercontent.com/GreyHak/dsp-csv-gen/master/TriggerButton.jpg)

The game does not keep all resource information loaded.  So once spreadsheet generation is requested there is a delay of less than a minute until the .csv file is created.

## Config File
The configuration file is called greyhak.dysonsphereprogram.resourcespreadsheetgen.cfg.  It is generated the first time you run the game with this mod installed.  On Windows 10 it is located at
"%PROGRAMFILES(X86)%\Steam\steamapps\common\Dyson Sphere Program\BepInEx\config\greyhak.dysonsphereprogram.resourcespreadsheetgen.cfg".  

### Output Path Configurable
By default the spreadsheet is saved on Windows to DSP_Star_Sector_Resources.csv to your Documents folder and on Linux to just DSP_Star_Sector_Resources.csv.  The spreadsheet output path can be changed by specifying a different path in this config file.

### Save Trigger Configurable
By default the generation of the spreadsheet is performed when starting and pausing a game.  Both of these triggers can be enabled/disabled in the config file.

### Planet Loading Configurable
By default this mod loads all planets.  This is the only way to get all the planet resource data.  This step can be skipped if you want to save on memory.  When disabling this flag the spreadsheet will indicate "Unloaded" for all fields that are unavailable because planet loading was skipped.

## Installation
This mod uses the BepInEx mod plugin framework.  So BepInEx must be installed to use this mod.  Find details for installing BepInEx [in their user guide](https://bepinex.github.io/bepinex_docs/master/articles/user_guide/installation/index.html#installing-bepinex-1).  This mod was tested with BepInEx x64 5.4.5.0 and Dyson Sphere Program 0.6.16.5775 on Windows 10.

To manually install this mod, add the DSPResourceSpreadsheetGenMod.dll to your %PROGRAMFILES(X86)%\Steam\steamapps\common\Dyson Sphere Program\BepInEx\plugins\ folder.

This mod can also be installed using ebkr's [r2modman dsp](https://dsp.thunderstore.io/package/ebkr/r2modman_dsp/) mod manager by clicking "Install with Mod Manager" on the [DSP Modding](https://dsp.thunderstore.io/package/GreyHak/DSP_Star_Sector_Resource_Spreadsheet_Generator/) site.

## Compatibility
This mod works with Touhma's Galactic Scale mod, but using these mods together will dramatically increase memory usage.  Running this mod with 1024 systems has been seen to use 16GB of RAM.  If this is a problem for you, there is a config setting to disable the loading of planets.  Disabling planet loading won't produce as much information, but will save on memory.  With 64 star systems this mod uses about 1.7GB.

## Open Source
The source code for this mod is available for download, review and forking on GitHub [here](https://github.com/GreyHak/dsp-csv-gen) under the BSD 3 clause license.

## Change Log
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
 - Incorporated change from operinko to create the output directory if needed.
 - Enhancement to README, most notably to include a note about memory usage when this mod is used with Touhma's Galactic Scale mod.
### v1.1.0
 - Added a config file for specifying the path of the output spreadsheet.
 - Exception text is improved.
### v1.0.1
 - Added missing BepInEx dependency to manifest file.
### v1.0.0
 - Initial release.
