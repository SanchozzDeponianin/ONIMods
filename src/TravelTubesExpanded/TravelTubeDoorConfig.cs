using TUNING;
using UnityEngine;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;

namespace TravelTubesExpanded
{
    public class TravelTubeDoorConfig : TravelTubeWallBridgeConfig
    {
        public new const string ID = "TravelTubeDoor";

        public override string[] GetRequiredDlcIds() => Utils.GetDlcIds(base.GetRequiredDlcIds());

        public override BuildingDef CreateBuildingDef()
        {
            const string anim = "tube_door_kanim";
            var def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 1,
                height: 1,
                anim: anim,
                hitpoints: BUILDINGS.HITPOINTS.TIER2,
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER2,
                construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER3,
                construction_materials: MATERIALS.PLASTICS,
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1,
                build_location_rule: BuildLocationRule.Tile,
                decor: BUILDINGS.DECOR.PENALTY.TIER0,
                noise: NOISE_POLLUTION.NONE);
            BuildingTemplates.CreateFoundationTileDef(def);
            def.UseStructureTemperature = false;
            def.Overheatable = false;
            def.Floodable = false;
            def.Entombable = false;
            def.ObjectLayer = ObjectLayer.Building;
            def.AudioCategory = "Plastic";
            def.AudioSize = "small";
            def.BaseTimeUntilRepair = -1f;
            def.PermittedRotations = PermittedRotations.R90;
            def.UtilityInputOffset = new CellOffset(0, 0);
            def.UtilityOutputOffset = new CellOffset(0, 2);
            def.SceneLayer = Grid.SceneLayer.BuildingFront;
            def.ForegroundLayer = Grid.SceneLayer.BuildingBack;
            def.LogicInputPorts = DoorConfig.CreateSingleInputPortList(new CellOffset(0, 0));
            PGameUtils.CopySoundsToAnim(anim, "door_external_kanim");
            SoundEventVolumeCache.instance.AddVolume(anim, "Open_DoorInternal", NOISE_POLLUTION.NOISY.TIER2);
            SoundEventVolumeCache.instance.AddVolume(anim, "Close_DoorInternal", NOISE_POLLUTION.NOISY.TIER2);
            def.AddSearchTerms(global::STRINGS.SEARCH_TERMS.TRANSPORT);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            var occupier = go.AddOrGet<SimCellOccupier>();
            occupier.doReplaceElement = true;
            occupier.movementSpeedMultiplier = DUPLICANTSTATS.MOVEMENT_MODIFIERS.PENALTY_3;
            occupier.notifyOnMelt = true;
            go.AddOrGet<BuildingHP>().destroyOnDamaged = true;
            go.AddOrGet<TileTemperature>();
            go.AddOrGet<TravelTubeBridge>();
            go.AddOrGet<OccupyArea>().objectLayers = new ObjectLayer[] { ObjectLayer.TravelTubeConnection };
            go.GetComponent<KPrefabID>().prefabInitFn += OnPrefabInit;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            base.DoPostConfigureComplete(go);
            var door = go.AddOrGet<Door>();
            door.poweredAnimSpeed = 8f;
            door.unpoweredAnimSpeed = 8f;
            door.doorType = Door.DoorType.Internal;
            door.doorOpeningSoundEventName = "Open_DoorInternal";
            door.doorClosingSoundEventName = "Close_DoorInternal";
            go.AddOrGet<AccessControl>().controlEnabled = true;
            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.Door;
            go.AddOrGet<Workable>().workTime = 3f;
            go.GetComponent<KBatchedAnimController>().initialAnim = "closed";
            go.AddOrGet<KBoxCollider2D>();
            Prioritizable.AddRef(go);
            Object.DestroyImmediate(go.GetComponent<BuildingEnabledButton>());
        }

        private static new void OnPrefabInit(GameObject inst)
        {
            foreach (var kbac in inst.GetComponentsInChildrenOnly<KBatchedAnimController>())
            {
                if (kbac.name.Contains("_fg"))
                {
                    AddSecondaryFGKbac(kbac, Grid.SceneLayer.TileMain, "wall_piece_fg");
                    break;
                }
            }
        }

        // извращение изза особенностей рендеринга жыдкостей начиная с У59
        // придётся делить постройку на три слоя (основа с боковинами - в жыже, передник и задник - без жыжы)
        // для этого добавим третий кбак, как дочерний к уже существующему фг кбаку
        // путём трюка с лауеринг. при этом все символы в старом кбаке станут скрытыми
        // нужно скрыть и показать нужные символы
        internal static void AddSecondaryFGKbac(KBatchedAnimController parent, Grid.SceneLayer layer, params HashedString[] symbols)
        {
            parent.SetBlendValue(KBatchedAnimInstanceData.BlendActiveOptions.LiquidVisibilityLayer, false);
            parent.SetBlendValue(KBatchedAnimInstanceData.BlendActiveOptions.WaterProof, true);
            parent.SetFGLayer(layer);
            var layering = parent.GetLayering();
            layering.SetIsLayer(false);
            layering.HideSymbols();
            layering.SetIsLayer(true);
            foreach (var symbol in symbols)
                parent.SetSymbolVisiblity(symbol, true);
            if (layering.layerControllers.TryGetValue(KAnim.SymbolFlags.FG, out var kbas) && kbas is KBatchedAnimController kbac)
            {
                foreach (var symbol in symbols)
                    kbac.SetSymbolVisiblity(symbol, false);
                kbac.SetSceneLayer(layer);
                kbac.SetBlendValue(KBatchedAnimInstanceData.BlendActiveOptions.LiquidVisibilityLayer, false);
                kbac.SetBlendValue(KBatchedAnimInstanceData.BlendActiveOptions.WaterProof, true);
            }
        }
    }
}
