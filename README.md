# Star Sector Resource Spreadsheet Generator for Dyson Sphere Program

**DSP Star Sector Resource Spreadsheet Generator** is a mod for the Unity game Dyson Sphere Program developed by Youthcat Studio and published by Gamera Game.  The game is available on [here](https://store.steampowered.com/app/1366540/Dyson_Sphere_Program/).

This mod generates a comma-seperated-values, csv, file containing all the resources on all the planets in all the star systems in the whole star sector.  This mod is intended to be used as a planning tool.

What to know what tidally locked planets are around high-luminosity stars?  Want to know what system has spinform stalagmite crystals, fractal silicon and crude oil to make particle broadband?  This mod is for you.

## Exported Details
The generated spreadsheet will contain columns for the following data.
 - Planet Name
 - Star Information
   - &nbsp; &nbsp; Star Name
   - &nbsp; &nbsp; Star Luminosity
   - &nbsp; &nbsp; Star Type
   - &nbsp; &nbsp; Star Mass
   - &nbsp; &nbsp; Star Position X, Y, Z
 - Planet Properties
   - &nbsp; &nbsp; Wind Strength
   - &nbsp; &nbsp; Luminosity
   - &nbsp; &nbsp; Planet Type
   - &nbsp; &nbsp; Land Percent
   - &nbsp; &nbsp; Singularity (e.g. Tidally Locked)
   - &nbsp; &nbsp; Planet/Moon
   - &nbsp; &nbsp; Orbital Inclination
 - Rocky Planet/Moon Resources
   - &nbsp; &nbsp; Ocean Type
   - &nbsp; &nbsp; Iron quantity
   - &nbsp; &nbsp; Copper quantity
   - &nbsp; &nbsp; Silicon quantity
   - &nbsp; &nbsp; Titanium quantity
   - &nbsp; &nbsp; Stone quantity
   - &nbsp; &nbsp; Coal quantity
   - &nbsp; &nbsp; Crude oil rate
   - &nbsp; &nbsp; Fire ice quantity
   - &nbsp; &nbsp; Kimberlite quantity
   - &nbsp; &nbsp; Fractal silicon quantity
   - &nbsp; &nbsp; Organic crystal quantity
   - &nbsp; &nbsp; Optical grating crystal quantity
   - &nbsp; &nbsp; Spiniform stalagmite crystal quantity
   - &nbsp; &nbsp; Unipolar magnet quantity
 - Gas Planet Resource
   - &nbsp; &nbsp; Hydrogen rate
   - &nbsp; &nbsp; Deuterium rate
   - &nbsp; &nbsp; Fire ice rate

## Usage
Generation of the resource spreadsheet is triggered by loading a save or pausing the game.
The game does not keep all resource information loaded.  So once spreadsheet generation is requested there is a delay of less than a minute until the .csv file is created.

The output path is configurable.
By default the spreadsheet is saved on Windows to (%USERPROFILE%\Documents\DSP_Star_Sector_Resources.csv) and on Linux to (DSP_Star_Sector_Resources.csv).

The configuration file is called greyhak.dysonsphereprogram.resourcespreadsheetgen.cfg.  It is generated the first time you run the game with this mod installed.  On Windows 10 it is located at
"%PROGRAMFILES(X86)%\Steam\steamapps\common\Dyson Sphere Program\BepInEx\config\greyhak.dysonsphereprogram.resourcespreadsheetgen.cfg".  The spreadsheet output path can be changed by specifying a different path in this config file.

## Installation
This mod uses the BepInEx mod plugin framework.  So BepInEx must be installed to use this mod.  Find details for installing BepInEx [in their user guide](https://bepinex.github.io/bepinex_docs/master/articles/user_guide/installation/index.html#installing-bepinex-1).  This mod was tested with BepInEx x64 5.4.5.0 and Dyson Sphere Program 0.6.16.5775 on Windows 10.

To manually install this mod, add the DSPResourceSpreadsheetGenMod.dll to your %PROGRAMFILES(X86)%\Steam\steamapps\common\Dyson Sphere Program\BepInEx\plugins\ folder.

This mod can also be installed using ebkr's [r2modman dsp](https://dsp.thunderstore.io/package/ebkr/r2modman_dsp/) mod manager by clicking "Install with Mod Manager" on the [DSP Modding](https://dsp.thunderstore.io/package/GreyHak/DSP_Star_Sector_Resource_Spreadsheet_Generator/) site.

## Compatibility
This mod works with Touhma's Galactic Scale mod, but using these mods together will dramatically increase memory usage.  Running this mod with 1024 systems has been seen to use 16GB of RAM.

## Open Source
The source code for this mod is available for download, review and forking on GitHub [here](https://github.com/GreyHak/dsp-csv-gen) under the BSD 3 clause license.
