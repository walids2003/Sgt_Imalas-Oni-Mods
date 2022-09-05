﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtilLibs;
using static UtilLibs.RocketryUtils;

namespace Rockets_TinyYetBig
{
    
    
    class SelectModuleSideScreenPatches
    {

        [HarmonyPatch(typeof(AdditionalDetailsPanel))]
        [HarmonyPatch("OnPrefabInit")]
        public static class GibCOllaps
        { 
            public static void Postfix(AdditionalDetailsPanel __instance)
            {
                var detailsPanel = (GameObject)typeof(AdditionalDetailsPanel).GetField("detailsPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);

                //Debug.Log("Detailspanel:");
                //UIUtils.ListAllChildren(detailsPanel.transform);
                //Debug.Log("LabelTemplate:");
                //UIUtils.ListAllChildren(__instance.attributesLabelTemplate.transform);
            }
        }

        
        [HarmonyPatch(typeof(SelectModuleSideScreen))]
        [HarmonyPatch("UpdateBuildableStates")]
        public static class HideEmptyCategories
        { 
            public static void Postfix(SelectModuleSideScreen __instance)
            {
                foreach (KeyValuePair<BuildingDef, GameObject> button in __instance.buttons)
                {
                    if(CategoryPatchTest.DisabledButtons.Contains(button.Key.PrefabID))
                    button.Value.SetActive(false);  
                }
                


                foreach(var category in RocketModuleList.GetRocketModuleList())
                {
                    bool keepCategory = false;
                    foreach(var item in category.Value)
                    {
                        TechItem techItem = Db.Get().TechItems.TryGet(item);
                        if (techItem != null)
                        {
                            if(DebugHandler.InstantBuildMode || Game.Instance.SandboxModeActive || techItem.IsComplete())
                                keepCategory = true;
                            break;
                        }
                        else
                        {
                            keepCategory = true;
                            break;
                        }
                    }
                    var categoryToDisable = __instance.categories.Find(categ => categ.name == category.Key.ToString());
                    if (categoryToDisable != null)
                    {
                        categoryToDisable.SetActive(keepCategory);         
                    }
                }
            }
        }


        
        [HarmonyPatch(typeof(SelectModuleSideScreen))]
        [HarmonyPatch(nameof(SelectModuleSideScreen.SpawnButtons))]
        public static class CategoryPatchTest
        {
            public static List<string> DisabledButtons = new List<string>();

            public static void ToggleCategory(List<string> items)
            {
                foreach(var id in items)
                {
                    if (DisabledButtons.Contains(id))
                    {
                        DisabledButtons.Remove(id);
                    }
                    else
                    {
                        DisabledButtons.Add(id);
                    }
                }
            }
            private static void ClearButtons(SelectModuleSideScreen _this)
            {
                foreach (KeyValuePair<BuildingDef, GameObject> button in _this.buttons)
                    Util.KDestroyGameObject(button.Value);
                for (int index = _this.categories.Count - 1; index >= 0; --index)
                    Util.KDestroyGameObject(_this.categories[index]);
                _this.categories.Clear();
                _this.buttons.Clear();
            }
            public static bool Prefix(SelectModuleSideScreen __instance)
            {
                DisabledButtons.Clear();
                ClearButtons(__instance);
                foreach (var category in RocketModuleList.GetRocketModuleList())
                {
                    if (
                        //category.Key != (int)RocketCategory.uncategorized && 
                        category.Value.Count>0){
                        GameObject categoryGO = Util.KInstantiateUI(__instance.categoryPrefab, __instance.categoryContent, true);
                        categoryGO.name = category.Key.ToString();
                        HierarchyReferences component = categoryGO.GetComponent<HierarchyReferences>();
#if DEBUG
                        Debug.Log("Category:");
                        UIUtils.ListAllChildren(categoryGO.transform);
                        //Debug.Log("HR:");
                        //UIUtils.ListChildrenHR(component);

#endif
                        __instance.categories.Add(categoryGO);

                        var header = component.GetReference<LocText>("label");
                        var copy = header.textStyleSetting;
                        copy.textColor = Color.black;

                        //header.key = ((RocketCategory)category.Key).ToString();
                        //header.enabled = true;
                        var headergo = categoryGO.transform.Find("Header").gameObject;
                        headergo.SetActive(true);
                        //headergo.rectTransform().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 100);

                        var buttonPrefab = __instance.transform.Find("Button").gameObject;



                        var CategoryText = headergo.transform.Find("Label").GetComponent<LocText>();
                        CategoryText.text = ((RocketCategory)category.Key).ToString().ToUpperInvariant();

                        var foldButtonGO = Util.KInstantiateUI(buttonPrefab, headergo.gameObject, true);
                        var foldButton = foldButtonGO.GetComponent<KButton>();

                        var rect = headergo.rectTransform().rect;

                        foldButtonGO.rectTransform().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, rect.width);
                        foldButtonGO.rectTransform().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, rect.height);
                       
                        foldButton.ClearOnClick();
                        foldButton.onClick += () => {
                            ToggleCategory(category.Value);
                            var refresh = typeof(SelectModuleSideScreen).GetMethod("UpdateBuildableStates", BindingFlags.NonPublic | BindingFlags.Instance);
                            refresh.Invoke(__instance, new[] { (System.Object)null });
                        };
                        foldButton.isInteractable = true;
                        var buttonText = foldButtonGO.transform.Find("Label").GetComponent<LocText>();
                        buttonText.text = ((RocketCategory)category.Key).ToString().ToUpperInvariant();
                        buttonText.textStyleSetting = copy;

                        headergo.transform.Find("BG").gameObject.SetActive(false);
                        CategoryText.gameObject.SetActive(false);

                        Transform reference = component.GetReference<Transform>("content");
                    List<GameObject> prefabsWithComponent = Assets.GetPrefabsWithComponent<RocketModuleCluster>();
                        foreach (string str in category.Value)
                        {
                            string id = str;
                            GameObject part = prefabsWithComponent.Find((Predicate<GameObject>)(p => p.PrefabID().Name == id));
                            if ((UnityEngine.Object)part == (UnityEngine.Object)null)
                            {
                                Debug.LogWarning((object)("Found an id [" + id + "] in moduleButtonSortOrder in SelectModuleSideScreen.cs that doesn't have a corresponding rocket part!"));
                            }
                            else
                            {
                                GameObject gameObject2 = Util.KInstantiateUI(__instance.moduleButtonPrefab, reference.gameObject, true);
                                gameObject2.GetComponentsInChildren<Image>()[1].sprite = Def.GetUISprite((object)part).first;
                                LocText componentInChildren = gameObject2.GetComponentInChildren<LocText>();
                                componentInChildren.text = part.GetProperName();
                                componentInChildren.alignment = TextAlignmentOptions.Bottom;
                                componentInChildren.enableWordWrapping = true;
                                gameObject2.GetComponent<MultiToggle>().onClick += (System.Action)(() => __instance.SelectModule(part.GetComponent<Building>().Def));
                                __instance.buttons.Add(part.GetComponent<Building>().Def, gameObject2);

                                BuildingDef selectedModuleReflec = (BuildingDef)typeof(SelectModuleSideScreen).GetField("selectedModuleDef", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);

                                if (selectedModuleReflec != (UnityEngine.Object)null)
                                    __instance.SelectModule(selectedModuleReflec);
                            }
                        }
                        Debug.Log("Category2:");
                        UIUtils.ListAllChildren(categoryGO.transform);
                    }
                }
                var updateMethod = typeof(SelectModuleSideScreen).GetMethod("UpdateBuildableStates", BindingFlags.NonPublic | BindingFlags.Instance);
                    updateMethod.Invoke(__instance, new[] { (System.Object)null });


                return false;
            }
        }
    }
}
