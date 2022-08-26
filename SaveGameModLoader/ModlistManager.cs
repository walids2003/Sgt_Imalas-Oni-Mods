﻿using HarmonyLib;
using KMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UtilLibs;

namespace SaveGameModLoader
{
    public class ModlistManager
    {
        public Dictionary<string,SaveGameModList> Modlists = new();
        private static readonly Lazy<ModlistManager> _instance = new Lazy<ModlistManager>(() => new ModlistManager());

        public static ModlistManager Instance { get { return _instance.Value; } }

        public GameObject ParentObjectRef;
        Dictionary<KMod.Label,bool> ModListDifferences = new Dictionary<KMod.Label,bool>();
        List<KMod.Label> MissingMods = new List<KMod.Label>();
        public bool IsSyncing { get; set; }
        public string ActiveSave = string.Empty;


        public ModlistManager()
        {
            GetAllStoredModlists();
        }

        public bool ModIsNotInSync(KMod.Mod mod)
        {
            if (mod.label.title == "SaveGameModSynchronizer")
                return false;
            return ModListDifferences.Keys.Contains(mod.label);
        }

        public SaveGameModList TryGetColonyModlist(string colonyName)
        {
            //GetAllStoredModlists();
            Modlists.TryGetValue(colonyName, out SaveGameModList result);
            //Debug.Log("ModList found for this savegame");
            return result;
        }


        public void InstantiateModView(List<KMod.Label> mods)
        {
            IsSyncing = true;
            AssignModDifferences(mods);

            var modScreen = Util.KInstantiateUI<ModsScreen>(ScreenPrefabs.Instance.modsMenu.gameObject, ParentObjectRef).transform;

            //UIUtils.ListAllChildren(modScreen);

            ///Set Title of Mod Sync Screen.
            modScreen.Find("Panel/Title/Title").GetComponent<LocText>().text = ModManagerStrings.STRINGS.UI.FRONTEND.MODSYNCING.MODDIFFS;

            var DetailsView = modScreen.Find("Panel/DetailsView").gameObject;
            var workShopButton = modScreen.Find("Panel/DetailsView/WorkshopButton");
            if (workShopButton == null)
            {
                Debug.LogError("Couldnt add buttons to Sync Menu");
                return;
            }
            ///Disable toggle all button
            var ToggleAll = modScreen.Find("Panel/DetailsView/ToggleAllButton");
            var ToggleAllButton = ToggleAll.GetComponent<KButton>();
            ToggleAllButton.isInteractable = false;
            ToggleAll.gameObject.SetActive(false);

            //UnityEngine.Object.Destroy(togglebtn);
            ///Add Syncing to close button
            var closeBtObj = modScreen.Find("Panel/DetailsView/CloseButton");
            var closeBt = closeBtObj.GetComponent<KButton>();
            closeBt.isInteractable = ModListDifferences.Count > 0 && ModListDifferences.Count > MissingMods.Count;
            closeBt.onClick += () => { SyncAllMods(modScreen.GetComponent<ModsScreen>(), null); };
            closeBtObj.Find("Text").GetComponent<LocText>().text = ModManagerStrings.STRINGS.UI.FRONTEND.MODSYNCING.SYNCSELECTED;

            var SyncAllButtonObject = Util.KInstantiateUI<RectTransform>(workShopButton.gameObject, DetailsView, true);
            SyncAllButtonObject.Find("Text").GetComponent<LocText>().text = ModManagerStrings.STRINGS.UI.FRONTEND.MODSYNCING.SYNCMODS;

            var SyncAllButton = SyncAllButtonObject.GetComponentInChildren<KButton>(true);
            //Button.GetComponent<LocText>().key = "STRINGS.UI.FRONTEND.MODSYNCING.SYNCMODS";
            SyncAllButton.ClearOnClick();
            SyncAllButton.isInteractable = ModListDifferences.Count > 0;
            SyncAllButton.onClick += () => { SyncAllMods(modScreen.GetComponent<ModsScreen>(), null); };

            var EntryPos2 = modScreen.Find("Panel").gameObject;

            var missingModListEntry = Util.KInstantiateUI<RectTransform>(workShopButton.gameObject, EntryPos2, true);
            missingModListEntry.name = "infoButton";
            var BtnText = missingModListEntry.Find("Text").GetComponent<LocText>();
            var bgColorImage = missingModListEntry.GetComponent<KImage>();
            var Btn = missingModListEntry.GetComponent<KButton>();


            if (MissingMods.Count == 0 && ModListDifferences.Count == 0)
            {
                BtnText.text = ModManagerStrings.STRINGS.UI.FRONTEND.MODSYNCING.ALLSYNCED;
                var ColorStyle = (ColorStyleSetting)ScriptableObject.CreateInstance("ColorStyleSetting");
                ColorStyle.inactiveColor = new Color(0.25f, 0.8f, 0.25f);
                ColorStyle.hoverColor = new Color(0.35f, 0.8f, 0.35f);
                bgColorImage.colorStyleSetting = ColorStyle;
                bgColorImage.ApplyColorStyleSetting();
                Btn.ClearOnClick();
                Btn.onClick += () =>
                {
                    ModsScreen screen = modScreen.GetComponent<ModsScreen>();
                    var method = typeof(ModsScreen).GetMethod("Exit", BindingFlags.NonPublic | BindingFlags.Instance);
                    method.Invoke(screen, null);
                };
            }
            else if (MissingMods.Count > 0)
            {
                var ColorStyle = (ColorStyleSetting)ScriptableObject.CreateInstance("ColorStyleSetting");
                ColorStyle.inactiveColor = new Color(1f, 0.25f, 0.25f);
                ColorStyle.hoverColor = new Color(1f, 0.35f, 0.35f);
                bgColorImage.colorStyleSetting = ColorStyle;
                bgColorImage.ApplyColorStyleSetting();
                BtnText.text = ModManagerStrings.STRINGS.UI.FRONTEND.MODSYNCING.MISSINGMOD;
                Btn.ClearOnClick();
                Btn.onClick += () =>
                {
                    ShowMissingMods();
                };
            }
            else
                UnityEngine.Object.Destroy(missingModListEntry.gameObject);

            // var infoHeader = Util.KInstantiateUI<RectTransform>(workShopButton.gameObject, ListView, true);
        }
        public void ShowMissingMods()
        {


            Manager.Dialog(Global.Instance.globalCanvas, 
                ModManagerStrings.STRINGS.UI.FRONTEND.MODSYNCING.MISSINGMODSTITLE, 
                string.Format(ModManagerStrings.STRINGS.UI.FRONTEND.MODSYNCING.MISSINGMODSDESC, 
                ListMissingMods()));

        }
        public string ListMissingMods()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine();
            Debug.LogWarning("------Mod Sync------");
            Debug.LogWarning("---[Missing Mods]---");

            for (int i = 0; i< MissingMods.Count-1; i++)
            {
                stringBuilder.Append(MissingMods[i].title + ",  ");
                Debug.LogWarning(MissingMods[i].title);
            }
            stringBuilder.Append(MissingMods[MissingMods.Count - 1].title);
            Debug.LogWarning(MissingMods[MissingMods.Count - 1].title);

            Debug.LogWarning("-----[List End]-----");
            Debug.LogWarning("------Mod Sync------");
            return stringBuilder.ToString();
        }


        public void SyncAllMods(ModsScreen modScreen, bool? enableAll)
        {
            Manager modManager = Global.Instance.modManager;

            foreach (var mod in this.ModListDifferences.Keys)
            {
                if (modManager.FindMod(mod) == null)
                {
                    Debug.LogWarning("Mod not found: " + mod.title);
                    continue;
                }

                bool enabled = enableAll == null? ModListDifferences[mod] : (bool)enableAll;
                modManager.EnableMod(mod, enabled, null);
            }

            var methodInfo = typeof(ModsScreen).GetMethod("Exit", BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo != null)
                methodInfo.Invoke(modScreen,null);
            if (ModListDifferences.Count > 0) 
            {
                ModListDifferences.Clear(); 
                MissingMods.Clear();
                AutoLoadOnRestart();
            }
        }

        public void AssignModDifferences(List<KMod.Label> modList)
        {
            KMod.Manager modManager = Global.Instance.modManager;

            var allMods = modManager.mods.Select(mod => mod.label).ToList();
            var enabledModLabels = modManager.mods.FindAll(mod => mod.IsActive() == true).Select(mod => mod.label).ToList();


            var enabledButNotSavedMods = enabledModLabels.Except(modList).ToList();
            var savedButNotEnabledMods = modList.Except(enabledModLabels).ToList();

            MissingMods = modList.Except(allMods).ToList();
            //Debug.Log("MissingMOds");
            //foreach (var m in MissingMods) Debug.Log(m.title);
            //Debug.Log("MissingMOds");

            ModListDifferences.Clear();
            foreach(var toDisable in enabledButNotSavedMods)
            {
                ModListDifferences.Add(toDisable, false);
            }
            foreach (var toEnable in savedButNotEnabledMods)
            {
                ModListDifferences.Add(toEnable, true);
            }
            var thisMod = modList.Find(mod => mod.title == "SaveGameModSynchronizer");
            ModListDifferences.Remove(thisMod);
            
        }
        void AutoLoadOnRestart()
        {
            if(ActiveSave!=string.Empty)
                KPlayerPrefs.SetString("AutoResumeSaveFile", ActiveSave);
            ActiveSave = string.Empty;
        }


        public void InstantiateModViewForPathOnly(string referencedPath)
        {
            ActiveSave = referencedPath;
            var mods = TryGetColonyModlist(SaveGameModList.GetModListFileName(referencedPath));
            if (mods == null)
            {
                Debug.LogError("No Modlist found for " + referencedPath);
                return;
            }
            var list = mods.TryGetModListEntry(referencedPath);

            if(list==null)
            {
                Debug.LogError("No Modlist found for " + referencedPath);
                return;
            }
            InstantiateModView(list);
        }

        public void GetAllStoredModlists()
        {
            Modlists.Clear();
            var files = Directory.GetFiles(ModAssets.ModPath);
            foreach(var modlist in files)
            {
                try
                {
                   //Debug.Log("Trying to load: " + modlist);
                   var list = SaveGameModList.ReadModlistListFromFile(modlist);
                    Modlists.Add(list.ReferencedColonySaveName, list);
                }
                catch(Exception e)
                {
                    Debug.LogError("Couln't load modlist from: " + modlist + ", Error: "+e);
                }
            }
            //Debug.Log("Found Mod Configs for " + files.Count() + " Colonies");
        }

        public bool CreateOrAddToModLists(string savePath,List<KMod.Label> list)
        {
            bool hasBeenInitialized = false;

            Modlists.TryGetValue(SaveGameModList.GetModListFileName(savePath),out SaveGameModList colonyModSave);

            if (colonyModSave == null)
            {
                hasBeenInitialized = true;
                   colonyModSave = new SaveGameModList(savePath);
            }
            bool subListInitialized = colonyModSave.AddOrUpdateEntryToModList(savePath, list);
            Modlists[SaveGameModList.GetModListFileName(savePath)] = colonyModSave;

            return hasBeenInitialized | subListInitialized;

        }
    }
}
