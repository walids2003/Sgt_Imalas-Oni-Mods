﻿using KSerialization;
using Rockets_TinyYetBig.Behaviours;
using Rockets_TinyYetBig.Elements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UtilLibs;
using static KAnim;
using static Rockets_TinyYetBig.ModAssets;
using static Rockets_TinyYetBig.STRINGS.UI_MOD.CLUSTERMAPROCKETSIDESCREEN;

namespace Rockets_TinyYetBig.SpaceStations
{
    class SpaceStation : Clustercraft, ISim4000ms
    {
        [Serialize]
        public string l_name = "Space Station";

        [Serialize]
        public int SpaceStationInteriorId = -1;

        [Serialize]
        public int _currentSpaceStationType = 0;

        public SpaceStationWithStats CurrentSpaceStationType => ModAssets.SpaceStationTypes[_currentSpaceStationType];

        [Serialize]
        public int IsOrbitalSpaceStationWorldId = -1;
        [Serialize]
        public bool IsDeconstructable = true;
        [Serialize]
        public bool BuildableInterior = true;
        [Serialize]
        public bool ShouldDrawBarriers = true;
        [Serialize]
        public bool IsDerelict = false;


        [Serialize]
        public bool Upgradeable = true;
        [Serialize]
        public int spaceStationLevelUnlock = 0;

        [Serialize]
        public Vector2I bottomLeftCorner;
        [Serialize]
        public Vector2I topRightCorner;

        public Vector2I InteriorSize = new Vector2I(102, 103);
        // public string InteriorTemplate = "emptySpacefor100"; 
        public string InteriorTemplate = Path.Combine("interiors","emptySpaceStationPrefab"); 

        public string ClusterAnimName = "space_station_small_kanim";
        public string InitialAnimName = "idle_loop";
        //public string IconAnimName = "station_3";

        public override List<AnimConfig> AnimConfigs => new List<AnimConfig>
        {
            new AnimConfig
            {
                animFile = Assets.GetAnim(ClusterAnimName),
                initialAnim = InitialAnimName
            }
        };

        public override string Name => this.l_name;
        //public override bool IsVisible => true;
        public override EntityLayer Layer => EntityLayer.POI;
        public override bool SpaceOutInSameHex() => false;
        public override ClusterRevealLevel IsVisibleInFOW => ClusterRevealLevel.Visible;
        //public override Sprite GetUISprite() => Assets.GetSprite("rocket_landing"); //Def.GetUISprite((object)this.gameObject).first;
        public override Sprite GetUISprite()
        {
            return Def.GetUISpriteFromMultiObjectAnim(AnimConfigs[0].animFile);
        }

        //public Sprite GetUISpriteAt(int i) => Def.GetUISpriteFromMultiObjectAnim(AnimConfigs[i].animFile);



        public static void SpawnNewSpaceStation(AxialI location)
        {
            if (!SpaceStationManager.Instance.CanConstructSpaceStation() || SpaceStationManager.IsSpaceStationAt(location) ||!SpaceStationManager.CanBuildStationAt(location))
                return;

            Vector3 position = new Vector3(-1f, -1f, 0.0f);
            GameObject sat = Util.KInstantiate(Assets.GetPrefab((Tag)SpaceStationConfig.ID), position);
            sat.SetActive(true);
            var spaceStation = sat.GetComponent<SpaceStation>();
            spaceStation.Location = location;
        }

        public override void OnSpawn()
        {
            //SgtLogger.debuglog("MY WorldID:" + SpaceStationInteriorId);
            if (SpaceStationInteriorId < 0)
            {
                var interiorWorld = SpaceStationManager.Instance.CreateSpaceStationInteriorWorld(gameObject,  InteriorTemplate, InteriorSize, BuildableInterior, null, Location);
                SpaceStationInteriorId = interiorWorld.id;
                SgtLogger.debuglog("new WorldID:" + SpaceStationInteriorId);
                SgtLogger.debuglog("ADDED NEW SPACE STATION INTERIOR");
            }
            if (!RTB_SavegameStoredSettings.Instance.StationInteriorWorlds.Contains(SpaceStationInteriorId))
                RTB_SavegameStoredSettings.Instance.StationInteriorWorlds.Add(SpaceStationInteriorId);
            base.OnSpawn();

            var world = ClusterManager.Instance.GetWorld(SpaceStationInteriorId);

            world.AddTag(ModAssets.Tags.IsSpaceStation);
            if(IsDerelict)
                world.AddTag(ModAssets.Tags.IsDerelict);
            if(!BuildableInterior)
                world.AddTag(ModAssets.Tags.NoBuildingAllowed);

            this.SetCraftStatus(CraftStatus.InFlight);

            var destinationSelector = gameObject.GetComponent<RocketClusterDestinationSelector>();
            destinationSelector.SetDestination(this.Location);
            var planet = ClusterGrid.Instance.GetVisibleEntityOfLayerAtAdjacentCell(this.Location, EntityLayer.Asteroid);
            if (planet != null)
            {
                IsOrbitalSpaceStationWorldId = planet.GetComponent<WorldContainer>().id;
            }

            var m_clusterTraveler = gameObject.GetComponent<ClusterTraveler>();
            m_clusterTraveler.getSpeedCB = new Func<float>(this.GetSpeed);
            m_clusterTraveler.getCanTravelCB = new Func<bool, bool>(this.CanTravel);
            m_clusterTraveler.onTravelCB = (System.Action)null;
            m_clusterTraveler.validateTravelCB = null;


            this.Subscribe<SpaceStation>(1102426921, NameChangedHandler);
            if(ShouldDrawBarriers)
                DrawBarriers();
        }
        private static EventSystem.IntraObjectHandler<SpaceStation> NameChangedHandler = new EventSystem.IntraObjectHandler<SpaceStation>((System.Action<SpaceStation, object>)((cmp, data) => cmp.SetStationName(data)));
        public void SetStationName(object newName)
        {
            SetStationName((string)newName);
        }

        public void SetStationName(string newName)
        {
            l_name = newName;
            base.name = "Space Station: " + newName;
            ClusterManager.Instance.Trigger(1943181844, newName);
        }
        private bool CanTravel(bool tryingToLand) => !tryingToLand;
        private float GetSpeed() => 1f;
        public void DestroySpaceStation()
        {
            this.SetExploding();
            SpaceStationManager.Instance.DestroySpaceStationInteriorWorld(this.SpaceStationInteriorId);
            UnityEngine.Object.Destroy(this.gameObject);
        }



        public void ClearAllBarriers(WorldContainer world, ref HashSet<int> cells)
        {
            for (var x = 0; x < world.WorldSize.x; x++)
            {
                for (var y = 0; y < world.WorldSize.y; y++)
                {
                    var cell = Grid.XYToCell(world.WorldOffset.x +x , world.WorldOffset.y + y);
                    if (Grid.Element[cell].id == ModElements.SpaceStationForceField.SimHash)
                    {
                        SimMessages.ReplaceElement(cell, SimHashes.Vacuum, null, 0);
                        cells.Add(cell);
                    }
                    Grid.Visible[cell] = byte.MaxValue;
                    Grid.PreventFogOfWarReveal[cell] = false;
                }
            }
        }

        public void DrawOuterBarriers(WorldContainer world)
        {

            // below world
            for (var x = 0; x < world.WorldSize.x; x++)
            {
                var cell = Grid.XYToCell(world.WorldOffset.x + x, world.WorldOffset.y);
                SimMessages.ReplaceElement(cell, ModElements.SpaceStationForceField.SimHash, null, 20000);
                Grid.Visible[cell] = 0;
                Grid.PreventFogOfWarReveal[cell] = true;
            }

            // left of world
            for (var y = 0; y < world.WorldSize.y; y++)
            {
                var cell = Grid.XYToCell(world.WorldOffset.x, world.WorldOffset.y + y);
                SimMessages.ReplaceElement(cell, ModElements.SpaceStationForceField.SimHash, null, 20000);
                Grid.Visible[cell] = 0;
                Grid.PreventFogOfWarReveal[cell] = true;
            }

            // right of world
            for (var y = 0; y < world.WorldSize.y; y++)
            {
                var cell = Grid.XYToCell(world.WorldOffset.x + world.WorldSize.x-1, world.WorldOffset.y + y);
                SimMessages.ReplaceElement(cell, ModElements.SpaceStationForceField.SimHash, null, 20000);
                Grid.Visible[cell] = 0;
                Grid.PreventFogOfWarReveal[cell] = true;
            }
        }

        const int lvl1Width = 40;
        const int lvl2Width = 70;
        const int lvl3Width = 100;

        public void DrawLeveledBarriers(WorldContainer world, int borderSize, bool locking = true)
        {

            bottomLeftCorner = new Vector2I(-borderSize/2 + (world.Width/2), -borderSize / 2 + (world.Height/2));
            topRightCorner = new Vector2I((borderSize/2) + (world.Width/2) - 1, (borderSize / 2 ) + (world.Height/2) - 1);

            SgtLogger.l($"Size: {borderSize} BottomLeft:  {bottomLeftCorner}, TopRight: {topRightCorner}");

            for (var x = 0; x < world.WorldSize.x; x++)
            {
                for (int y = 0; y < world.WorldSize.y; y++)
                {
                    var cell = Grid.XYToCell(world.WorldOffset.x + x, world.WorldOffset.y + y);
                    if (x < bottomLeftCorner.x || x > topRightCorner.x || y < bottomLeftCorner.y || y > topRightCorner.y)
                    {
                        SimMessages.ReplaceElement(cell, locking ? ModElements.SpaceStationForceField.SimHash : SimHashes.Vacuum, null, locking? 20000 : 0);
                        Grid.Visible[cell] = locking ? byte.MinValue : byte.MaxValue;
                        Grid.PreventFogOfWarReveal[cell] = locking;
                    }
                    else
                    {
                        Grid.Visible[cell] = byte.MaxValue;
                        Grid.PreventFogOfWarReveal[cell] = false;
                    }

                }
            }
        }

        int counter = 0;
        public new void Sim4000ms(float dt)
        {
            return;
            counter++;
            counter %= 9;

            if(counter==0)
            {
                var world = ClusterManager.Instance.GetWorld(SpaceStationInteriorId);
                DrawLeveledBarriers(world, lvl1Width);
            }
            if (counter == 3)
            {
                var world = ClusterManager.Instance.GetWorld(SpaceStationInteriorId);
                DrawLeveledBarriers(world, lvl1Width,false);
                DrawLeveledBarriers(world, lvl2Width);                
            }
            if(counter == 6)
            {

                var world = ClusterManager.Instance.GetWorld(SpaceStationInteriorId);
                DrawLeveledBarriers(world, lvl2Width, false);
                DrawLeveledBarriers(world, lvl3Width);
               // DrawOuterBarriers(world);
            }            
        }

        public void DrawBarriers()
        {
            if (!ShouldDrawBarriers)
                return;

            if (SpaceStationInteriorId == -1)
            {
                SgtLogger.l("No world for space station found, id is -1");
                return;
            }
            var world = ClusterManager.Instance.GetWorld(SpaceStationInteriorId);
            DrawOuterBarriers(world);
            DrawLeveledBarriers(world, lvl1Width);
        }

        public override void OnCleanUp()
        {
            if (RTB_SavegameStoredSettings.Instance.StationInteriorWorlds.Contains(SpaceStationInteriorId))
                RTB_SavegameStoredSettings.Instance.StationInteriorWorlds.Remove(SpaceStationInteriorId);
            base.OnCleanUp();
        }

    }
}
