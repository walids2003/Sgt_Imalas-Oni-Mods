﻿using Rockets_TinyYetBig.Behaviours;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rockets_TinyYetBig
{
    class ModAssets
    {
        public static Components.Cmps<DockingManager> Dockables = new Components.Cmps<DockingManager>();

        public static int InnerLimit = 0;
        public static int Rings = 0;
        public class StatusItems
        {
            public static StatusItem RTB_ModuleGeneratorNotPowered;
            public static StatusItem RTB_ModuleGeneratorPowered;
            public static StatusItem RTB_AlwaysActiveOn;
            public static StatusItem RTB_AlwaysActiveOff;

            public static void Register()
            {
                RTB_ModuleGeneratorNotPowered = new StatusItem(
                      "RTB_MODULEGENERATORNOTPOWERED",
                      "BUILDING",
                      string.Empty,
                      StatusItem.IconType.Info,
                      NotificationType.Neutral,
                      false,
                      OverlayModes.Power.ID
                      );
                RTB_ModuleGeneratorPowered = new StatusItem(
                   "RTB_MODULEGENERATORPOWERED",
                   "BUILDING",
                   string.Empty,
                   StatusItem.IconType.Info,
                   NotificationType.Neutral,
                   false,
                   OverlayModes.Power.ID);
                RTB_AlwaysActiveOn = new StatusItem(
                    "RTB_MODULEGENERATORALWAYSACTIVEPOWERED",
                    "BUILDING",
                    string.Empty,
                    StatusItem.IconType.Info,
                    NotificationType.Neutral,
                    false,
                    OverlayModes.Power.ID); 
                RTB_AlwaysActiveOff = new StatusItem(
                     "RTB_MODULEGENERATORALWAYSACTIVENOTPOWERED",
                     "BUILDING",
                     string.Empty,
                     StatusItem.IconType.Info,
                     NotificationType.Neutral,
                     false,
                     OverlayModes.Power.ID);


                RTB_ModuleGeneratorNotPowered.resolveStringCallback = (Func<string, object, string>)((str, data) =>
                {
                    Generator generator = (RTB_ModuleGenerator)data;
                    str = str.Replace("{ActiveWattage}", GameUtil.GetFormattedWattage(0.0f));
                    str = str.Replace("{MaxWattage}", GameUtil.GetFormattedWattage(generator.WattageRating));
                    return str;
                });
                RTB_ModuleGeneratorPowered.resolveStringCallback = (Func<string, object, string>)((str, data) =>
                {
                    Generator generator = (RTB_ModuleGenerator)data;
                    str = str.Replace("{ActiveWattage}", GameUtil.GetFormattedWattage(generator.WattageRating));
                    str = str.Replace("{MaxWattage}", GameUtil.GetFormattedWattage(generator.WattageRating));
                    return str;
                });
                RTB_AlwaysActiveOff.resolveStringCallback = (Func<string, object, string>)((str, data) =>
                {
                    Generator generator = (RTB_ModuleGenerator)data;
                    str = str.Replace("{ActiveWattage}", GameUtil.GetFormattedWattage(0.0f));
                    str = str.Replace("{MaxWattage}", GameUtil.GetFormattedWattage(generator.WattageRating));
                    return str;
                });
                RTB_AlwaysActiveOn.resolveStringCallback = (Func<string, object, string>)((str, data) =>
                {
                    Generator generator = (RTB_ModuleGenerator)data;
                    str = str.Replace("{ActiveWattage}", GameUtil.GetFormattedWattage(generator.WattageRating));
                    str = str.Replace("{MaxWattage}", GameUtil.GetFormattedWattage(generator.WattageRating));
                    return str;
                });

                Debug.Log("Status items initialized");

            }
        }
    }
}
