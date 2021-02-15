//
// Copyright (c) 2021, Aaron Shumate
// All rights reserved.
//
// This source code is licensed under the BSD-style license found in the
// LICENSE.txt file in the root directory of this source tree. 
//
// Dyson Sphere Program is developed by Youthcat Studio and published by Gamera Game.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.IO;
using BepInEx.Logging;
using System.Security;

namespace StarSectorResourceSpreadsheetGenerator
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    public class SpreadsheetGenMod : BaseUnityPlugin  // Plugin config: "C:\Program Files (x86)\Steam\steamapps\common\Dyson Sphere Program\BepInEx\config\BepInEx.cfg"
    {
        public const string pluginGuid = "greyhak.dysonsphereprogram.resourcespreadsheetgen";
        public const string pluginName = "DSP Star Sector Resource Spreadsheet Generator";
        public const string pluginVersion = "1.1.0.0";
        public static bool spreadsheetGenRequestFlag = false;
        public static string spreadsheetFileName = "default.csv";
        new internal static ManualLogSource Logger;
        new internal static BepInEx.Configuration.ConfigFile Config;

        public void Awake()
        {
            SpreadsheetGenMod.Logger = base.Logger;  // "C:\Program Files (x86)\Steam\steamapps\common\Dyson Sphere Program\BepInEx\LogOutput.log"
            SpreadsheetGenMod.Config = base.Config;

            // Determine the default spreadsheet path and configured spreadsheet path.
            spreadsheetFileName = "DSP_Star_Sector_Resources.csv";
            if (Environment.GetEnvironmentVariable("USERPROFILE") != null)
            {
                spreadsheetFileName = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Documents\" + spreadsheetFileName);
            }
            spreadsheetFileName = Config.Bind<string>("Output", "SpreadsheetFileName", spreadsheetFileName, "Path to the output spreadsheet.").Value;
            Logger.LogInfo("Will use spreadsheet path \"" + spreadsheetFileName + "\"");


            Harmony harmony = new Harmony(pluginGuid);

            System.Reflection.MethodInfo originalBegin = AccessTools.Method(typeof(GameMain), "Begin");
            System.Reflection.MethodInfo originalPause = AccessTools.Method(typeof(GameMain), "Pause");
            System.Reflection.MethodInfo originalPlanetLoadPrim = AccessTools.Method(typeof(PlanetData), "NotifyLoaded");  // This one works for each planet with a delay. This acts more like a reoccuring interval even onces the requested loads are complete.
            System.Reflection.MethodInfo originalPlanetLoadAlt = AccessTools.Method(typeof(PlanetAlgorithm), "GenerateVeins");  // This one works for each planet immediately.

            System.Reflection.MethodInfo myQueueLoading = AccessTools.Method(typeof(SpreadsheetGenMod), "QueuePlanetLoading");
            System.Reflection.MethodInfo myNotifyLoaded = AccessTools.Method(typeof(SpreadsheetGenMod), "OnPlanetFactoryLoaded");

            harmony.Patch(originalBegin, new HarmonyMethod(myQueueLoading));  // Run mine before
            harmony.Patch(originalPause, new HarmonyMethod(myQueueLoading));  // Run mine before
            harmony.Patch(originalPlanetLoadPrim, null, new HarmonyMethod(myNotifyLoaded));  // Run mine after

            Logger.LogInfo("Initialization complete.");
        }

        // Called on save load and game pause.  Queues planet loading which will trigger OnFactoryLoaded().
        public static void QueuePlanetLoading()
        {
            if (GameMain.gameName == "0")
            {
                SpreadsheetGenMod.Logger.LogInfo("Ignoring load screen.");
                return;
            }

            SpreadsheetGenMod.Logger.LogInfo("Checking for planets to load...");

            uint loadRequests = 0;
            foreach (StarData star in GameMain.universeSimulator.galaxyData.stars)
            {
                foreach (PlanetData planet in star.planets)
                {
                    if ((planet.type != EPlanetType.Gas) && (planet.veinGroups.Length == 0))
                    {
                        // PlanetModelingManager.PlanetComputeThreadMain (static, but private) -> PlanetAlgorithm.GenerateVeins
                        //PlanetModelingManager.Algorithm(planet).GenerateVeins(false);  // Fails when called directly because something is null.
                        planet.Load();
                        loadRequests++;
                    }
                }
            }

            if (loadRequests == 0)
            {
                SpreadsheetGenMod.Logger.LogInfo("Planets already loaded.  Proceeding with resource spreadsheet generation.");
                GenerateResourceSpreadsheet();
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendFormat("Requested {0} planets be loaded.  Waiting for planets to load.", loadRequests);
                SpreadsheetGenMod.Logger.LogInfo(sb.ToString());
                SpreadsheetGenMod.spreadsheetGenRequestFlag = true;
            }
        }

        // Called when each planet loads.  When all planets are loaded, will call GenerateResourceSpreadsheet().
        public static void OnPlanetFactoryLoaded()
        {
            //SpreadsheetGenMod.Logger.LogInfo("Planet loaded.");

            if (SpreadsheetGenMod.spreadsheetGenRequestFlag)
            {
                SpreadsheetGenMod.Logger.LogInfo("Checking if there are still unloaded planets...");

                uint unloadedPlanetCount = 0;
                foreach (StarData star in GameMain.universeSimulator.galaxyData.stars)
                {
                    foreach (PlanetData planet in star.planets)
                    {
                        if ((planet.type != EPlanetType.Gas) && (planet.veinGroups.Length == 0))
                        {
                            unloadedPlanetCount++;
                        }
                    }
                }

                if (unloadedPlanetCount == 0)
                {
                    SpreadsheetGenMod.spreadsheetGenRequestFlag = false;
                    SpreadsheetGenMod.Logger.LogInfo("Planet loading completed.  Proceeding with resource spreadsheet generation.");
                    GenerateResourceSpreadsheet();
                }
            }
        }

        // Called when all planets are loaded.  Saves resource spreadsheet.
        public static void GenerateResourceSpreadsheet()
        {
            try
            {
                SpreadsheetGenMod.Logger.LogInfo("Begin resource spreadsheet generation...");

                var sb = new StringBuilder();
                sb.Append("Planet Name,Star Name,Star Luminosity,Star Type,Star Mass,Star Position X,Star Position Y,Star Position Z,Wind Strength,Luminosity,Planet Type,Land Percent,Singularity,Planet/Moon,Orbit Inclination,Ocean,");
                //sb.Append("Ocean,Iron Ore,Copper Ore,Silicon Ore,Titanium Ore,Stone Ore,Coal Ore,Crude Oil,Fire Ice,Kimberlite Ore,Fractal Silicon,Spiniform Stalagmite Crystal,Optical Grating Crystal,Bamboo,Unipolar Magnet,");
                foreach (VeinProto item in LDB.veins.dataArray)
                {
                    sb.AppendFormat("{0},", item.name);
                }
                int[] gases = { 1120, 1121, 1011 };
                foreach (int item in gases)
                {
                    sb.AppendFormat("{0},", LDB.items.Select(item).name);
                }
                sb.Append("\n");

                foreach (StarData star in GameMain.universeSimulator.galaxyData.stars)
                {
                    foreach (PlanetData planet in star.planets)
                    {
                        sb.AppendFormat("{0},", planet.displayName);
                        sb.AppendFormat("{0},", star.displayName);
                        sb.AppendFormat("{0},", star.luminosity);
                        sb.AppendFormat("{0},", star.typeString);
                        sb.AppendFormat("{0},", star.mass);
                        sb.AppendFormat("{0},", star.position.x);
                        sb.AppendFormat("{0},", star.position.y);
                        sb.AppendFormat("{0},", star.position.z);

                        sb.AppendFormat("{0},", planet.windStrength);
                        sb.AppendFormat("{0},", planet.luminosity);
                        sb.AppendFormat("{0},", planet.typeString);
                        sb.AppendFormat("{0},", planet.landPercent);
                        sb.AppendFormat("\"{0}\",", planet.singularity);
                        sb.AppendFormat("{0},", planet.orbitAround);  // Mostly 0, but also 1-4
                        sb.AppendFormat("{0},", planet.orbitInclination);

                        if (planet.type == EPlanetType.Gas)
                        {
                            sb.Append("None,");  // Ocean
                            foreach (VeinProto item in LDB.veins.dataArray)
                            {
                                sb.Append("0,");
                            }
                            foreach (int item in gases)
                            {
                                int index = Array.IndexOf(planet.gasItems, item);
                                if (index == -1)
                                {
                                    sb.Append("0,");
                                }
                                else
                                {
                                    sb.AppendFormat("{0},", planet.gasSpeeds[index]);
                                }
                            }
                            sb.Append("\n");
                        }
                        else
                        {
                            if (planet.waterItemId == 0)
                            {
                                sb.Append("None,");
                            }
                            else if (planet.waterItemId == -1)
                            {
                                sb.Append("Lava,");
                            }
                            else
                            {
                                ItemProto waterItem = LDB.items.Select(planet.waterItemId);
                                sb.AppendFormat("{0},", waterItem.name);
                            }

                            if (planet.veinGroups.Length == 0)
                            {   // Theoretically this shouldn't happen.
                                planet.Load();

                                foreach (VeinProto item in LDB.veins.dataArray)
                                {
                                    sb.Append("Unloaded,");
                                }
                            }
                            else
                            {
                                EVeinType type = (EVeinType)1;
                                foreach (VeinProto item in LDB.veins.dataArray)
                                {
                                    long amount = planet.veinAmounts[(int)type];
                                    if (type == EVeinType.Oil)
                                    {
                                        sb.AppendFormat("{0},", (double)amount * VeinData.oilSpeedMultiplier);
                                    }
                                    else
                                    {
                                        sb.AppendFormat("{0},", amount);
                                    }
                                    type++;
                                }
                            }
                            foreach (int item in gases)
                            {
                                sb.Append("0,");
                            }
                            sb.Append("\n");
                        }
                    }
                }

                // Make sure the folder we're trying to write in exists.
                // {username}/Documents doesn't always exist on Wine platform.
                System.IO.FileInfo file = new System.IO.FileInfo(spreadsheetFileName);
                file.Directory.Create(); // If the directory already exists, this method does nothing.

                File.WriteAllText(spreadsheetFileName, sb.ToString());

                SpreadsheetGenMod.Logger.LogInfo("Completed saving resource spreadsheet.");
            }
            catch (ArgumentNullException e)
            {
                SpreadsheetGenMod.Logger.LogInfo("ERROR: ArgumentNullException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch (ArgumentException e)
            {
                SpreadsheetGenMod.Logger.LogInfo("ERROR: ArgumentException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch (PathTooLongException e)
            {
                SpreadsheetGenMod.Logger.LogInfo("ERROR: PathTooLongException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch (DirectoryNotFoundException e)
            {
                SpreadsheetGenMod.Logger.LogInfo("ERROR: DirectoryNotFoundException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch (IOException e)
            {
                SpreadsheetGenMod.Logger.LogInfo("ERROR: IOException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                SpreadsheetGenMod.Logger.LogInfo("ERROR: UnauthorizedAccessException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch (NotSupportedException e)
            {
                SpreadsheetGenMod.Logger.LogInfo("ERROR: NotSupportedException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch (SecurityException e)
            {
                SpreadsheetGenMod.Logger.LogInfo("ERROR: SecurityException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch
            {
                SpreadsheetGenMod.Logger.LogInfo("ERROR: Exception (catch-all) while generating and saving resource spreadsheet.");
            }
        }
    }
}
