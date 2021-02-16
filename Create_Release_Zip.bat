@ECHO OFF
"%ProgramFiles%\7-Zip\7z" a ..\GreyHak-DSP_Star_Sector_Resource_Spreadsheet_Generator-9.zip icon.png LICENSE.txt manifest.json README.md
cd bin\Release
"%ProgramFiles%\7-Zip\7z" u ..\..\..\GreyHak-DSP_Star_Sector_Resource_Spreadsheet_Generator-9.zip DSPResourceSpreadsheetGenMod.dll
