using HarmonyLib;
using UnityEngine;
using Klei.AI;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace AthleticsGenerator
{
    internal sealed class Patches : KMod.UserMod2
    {
        public static AttributeConverter ManualGeneratorPower;

        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit(Harmony harmony)
        {
            Utils.InitLocalization(typeof(STRINGS));
            harmony.PatchAll();
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            var formatter = new StandardAttributeFormatter(GameUtil.UnitClass.Power, GameUtil.TimeSlice.None);
            ManualGeneratorPower = Db.Get().AttributeConverters.Create(nameof(ManualGeneratorPower), "Manual Generator Power",
                STRINGS.DUPLICANTS.ATTRIBUTES.ATHLETICS.POWERMODIFIER, Db.Get().Attributes.Athletics,
                ModOptions.Instance.watts_per_level, 0f, formatter);
        }

        [HarmonyPatch(typeof(ManualGeneratorConfig), nameof(ManualGeneratorConfig.CreateBuildingDef))]
        private static class ManualGeneratorConfig_CreateBuildingDef
        {
            private static void Postfix(BuildingDef __result)
            {
                if (ModOptions.Instance.enable_meter)
                {
                    var kanim = ModOptions.Instance.enable_light ? "generatormanual_meter_light_kanim" : "generatormanual_meter_kanim";
                    PGameUtils.CopySoundsToAnim(kanim, "generatormanual_kanim");
                    __result.AnimFiles[0] = Assets.GetAnim(kanim);
                }
            }
        }

        [HarmonyPatch(typeof(ManualGeneratorConfig), nameof(ManualGeneratorConfig.DoPostConfigureComplete))]
        private static class ManualGeneratorConfig_DoPostConfigureComplete
        {
            private static void Postfix(GameObject go)
            {
                go.AddOrGet<AthleticsGenerator>();
            }
        }
    }
}
