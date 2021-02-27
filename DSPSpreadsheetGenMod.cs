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
using UnityEngine.UI;
using System.IO;
using BepInEx.Logging;
using System.Security;
//using System.Security.Permissions;

//[module: UnverifiableCode]
//#pragma warning disable CS0618 // Type or member is obsolete
//[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
//#pragma warning restore CS0618 // Type or member is obsolete
namespace StarSectorResourceSpreadsheetGenerator
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    public class SpreadsheetGenMod : BaseUnityPlugin  // Plugin config: "C:\Program Files (x86)\Steam\steamapps\common\Dyson Sphere Program\BepInEx\config\BepInEx.cfg"
    {
        public const string pluginGuid = "greyhak.dysonsphereprogram.resourcespreadsheetgen";
        public const string pluginName = "DSP Star Sector Resource Spreadsheet Generator";
        public const string pluginVersion = "1.2.0.0";
        public static bool enablePlanetLoadingFlag = true;
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
            if (Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) != "")
            {
                spreadsheetFileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar + spreadsheetFileName;
            }
            spreadsheetFileName = Config.Bind<string>("Output", "SpreadsheetFileName", spreadsheetFileName, "Path to the output spreadsheet.").Value;

            enablePlanetLoadingFlag = Config.Bind<bool>("Enable", "LoadAllPlanets", true, "Planet loading is needed to get all resource data, but you can skip this step for memory efficiency.").Value;
            bool enableOnStartTrigger = Config.Bind<bool>("Enable", "SaveOnStart", false, "Whether or not saving should be triggered by starting a game.").Value;
            bool enableOnPauseTrigger = Config.Bind<bool>("Enable", "SaveOnPause", false, "Whether or not saving should be triggered by pausing the game.").Value;

            Logger.LogInfo("Will use spreadsheet path \"" + spreadsheetFileName + "\"");

            Harmony harmony = new Harmony(pluginGuid);
            harmony.PatchAll(typeof(SpreadsheetGenMod));

            System.Reflection.MethodInfo originalBegin = AccessTools.Method(typeof(GameMain), "Begin");
            System.Reflection.MethodInfo originalPause = AccessTools.Method(typeof(GameMain), "Pause");
            System.Reflection.MethodInfo myQueueLoading = AccessTools.Method(typeof(SpreadsheetGenMod), "QueuePlanetLoading");

            if (enableOnStartTrigger)
            {
                Logger.LogInfo("Enabling trigger to save on starting a game");
                harmony.Patch(originalBegin, new HarmonyMethod(myQueueLoading));  // Run mine before
            }
            if (enableOnPauseTrigger)
            {
                Logger.LogInfo("Enabling trigger to save on pausing a game");
                harmony.Patch(originalPause, new HarmonyMethod(myQueueLoading));  // Run mine before
            }

            Logger.LogInfo("Initialization complete.");
        }

        // Called on save load and game pause.  Queues planet loading which will trigger OnFactoryLoaded().
        public static void QueuePlanetLoading()
        {
            if (GameMain.gameName == "0")
            {
                Logger.LogInfo("Ignoring load screen.");
                return;
            }

            if (!enablePlanetLoadingFlag)
            {
                Logger.LogInfo("Skipping planet load check.  Proceeding with resource spreadsheet generation.  Speadsheet will likely be incomplete.");
                GenerateResourceSpreadsheet();
                return;
            }

            Logger.LogInfo("Checking for planets to load...");

            uint loadRequests = 0;
            foreach (StarData star in GameMain.universeSimulator.galaxyData.stars)
            {
                foreach (PlanetData planet in star.planets)
                {
                    if ((planet.type != EPlanetType.Gas) && (planet.veinGroups.Length == 0))
                    {
                        // PlanetModelingManager.PlanetComputeThreadMain (static, but private) -> PlanetAlgorithm.GenerateVeins
                        // Unable to call GenerateVeins directly because it depends on PlanetRawData which isn't available.
                        planet.Load();
                        loadRequests++;
                    }
                }
            }

            if (loadRequests == 0)
            {
                Logger.LogInfo("Planets already loaded.  Proceeding with resource spreadsheet generation.");
                GenerateResourceSpreadsheet();
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendFormat("Requested {0} planets be loaded.  Waiting for planets to load.", loadRequests);
                Logger.LogInfo(sb.ToString());
                SpreadsheetGenMod.spreadsheetGenRequestFlag = true;
            }
        }

        // Called when each planet loads.  When all planets are loaded, will call GenerateResourceSpreadsheet().
        //[HarmonyPrefix, HarmonyPatch(typeof(PlanetData), "NotifyLoaded")]  // This one works for each planet with a delay. This acts more like a reoccuring interval even onces the requested loads are complete.
        //public static void PlanetData_NotifyLoaded_Prefix()
        [HarmonyPostfix, HarmonyPatch(typeof(PlanetAlgorithm), "GenerateVeins")]  // This one works for each planet immediately.
        public static void PlanetAlgorithm_GenerateVeins_Postfix()
        {
            //Logger.LogInfo("Planet loaded.");

            if (SpreadsheetGenMod.spreadsheetGenRequestFlag)
            {
                Logger.LogInfo("Checking if there are still unloaded planets...");

                int planetCount = 0;
                uint unloadedPlanetCount = 0;
                foreach (StarData star in GameMain.universeSimulator.galaxyData.stars)
                {
                    planetCount += star.planets.Length;
                    foreach (PlanetData planet in star.planets)
                    {
                        if ((planet.type != EPlanetType.Gas) && (planet.veinGroups.Length == 0))
                        {
                            unloadedPlanetCount++;
                        }
                    }
                }

                progressImage.fillAmount = ((float)planetCount - unloadedPlanetCount) / planetCount;

                if (unloadedPlanetCount == 0)
                {
                    SpreadsheetGenMod.spreadsheetGenRequestFlag = false;
                    Logger.LogInfo("Planet loading completed.  Proceeding with resource spreadsheet generation.");
                    GenerateResourceSpreadsheet();
                    progressImage.fillAmount = 0;
                }
            }
        }

        // Called when all planets are loaded.  Saves resource spreadsheet.
        public static void GenerateResourceSpreadsheet()
        {
            try
            {
                Logger.LogInfo("Begin resource spreadsheet generation...");

                var sb = new StringBuilder();
                sb.Append("Planet Name,Star Name,Star Dyson Luminosity,Star Type,Star Mass,Star Position X,Star Position Y,Star Position Z,Wind Strength,Luminosity on Planet,Planet Type,Land Percent,Singularity,Planet/Moon,Orbit Inclination,Ocean,");
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
                        //sb.AppendFormat("{0},", star.luminosity);  // Removed to avoid confusion
                        sb.AppendFormat("{0},", star.dysonLumino);
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
                            {
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

                Logger.LogInfo("Completed saving resource spreadsheet.");
            }
            catch (ArgumentNullException e)
            {
                Logger.LogInfo("ERROR: ArgumentNullException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch (ArgumentException e)
            {
                Logger.LogInfo("ERROR: ArgumentException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch (PathTooLongException e)
            {
                Logger.LogInfo("ERROR: PathTooLongException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch (DirectoryNotFoundException e)
            {
                Logger.LogInfo("ERROR: DirectoryNotFoundException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch (IOException e)
            {
                Logger.LogInfo("ERROR: IOException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.LogInfo("ERROR: UnauthorizedAccessException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch (NotSupportedException e)
            {
                Logger.LogInfo("ERROR: NotSupportedException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch (SecurityException e)
            {
                Logger.LogInfo("ERROR: SecurityException while generating and saving resource spreadsheet: " + e.Message);
            }
            catch
            {
                Logger.LogInfo("ERROR: Exception (catch-all) while generating and saving resource spreadsheet.");
            }
        }

        public static RectTransform triggerButton;
        public static Sprite triggerSprite;
        public static Image progressImage;

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), "Begin")]
        public static void GameMain_Begin_Prefix()
        {
            Logger.LogInfo("Begin");
            if (GameMain.instance != null && GameObject.Find("Game Menu/button-1-bg") && !GameObject.Find("greyhak-csv-trigger-button"))
            {
                Logger.LogInfo("Loading button");
                RectTransform parent = GameObject.Find("Game Menu").GetComponent<RectTransform>();
                RectTransform prefab = GameObject.Find("Game Menu/button-1-bg").GetComponent<RectTransform>();
                Vector3 referencePosition = GameObject.Find("Game Menu/button-1-bg").GetComponent<RectTransform>().localPosition;
                triggerButton = GameObject.Instantiate<RectTransform>(prefab);
                triggerButton.gameObject.name = "greyhak-csv-trigger-button";
                triggerButton.GetComponent<UIButton>().tips.tipTitle = "Spreadsheet Generation";
                triggerButton.GetComponent<UIButton>().tips.tipText = "Click to generate resource spreadsheet.";
                triggerButton.GetComponent<UIButton>().tips.delay = 0f;
                triggerButton.transform.Find("button-1/icon").GetComponent<Image>().sprite = GetSprite();
                triggerButton.SetParent(parent);
                triggerButton.localScale = new Vector3(0.35f, 0.35f, 0.35f);
                triggerButton.localPosition = new Vector3(referencePosition.x + 145f, referencePosition.y + 87f, referencePosition.z);
                triggerButton.GetComponent<UIButton>().OnPointerDown(null);
                triggerButton.GetComponent<UIButton>().OnPointerEnter(null);
                triggerButton.GetComponent<UIButton>().button.onClick.AddListener(() =>
                {
                    QueuePlanetLoading();
                });

                Image prefabProgress = GameObject.Find("tech-progress").GetComponent<Image>();
                progressImage = GameObject.Instantiate<Image>(prefabProgress);
                progressImage.gameObject.name = "greyhak-cvs-trigger-image";
                progressImage.fillAmount = 0.0f;
                //progressImage.color = new Color(0.2f, 0.2f, 1);
                progressImage.type = Image.Type.Filled;
                progressImage.rectTransform.SetParent(parent);
                progressImage.rectTransform.localScale = new Vector3(3.0f, 3.0f, 3.0f);
                progressImage.rectTransform.localPosition = new Vector3(referencePosition.x + 145.5f, referencePosition.y + 86.6f, referencePosition.z);

                // Switch from circle-thin to round-50px-border
                Sprite sprite = Resources.Load<Sprite>("UI/Textures/Sprites/round-50px-border");
                progressImage.sprite = GameObject.Instantiate<Sprite>(sprite);
                Logger.LogInfo("Button load complete");
            }
        }

        public static Sprite GetSprite()
        {
            Texture2D tex = new Texture2D(48, 48, TextureFormat.RGBA32, false);
            Color color = new Color(1, 1, 1, 1);

            // Draw a plane like the one re[resending drones in the Mecha Panel...
            for (int x = 0; x < 48; x++)
            {
                for (int y = 0; y < 48; y++)
                {
                    if (((x >= 3) && (x <= 44) && (y >= 3) && (y <= 5)) ||  // top
                        ((x >= 3) && (x <= 44) && (y >= 33) && (y <= 36)) ||
                        ((x >= 3) && (x <= 44) && (y >= 42) && (y <= 44)) ||
                        ((x >= 2) && (x <= 5) && (y >= 3) && (y <= 44)) ||  // left
                        ((x >= 12) && (x <= 14) && (y >= 3) && (y <= 44)) ||
                        ((x >= 27) && (x <= 29) && (y >= 3) && (y <= 44)) ||
                        ((x >= 42) && (x <= 45) && (y >= 3) && (y <= 44)))
                    {
                        tex.SetPixel(x, y, color);
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
            }

            tex.name = "greyhak-cvs-trigger-icon";
            tex.Apply();

            return Sprite.Create(tex, new Rect(0f, 0f, 48f, 48f), new Vector2(0f, 0f), 1000);
        }
    }
}
