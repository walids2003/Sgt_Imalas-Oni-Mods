﻿using HarmonyLib;
using Klei;
using KMod;
using System;
using System.Collections.Generic;
using System.IO;
using UtilLibs;

namespace ClusterTraitGenerationManager
{
    public class Mod : UserMod2
    {
        public static Harmony harmonyInstance;
        public override void OnLoad(Harmony harmony)
        {
            harmonyInstance = harmony;
            base.OnLoad(harmony);
            ModAssets.LoadAssets(); 
            
            SgtLogger.debuglog("Initializing file paths..");
            ModAssets.CustomClusterTemplatesPath = FileSystem.Normalize(Path.Combine(Path.Combine(Manager.GetDirectory(), "config"), "CustomClusterPresetTemplates"));


            SgtLogger.debuglog("Initializing folders..");
            try
            {
                System.IO.Directory.CreateDirectory(ModAssets.CustomClusterTemplatesPath);
            }
            catch (Exception e)
            {
                SgtLogger.error("Could not create folder, Exception:\n" + e);
            }
            SgtLogger.log("Folders succesfully initialized");

            SgtLogger.LogVersion(this, harmony);
#if DEBUG
            //Debug.LogError("Error THIS IS NOT RELEASE");
#endif
        }
        public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<KMod.Mod> mods)
        {
            base.OnAllModsLoaded(harmony, mods);
            CompatibilityNotifications.FlagLoggingPrevention(mods);

            CompatibilityNotifications.CheckAndAddIncompatibles("CGSMMerged", "Cluster Generation Manager", "Cluster Generation Settings Manager");
            CompatibilityNotifications.CheckAndAddIncompatibles("Mod.WGSM", "Cluster Generation Manager","WGSM - World Generation Settings Manager");

        }
    }
}
