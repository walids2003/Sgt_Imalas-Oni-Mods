﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Rockets_TinyYetBig.Patches
{
    class PetrolRocketUseAllCombustiblesPatch
    {
        [HarmonyPatch(typeof(KeroseneEngineClusterConfig), "DoPostConfigureComplete")]
        public static class ReplaceFuelTagInPetrolRocket
        {
            public static void Postfix(GameObject go)
            {
                if (Config.Instance.EthanolEngines) { 
                RocketEngineCluster rocketEngineCluster = go.GetComponent<RocketEngineCluster>();
                rocketEngineCluster.fuelTag = GameTags.CombustibleLiquid;
                }
            }

        }
        [HarmonyPatch(typeof(KeroseneEngineClusterSmallConfig), "DoPostConfigureComplete")]
        public static class ReplaceFuelTagInSmallPetrolRocket
        {
            public static void Postfix(GameObject go)
            {
                if (Config.Instance.EthanolEngines)
                {
                    RocketEngineCluster rocketEngineCluster = go.GetComponent<RocketEngineCluster>();
                    rocketEngineCluster.fuelTag = GameTags.CombustibleLiquid;
                    FuelTank tank = go.GetComponent<FuelTank>();
                    tank.FuelType = GameTags.CombustibleLiquid;
                }
            }

        }
        [HarmonyPatch(typeof(LiquidFuelTankClusterConfig), nameof(LiquidFuelTankClusterConfig.DoPostConfigureComplete))]
        public static class ReplaceFuelTagInBigFuelTankRocket
        {
            public static void Postfix(GameObject go)
            {
                if (Config.Instance.EthanolEngines)
                {
                    var consumer = go.GetComponent<ConduitConsumer>();
                    consumer.capacityTag = ModAssets.Tags.RocketFuelTag;
                    UnityEngine.Object.Destroy(go.GetComponent<ManualDeliveryKG>());
                }
            }

        }
        [HarmonyPatch(typeof(Localization))]
        [HarmonyPatch("Initialize")]
        public static class StringReplacementPetroleum
        {
            public static void Prefix()
            {
                if (Config.Instance.EthanolEngines)
                {
                    global::STRINGS.BUILDINGS.PREFABS.KEROSENEENGINECLUSTER.EFFECT = STRINGS.MODIFIEDVANILLASTRINGS.KEROSENEENGINECLUSTER_EFFECT;
                    global::STRINGS.BUILDINGS.PREFABS.KEROSENEENGINECLUSTERSMALL.EFFECT = STRINGS.MODIFIEDVANILLASTRINGS.KEROSENEENGINECLUSTERSMALL_EFFECT;
                }
                
            }

        }
    }
}
//