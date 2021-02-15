# Star Sector Resource Spreadsheet Generator for Dyson Sphere Program

**DSP Star Sector Resource Spreadsheet Generator** is a mod for the Unity game Dyson Sphere Program developed by Youthcat Studio and published by Gamera Game.  The game is available on [here](https://store.steampowered.com/app/1366540/Dyson_Sphere_Program/).

This mod generates a comma-seperated-values, csv, file containing all the resources on all the planets in all the star systems in the whole star sector.  This mod is intended to be used as a planning tool.

What to know what tidally locked planets are around high-luminosity stars?  Want to know what system has spinform stalagmite crystals, fractal silicon and crude oil to make particle broadband?  This mod is for you.

## Exported Details
The generated spreadsheet will contain columns for the following data.
 - Planet Name
 - Star Information
	 - Star Name
	 - Star Luminosity
	 - Star Type
	 - Star Mass
	 - Star Position X, Y, Z
 - Planet Properties
	 - Wind Strength
	 - Luminosity
	 - Planet Type
	 - Land Percent
	 - Singularity (e.g. Tidally Locked)
	 - Planet/Moon
	 - Orbital Inclination
 - Rocky Planet/Moon Resources
	 - Ocean Type
	 - Iron quantity
	 - Copper quantity
	 - Silicon quantity
	 - Titanium quantity
	 - Stone quantity
	 - Coal quantity
	 - Crude oil rate
	 - Fire ice quantity
	 - Kimberlite quantity
	 - Fractal silicon quantity
	 - Organic crystal quantity
	 - Optical grating crystal quantity
	 - Spiniform stalagmite crystal quantity
	 - Unipolar magnet quantity
 - Gas Planet Resource
	 - Hydrogen rate
	 - Deuterium rate
	 - Fire ice rate

## Usage
Generation of the resource spreadsheet is triggered by loading a save or pausing the game.
The game does not keep all resource information loaded.  So once spreadsheet generation is requested there is a delay of less than a minute until the .csv file is created.

On Windows the spreadsheet is saved to
 - %USERPROFILE%\Documents\DSP_Star_Sector_Resources.csv

On Linux the spreadsheet is saved to DSP_Star_Sector_Resources.csv without any specific subdirectory.

## Installation
This mod uses the BepInEx mod plugin framework.  So BepInEx must be installed to use this mod.  Find details for installing BepInEx [in their user guide](https://bepinex.github.io/bepinex_docs/master/articles/user_guide/installation/index.html#installing-bepinex-1).  This mod was tested with BepInEx x64 5.4.5.0 and Dyson Sphere Program 0.6.16.5775 on Windows 10.

To manually install this mod, add the DSPResourceSpreadsheetGenMod.dll to your %PROGRAMFILES(X86)%\Steam\steamapps\common\Dyson Sphere Program\BepInEx\plugins\ folder.

## Open Source
The source code for this mod is available for download, review and forking on GitHub [here](https://github.com/GreyHak/dsp-csv-gen) under the BSD 3 clause license.
