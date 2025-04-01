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
using System.Text;
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
using System.ComponentModel;
using System.Reflection.Emit;
using System.Reflection;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
namespace StarSectorResourceSpreadsheetGenerator
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInDependency("dsp.galactic-scale.2", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess("DSPGAME.exe")]
    [BepInProcess("Dyson Sphere Program.exe")]
    public class SpreadsheetGenMod : BaseUnityPlugin  // Plugin config: "C:\Program Files (x86)\Steam\steamapps\common\Dyson Sphere Program\BepInEx\config\BepInEx.cfg"
    {
        public const string pluginGuid = "greyhak.dysonsphereprogram.resourcespreadsheetgen";
        public const string pluginName = "DSP Star Sector Resource Spreadsheet Generator";
        public const string pluginVersion = "4.0.1";

        public static bool spreadsheetGenRequestFlag = false;
        public static readonly List<PlanetData> planetsToLoad = new List<PlanetData> { };
        public static readonly Dictionary<int, string> planetResourceData = new Dictionary<int, string>();
        public static bool checkForPlanetsToUnload = false;
        public static CultureInfo spreadsheetLocale = CultureInfo.CurrentUICulture;
        new internal static ManualLogSource Logger;
        new internal static BepInEx.Configuration.ConfigFile Config;
        public static readonly int[] gases = { 1120, 1121, 1011 };
        public static int planetCount = 0;
        public static long[] veinAmounts = new long[64];
        public static StringBuilder stringBuilder;

        public static BepInEx.Configuration.ConfigEntry<bool> enablePlanetLoadingFlag;
        public static BepInEx.Configuration.ConfigEntry<string> spreadsheetFileNameTemplate;
        public static BepInEx.Configuration.ConfigEntry<string> spreadsheetColumnSeparator;
        public static BepInEx.Configuration.ConfigEntry<int> spreadsheetFloatPrecision;
        public Harmony harmony;

        public enum DistanceFromTypeEnum
        {
            [Description("Disable post-generation distance calculations")]
            Disabled,
            [Description("EXCEL for post-generation distance calculations")]
            Excel,
            [Description("OPEN CALC for post-generation distance calculations")]
            OpenCalc,
        };
        public static BepInEx.Configuration.ConfigEntry<DistanceFromTypeEnum> includeDistanceFromCalculations;

        public class ConfigExtraFlags
        {
            public static BepInEx.Configuration.ConfigEntry<bool> starAge;
            public static BepInEx.Configuration.ConfigEntry<bool> starColor;
            public static BepInEx.Configuration.ConfigEntry<bool> starLifetime;
            public static BepInEx.Configuration.ConfigEntry<bool> starMass;
            public static BepInEx.Configuration.ConfigEntry<bool> starRadius;
            public static BepInEx.Configuration.ConfigEntry<bool> starMaxSphereRadius;
            public static BepInEx.Configuration.ConfigEntry<bool> starTemperature;
            public static BepInEx.Configuration.ConfigEntry<bool> distanceFromStarClusterCenter;
            public static BepInEx.Configuration.ConfigEntry<bool> planetOrbitalPeriod;
            public static BepInEx.Configuration.ConfigEntry<bool> planetOrbitAround;
            public static BepInEx.Configuration.ConfigEntry<bool> planetOrbitAroundPlanet;
            public static BepInEx.Configuration.ConfigEntry<bool> planetOrbitInclination;
            public static BepInEx.Configuration.ConfigEntry<bool> planetOrbitLongitude;
            public static BepInEx.Configuration.ConfigEntry<bool> planetOrbitPhase;
            public static BepInEx.Configuration.ConfigEntry<bool> planetOrbitRadius;
            public static BepInEx.Configuration.ConfigEntry<bool> planetRealRadius;
            public static BepInEx.Configuration.ConfigEntry<bool> planetRotationPeriod;
            public static BepInEx.Configuration.ConfigEntry<bool> planetRotationPhase;
            public static BepInEx.Configuration.ConfigEntry<bool> planetSunDistance;
            public static BepInEx.Configuration.ConfigEntry<bool> veinCounts;
        }

        private static readonly object planetComputeThreadMutexLock = new object();

        public void Awake()
        {
            SpreadsheetGenMod.Logger = base.Logger;  // "C:\Program Files (x86)\Steam\steamapps\common\Dyson Sphere Program\BepInEx\LogOutput.log"
            SpreadsheetGenMod.Config = base.Config;

            // Determine the default spreadsheet path and configured spreadsheet path.
            string spreadsheetFileNameDefault = "DSP_Star_Sector_Resources_${seed}-${starCount}.csv";
            if (Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) != "")
            {
                spreadsheetFileNameDefault = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar + spreadsheetFileNameDefault;
            }
            spreadsheetFileNameTemplate = Config.Bind<string>("Output", "SpreadsheetFileName", spreadsheetFileNameDefault, "Path to the output spreadsheet.  You can use ${seed} and ${starCount} as placeholders and the mod will insert them into the filename.");
            spreadsheetColumnSeparator = Config.Bind<string>("Output", "SpreadsheetColumnSeparator", ",", "Character to use as Separator in the generated file.");
            spreadsheetFloatPrecision = Config.Bind<int>("Output", "SpreadsheetFloatPrecision", -1, "Decimals to use when exporting floating point numbers. Use -1 to disable rounding.");
            spreadsheetLocale = new CultureInfo(Config.Bind<string>("Output", "SpreadsheetLocale", spreadsheetLocale.Name, "Locale to use for exporting numbers.").Value, false);
            includeDistanceFromCalculations = Config.Bind<DistanceFromTypeEnum>("Output", "DistanceFrom", DistanceFromTypeEnum.Disabled, "This feature provides the ability to choose any planet, and calculate the distance from that planet to all other planets. This feature supports Microsoft Excel and OpenOffice Calc.");

            enablePlanetLoadingFlag = Config.Bind<bool>("Enable", "LoadAllPlanets", true, "Planet loading is needed to get all resource data, but you can skip this step if you want results fast.");

            ConfigExtraFlags.starAge = Config.Bind<bool>("ExtraData", "StarAge", false, "Add stars' age to the spreadsheet");
            ConfigExtraFlags.starColor = Config.Bind<bool>("ExtraData", "StarColor", false, "Add stars' color to the spreadsheet");
            ConfigExtraFlags.starLifetime = Config.Bind<bool>("ExtraData", "StarLifetime", false, "Add stars' lifetime to the spreadsheet");
            ConfigExtraFlags.starMass = Config.Bind<bool>("ExtraData", "StarMass", false, "Add stars' mass to the spreadsheet");
            ConfigExtraFlags.starRadius = Config.Bind<bool>("ExtraData", "StarRadius", false, "Add stars' radius to the spreadsheet");
            ConfigExtraFlags.starMaxSphereRadius = Config.Bind<bool>("ExtraData", "StarMaxSphereRadius", false, "The star's maximum dyson shell layer 'orbit radius'.  This is calculated by round(StarData.dysonRadius * 800) * 100.");
            ConfigExtraFlags.starTemperature = Config.Bind<bool>("ExtraData", "StarTemperature", false, "Add stars' temperature to the spreadsheet");
            ConfigExtraFlags.distanceFromStarClusterCenter = Config.Bind<bool>("ExtraData", "DistanceFromStarClusterCenter", false, "Add star's distance from the center of the star cluster.  This is typically the location of the initial planet's star.");
            ConfigExtraFlags.planetOrbitalPeriod = Config.Bind<bool>("ExtraData", "PlanetOrbitalPeriod", false, "Add planets' orbital period to the spreadsheet");
            ConfigExtraFlags.planetOrbitAround = Config.Bind<bool>("ExtraData", "PlanetOrbitAround", false, "Add type of orbital relationship to the spreadsheet");
            ConfigExtraFlags.planetOrbitAroundPlanet = Config.Bind<bool>("ExtraData", "PlanetOrbitAroundPlanet", false, "Add name of planet the planets orbit, if they orbit a planet, to the spreadsheet");
            ConfigExtraFlags.planetOrbitInclination = Config.Bind<bool>("ExtraData", "PlanetOrbitInclination", false, "Add planets' orbit inclination to the spreadsheet");
            ConfigExtraFlags.planetOrbitLongitude = Config.Bind<bool>("ExtraData", "PlanetOrbitLongitude", false, "Add planets' orbit longitude to the spreadsheet");
            ConfigExtraFlags.planetOrbitPhase = Config.Bind<bool>("ExtraData", "PlanetOrbitPhase", false, "Add planets' orbit phase to the spreadsheet");
            ConfigExtraFlags.planetOrbitRadius = Config.Bind<bool>("ExtraData", "PlanetOrbitRadius", false, "Add planets' orbit radius to the spreadsheet");
            ConfigExtraFlags.planetRealRadius = Config.Bind<bool>("ExtraData", "PlanetRealRadius", false, "Add planets' real radius to the spreadsheet");
            ConfigExtraFlags.planetRotationPeriod = Config.Bind<bool>("ExtraData", "PlanetRotationPeriod", false, "Add planets' rotation period to the spreadsheet");
            ConfigExtraFlags.planetRotationPhase = Config.Bind<bool>("ExtraData", "PlanetRotationPhase", false, "Add planets' rotation phase to the spreadsheet");
            ConfigExtraFlags.planetSunDistance = Config.Bind<bool>("ExtraData", "PlanetSunDistance", false, "Add planets' distance from their star to the spreadsheet");
            ConfigExtraFlags.veinCounts = Config.Bind<bool>("ExtraData", "VeinCounts", false, "Add the number of veins for each vein resource in addition to the total number of items.");

            Config.SettingChanged += OnConfigSettingChanged;

            Logger.LogInfo("Will use spreadsheet path \"" + spreadsheetFileNameTemplate.Value + "\"");

            harmony = new Harmony(pluginGuid);
            harmony.PatchAll(typeof(SpreadsheetGenMod));

            // GS2 compatibility patch 
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("dsp.galactic-scale.2", out var pluginInfo))
            {
                try
                {
                    // GS2 overwrite PlanetCalculateThreadMain in GalacticScale.Modeler
                    // So this patch add checking after calcPlanet.NotifyCalculated
                    var transplier = new HarmonyMethod(typeof(SpreadsheetGenMod).GetMethod("PlanetCalculateThreadMain_Transpiler"));
                    Type classType = pluginInfo.Instance.GetType().Assembly.GetType("GalacticScale.Modeler");
                    harmony.Patch(AccessTools.Method(classType, "Calculate"), null, null, transplier);
                    Logger.LogInfo("GalacticScale compatibility patch success!");
                }
                catch (Exception e)
                {
                    Logger.LogError("Fail to patch GalacticScale.Modeler.Calculate");
                    Logger.LogError(e);
                }
            }

            Logger.LogInfo("Initialization complete.");
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf(); //Harmony.UnpatchID(pluginGuid) works for BepInEx 5.4.18 and above versions
        }

        public static void OnConfigSettingChanged(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
        {
            BepInEx.Configuration.ConfigDefinition changedSetting = e.ChangedSetting.Definition;
            if (changedSetting.Section == "Output" && changedSetting.Key == "SpreadsheetFileName") { }
            else if (changedSetting.Section == "Enable" && changedSetting.Key == "LoadAllPlanets") { }
            else  // All other config changes result in wiping the available data...
            {
                Monitor.Enter(planetComputeThreadMutexLock);
                spreadsheetGenRequestFlag = false;
                planetsToLoad.Clear();
                planetResourceData.Clear();
                if (progressImage != null)
                {
                    progressImage.fillAmount = 0;
                }
                Monitor.Exit(planetComputeThreadMutexLock);

                if (changedSetting.Section == "Output" && changedSetting.Key == "SpreadsheetLocale")
                {
                    spreadsheetLocale = new CultureInfo(Config.Bind<string>("Output", "SpreadsheetLocale", spreadsheetLocale.Name, "Locale to use for exporting numbers.").Value, false);
                }
            }
        }

        // Called to start spreadsheet generation.
        public static void QueuePlanetLoading()
        {
            if (DSPGame.IsMenuDemo)
            {
                Logger.LogInfo("Ignoring load screen.");
                return;
            }

            Monitor.Enter(planetComputeThreadMutexLock);
            bool localSpreadsheetGenRequestFlag = spreadsheetGenRequestFlag;
            Monitor.Exit(planetComputeThreadMutexLock);
            {
                if (localSpreadsheetGenRequestFlag)
                {
                    Logger.LogInfo("Spreadsheet generation already in progress.");
                    return;
                }
            }

            Logger.LogInfo("Scanning planets...");
            planetsToLoad.Clear();

            StringBuilder sb = new StringBuilder(8192);
            foreach (StarData star in GameMain.universeSimulator.galaxyData.stars)
            {
                foreach (PlanetData planet in star.planets)
                {
                    if (!planetResourceData.ContainsKey(planet.id))
                    {
                        if (planet.scanned)
                        {
                            CapturePlanetResourceData(planet, sb);
                        }
                        else if (enablePlanetLoadingFlag.Value)
                        {
                            planetsToLoad.Add(planet);
                            // Code for testing simultanious loading.
                            //System.Random random = new System.Random(42);  // Move this before the loop when using to test
                            //if (random.NextDouble() < 0.1)  // 10%
                            //    planet.Load();
                        }
                    }
                }
            }

            if (planetsToLoad.Count == 0)
            {
                Logger.LogInfo("Planet resource data already available.  Proceeding with resource spreadsheet generation.");
                GenerateResourceSpreadsheet();
            }
            else if (!enablePlanetLoadingFlag.Value)
            {
                Logger.LogInfo("Skipping planet load.  Proceeding with resource spreadsheet generation.  Speadsheet will be incomplete.");
                GenerateResourceSpreadsheet();
            }
            else
            {
                Logger.LogInfo(planetsToLoad.Count.ToString() + " planets to be loaded.  Waiting for planets to load.");

                Monitor.Enter(planetComputeThreadMutexLock);
                spreadsheetGenRequestFlag = true;
                foreach (PlanetData planet in planetsToLoad)
                {
                    planet.RunScanThread();
                }
                Monitor.Exit(planetComputeThreadMutexLock);
            }
        }

        // This function duplicates a part of PlanetModelingManager.PlanetComputeThreadMain
        public static void QuickPlanetLoad(PlanetData planetData)
        {
            PlanetAlgorithm planetAlgorithm = PlanetModelingManager.Algorithm(planetData);
            if (planetAlgorithm != null)
            {
                if (planetData.data == null)
                {
                    planetData.data = new PlanetRawData(planetData.precision);
                    planetData.modData = planetData.data.InitModData(planetData.modData);
                    planetData.data.CalcVerts();
                    planetData.aux = new PlanetAuxData(planetData);
                    planetAlgorithm.GenerateTerrain(planetData.mod_x, planetData.mod_y);
                    planetAlgorithm.CalcWaterPercent();
                }
                if (planetData.factory == null)
                {
                    //planetAlgorithm.GenerateVegetables();
                    planetAlgorithm.GenerateVeins();
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), "End")]
        public static void GameMain_End_Prefix()
        {
            Monitor.Enter(planetComputeThreadMutexLock);
            spreadsheetGenRequestFlag = false;
            planetsToLoad.Clear();
            planetResourceData.Clear();
            Monitor.Exit(planetComputeThreadMutexLock);

            /******************************************************************/
            /* The code below fixes a bug in the original game which this mod */
            /* just makes appear more easily.  The original game doesn't      */
            /* properly handle PlanetModelingManager resetting when a game    */
            /* ends.  Such resetting is only needed when a planet/factory is  */
            /* being loaded, a rare occurance if it weren't for this mod.     */
            /******************************************************************/

            if (PlanetModelingManager.genPlanetReqList.Count == 0 &&
                PlanetModelingManager.fctPlanetReqList.Count == 0 &&
                PlanetModelingManager.modPlanetReqList.Count == 0 &&
                PlanetModelingManager.scnPlanetReqList.Count == 0)
            {
                Logger.LogInfo("Planet modeling reset is not needed.");
                return;
            }

            Logger.LogInfo("Stopping planet modeling thread.");
            PlanetModelingManager.EndPlanetComputeThread();
            PlanetModelingManager.EndPlanetScanThread();
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
            PlanetModelingManager.scnPlanetReqList.Clear();  // RequestCalcStar or RequestCalcPlanet -> PlanetComputeThreadMain

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
            PlanetModelingManager.StartPlanetScanThread();
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(PlanetModelingManager), nameof(PlanetModelingManager.PlanetScanThreadMain))]
        public static IEnumerable<CodeInstruction> PlanetScanThreadMain_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Use transpiler due to PlanetData.NotifyCalculated is inlined
            try
            {
                CodeMatcher matcher = new CodeMatcher(instructions)
                    .MatchForward(true, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == nameof(PlanetData.NotifyScanEnded)))
                    .Advance(1);

                matcher.InsertAndAdvance(
                    matcher.InstructionAt(-2),
                    HarmonyLib.Transpilers.EmitDelegate<Action<PlanetData>>((planet) =>
                    {
                        Monitor.Enter(planetComputeThreadMutexLock);
                        if (spreadsheetGenRequestFlag)
                        {
                            if (planetsToLoad.Contains(planet))
                            {
                                if (stringBuilder == null)
                                {
                                    stringBuilder = new StringBuilder(8192);
                                }
                                CapturePlanetResourceData(planet, stringBuilder);
                                if (planetResourceData.Count == planetCount)
                                {
                                    GenerateResourceSpreadsheet();
                                    stringBuilder = null;
                                }

                                // Free resource to reduce RAM usage
                                if (planet.data != null)
                                {
                                    planet.data.Free();
                                    planet.data = null;
                                }
                                planet.modData = null;
                                planet.aux = null;
                                planet.scanned = false;
                            }
                        }
                        Monitor.Exit(planetComputeThreadMutexLock);
                    }));

                return matcher.InstructionEnumeration();
            }
            catch
            {
                Logger.LogError("Transpiler PlanetCalculateThreadMain failed. Mod version not compatible with game version.");
                return instructions;
            }
        }

        // Called when all planets are loaded.  Saves resource spreadsheet.
        public static void GenerateResourceSpreadsheet()
        {
            try
            {
                Logger.LogInfo("Begin resource spreadsheet generation...");

                var sb = new StringBuilder(8192);
                //"Distance to target" fields and computations
                int currLineNum = 1;
                if (includeDistanceFromCalculations.Value != DistanceFromTypeEnum.Disabled)
                {
                    sb.Append("Distance Target").Append(spreadsheetColumnSeparator.Value); // Col A
                    sb.Append(spreadsheetColumnSeparator.Value);// Col B
                    sb.Append(spreadsheetColumnSeparator.Value);// Col C
                    sb.Append(spreadsheetColumnSeparator.Value);// Col D
                    sb.Append(spreadsheetColumnSeparator.Value);// Col E
                    if (includeDistanceFromCalculations.Value == DistanceFromTypeEnum.OpenCalc)
                    {
                        escapeAddValue(sb, "=VLOOKUP($A$2;$A$4:$H$___HERE_TOTAL_LINE_NUMBER___;6;FALSE)");// Col F ==> Current target X
                        escapeAddValue(sb, "=VLOOKUP($A$2;$A$4:$H$___HERE_TOTAL_LINE_NUMBER___;7;FALSE)");// Col G ==> Current target Y
                        escapeAddValue(sb, "=VLOOKUP($A$2;$A$4:$H$___HERE_TOTAL_LINE_NUMBER___;8;FALSE)");// Col H ==> Current target Z
                    }
                    else if (includeDistanceFromCalculations.Value == DistanceFromTypeEnum.Excel)
                    {
                        escapeAddValue(sb, "=VLOOKUP($A$2,$A$4:$H$___HERE_TOTAL_LINE_NUMBER___,6,FALSE)");// Col F ==> Current target X
                        escapeAddValue(sb, "=VLOOKUP($A$2,$A$4:$H$___HERE_TOTAL_LINE_NUMBER___,7,FALSE)");// Col G ==> Current target Y
                        escapeAddValue(sb, "=VLOOKUP($A$2,$A$4:$H$___HERE_TOTAL_LINE_NUMBER___,8,FALSE)");// Col H ==> Current target Z
                    }
                    sb.Append(Environment.NewLine);
                    currLineNum++;
                    sb.Append("!!! Replace this cell with any of the planet names from cells A4 to A___HERE_TOTAL_LINE_NUMBER___").Append(Environment.NewLine);
                    currLineNum++;
                }
                // column headers
                sb.Append("Planet Name").Append(spreadsheetColumnSeparator.Value);
                sb.Append("Star Name").Append(spreadsheetColumnSeparator.Value);
                sb.Append("Star Dyson Luminosity").Append(spreadsheetColumnSeparator.Value);
                sb.Append("Star Type").Append(spreadsheetColumnSeparator.Value);
                if (includeDistanceFromCalculations.Value != DistanceFromTypeEnum.Disabled)
                    sb.Append("Distance to target").Append(spreadsheetColumnSeparator.Value);
                sb.Append("Star Position X").Append(spreadsheetColumnSeparator.Value);
                sb.Append("Star Position Y").Append(spreadsheetColumnSeparator.Value);
                sb.Append("Star Position Z").Append(spreadsheetColumnSeparator.Value);

                if (ConfigExtraFlags.starAge.Value) { sb.Append("Star Age").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.starColor.Value) { sb.Append("Star Color").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.starLifetime.Value) { sb.Append("Star Lifetime").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.starMass.Value) { sb.Append("Star Mass").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.starRadius.Value) { sb.Append("Star Radius").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.starMaxSphereRadius.Value) { sb.Append("Max Sphere Radius").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.starTemperature.Value) { sb.Append("Star Temperature").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.distanceFromStarClusterCenter.Value) { sb.Append("Distance from Cluster Center").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.planetOrbitalPeriod.Value) { sb.Append("Orbital Period").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.planetOrbitAround.Value) { sb.Append("Planet/Moon").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.planetOrbitAroundPlanet.Value) { sb.Append("Orbiting").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.planetOrbitInclination.Value) { sb.Append("Orbit Inclination").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.planetOrbitLongitude.Value) { sb.Append("Orbit Longitude").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.planetOrbitPhase.Value) { sb.Append("Orbit Phase").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.planetOrbitRadius.Value) { sb.Append("Orbit Radius").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.planetRealRadius.Value) { sb.Append("Real Radius").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.planetRotationPeriod.Value) { sb.Append("Rotation Period").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.planetRotationPhase.Value) { sb.Append("Rotation Phase").Append(spreadsheetColumnSeparator.Value); }
                if (ConfigExtraFlags.planetSunDistance.Value) { sb.Append("Distance from Star").Append(spreadsheetColumnSeparator.Value); }

                sb.Append("Wind Strength").Append(spreadsheetColumnSeparator.Value);
                sb.Append("Luminosity on Planet").Append(spreadsheetColumnSeparator.Value);
                sb.Append("Planet Type").Append(spreadsheetColumnSeparator.Value);
                sb.Append("Land Percent").Append(spreadsheetColumnSeparator.Value);
                sb.Append("Singularity").Append(spreadsheetColumnSeparator.Value);
                sb.Append("Ocean").Append(spreadsheetColumnSeparator.Value);

                EVeinType type = (EVeinType)1;
                foreach (VeinProto item in LDB.veins.dataArray)
                {
                    if (type == EVeinType.Oil || !ConfigExtraFlags.veinCounts.Value)
                    {
                        sb.Append(item.name).Append(spreadsheetColumnSeparator.Value);
                    }
                    else
                    {
                        sb.Append(item.name + " (items)").Append(spreadsheetColumnSeparator.Value);
                        sb.Append(item.name + " (veins)").Append(spreadsheetColumnSeparator.Value);
                    }
                    type++;
                }
                foreach (int item in gases)
                {
                    sb.Append(LDB.items.Select(item).name).Append(spreadsheetColumnSeparator.Value);
                }
                sb.Append(Environment.NewLine);
                currLineNum++;

                foreach (StarData star in GameMain.universeSimulator.galaxyData.stars)
                {
                    foreach (PlanetData planet in star.planets)
                    {
                        if (planetResourceData.ContainsKey(planet.id))
                        {
                            sb.Append(planetResourceData[planet.id].Replace("___HERE_CURRENT_LINE_NUMBER___", currLineNum.ToString()));
                            currLineNum++;
                        }
                        else
                        {
                            Logger.LogError("ERROR: Missing resource data for " + planet.displayName);
                            sb.AppendFormat("{0}\n", planet.displayName);
                            currLineNum++;
                        }
                    }
                }
                // insert values for possible placeholders in filename
                String spreadsheetFileName = spreadsheetFileNameTemplate.Value;
                spreadsheetFileName = spreadsheetFileName.Replace("${seed}", GameMain.galaxy.seed.ToString("D8"));  // D8 will prefix the integer with zeros to make it always 8 characters long.
                spreadsheetFileName = spreadsheetFileName.Replace("${starCount}", GameMain.galaxy.starCount.ToString());

                // Make sure the folder we're trying to write in exists.
                // {username}/Documents doesn't always exist on Wine platform.
                System.IO.FileInfo file = new System.IO.FileInfo(spreadsheetFileName);
                file.Directory.Create(); // If the directory already exists, this method does nothing.

                string content = sb.ToString();
                content = content.Replace("___HERE_TOTAL_LINE_NUMBER___", (currLineNum - 1).ToString());
                File.WriteAllText(spreadsheetFileName, content);

                Logger.LogInfo("Completed saving resource spreadsheet.");
            }
            catch (ArgumentNullException e)
            {
                Logger.LogInfo("ERROR: ArgumentNullException while generating and saving resource spreadsheet: " + e.Message + ";" + e.ToString());
            }
            catch (ArgumentException e)
            {
                Logger.LogInfo("ERROR: ArgumentException while generating and saving resource spreadsheet: " + e.Message + ";" + e.ToString());
            }
            catch (PathTooLongException e)
            {
                Logger.LogInfo("ERROR: PathTooLongException while generating and saving resource spreadsheet: " + e.Message + ";" + e.ToString());
            }
            catch (DirectoryNotFoundException e)
            {
                Logger.LogInfo("ERROR: DirectoryNotFoundException while generating and saving resource spreadsheet: " + e.Message + ";" + e.ToString());
            }
            catch (IOException e)
            {
                Logger.LogInfo("ERROR: IOException while generating and saving resource spreadsheet: " + e.Message + ";" + e.ToString());
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.LogInfo("ERROR: UnauthorizedAccessException while generating and saving resource spreadsheet: " + e.Message + ";" + e.ToString());
            }
            catch (NotSupportedException e)
            {
                Logger.LogInfo("ERROR: NotSupportedException while generating and saving resource spreadsheet: " + e.Message + ";" + e.ToString());
            }
            catch (SecurityException e)
            {
                Logger.LogInfo("ERROR: SecurityException while generating and saving resource spreadsheet: " + e.Message+";" + e.ToString());
            }
            catch(Exception e)
            {
                Logger.LogInfo("ERROR: Exception (catch-all) while generating and saving resource spreadsheet." + e.Message + ";" + e.ToString());
            }

            progressImage.fillAmount = 0;
            spreadsheetGenRequestFlag = false;
        }

        public static void CapturePlanetResourceData(PlanetData planet, StringBuilder sb)
        {
            StarData star = planet.star;
            string floatFormat = "";
            if (spreadsheetFloatPrecision.Value >= 0)
            {
                floatFormat = "F" + spreadsheetFloatPrecision.ToString();
            }

            sb.Length = 0;
            escapeAddValue(sb, planet.displayName);
            escapeAddValue(sb, star.displayName);
            escapeAddValue(sb, star.dysonLumino.ToString(floatFormat, spreadsheetLocale));
            escapeAddValue(sb, star.typeString);
            if (includeDistanceFromCalculations.Value == DistanceFromTypeEnum.OpenCalc)
                escapeAddValue(sb, "=SQRT(SUMXMY2($F$1:$H$1;F___HERE_CURRENT_LINE_NUMBER___:H___HERE_CURRENT_LINE_NUMBER___))");
            else if (includeDistanceFromCalculations.Value == DistanceFromTypeEnum.Excel)
                escapeAddValue(sb, "=SQRT(SUMXMY2($F$1:$H$1,F___HERE_CURRENT_LINE_NUMBER___:H___HERE_CURRENT_LINE_NUMBER___))");
            escapeAddValue(sb, star.position.x.ToString(floatFormat, spreadsheetLocale));
            escapeAddValue(sb, star.position.y.ToString(floatFormat, spreadsheetLocale));
            escapeAddValue(sb, star.position.z.ToString(floatFormat, spreadsheetLocale));

            if (ConfigExtraFlags.starAge.Value) { escapeAddValue(sb, star.age.ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.starColor.Value) { escapeAddValue(sb, star.color.ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.starLifetime.Value) { escapeAddValue(sb, star.lifetime.ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.starMass.Value) { escapeAddValue(sb, star.mass.ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.starRadius.Value) { escapeAddValue(sb, star.radius.ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.starMaxSphereRadius.Value) { escapeAddValue(sb, (Math.Round(star.dysonRadius * 800) * 100).ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.starTemperature.Value) { escapeAddValue(sb, star.temperature.ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.distanceFromStarClusterCenter.Value) { escapeAddValue(sb, Vector3.Distance(star.position, new Vector3(0, 0, 0)).ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.planetOrbitalPeriod.Value) { escapeAddValue(sb, planet.orbitalPeriod.ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.planetOrbitAround.Value) { escapeAddValue(sb, planet.orbitAround.ToString()); }
            if (ConfigExtraFlags.planetOrbitAroundPlanet.Value)
            {
                if (planet.orbitAroundPlanet == null)
                {
                    sb.Append(spreadsheetColumnSeparator.Value);
                }
                else
                {
                    escapeAddValue(sb, planet.orbitAroundPlanet.displayName);
                }
            }
            if (ConfigExtraFlags.planetOrbitInclination.Value) { escapeAddValue(sb, planet.orbitInclination.ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.planetOrbitLongitude.Value) { escapeAddValue(sb, planet.orbitLongitude.ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.planetOrbitPhase.Value) { escapeAddValue(sb, planet.orbitPhase.ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.planetOrbitRadius.Value) { escapeAddValue(sb, planet.orbitRadius.ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.planetRealRadius.Value) { escapeAddValue(sb, planet.realRadius.ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.planetRotationPeriod.Value) { escapeAddValue(sb, planet.rotationPeriod.ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.planetRotationPhase.Value) { escapeAddValue(sb, planet.rotationPhase.ToString(floatFormat, spreadsheetLocale)); }
            if (ConfigExtraFlags.planetSunDistance.Value) { escapeAddValue(sb, planet.sunDistance.ToString(floatFormat, spreadsheetLocale)); }

            escapeAddValue(sb, planet.windStrength.ToString(floatFormat, spreadsheetLocale));
            escapeAddValue(sb, planet.luminosity.ToString(floatFormat, spreadsheetLocale));
            escapeAddValue(sb, planet.typeString);
            escapeAddValue(sb, planet.landPercent.ToString(floatFormat, spreadsheetLocale));
            escapeAddValue(sb, planet.singularity.ToString());  

            if (planet.type == EPlanetType.Gas)
            {
                escapeAddValue(sb, "None");  // Ocean
                EVeinType type = (EVeinType)1;
                foreach (VeinProto item in LDB.veins.dataArray)
                {
                    escapeAddValue(sb, "0");
                    if (type != EVeinType.Oil && ConfigExtraFlags.veinCounts.Value)
                    {
                        escapeAddValue(sb, "0");
                    }
                    type++;
                }
                foreach (int item in gases)
                {
                    int index = Array.IndexOf(planet.gasItems, item);
                    if (index == -1)
                    {
                        escapeAddValue(sb, "0");
                    }
                    else
                    {
                        escapeAddValue(sb, planet.gasSpeeds[index].ToString(floatFormat, spreadsheetLocale));
                    }
                }
            }
            else
            {
                if (planet.waterItemId == 0)
                {
                    escapeAddValue(sb, "None");
                }
                else if (planet.waterItemId == -1)
                {
                    escapeAddValue(sb, "Lava");
                }
                else if (planet.waterItemId == -2)
                {
                    escapeAddValue(sb, "Ice");
                }
                else
                {
                    try
                    {
                        ItemProto waterItem = LDB.items.Select(planet.waterItemId);  // If this fails, it throws an exception which will hang the spreadsheet generation if not caught.
                        escapeAddValue(sb, waterItem.name);
                    }
                    catch
                    {
                        escapeAddValue(sb, $"UNKNOWN ocean type {planet.waterItemId}.  Please write a ticket at https://github.com/GreyHak/dsp-csv-gen/issues.  Thank you.");
                    }
                }

                if (planet.runtimeVeinGroups == null)
                {
                    EVeinType type = (EVeinType)1;
                    foreach (VeinProto item in LDB.veins.dataArray)
                    {
                        escapeAddValue(sb, "Unloaded");
                        if (type != EVeinType.Oil && ConfigExtraFlags.veinCounts.Value)
                        {
                            escapeAddValue(sb, "Unloaded");
                        }
                        type++;
                    }
                }
                else
                {
                    HashSet<int> tmp_ids = new HashSet<int>();
                    planet.SummarizeVeinAmountsByFilter(ref veinAmounts, tmp_ids, 0);

                    EVeinType type = (EVeinType)1;
                    foreach (VeinProto item in LDB.veins.dataArray)
                    {
                        long amount = veinAmounts[(int)type];
                        if (type == EVeinType.Oil)
                        {
                            escapeAddValue(sb, ((double)amount * VeinData.oilSpeedMultiplier).ToString(floatFormat, spreadsheetLocale));
                        }
                        else
                        {
                            escapeAddValue(sb, amount.ToString(spreadsheetLocale));

                            if (ConfigExtraFlags.veinCounts.Value)
                            {
                                long numVeins = 0;
                                foreach (VeinGroup veinGroup in planet.runtimeVeinGroups)
                                {
                                    if (veinGroup.type == type)
                                    {
                                        numVeins += veinGroup.count;
                                    }
                                }

                                escapeAddValue(sb, numVeins.ToString(spreadsheetLocale));
                            }
                        }
                        type++;
                    }
                }
                foreach (int _ in gases)
                {
                    escapeAddValue(sb, "0");
                }
            }

            sb.Append(Environment.NewLine);

            planetResourceData[planet.id] = sb.ToString();
            progressImage.fillAmount = (float)planetResourceData.Count / planetCount;
        }

        /**
         * Adds a value to the CSV, dealing with quotes and comas
         */
        public static void escapeAddValue(StringBuilder sb, string value)
        {
            bool hasComa = value.Contains(",");
            string safeValue = value.Replace('"','_');
            if (hasComa)
                sb.Append('"');
            sb.Append(safeValue);
            if (hasComa)
                sb.Append('"');
            sb.Append(spreadsheetColumnSeparator.Value);
        }

        public static RectTransform triggerButton;
        public static Sprite triggerSprite;
        public static Image progressImage;

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), "Begin")]
        public static void GameMain_Begin_Prefix()
        {
            //Logger.LogInfo("Game beginning");

            Monitor.Enter(planetComputeThreadMutexLock);
            spreadsheetGenRequestFlag = false;
            planetsToLoad.Clear();
            planetResourceData.Clear();

            planetCount = 0;
            foreach (StarData star in GameMain.universeSimulator.galaxyData.stars)
            {
                planetCount += star.planets.Length;
            }
            Monitor.Exit(planetComputeThreadMutexLock);

            if (GameMain.instance != null && GameObject.Find("Game Menu/button-1-bg"))
            {
                if (progressImage != null)
                {
                    progressImage.fillAmount = 0;
                }
                else
                {
                    //Logger.LogInfo("Loading button");
                    RectTransform parent = GameObject.Find("Game Menu").GetComponent<RectTransform>();
                    RectTransform prefab = GameObject.Find("Game Menu/button-1-bg").GetComponent<RectTransform>();
                    Vector3 referencePosition = prefab.localPosition;
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
                    progressImage.fillAmount = 0;
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

        [HarmonyPrefix, HarmonyPatch(typeof(PlanetModelingManager), nameof(PlanetModelingManager.ModelingPlanetMain))]
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
    }
}
