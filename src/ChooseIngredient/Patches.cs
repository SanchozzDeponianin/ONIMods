using System;
using System.Linq;
using STRINGS;
using TUNING;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace ChooseIngredient
{
    internal class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
            ModOptions.Reload();
        }

        internal static Type PipedEverythingConsumerS;

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
            PipedEverythingConsumerS = PPatchTools.GetTypeSafe("PipedEverything.ConduitConsumerOptionalSolid", "PipedEverything");
        }

        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            TagManager.Create(GameTags.Filter.Name, MISC.TAGS.FILTER);
            TagManager.Create(GameTags.IceOre.Name, MISC.TAGS.ICEORE);
            foreach (var id in new[]
            {
                EthanolDistilleryConfig.ID,
                WoodGasGeneratorConfig.ID,
                CampfireConfig.ID ,
                AirFilterConfig.ID,
                WaterPurifierConfig.ID,
                IceCooledFanConfig.ID,
            })
            {
                var go = Assets.GetBuildingDef(id).BuildingComplete;
                go.AddOrGet<Storage>().storageFullMargin = STORAGE.STORAGE_LOCKER_FILLED_MARGIN;
                var filterable = go.AddOrGet<TreeFilterable>();
                filterable.dropIncorrectOnFilterChange = true;
                filterable.preventAutoAddOnDiscovery = true;
                filterable.filterByStorageCategoriesOnSpawn = false;
                filterable.autoSelectStoredOnLoad = false;
                filterable.copySettingsEnabled = false;
                filterable.uiHeight = TreeFilterable.UISideScreenHeight.Short;
                var requested_tag = go.AddOrGet<ManualDeliveryKG>().RequestedItemTag;
                var flat = go.AddOrGet<FlatTagFilterable>();
                flat.headerText = requested_tag.ProperNameStripLink();
                flat.displayOnlyDiscoveredTags = !ModOptions.Instance.show_all;
                var tags = ElementLoader.FindElements(e => e.HasTag(requested_tag)).Select(e => e.tag);
                flat.tagOptions.AddRange(tags);
                if (ModOptions.Instance.allow_all)
                    flat.selectedTags.AddRange(tags);
                go.AddOrGet<MDKGChooser>();
                go.AddOrGet<CopyBuildingSettings>();
            }
        }
    }
}
