using System.Reflection;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace CrabsProfit
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
            ModOptions.Reload();
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [HarmonyPatch(typeof(EntityTemplates), nameof(EntityTemplates.ExtendEntityToBasicCreature),
            typeof(EntityTemplates.ExtendEntityToBasicCreatureData))]
        private static class EntityTemplates_ExtendEntityToBasicCreature
        {
            private static void Prefix(EntityTemplates.ExtendEntityToBasicCreatureData data)
            {
                if (data.template.PrefabID() == CrabFreshWaterConfig.ID)
                {
                    data.onDeathDropsID = new[] { CrabFreshWaterShellConfig.ID }.Concat(data.onDeathDropsID);
                    data.onDeathDropsCount = new[] { ModOptions.Instance.AdultShellMass }.Concat(data.onDeathDropsCount);
                }
                else if (data.template.PrefabID() == BabyCrabFreshWaterConfig.ID)
                {
                    data.onDeathDropsID = new[] { CrabFreshWaterShellConfig.ID }.Concat(data.onDeathDropsID);
                    data.onDeathDropsCount = new[] { ModOptions.Instance.BabyShellMass }.Concat(data.onDeathDropsCount);
                }
            }
        }

        [HarmonyPatch(typeof(BabyCrabFreshWaterConfig), nameof(BabyCrabFreshWaterConfig.CreatePrefab))]
        private static class BabyCrabFreshWaterConfig_CreatePrefab
        {
            private static void Postfix(GameObject __result)
            {
                var def = __result.AddOrGetDef<BabyMonitor.Def>();
                def.onGrowDropID = CrabFreshWaterShellConfig.ID;
                def.onGrowDropUnits = ModOptions.Instance.BabyShellMass;
            }
        }

        // подавляем всплывающее
        [HarmonyPatch]
        private static class PopFXManager_SpawnFX
        {
            private static bool Prepare() => TargetMethod() != null;
            private static MethodBase TargetMethod()
            {
                return typeof(PopFXManager).GetOverloadWithMostArguments(nameof(PopFXManager.SpawnFX), false,
                    typeof(Sprite), typeof(Sprite), typeof(string));
            }
            private static bool Prefix(string text, ref PopFX __result)
            {
                if (text == RandomOreConfig.ProperName)
                {
                    __result = null;
                    return false;
                }
                return true;
            }
        }
    }
}
