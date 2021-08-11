[必读](https://github.com/GreyHak/dsp-csv-gen#%E6%88%B4%E6%A3%AE%E7%90%83%E8%AE%A1%E5%88%92%E6%98%9F%E5%8C%BA%E8%B5%84%E6%BA%90%E8%A1%A8%E7%94%9F%E6%88%90%E5%99%A8)

# Star Sector Resource Spreadsheet Generator for Dyson Sphere Program

**DSP Star Sector Resource Spreadsheet Generator** is a mod for the Unity game Dyson Sphere Program developed by Youthcat Studio and published by Gamera Game.  The game is available on [here](https://store.steampowered.com/app/1366540/Dyson_Sphere_Program/).

This mod generates a comma-seperated-values, csv, file containing all the resources on all the planets in all the star systems in the whole star sector.  This mod is intended to be used as a planning tool.

What to know what tidally locked planets are around high-luminosity stars?  Want to know what system has spinform stalagmite crystals, fractal silicon and crude oil to make particle broadband?  This mod is for you.

Chinese support: The spreadsheet generation has been tested with Dyson Sphere Program configured for Chinese.  The generated file is always in a UTF-8 encoding.  If you want Chinese characters to appear correctly in your spreadsheet program, you need to open it using a UTF-8 encoding.  This has been tested in [OpenOffice Calc](https://www.openoffice.org/product/calc.html) by selecting the character set "Unicode (UTF-8)".

If you like this mod, please click the thumbs up at the [top of the page](https://dsp.thunderstore.io/package/GreyHak/DSP_Star_Sector_Resource_Spreadsheet_Generator/) (next to the Total rating).  That would be a nice thank you for me, and help other people to find a mod you enjoy.

If you have issues with this mod, please report them on [GitHub](https://github.com/GreyHak/dsp-csv-gen/issues).  You can also contact me at GreyHak#2995 on the [DSP Modding](https://discord.gg/XxhyTNte) Discord #tech-support channel.

## Usage
Generation of the resource spreadsheet is triggered with a button click, by loading a save or by pausing the game.
![Trigger Button image](https://raw.githubusercontent.com/GreyHak/dsp-csv-gen/master/TriggerButton.jpg)

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
   - _ Number of veins for each resource can be configurable enabled
 - Gas Planet Resource
   - _ Hydrogen rate
   - _ Deuterium rate
   - _ Fire ice rate

## Config Settings
The configuration file is called `greyhak.dysonsphereprogram.resourcespreadsheetgen.cfg`.  It is generated the first time you run the game with this mod installed.  On Windows 10 it is located at
 - If you installed manually:  `%PROGRAMFILES(X86)%\Steam\steamapps\common\Dyson Sphere Program\BepInEx\config\greyhak.dysonsphereprogram.resourcespreadsheetgen.cfg`
 - If you installed with r2modman:  `C:\Users\<username>\AppData\Roaming\r2modmanPlus-local\DysonSphereProgram\profiles\Default\BepInEx\config\greyhak.dysonsphereprogram.resourcespreadsheetgen.cfg`

This mod is also compatible with [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) which provides an in-game GUI for changing the settings in real-time.

![Config Settings Window image](https://raw.githubusercontent.com/GreyHak/dsp-csv-gen/master/ConfigSettingsWindow.jpg)

Settings include:
 - **Output Path:**  By default the spreadsheet is saved on Windows to `DSP_Star_Sector_Resources_${seed}-${starCount}.csv` to your Documents folder and on Linux to just `DSP_Star_Sector_Resources_${seed}-${starCount}.csv`.  The spreadsheet output path can be changed by specifying a different path in this config file.
 - **Planet Loading and Unloading:**  By default this mod loads all planets.  This is the only way to get all the planet resource data.  When disabling this flag the spreadsheet will indicate "Unloaded" for all fields that are unavailable because planet loading was skipped.  When planet loading is enabled, you can also enable planet unloading to save on memory.
 - **Spreadsheet Formatting:**  By default columns are separated with a comma, but that can be changed.  By default floats are in your local format, but that can be changed.  By default floats are output with their full precision, but that change be changed.
 - **More Data:**  17 parameters, 7 for the star and 10 for the planet, can be configurably enabled.  Less clutter for most people.  Extra data for those that want it.

## Installation
This mod uses the BepInEx mod plugin framework.  So BepInEx must be installed to use this mod.  Find details for installing BepInEx [in their user guide](https://bepinex.github.io/bepinex_docs/master/articles/user_guide/installation/index.html#installing-bepinex-1).  This mod was tested with BepInEx x64 5.4.11.0 and Dyson Sphere Program 0.8.19.7677 on Windows 10.

To manually install this mod, add the `DSPResourceSpreadsheetGenMod.dll` to your `%PROGRAMFILES(X86)%\Steam\steamapps\common\Dyson Sphere Program\BepInEx\plugins\` folder.

This mod can also be installed using ebkr's [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/) mod manager by clicking "Install with Mod Manager" on the [DSP Modding](https://dsp.thunderstore.io/package/GreyHak/DSP_Star_Sector_Resource_Spreadsheet_Generator/) site.

## Open Source
The source code for this mod is available for download, review and forking on GitHub [here](https://github.com/GreyHak/dsp-csv-gen) under the BSD 3 clause license.

# 戴森球计划——星区资源表生成器

本mod会生成一个由逗号分隔值的.csv文件，包含整个星区中全部恒星系、行星的全部资源。本mod旨在用作规划工具。

如果您喜欢这个mod，请点击[页面顶部](https://dsp.thunderstore.io/package/GreyHak/DSP_Star_Sector_Resource_Spreadsheet_Generator/)的大拇指（总评分旁），这是对我最好的鼓励，也能帮助其他人找到您喜欢使用的mod。

如果您在使用本mod时出现问题，请在GitHub上报告。我会尽量在12小时内回复。您也可以通过Discord在[DSP Modding](https://discord.com/invite/XxhyTNte)的#tech-support频道找到与我联系，我的ID是GreyHak#2995。

中文支持：

经过中文配置的测试，由于生成的文件始终采用UTF-8编码，将会产生乱码。如果你想要获得中文表格，需要用UTF-8编码格式将其打开。该方法已在[OpenOffice](https://www.openoffice.org/product/calc.html)中得到验证。您也可以尝试用记事本打开.csv文件，选择另存为，在编码方式中选择ANSI编码，不改变.csv后缀。此方法得到的.csv文件可用Windows Office打开。

## 用法
通过单击菜单上的按钮、加载存档、暂停游戏来触发mod生成资源表。

![Trigger Button image](https://raw.githubusercontent.com/GreyHak/dsp-csv-gen/master/TriggerButton.jpg)

游戏不会保留所有已加载的资源信息。所以手动触发生成表格的命令时，按钮会有一定的延迟，直到.csv文件创建完成。你可以在HUD菜单上看到进度。对于64星的星区，将冷却32秒。

## 导出的信息
生成的表格将包含以下数据列。
 - 行星名称
 - 恒星信息
	- _ 恒星名称
	- _ 恒星光度
	- _ 恒星类型
	- _ 恒星位置X,Y,Z
	- _ 更多请参考配置文件。
 - 行星属性
	- _ 风利用率
	- _ 光利用率
	- _ 行星类型
	- _ 陆地占比
	- _ 奇异性（如潮汐锁定）
	- _ 更多请参考配置文件
 - 岩类行星/卫星资源
	- _ 海洋类型
	- _ 铁矿
	- _ 铜矿
	- _ 硅矿
	- _ 钛矿
	- _ 石矿
	- _ 煤矿
	- _ 原油产率
	- _ 可燃冰矿
	- _ 金伯利矿
	- _ 分型硅矿
	- _ 有机晶体矿
	- _ 光栅石矿
	- _ 刺笋晶体矿
	- _ 单极磁石矿
 - 气体星球资源
	- _ 氢产率
	- _ 氘产率
	- _ 可燃冰产率

## 配置文件
配置文件名为`greyhak.dysonsphereprogram.resourcespreadsheetgen.cfg`。该文件在您安装mod后首次运行游戏时自动生成。它在Windows10上的位置为
 - 手动安装：`%PROGRAMFILES(X86)%\Steam\steamapps\common\Dyson Sphere Program\BepInEx\config\greyhak.dysonsphereprogram.resourcespreadsheetgen.cfg`
 - 使用r2modman安装：`C:\Users\<username>\AppData\Roaming\r2modmanPlus-local\DysonSphereProgram\profiles\Default\BepInEx\config\greyhak.dysonsphereprogram.resourcespreadsheetgen.cfg`

![Config Settings Window image](https://raw.githubusercontent.com/GreyHak/dsp-csv-gen/master/ConfigSettingsWindow.jpg)

设置包括：
 - **输出路径**：默认情况下，Windows系统上将以文件名`DSP_Star_Sector_Resources_${seed}-${starCount}.csv`保存到您的“文档”文件夹中，在Linux系统上将直接保存为`DSP_Star_Sector_Resources_${seed}-${starCount}.csv`。可以通过在配置文件中指定为其他路径。
 - **行星加载与否**：默认情况下，本mod会加载所有星球，只有这样才能加载全部星球的资源数据。禁用该项时，表格中将由于跳过加载而显示“未加载（Unloaded）”。您也可以通过禁用加载来节省内存。
 - **表格格式设置**：默认情况下，列之间用逗号分隔，可以更改。浮点数将应用您的本地格式，可以更改。浮点数将以全精度输出，可以更改。
 - **更多数据**：17个参数可供选择启用，7个恒星参数，10个行星参数，如温度、年龄、颜色等。通常这些参数并没有什么用，但总有人需要。

## 安装
本mod需要BepInEx mod插件框架，所以要使用此mod必须先安装BepInEx。请在[用户指南](https://bepinex.github.io/bepinex_docs/master/articles/user_guide/installation/index.html#installing-bepinex-1)中查阅BepInEx的安装信息。本mod已在Windows10上经BepInEx x64 5.4.11.0和戴森球计划0.8.19.7677版本进行测试。

要手动安装本mod，请添加`DSPResourceSpreadsheetGenMod.dll`至`%PROGRAMFILES(X86)%\Steam\steamapps\common\Dyson Sphere Program\BepInEx\plugins\`文件夹。

您也可以使用ebkr的[r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/) mod管理器，通过[DSP Modding](https://dsp.thunderstore.io/package/GreyHak/DSP_Star_Sector_Resource_Spreadsheet_Generator/)上的“Install With Mod Manager”按钮来安装本mod。

## 开源
在BSD3条例许可下，本mod的源代码可供在[GitHub](https://github.com/GreyHak/dsp-csv-gen)下载、查看、克隆。

## Change Log
### v3.2.1
 - Added new optional output for the star's maximum dyson sphere shell "orbital radius".
### v3.2.0
 - New feature added to allow users to select which planet they want to use has a reference for disance calculations.  Planet is selected in the spreadsheet after the spreadsheet is generated.  This feature is enabled with the Output DistanceFrom configuration setting.  New feature with support for OpenOffice Calc added by [ThomasBlt](https://github.com/ThomasBlt).  His submission has been enhanced to make it optional (disabled by default), and add support for Microsoft Excel in addition to OpenOffice Calc.
 - Bug fixed which was causing data to show as Unloaded.
 - Bug fix for planets with commas. Fix from [ThomasBlt](https://github.com/ThomasBlt).
### v3.1.4
 - New ocean type from v3.1.3 now identified as Ice.  Thanks to [Posi](https://github.com/posi74de) for the report and the information.
### v3.1.3 (Beta)
 - This update fixes a hanging bug caused by a recent Dyson Sphere Program update.
### v3.1.2
 - Fixed an error which wasn't outputting the available information when configured not to load planets.
 - Added new optional output for the number of veins of each resource on a planet.
 - Fixed a graphical glitch if config changes were made in game while spreadsheet generation was in progress.
 - Minor code cleanup for efficiency.
### v3.1.1
 - Minor bug fix in log entry of filename.
### v3.1.0
 - Added in-game reconfiguration changes and compatibility with [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager).
 - Chinese README translation by Heihong.
### v3.0.3
 - Added an additional output parameter, distance from star cluster center, which can be enabled in the config file.
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
