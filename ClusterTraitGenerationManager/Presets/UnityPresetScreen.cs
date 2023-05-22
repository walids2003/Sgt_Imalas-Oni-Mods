﻿using Database;
using Epic.OnlineServices.Sessions;
using FMOD;
using Klei.AI;
using Klei.CustomSettings;
using KMod;
using ProcGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using TUNING;
using UnityEngine;
using UnityEngine.UI;
using UtilLibs;
using UtilLibs.UIcmp;
using static ResearchTypes;
using static SandboxSettings;
using static STRINGS.DUPLICANTS;
using static STRINGS.DUPLICANTS.CHORES;
using static STRINGS.UI.DETAILTABS.PERSONALITY.RESUME;
using static ClusterTraitGenerationManager.STRINGS.UI;
using Satsuma;
using static Operational;
using System.Security.Policy;
using static BestFit;
using YamlDotNet.Serialization;
using static STRINGS.UI.FRONTEND;
using static ClusterTraitGenerationManager.CGSMClusterManager;
using static ClusterTraitGenerationManager.STRINGS.UI.CGM.INDIVIDUALSETTINGS.BUTTONS;
using static STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS;
using static STRINGS.DUPLICANTS.TRAITS;
using static ClusterTraitGenerationManager.CustomClusterSettingsPreset;
using System.Net;
using static STRINGS.CODEX;

namespace ClusterTraitGenerationManager
{
    internal class UnityPresetScreen : FScreen
    {
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS0414 // Remove unused private members
        new bool ConsumeMouseScroll = true; // do not remove!!!!
#pragma warning restore CS0414 // Remove unused private members
#pragma warning restore IDE0051 // Remove unused private members
        public static UnityPresetScreen Instance = null;


        public FButton GeneratePresetButton;
        public FButton CloseButton;
        public FButton ApplyButton;

        public GameObject InfoHeaderPrefab;
        public GameObject InfoRowPrefab;
        public GameObject InfoRowSimple;
        public GameObject InfoSpacer;
        public GameObject InfoScreenContainer;

        public GameObject PresetListContainer;
        public GameObject PresetListPrefab;

        public FButton OpenPresetFolder;
        public FButton ClearSearchBar;
        public FInputField2 Searchbar;

        public bool CurrentlyActive = false;
        private bool HoveringPrio = false;

        ///Preset
        CustomClusterSettingsPreset CurrentlySelected;
        ///Referenced Stats to apply presets to.

        Dictionary<CustomClusterSettingsPreset, GameObject> Presets = new Dictionary<CustomClusterSettingsPreset, GameObject>();
        //List<GameObject> InformationObjects = new List<GameObject>();

        Dictionary<int, Tuple<FButton, LocText, Image>> ScheduleBlocks = new Dictionary<int, Tuple<FButton, LocText, Image>>();
        LocText TitleHolder = null;
        Image IsActiveAsDefaultSchedule = null;
        FButton IsActiveAsDefaultScheduleBtn = null;
        Image IsActiveAsDefaultScheduleBG = null;

        string RefName;

        public static void ShowWindow(CustomClusterData toLoadFrom, System.Action onClose, string refName = "")
        {
            if (Instance == null)
            {
                var screen = Util.KInstantiateUI(ModAssets.PresetScreen, FrontEndManager.Instance.gameObject, true);
                Instance = screen.AddOrGet<UnityPresetScreen>();
                Instance.Init();
            }
            Instance.Show(true);
            Instance.ConsumeMouseScroll = true;
            Instance.transform.SetAsLastSibling();
            Instance.LoadAllPresets();
            Instance.RefName = refName;
            Instance.LoadTemporalPreset(toLoadFrom);
            Instance.OnCloseAction = onClose;
            Instance.Searchbar.Text = string.Empty;
        }

        private bool init;
        private System.Action OnCloseAction;
        private CustomClusterData referencedCluster;

        public void LoadTemporalPreset(CustomClusterData toGenerateFrom)
        {
            referencedCluster = toGenerateFrom;
            CustomClusterSettingsPreset tempStats = CustomClusterSettingsPreset.CreateFromCluster(toGenerateFrom, RefName);
            SetAsCurrent(tempStats);
        }

        public override void OnKeyDown(KButtonEvent e)
        {
            if (e.TryConsume(Action.MouseRight))
            {
                this.Show(false);
            }
            if (e.TryConsume(Action.Escape))
            {
                this.Show(false);
            }
            base.OnKeyDown(e);
        }

        void LoadAllPresets()
        {
            foreach (var existing in Presets.Values)
            {
                Destroy(existing.gameObject);
            }
            Presets.Clear();
            foreach (var loadedPreset in LoadPresets())
            {
                AddUiElementForPreset(loadedPreset);
            }
        }


        public static List<CustomClusterSettingsPreset> LoadPresets()
        {
            List<CustomClusterSettingsPreset> minionStatConfigs = new List<CustomClusterSettingsPreset>();
            var files = new DirectoryInfo(ModAssets.CustomClusterTemplatesPath).GetFiles();

            for (int i = 0; i < files.Count(); i++)
            {
                var File = files[i];
                try
                {
                    var preset = CustomClusterSettingsPreset.ReadFromFile(File);
                    if (preset != null)
                    {
                        minionStatConfigs.Add(preset);
                    }
                }
                catch (Exception e)
                {
                    SgtLogger.logError("Couln't load priority preset from: " + File.FullName + ",\nError: " + e.ToString());
                }
            }
            minionStatConfigs = minionStatConfigs.OrderBy(entry => entry.ConfigName).ToList();
            return minionStatConfigs;
        }

        private bool AddUiElementForPreset(CustomClusterSettingsPreset config)
        {
            if (!Presets.ContainsKey(config))
            {
                var PresetHolder = Util.KInstantiateUI(PresetListPrefab, PresetListContainer, true);
                //PresetHolder.transform.Find("TraitImage").gameObject.SetActive(false);
                var img = PresetHolder.transform.Find("TraitImage").GetComponent<Image>();
                //InDefaultListImage(img, config.InDefaultList);

                UIUtils.TryChangeText(PresetHolder.transform, "Label", config.ConfigName);
                PresetHolder.transform.Find("RenameButton").FindOrAddComponent<FButton>().OnClick +=
                    () => config.OpenPopUpToChangeName(
                        () =>
                            {
                                UIUtils.TryChangeText(PresetHolder.transform, "Label", config.ConfigName);
                                RebuildInformationPanel();
                            }
                        );

                PresetHolder.transform.Find("AddThisTraitButton").FindOrAddComponent<FButton>().OnClick += () => SetAsCurrent(config);
                PresetHolder.transform.Find("DeleteButton").FindOrAddComponent<FButton>().OnClick += () => DeletePreset(config);


                //UIUtils.AddSimpleTooltipToObject(PresetHolder.transform.Find("RenameButton"), STRINGS.UI.PRESETWINDOWDUPEPRIOS.HORIZONTALLAYOUT.OBJECTLIST.SCROLLAREA.CONTENT.PRESETENTRYPREFAB.RENAMEPRESETTOOLTIP);
                //UIUtils.AddSimpleTooltipToObject(PresetHolder.transform.Find("DeleteButton"), STRINGS.UI.PRESETWINDOWDUPEPRIOS.HORIZONTALLAYOUT.OBJECTLIST.SCROLLAREA.CONTENT.PRESETENTRYPREFAB.DELETEPRESETTOOLTIP);
                Presets[config] = PresetHolder;
                return true;
            }
            return false;
        }

        void DeletePreset(CustomClusterSettingsPreset config)
        {
            System.Action Delete = () =>
            {
                if (Presets.ContainsKey(config))
                {
                    Destroy(Presets[config]);
                    Presets.Remove(config);
                    config.DeleteFile();
                }
            };
            System.Action nothing = () =>
            { };

            KMod.Manager.Dialog(Global.Instance.globalCanvas,
           string.Format(STRINGS.UI.PRESETWINDOWCLUSTERPRESETS.DELETEWINDOW.TITLE, config.ConfigName),
           string.Format(STRINGS.UI.PRESETWINDOWCLUSTERPRESETS.DELETEWINDOW.DESC, config.ConfigName),
           STRINGS.UI.PRESETWINDOWCLUSTERPRESETS.DELETEWINDOW.YES,
           Delete,
           STRINGS.UI.PRESETWINDOWCLUSTERPRESETS.DELETEWINDOW.CANCEL
           , nothing
           );
        }

        void SetAsCurrent(CustomClusterSettingsPreset config)
        {
            CurrentlySelected = config;
            RebuildInformationPanel();
        }

        void RebuildInformationPanel()
        {
            if (CurrentlySelected == null)
                return;

            var settingsInstance = CustomGameSettings.Instance;
            foreach (var kvp in GameSettingsTexts)
            {
                kvp.Value.text = kvp.Key.label + ": " + settingsInstance.GetCurrentQualitySetting(kvp.Key).id; 
            }
            GameSettingsTexts[CustomGameSettingConfigs.WorldgenSeed].text = CustomGameSettingConfigs.WorldgenSeed.label + ": " + CustomGameSettingConfigs.WorldgenSeed.GetLevel(CurrentlySelected.Seed).label;
            GameSettingsTexts[CustomGameSettingConfigs.ImmuneSystem].text = CustomGameSettingConfigs.ImmuneSystem.label + ": " + CustomGameSettingConfigs.ImmuneSystem.GetLevel(CurrentlySelected.ImmuneSystem).label;
            GameSettingsTexts[CustomGameSettingConfigs.CalorieBurn].text = CustomGameSettingConfigs.CalorieBurn.label + ": " + CustomGameSettingConfigs.CalorieBurn.GetLevel(CurrentlySelected.CalorieBurn).label;
            GameSettingsTexts[CustomGameSettingConfigs.Morale].text = CustomGameSettingConfigs.Morale.label + ": " + CustomGameSettingConfigs.Morale.GetLevel(CurrentlySelected.Morale).label;
            GameSettingsTexts[CustomGameSettingConfigs.Durability].text = CustomGameSettingConfigs.Durability.label + ": " + CustomGameSettingConfigs.Durability.GetLevel(CurrentlySelected.Durability).label;
            GameSettingsTexts[CustomGameSettingConfigs.MeteorShowers].text = CustomGameSettingConfigs.MeteorShowers.label + ": " + CustomGameSettingConfigs.MeteorShowers.GetLevel(CurrentlySelected.MeteorShowers).label; 
            if (DlcManager.IsExpansion1Active())
                GameSettingsTexts[CustomGameSettingConfigs.Radiation].text = CustomGameSettingConfigs.Radiation.label + ": " + CustomGameSettingConfigs.Radiation.GetLevel(CurrentlySelected.Radiation).label;
            GameSettingsTexts[CustomGameSettingConfigs.Stress].text = CustomGameSettingConfigs.Stress.label + ": " + CustomGameSettingConfigs.Stress.GetLevel(CurrentlySelected.Stress).label;
            GameSettingsTexts[CustomGameSettingConfigs.StressBreaks].text = CustomGameSettingConfigs.StressBreaks.label + ": " + CustomGameSettingConfigs.StressBreaks.GetLevel(CurrentlySelected.StressBreaks).label;
            GameSettingsTexts[CustomGameSettingConfigs.SandboxMode].text = CustomGameSettingConfigs.SandboxMode.label + ": " + CustomGameSettingConfigs.SandboxMode.GetLevel(CurrentlySelected.SandboxMode).label;
            GameSettingsTexts[CustomGameSettingConfigs.CarePackages].text = CustomGameSettingConfigs.CarePackages.label + ": " + CustomGameSettingConfigs.CarePackages.GetLevel(CurrentlySelected.CarePackages).label;
            GameSettingsTexts[CustomGameSettingConfigs.FastWorkersMode].text = CustomGameSettingConfigs.FastWorkersMode.label + ": " + CustomGameSettingConfigs.FastWorkersMode.GetLevel(CurrentlySelected.FastWorkersMode).label;
            GameSettingsTexts[CustomGameSettingConfigs.SaveToCloud].text = CustomGameSettingConfigs.SaveToCloud.label + ": " + CustomGameSettingConfigs.SaveToCloud.GetLevel(CurrentlySelected.SaveToCloud).label;
            GameSettingsTexts[CustomGameSettingConfigs.Teleporters].text = CustomGameSettingConfigs.Teleporters.label + ": " + CustomGameSettingConfigs.Teleporters.GetLevel(CurrentlySelected.Teleporters).label;

            TitleHolder.text = CurrentlySelected.ConfigName;
            GeneratePresetButton.SetInteractable(!Presets.ContainsKey(CurrentlySelected));

            foreach (var item in StarmapItemContainers)
                UnityEngine.Object.Destroy(item);
            StarmapItemContainers.Clear();

            StarmapItemContainers.Add(Util.KInstantiateUI(InfoSpacer, InfoScreenContainer, true));

            if (CurrentlySelected.StarterPlanet != null)
            {
                var starterHeader = Util.KInstantiateUI(InfoHeaderPrefab, InfoScreenContainer, true);
                starterHeader.transform.Find("Label").GetComponent<LocText>().text = CUSTOMCLUSTERUI.CATEGORYENUM.START + ":"; //TODO
                StarmapItemContainers.Add(starterHeader);

                CreateUIItemForStarmapItem(CurrentlySelected.StarterPlanet);
            }

            if (CurrentlySelected.WarpPlanet != null)
            {
                var warpHeader = Util.KInstantiateUI(InfoHeaderPrefab, InfoScreenContainer, true);
                warpHeader.transform.Find("Label").GetComponent<LocText>().text = CUSTOMCLUSTERUI.CATEGORYENUM.WARP + ":"; //TODO
                StarmapItemContainers.Add(warpHeader);

                CreateUIItemForStarmapItem(CurrentlySelected.WarpPlanet);
            }

            if (CurrentlySelected.OuterPlanets.Count > 0)
            {
                var outerHeader = Util.KInstantiateUI(InfoHeaderPrefab, InfoScreenContainer, true);
                outerHeader.transform.Find("Label").GetComponent<LocText>().text = CUSTOMCLUSTERUI.CATEGORYENUM.OUTER + ":"; //TODO
                StarmapItemContainers.Add(outerHeader);
            }

            var combined = new List<SerializableStarmapItem>();
            combined.AddRange(CurrentlySelected.OuterPlanets.Values);
            combined.AddRange(CurrentlySelected.POIs.Values);
            bool reachedPOI = false;



            for (int i = 0; i < combined.Count; i++)
            {
                var item = combined[i];

                if (item.category == StarmapItemCategory.POI && reachedPOI == false)
                {
                    var poi = Util.KInstantiateUI(InfoHeaderPrefab, InfoScreenContainer, true);
                    poi.transform.Find("Label").GetComponent<LocText>().text = CUSTOMCLUSTERUI.CATEGORYENUM.POI+":"; //TODO
                    StarmapItemContainers.Add(poi);
                    reachedPOI = true;
                }

                CreateUIItemForStarmapItem(item);
            }
        }

        static async Task ExecuteWithDelay(int ms, System.Action action)
        {
            await Task.Delay(ms);
            action.Invoke();
        }

        Dictionary<SettingConfig, LocText> GameSettingsTexts = new Dictionary<SettingConfig, LocText>();

        List<GameObject> StarmapItemContainers = new List<GameObject>();

        GameObject CreateUIItemForStarmapItem(SerializableStarmapItem item)
        {
            if (item == null)
                return null;

            if (!PlanetoidDict().ContainsKey(item.itemID))
            {
                SgtLogger.warning(item.itemID + " not found!");
                return null;
            }

            var starmapItem = PlanetoidDict()[item.itemID];

            var planetObject = Util.KInstantiateUI(InfoRowPrefab, InfoScreenContainer, true);

            var infoText = starmapItem.DisplayName;
            if (item.maxNumberToSpawn != 1 || item.category == StarmapItemCategory.POI)
                infoText += ": x" + item.numberToSpawn ;

            UIUtils.TryChangeText(planetObject.transform, "Label", infoText);

            planetObject.transform.Find("Label/TraitImage").TryGetComponent<Image>(out var image);
            image.sprite = starmapItem.planetSprite;

            var imageContainer = planetObject.transform.Find("IconContainer").gameObject;
            if (item.category != StarmapItemCategory.POI)
            {
                var traitImagePrefab = planetObject.transform.Find("IconContainer/TraitImage").gameObject;
                if (item.planetTraits == null)
                {
                    imageContainer.gameObject.SetActive(false);
                }
                else
                {
                    foreach (var trait in item.planetTraits)
                    {
                        if (ModAssets.AllTraitsWithRandomDict.ContainsKey(trait))
                        {
                            var worldTrait = ModAssets.AllTraitsWithRandomDict[trait];
                            Util.KInstantiateUI(traitImagePrefab, imageContainer, true).TryGetComponent<Image>(out var traitImage);
                            traitImage.color = Util.ColorFromHex(worldTrait.colorHex);
                            traitImage.sprite = Assets.GetSprite(worldTrait.filePath.Substring(worldTrait.filePath.LastIndexOf("/") + 1));
                        }
                    }
                }
            }
            else
            {
            }
            StarmapItemContainers.Add(planetObject);
            return planetObject;
        }

        private void Init()
        {
            UIUtils.ListAllChildrenPath(this.transform);
            //UIUtils.TryChangeText(transform, "Title", TITLESCHEDULES);
            int i = 1;
            GeneratePresetButton = transform.Find("HorizontalLayout/ItemInfo/Buttons/GenerateFromCurrent").FindOrAddComponent<FButton>();
            CloseButton = transform.Find("HorizontalLayout/ItemInfo/Buttons/CloseButton").FindOrAddComponent<FButton>();
            ApplyButton = transform.Find("HorizontalLayout/ItemInfo/Buttons/ApplyPresetButton").FindOrAddComponent<FButton>();

            OpenPresetFolder = transform.Find("HorizontalLayout/ObjectList/SearchBar/FolderButton").FindOrAddComponent<FButton>();
            OpenPresetFolder.OnClick += () => Process.Start(new ProcessStartInfo(ModAssets.CustomClusterTemplatesPath) { UseShellExecute = true });

            Searchbar = transform.Find("HorizontalLayout/ObjectList/SearchBar/Input").FindOrAddComponent<FInputField2>();
            Searchbar.OnValueChanged.AddListener(ApplyFilter);
            Searchbar.Text = string.Empty;


            ClearSearchBar = transform.Find("HorizontalLayout/ObjectList/SearchBar/DeleteButton").FindOrAddComponent<FButton>();
            ClearSearchBar.OnClick += () => Searchbar.Text = string.Empty;

            ApplyButton.OnClick += () =>
            {
                CurrentlySelected.ApplyPreset();
                this.OnCloseAction.Invoke();
                this.Show(false);
            };
            ///OpenFolder

            CloseButton.OnClick += () => this.Show(false);
            GeneratePresetButton.OnClick += () =>
            {
                bool added = AddUiElementForPreset(CurrentlySelected);
                if (added)
                {
                    CurrentlySelected.WriteToFile();
                    CurrentlySelected.OpenPopUpToChangeName(
                            () =>
                                {
                                    if (this.CurrentlyActive && Presets[CurrentlySelected] != null)
                                    {
                                        UIUtils.TryChangeText(Presets[CurrentlySelected].transform, "Label", CurrentlySelected.ConfigName);
                                        RebuildInformationPanel();
                                    }
                                }
                            );
                    RebuildInformationPanel();
                }
            };


            //UIUtils.AddSimpleTooltipToObject(GeneratePresetButton.transform, HORIZONTALLAYOUT.ITEMINFO.BUTTONS.GENERATEFROMCURRENT.TOOLTIP);
            //UIUtils.AddSimpleTooltipToObject(CloseButton.transform, HORIZONTALLAYOUT.ITEMINFO.BUTTONS.CLOSEBUTTON.TOOLTIP);
            //UIUtils.AddSimpleTooltipToObject(ApplyButton.transform, HORIZONTALLAYOUT.ITEMINFO.BUTTONS.APPLYPRESETBUTTON.TOOLTIP);

            //UIUtils.AddSimpleTooltipToObject(ClearSearchBar.transform, HORIZONTALLAYOUT.OBJECTLIST.SEARCHBAR.CLEARTOOLTIP);
            //UIUtils.AddSimpleTooltipToObject(OpenPresetFolder.transform, HORIZONTALLAYOUT.OBJECTLIST.SEARCHBAR.OPENFOLDERTOOLTIP);

            InfoHeaderPrefab = transform.Find("HorizontalLayout/ItemInfo/ScrollArea/Content/HeaderPrefab").gameObject;
            InfoRowPrefab = transform.Find("HorizontalLayout/ItemInfo/ScrollArea/Content/ListViewEntryPrefab").gameObject;
            InfoRowSimple = transform.Find("HorizontalLayout/ItemInfo/ScrollArea/Content/ItemPrefab").gameObject;
            InfoSpacer = Util.KInstantiateUI(transform.Find("HorizontalLayout/ItemInfo/ScrollArea/Content/ItemPrefab").gameObject);
            UIUtils.FindAndDestroy(InfoSpacer.transform, "Label");
            InfoSpacer.rectTransform().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 20);

            InfoScreenContainer = transform.Find("HorizontalLayout/ItemInfo/ScrollArea/Content").gameObject;
            PresetListContainer = transform.Find("HorizontalLayout/ObjectList/ScrollArea/Content").gameObject;
            PresetListPrefab = transform.Find("HorizontalLayout/ObjectList/ScrollArea/Content/PresetEntryPrefab").gameObject;

            var Name = Util.KInstantiateUI(InfoHeaderPrefab, InfoScreenContainer, true);
            //UIUtils.TryChangeText(Name.transform, "Label", "\"" + CurrentlySelected.ConfigName + "\"");
            TitleHolder = Name.transform.Find("Label").GetComponent<LocText>();

            var spacer = Util.KInstantiateUI(InfoSpacer, InfoScreenContainer, true);


            var GameSettingTitle = Util.KInstantiateUI(InfoHeaderPrefab, InfoScreenContainer, true);
            var GameSettingTitleText = GameSettingTitle.transform.Find("Label").GetComponent<LocText>();
            GameSettingTitleText.text = global::STRINGS.UI.FRONTEND.NEWGAMESETTINGS.HEADER;

            var WorldgenSeed = Util.KInstantiateUI(InfoRowSimple, InfoScreenContainer, true).transform.Find("Label").gameObject.AddOrGet<LocText>();
            WorldgenSeed.text = global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.WORLDGEN_SEED.NAME;
            UIUtils.AddSimpleTooltipToObject(WorldgenSeed.transform.parent, global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.WORLDGEN_SEED.TOOLTIP, alignCenter: true, onBottom: true);
            GameSettingsTexts[CustomGameSettingConfigs.WorldgenSeed] = WorldgenSeed;

            // UIUtils.AddSimpleTooltipToObject(transform.Find("Content/Warning"), STRINGS.UI.CUSTOMGAMESETTINGSCHANGER.CHANGEWARNINGTOOLTIP);

            var ImmuneSystem = Util.KInstantiateUI(InfoRowSimple, InfoScreenContainer, true).transform.Find("Label").gameObject.AddOrGet<LocText>();
            ImmuneSystem.text = global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.IMMUNESYSTEM.NAME;
            UIUtils.AddSimpleTooltipToObject(ImmuneSystem.transform.parent, global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.IMMUNESYSTEM.TOOLTIP, alignCenter: true, onBottom: true);
            GameSettingsTexts[CustomGameSettingConfigs.ImmuneSystem] = ImmuneSystem;

            var CalorieBurn = Util.KInstantiateUI(InfoRowSimple, InfoScreenContainer, true).transform.Find("Label").gameObject.AddOrGet<LocText>();
            CalorieBurn.text = global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.CALORIE_BURN.NAME;
            UIUtils.AddSimpleTooltipToObject(CalorieBurn.transform.parent, global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.CALORIE_BURN.TOOLTIP, alignCenter: true, onBottom: true);
            GameSettingsTexts[CustomGameSettingConfigs.CalorieBurn] = CalorieBurn;

            var Morale = Util.KInstantiateUI(InfoRowSimple, InfoScreenContainer, true).transform.Find("Label").gameObject.AddOrGet<LocText>();
            Morale.text = global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.MORALE.NAME;
            UIUtils.AddSimpleTooltipToObject(Morale.transform.parent, global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.MORALE.TOOLTIP, alignCenter: true, onBottom: true);
            GameSettingsTexts[CustomGameSettingConfigs.Morale] = Morale;

            var Durability = Util.KInstantiateUI(InfoRowSimple, InfoScreenContainer, true).transform.Find("Label").gameObject.AddOrGet<LocText>();
            Durability.text = global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.DURABILITY.NAME;
            UIUtils.AddSimpleTooltipToObject(Durability.transform.parent, global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.DURABILITY.TOOLTIP, alignCenter: true, onBottom: true);
            GameSettingsTexts[CustomGameSettingConfigs.Durability] = Durability;

            var MeteorShowers = Util.KInstantiateUI(InfoRowSimple, InfoScreenContainer, true).transform.Find("Label").gameObject.AddOrGet<LocText>();
            MeteorShowers.text = global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.METEORSHOWERS.NAME;
            UIUtils.AddSimpleTooltipToObject(MeteorShowers.transform.parent, global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.METEORSHOWERS.TOOLTIP, alignCenter: true, onBottom: true);
            GameSettingsTexts[CustomGameSettingConfigs.MeteorShowers] = MeteorShowers;

            var Radiation = Util.KInstantiateUI(InfoRowSimple, InfoScreenContainer, true).transform.Find("Label").gameObject.AddOrGet<LocText>();
            Radiation.text = global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.RADIATION.NAME;
            UIUtils.AddSimpleTooltipToObject(Radiation.transform.parent, global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.RADIATION.TOOLTIP, alignCenter: true, onBottom: true);
            GameSettingsTexts[CustomGameSettingConfigs.Radiation] = Radiation;

            var Stress = Util.KInstantiateUI(InfoRowSimple, InfoScreenContainer, true).transform.Find("Label").gameObject.AddOrGet<LocText>();
            Stress.text = global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.STRESS.NAME;
            UIUtils.AddSimpleTooltipToObject(Stress.transform.parent, global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.STRESS.TOOLTIP, alignCenter: true, onBottom: true);
            GameSettingsTexts[CustomGameSettingConfigs.Stress] = Stress;



            var StressBreaks = Util.KInstantiateUI(InfoRowSimple, InfoScreenContainer, true).transform.Find("Label").gameObject.AddOrGet<LocText>();
            StressBreaks.text = global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.STRESS_BREAKS.NAME;
            UIUtils.AddSimpleTooltipToObject(StressBreaks.transform.parent, global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.STRESS_BREAKS.TOOLTIP, alignCenter: true, onBottom: true);
            GameSettingsTexts[CustomGameSettingConfigs.StressBreaks] = StressBreaks;

            var CarePackages = Util.KInstantiateUI(InfoRowSimple, InfoScreenContainer, true).transform.Find("Label").gameObject.AddOrGet<LocText>();
            CarePackages.text = global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.CAREPACKAGES.NAME;
            UIUtils.AddSimpleTooltipToObject(CarePackages.transform.parent, global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.CAREPACKAGES.TOOLTIP, alignCenter: true, onBottom: true);
            GameSettingsTexts[CustomGameSettingConfigs.CarePackages] = CarePackages;

            var SandboxMode = Util.KInstantiateUI(InfoRowSimple, InfoScreenContainer, true).transform.Find("Label").gameObject.AddOrGet<LocText>();
            SandboxMode.text = global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.SANDBOXMODE.NAME;
            UIUtils.AddSimpleTooltipToObject(SandboxMode.transform.parent, global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.SANDBOXMODE.TOOLTIP, alignCenter: true, onBottom: true);
            GameSettingsTexts[CustomGameSettingConfigs.SandboxMode] = SandboxMode;

            var FastWorkersMode = Util.KInstantiateUI(InfoRowSimple, InfoScreenContainer, true).transform.Find("Label").gameObject.AddOrGet<LocText>();
            FastWorkersMode.text = global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.FASTWORKERSMODE.NAME;
            UIUtils.AddSimpleTooltipToObject(FastWorkersMode.transform.parent, global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.FASTWORKERSMODE.TOOLTIP, alignCenter: true, onBottom: true);
            GameSettingsTexts[CustomGameSettingConfigs.FastWorkersMode] = FastWorkersMode;

            var SaveToCloud = Util.KInstantiateUI(InfoRowSimple, InfoScreenContainer, true).transform.Find("Label").gameObject.AddOrGet<LocText>();
            SaveToCloud.text = global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.SAVETOCLOUD.NAME;
            UIUtils.AddSimpleTooltipToObject(SaveToCloud.transform.parent, global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.SAVETOCLOUD.TOOLTIP, alignCenter: true, onBottom: true);
            GameSettingsTexts[CustomGameSettingConfigs.SaveToCloud] = SaveToCloud;

            var Teleporters = Util.KInstantiateUI(InfoRowSimple, InfoScreenContainer, true).transform.Find("Label").gameObject.AddOrGet<LocText>();
            Teleporters.text = global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.TELEPORTERS.NAME;
            UIUtils.AddSimpleTooltipToObject(Teleporters.transform.parent, global::STRINGS.UI.FRONTEND.CUSTOMGAMESETTINGSSCREEN.SETTINGS.TELEPORTERS.TOOLTIP, alignCenter: true, onBottom: true);
            GameSettingsTexts[CustomGameSettingConfigs.Teleporters] = Teleporters;

            init = true;
        }

        public void ApplyFilter(string filterstring = "")
        {
            foreach (var go in Presets)
            {
                go.Value.SetActive(filterstring == string.Empty ? true : go.Key.ConfigName.ToLowerInvariant().Contains(filterstring.ToLowerInvariant()));
            }
        }

        public override void OnShow(bool show)
        {
            base.OnShow(show);
            if (!init)
            {
                Init();
            }

            if (show)
            {
                CurrentlyActive = show;
            }
            else
            {
                DeactivateStatusWithDelay(600);
            }
        }
        async Task DeactivateStatusWithDelay(int ms)
        {
            await Task.Delay(ms);
            CurrentlyActive = false;
        }
    }
}
