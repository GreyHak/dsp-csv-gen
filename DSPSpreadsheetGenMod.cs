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
using System.Globalization;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using BepInEx.Logging;
using System.Security;
using System.Threading;
using System.Security.Permissions;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
namespace StarSectorResourceSpreadsheetGenerator
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    public class SpreadsheetGenMod : BaseUnityPlugin  // Plugin config: "C:\Program Files (x86)\Steam\steamapps\common\Dyson Sphere Program\BepInEx\config\BepInEx.cfg"
    {
        public const string pluginGuid = "greyhak.dysonsphereprogram.resourcespreadsheetgen";
        public const string pluginName = "DSP Star Sector Resource Spreadsheet Generator";
        public const string pluginVersion = "2.0.1.0";
        public static bool enablePlanetLoadingFlag = true;
        public static bool enablePlanetUnloadingFlag = true;
        public static bool enableOnStartTrigger = false;
        public static bool enableOnPauseTrigger = false;
        public static bool spreadsheetGenRequestFlag = false;
        public static Dictionary<int, string> planetResourceData = new Dictionary<int, string>();
        public static bool checkForPlanetsToUnload = false;
        public static string spreadsheetFileNameTemplate = "default.csv";
        public static string spreadsheetColumnSeparator = ",";
        public static CultureInfo spreadsheetLocale = CultureInfo.CurrentUICulture;
        public static int spreadsheetFloatPrecision = -1;
        new internal static ManualLogSource Logger;
        new internal static BepInEx.Configuration.ConfigFile Config;
        public static readonly int[] gases = { 1120, 1121, 1011 };

        public void Awake()
        {
            SpreadsheetGenMod.Logger = base.Logger;  // "C:\Program Files (x86)\Steam\steamapps\common\Dyson Sphere Program\BepInEx\LogOutput.log"
            SpreadsheetGenMod.Config = base.Config;

            // Determine the default spreadsheet path and configured spreadsheet path.
            spreadsheetFileNameTemplate = "DSP_Star_Sector_Resources_${seed}-${starCount}.csv";
            if (Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) != "")
            {
                spreadsheetFileNameTemplate = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar + spreadsheetFileNameTemplate;
            }
            spreadsheetFileNameTemplate = Config.Bind<string>("Output", "SpreadsheetFileName", spreadsheetFileNameTemplate, "Path to the output spreadsheet.  You can use ${seed} and ${starCount} as placeholders and the mod will insert them into the filename.").Value;
            spreadsheetColumnSeparator = Config.Bind<string>("Output", "SpreadsheetColumnSeparator", spreadsheetColumnSeparator, "Character to use as Separator in the generated file.").Value;
            spreadsheetFloatPrecision = Config.Bind<int>("Output", "SpreadsheetFloatPrecision", spreadsheetFloatPrecision, "Decimals to use when exporting floating point numbers. Use -1 to disable rounding.").Value;
            spreadsheetLocale = new CultureInfo(Config.Bind<string>("Output", "SpreadsheetLocale", spreadsheetLocale.Name, "Locale to use for exporting numbers.").Value, false);

            enablePlanetLoadingFlag = Config.Bind<bool>("Enable", "LoadAllPlanets", enablePlanetLoadingFlag, "Planet loading is needed to get all resource data, but you can skip this step for memory efficiency.").Value;
            enablePlanetUnloadingFlag = Config.Bind<bool>("Enable", "UnloadPlanets", enablePlanetUnloadingFlag, "Once planets are loaded to obtain their resource data, unload them to conserve memory.  (This setting is only used if LoadAllPlanets is true.)").Value;
            enableOnStartTrigger = Config.Bind<bool>("Enable", "SaveOnStart", enableOnStartTrigger, "Whether or not spreadsheet generation should be triggered by starting a game.").Value;
            enableOnPauseTrigger = Config.Bind<bool>("Enable", "SaveOnPause", enableOnPauseTrigger, "Whether or not spreadsheet generation should be triggered by pausing the game.").Value;

            Logger.LogInfo("Will use spreadsheet path \"" + spreadsheetFileNameTemplate + "\"");

            Harmony harmony = new Harmony(pluginGuid);
            harmony.PatchAll(typeof(SpreadsheetGenMod));

            Logger.LogInfo("Initialization complete.");
        }

        // Called on save load and game pause.  Queues planet loading which will trigger OnFactoryLoaded().
        public static void QueuePlanetLoading()
        {
            if (DSPGame.IsMenuDemo)
            {
                Logger.LogInfo("Ignoring load screen.");
                return;
            }

            if (spreadsheetGenRequestFlag)
            {
                Logger.LogInfo("Spreadsheet generation already in progress.");
                return;
            }

            Logger.LogInfo("Scanning planets...");

            int planetCount = 0;
            uint loadedPlanetCount = 0;
            uint loadRequests = 0;
            StarData closestStarWithUnloadedPlanets = null;
            float distanceToClosestStarWithUnloadedPlanets = float.MaxValue;
            foreach (StarData star in GameMain.universeSimulator.galaxyData.stars)
            {
                bool hasUnloadedPlanetsFlag = false;
                planetCount += star.planets.Length;
                foreach (PlanetData planet in star.planets)
                {
                    if ((planet.type != EPlanetType.Gas) && (planet.veinGroups.Length == 0))
                    {
                        if (!planetResourceData.ContainsKey(planet.id))
                        {
                            hasUnloadedPlanetsFlag = true;
                            loadRequests++;
                        }
                    }
                    else
                    {
                        planetResourceData[planet.id] = CapturePlanetResourceData(planet);
                        loadedPlanetCount++;
                    }
                }
                if (hasUnloadedPlanetsFlag)
                {
                    float distanceToStar = Vector3.Distance(GameMain.localStar.position, star.position);
                    if (distanceToStar < distanceToClosestStarWithUnloadedPlanets)
                    {
                        distanceToClosestStarWithUnloadedPlanets = distanceToStar;
                        closestStarWithUnloadedPlanets = star;
                    }                    
                }
            }

            if (loadRequests == 0)
            {
                Logger.LogInfo("Planet resource data already available.  Proceeding with resource spreadsheet generation.");
                GenerateResourceSpreadsheet();
            }
            else if (!enablePlanetLoadingFlag)
            {
                Logger.LogInfo("Skipping planet load.  Proceeding with resource spreadsheet generation.  Speadsheet will likely be incomplete.");
                GenerateResourceSpreadsheet();
            }
            else
            {
                Logger.LogInfo(loadRequests.ToString() + " planets to be loaded.  Waiting for planets to load.");
                spreadsheetGenRequestFlag = true;
                checkForPlanetsToUnload = true;
                progressImage.fillAmount = (float)loadedPlanetCount / planetCount;

                Logger.LogInfo("Requesting " + closestStarWithUnloadedPlanets.planets.Length + " planets be loaded around " + closestStarWithUnloadedPlanets.displayName);
                foreach (PlanetData planet in closestStarWithUnloadedPlanets.planets)
                {
                    // PlanetModelingManager.PlanetComputeThreadMain (static, but private) -> PlanetAlgorithm.GenerateVeins
                    // Unable to call GenerateVeins directly because it depends on PlanetRawData which isn't available.
                    planet.Load();
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), "End")]
        public static void GameMain_End_Prefix()
        {
            spreadsheetGenRequestFlag = false;
            planetResourceData.Clear();

            /******************************************************************/
            /* The code below fixes a bug in the original game which this mod */
            /* just makes appear more easily.  The original game doesn't      */
            /* properly handle PlanetModelingManager resetting when a game    */
            /* ends.  Such resetting is only needed when a planet/factory is  */
            /* being loaded, a rare occurance if it weren't for this mod.     */
            /******************************************************************/

            if (PlanetModelingManager.genPlanetReqList.Count == 0 &&
                PlanetModelingManager.fctPlanetReqList.Count == 0 &&
                PlanetModelingManager.modPlanetReqList.Count == 0)
            {
                Logger.LogInfo("Planet modeling reset is not needed.");
                return;
            }

            Logger.LogInfo("Stopping planet modeling thread.");
            PlanetModelingManager.EndPlanetComputeThread();
            Thread.Sleep(100);

            uint sleepIterationCount = 1;
            while (PlanetModelingManager.planetComputeThreadFlag == PlanetModelingManager.ThreadFlag.Ending &&
                ++sleepIterationCount < 100)  // Don't wait more than 10 seconds.  (It shouldn't even enter this loop.)
            {
                Thread.Sleep(100);
            }

            Logger.LogInfo("Clearing planet modeling queues.");
            PlanetModelingManager.genPlanetReqList.Clear();  // RequestLoadStar or RequestLoadPlanet -> PlanetComputeThreadMain
            PlanetModelingManager.fctPlanetReqList.Clear();  // RequestLoadPlanetFactory -> LoadingPlanetFactoryCoroutine
            PlanetModelingManager.modPlanetReqList.Clear();  // ModelingPlanetCoroutine -> ModelingPlanetMain

            if (PlanetModelingManager.currentModelingPlanet != null)
            {
                Logger.LogInfo("Cancelling planet modeling for " + PlanetModelingManager.currentModelingPlanet.name);
                PlanetModelingManager.currentModelingPlanet.Unload();
                PlanetModelingManager.currentModelingPlanet.factoryLoaded = false;
                PlanetModelingManager.currentModelingPlanet = null;
                PlanetModelingManager.currentModelingStage = 0;
                PlanetModelingManager.currentModelingSeamNormal = 0;
            }

            if (PlanetModelingManager.currentFactingPlanet != null)
            {
                Logger.LogInfo("Cancelling planet factory modeling for " + PlanetModelingManager.currentFactingPlanet.name);
                PlanetModelingManager.currentFactingPlanet.UnloadFactory();
                PlanetModelingManager.currentFactingPlanet.factoryLoaded = false;
                PlanetModelingManager.currentFactingPlanet = null;
                PlanetModelingManager.currentFactingStage = 0;
            }

            Logger.LogInfo("Resetting planet modeling manager so we don't get a magenta planet when a new game begins.");
            for (int num57 = 0; num57 < PlanetModelingManager.tmpMeshList.Count; num57++)
            {
                UnityEngine.Object.Destroy(PlanetModelingManager.tmpMeshList[num57]);
            }
            UnityEngine.Object.Destroy(PlanetModelingManager.tmpPlanetGameObject);
            PlanetModelingManager.tmpPlanetGameObject = null;
            PlanetModelingManager.tmpPlanetBodyGameObject = null;
            PlanetModelingManager.tmpPlanetReformGameObject = null;
            PlanetModelingManager.tmpPlanetReformRenderer = null;
            PlanetModelingManager.tmpMeshList.Clear();
            PlanetModelingManager.tmpTris.Clear();
            PlanetModelingManager.tmpVerts.Clear();
            PlanetModelingManager.tmpNorms.Clear();
            PlanetModelingManager.tmpTgnts.Clear();
            PlanetModelingManager.tmpUvs.Clear();
            PlanetModelingManager.tmpUv2s.Clear();
            PlanetModelingManager.currentModelingPlanet = null;
            PlanetModelingManager.currentModelingStage = 0;
            PlanetModelingManager.currentModelingSeamNormal = 0;

            Logger.LogInfo("Restarting planet modeling thread.");
            PlanetModelingManager.StartPlanetComputeThread();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlanetData), "NotifyLoaded")]
        public static void PlanetData_NotifyLoaded_Prefix()
        {
            Logger.LogInfo("Planet loaded.");

            // Resource data is captured whether spreadsheetGenRequestFlag is true or not.
            // This way, if enablePlanetLoadingFlag and enablePlanetUnloadingFlag are both true,
            // and the generation is triggered a second time, reloading won't be needed.
            // This will also improve spreatsheet generation when planet loading is not enabled.
            if (spreadsheetGenRequestFlag || (enablePlanetLoadingFlag && enablePlanetUnloadingFlag) || !enablePlanetLoadingFlag)
            {
                foreach (StarData star in GameMain.universeSimulator.galaxyData.stars)
                {
                    foreach (PlanetData planet in star.planets)
                    {
                        if (!planetResourceData.ContainsKey(planet.id) && (planet.loaded || (planet.veinGroups.Length != 0)))
                        {
                            planetResourceData[planet.id] = CapturePlanetResourceData(planet);
                        }
                    }
                }
            }

            if (spreadsheetGenRequestFlag)
            {
                Logger.LogInfo("Checking if there are still unloaded planets...");

                int planetCount = 0;
                uint loadedPlanetCount = 0;
                StarData closestStarWithUnloadedPlanets = null;
                float distanceToClosestStarWithUnloadedPlanets = float.MaxValue;
                foreach (StarData star in GameMain.universeSimulator.galaxyData.stars)
                {
                    bool hasUnloadedUnqueuedPlanetsFlag = false;
                    planetCount += star.planets.Length;
                    foreach (PlanetData planet in star.planets)
                    {
                        if (planetResourceData.ContainsKey(planet.id))
                        {
                            loadedPlanetCount++;
                        }
                        else if ((planet.type != EPlanetType.Gas) && (planet.veinGroups.Length == 0) && !planet.loading)
                        {
                            hasUnloadedUnqueuedPlanetsFlag = true;
                        }
                        // else should be in the process of being loaded
                    }
                    if (hasUnloadedUnqueuedPlanetsFlag)
                    {
                        float distanceToStar = Vector3.Distance(GameMain.localStar.position, star.position);
                        if (distanceToStar < distanceToClosestStarWithUnloadedPlanets)
                        {
                            distanceToClosestStarWithUnloadedPlanets = distanceToStar;
                            closestStarWithUnloadedPlanets = star;
                        }
                    }
                }

                progressImage.fillAmount = (float)loadedPlanetCount / planetCount;

                int modelingTotal = PlanetModelingManager.genPlanetReqList.Count + PlanetModelingManager.fctPlanetReqList.Count + PlanetModelingManager.modPlanetReqList.Count;
                if (modelingTotal > 10)
                {
                    Logger.LogInfo("Waiting while " + modelingTotal.ToString() + " planets are modeled");
                    return;
                }

                if (closestStarWithUnloadedPlanets != null)
                {
                    Logger.LogInfo("Requesting " + closestStarWithUnloadedPlanets.planets.Length + " planets be loaded around " + closestStarWithUnloadedPlanets.displayName);
                    foreach (PlanetData planet in closestStarWithUnloadedPlanets.planets)
                    {
                        planet.Load();
                    }
                    return;
                }

                if (loadedPlanetCount == planetCount)
                {
                    spreadsheetGenRequestFlag = false;
                    Logger.LogInfo("Planet loading completed.  Proceeding with resource spreadsheet generation.");
                    GenerateResourceSpreadsheet();
                    progressImage.fillAmount = 0;
                }
                else
                {
                    Logger.LogInfo("Waiting for final " + (planetCount - loadedPlanetCount).ToString() + " planet(s) to load");
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
                List<String> columns = new List<String>
                {
                    "Planet Name",
                    "Star Name",
                    "Star Dyson Luminosity",
                    "Star Type",
                    "Star Mass",
                    "Star Position X",
                    "Star Position Y",
                    "Star Position Z",
                    "Wind Strength",
                    "Luminosity on Planet",
                    "Planet Type",
                    "Land Percent",
                    "Singularity",
                    "Planet/Moon",
                    "Orbit Inclination",
                    "Ocean"
                };

                foreach (VeinProto item in LDB.veins.dataArray)
                {
                    columns.Add(item.name);
                }
                foreach (int item in gases)
                {
                    columns.Add(LDB.items.Select(item).name);
                }
                foreach (string entry in columns)
                {
                    sb.Append(entry).Append(spreadsheetColumnSeparator);
                }
                sb.Append(Environment.NewLine);

                foreach (StarData star in GameMain.universeSimulator.galaxyData.stars)
                {
                    foreach (PlanetData planet in star.planets)
                    {
                        if (planetResourceData.ContainsKey(planet.id))
                        {
                            sb.Append(planetResourceData[planet.id]);
                        }
                        else
                        {
                            Logger.LogError("ERROR: Missing resource data for " + planet.displayName);
                            sb.AppendFormat("{0}\n", planet.displayName);
                        }
                    }
                }
                // insert values for possible placeholders in filename
                String spreadsheetFileName = spreadsheetFileNameTemplate;
                spreadsheetFileName = spreadsheetFileName.Replace("${seed}", GameMain.galaxy.seed.ToString("D8"));
                spreadsheetFileName = spreadsheetFileName.Replace("${starCount}", GameMain.galaxy.starCount.ToString());

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

        public static String CapturePlanetResourceData(PlanetData planet)
        {
            StarData star = planet.star;
            string floatFormat = "";
            if (spreadsheetFloatPrecision >= 0)
            {
                floatFormat = "F" + spreadsheetFloatPrecision.ToString();
            }

            var sb = new StringBuilder();
            List<String> line = new List<String>
            {
                planet.displayName,
                star.displayName,
                star.dysonLumino.ToString(floatFormat, spreadsheetLocale),
                star.typeString,
                star.mass.ToString(floatFormat, spreadsheetLocale),
                star.position.x.ToString(floatFormat, spreadsheetLocale),
                star.position.y.ToString(floatFormat, spreadsheetLocale),
                star.position.z.ToString(floatFormat, spreadsheetLocale),
                planet.windStrength.ToString(floatFormat, spreadsheetLocale),
                planet.luminosity.ToString(floatFormat, spreadsheetLocale),
                planet.typeString,
                planet.landPercent.ToString(floatFormat, spreadsheetLocale),
                planet.singularity.ToString(),
                planet.orbitAround.ToString(),
                planet.orbitInclination.ToString(floatFormat, spreadsheetLocale)
            };

            if (planet.type == EPlanetType.Gas)
            {
                line.Add("None,");  // Ocean
                foreach (VeinProto item in LDB.veins.dataArray)
                {
                    line.Add("0");
                }
                foreach (int item in gases)
                {
                    int index = Array.IndexOf(planet.gasItems, item);
                    if (index == -1)
                    {
                        line.Add("0");
                    }
                    else
                    {
                        line.Add(planet.gasSpeeds[index].ToString(floatFormat, spreadsheetLocale));
                    }
                }
                foreach(string entry in line)
                {
                    sb.Append(entry).Append(spreadsheetColumnSeparator);
                }
                sb.Append(Environment.NewLine);
            }
            else
            {
                if (planet.waterItemId == 0)
                {
                    line.Add("None");
                }
                else if (planet.waterItemId == -1)
                {
                    line.Add("Lava");
                }
                else
                {
                    ItemProto waterItem = LDB.items.Select(planet.waterItemId);
                    line.Add(waterItem.name);
                }

                if (planet.veinGroups.Length == 0)
                {
                    foreach (VeinProto item in LDB.veins.dataArray)
                    {
                        line.Add("Unloaded");
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
                            line.Add(((double)amount * VeinData.oilSpeedMultiplier).ToString(floatFormat, spreadsheetLocale));
                        }
                        else
                        {
                            line.Add(amount.ToString(spreadsheetLocale));
                        }
                        type++;
                    }
                }
                foreach (int item in gases)
                {
                    line.Add("0");
                }
                foreach (string entry in line)
                {
                    sb.Append(entry).Append(spreadsheetColumnSeparator);
                }
                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        public static RectTransform triggerButton;
        public static Sprite triggerSprite;
        public static Image progressImage;

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), "Begin")]
        public static void GameMain_Begin_Prefix()
        {
            spreadsheetGenRequestFlag = false;
            planetResourceData.Clear();

            Logger.LogInfo("Game beginning");
            if (GameMain.instance != null && GameObject.Find("Game Menu/button-1-bg"))
            {
                if (GameObject.Find("greyhak-csv-trigger-button"))
                {
                    progressImage.fillAmount = 0;
                }
                else
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

            if (enableOnStartTrigger)
            {
                QueuePlanetLoading();
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), "Pause")]
        public static void GameMain_Pause_Prefix()
        {
            if (enableOnPauseTrigger)
            {
                QueuePlanetLoading();
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

        [HarmonyPrefix, HarmonyPatch(typeof(PlanetModelingManager), "ModelingPlanetMain")]
        public static bool PlanetModelingManager_ModelingPlanetMain_Prefix(PlanetData planet)
        {
            /******************************************************************/
            /* This patch corrects for a bug in the original game.  Hopefully */
            /* the bug fix handled by GameMain_End_Prefix() above will make   */
            /* this patch unnecessary, but it's here just in case.            */
            /******************************************************************/

            if (PlanetModelingManager.currentModelingPlanet != null && PlanetModelingManager.currentModelingStage == 2 && planet.data == null)
            {
                Logger.LogInfo("PlanetRawData null for planet " + planet.name);
                PlanetModelingManager.currentModelingPlanet.Unload();
                PlanetModelingManager.currentModelingPlanet.factoryLoaded = false;
                PlanetModelingManager.currentModelingPlanet = null;
                PlanetModelingManager.currentModelingStage = 0;
                PlanetModelingManager.currentModelingSeamNormal = 0;
                return false;
            }
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameData), "GameTick")]
        public static void GameData_GameTick_Postfix()
        {
            if (checkForPlanetsToUnload && enablePlanetLoadingFlag && enablePlanetUnloadingFlag && GameMain.localStar != null && GameMain.mainPlayer != null)
            {
                foreach (StarData star in GameMain.universeSimulator.galaxyData.stars)
                {
                    foreach (PlanetData planet in star.planets)
                    {
                        if (PlanetModelingManager.currentModelingPlanet != null && planet.id == PlanetModelingManager.currentModelingPlanet.id)
                        {
                            //Logger.LogInfo("Skipping planet " + planet.displayName + " being modelled");
                        }
                        else if (PlanetModelingManager.currentFactingPlanet != null && planet.id == PlanetModelingManager.currentFactingPlanet.id)
                        {
                            //Logger.LogInfo("Skipping planet " + planet.displayName + " whos factory is being modelled");
                        }
                        else if (star.id != GameMain.localStar.id)
                        {
                            if (planet.loaded)
                            {
                                Logger.LogInfo("Unloading planet " + planet.displayName);
                                planet.Unload();
                            }
                        }
                        else if (planet.id != GameMain.mainPlayer.planetId)
                        {
                            if (planet.factoryLoaded)
                            {
                                Logger.LogInfo("Unloading planet factory " + planet.displayName);
                                planet.UnloadFactory();
                            }
                        }
                    }
                }

                if (!spreadsheetGenRequestFlag &&
                    PlanetModelingManager.genPlanetReqList.Count == 0 &&
                    PlanetModelingManager.fctPlanetReqList.Count == 0 &&
                    PlanetModelingManager.modPlanetReqList.Count == 0)
                {
                    checkForPlanetsToUnload = false;
                }
            }
        }
    }
}
