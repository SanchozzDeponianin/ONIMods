using System.Collections.Generic;
using System.Linq;
using System.Text;
using Database;
using Klei.AI;
using STRINGS;
using HarmonyLib;
using SanchozzONIMods.Lib;
using SanchozzONIMods.Shared;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace MorePlantMutations
{
    internal class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            RotPileSilentNotification.Patch(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
            new POptions().RegisterOptions(this, typeof(ModOptions));
            ModOptions.Reload();
        }

        public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<KMod.Mod> mods)
        {
            try
            {
                ModOptions.BPTOptionsType = PPatchTools.GetTypeSafe("BetterPlantTending.ModOptions", "BetterPlantTending");
                if (ModOptions.Instance.bpt_intergration.enable && ModOptions.BPTOptionsType != null)
                {
                    var instance = Traverse.Create(ModOptions.BPTOptionsType).Property(nameof(ModOptions.Instance)).GetValue();
                    var traverse = Traverse.Create(instance);
                    ModOptions.Instance.glowstick.adjust_radiation_by_grow_speed
                        = traverse.Property<bool>("coldbreather_adjust_radiation_by_grow_speed").Value;
                    ModOptions.Instance.glowstick.decrease_radiation_by_wildness
                        = traverse.Property<bool>("coldbreather_decrease_radiation_by_wildness").Value;
                }
            }
            catch (System.Exception e)
            {
                Utils.LogExcWarn(e);
            }
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            GlowStickMutation.CreateModifiers();
        }

        // если просто добавить аним оверриды, какого то хрена при рендеренге происходит перепутывание
        // текстур между мутантами с клевскими и своими оверридами
        // обходное решение: в кбак каждого мутанта запихивать все возможные аним файлы с оверридами

        private static KAnimFile[] mutant_swap_anims;

        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            var anim_names = new List<string>();
            foreach (var mutation in Db.Get().PlantMutations.resources)
            {
                if (mutation.symbolOverrideInfo != null && mutation.symbolOverrideInfo.Count > 0)
                {
                    foreach (var info in mutation.symbolOverrideInfo)
                    {
                        if (!anim_names.Contains(info.sourceAnim))
                            anim_names.Add(info.sourceAnim);
                    }
                }
            }
            var anims = new List<KAnimFile>();
            foreach (var name in anim_names)
            {
                if (Assets.TryGetAnim(name, out var anim))
                    anims.Add(anim);
            }
            mutant_swap_anims = anims.ToArray();
            // атрибут лучше засунуть в префаб, иначе при загрузке сейфа эффект может не примениться
            foreach (var plant in Assets.GetPrefabsWithComponent<MutantPlant>())
            {
                if (!plant.HasTag(GameTags.Seed) && !plant.HasTag(GameTags.CropSeed) && !plant.HasTag(GameTags.Compostable)
                    && plant.TryGetComponent(out Modifiers modifiers)
                    && !modifiers.initialAttributes.Contains(GlowStickMutation.EmitRadsMultiplier.Id))
                {
                    modifiers.initialAttributes.Add(GlowStickMutation.EmitRadsMultiplier.Id);
                }
            }
        }

        [HarmonyPatch(typeof(PlantMutation), nameof(PlantMutation.ApplyVisualTo))]
        private static class PlantMutation_ApplyVisualTo
        {
            private static void Prefix(PlantMutation __instance, MutantPlant target)
            {
                if (__instance.symbolOverrideInfo != null && __instance.symbolOverrideInfo.Count > 0
                    && target.TryGetComponent(out KBatchedAnimController kbac))
                {
                    var anims = kbac.AnimFiles;
                    foreach (var swap in mutant_swap_anims)
                    {
                        if (!anims.Contains(swap))
                            anims = anims.Append(swap);
                    }
                    kbac.SwapAnims(anims);
                }
                if (__instance.Id == "GlowStick")
                    target.FindOrAdd<GlowStickMutation>();
            }
        }

        [HarmonyPatch(typeof(PlantMutation), nameof(PlantMutation.GetTooltip))]
        private static class PlantMutation_GetTooltip
        {
            private static StringBuilder builder = new();
            private static void Postfix(PlantMutation __instance, ref string __result)
            {
                if (__instance.Id == "GlowStick")
                {
                    builder.Append(__result);
                    builder.Append(DUPLICANTS.TRAITS.TRAIT_DESCRIPTION_LIST_ENTRY);
                    builder.Append(DUPLICANTS.TRAITS.GLOWSTICK.SHORT_DESC_TOOLTIP);
                    __result = builder.ToString();
                    builder.Clear();
                }
            }
        }

        // радиационная анимация на каждой ветке это слишком
        [HarmonyPatch(typeof(PlantMutation), nameof(PlantMutation.CreateFXObject))]
        private static class PlantMutation_CreateFXObject
        {
            private static bool Prefix(MutantPlant target, string anim)
            {
                return !(target.HasTag(GameTags.PlantBranch) && anim == "more_mutate_rad_fx_kanim");
            }
        }

        private const string old_snaps_anim = "mutate_snaps_kanim";
        private const string new_snaps_anim = "more_mutate_snaps_kanim";

        [HarmonyPatch(typeof(PlantMutations), MethodType.Constructor)]
        [HarmonyPatch(new System.Type[] { typeof(ResourceSet) })]
        private static class PlantMutations_Constructor
        {
            private static void Postfix(PlantMutations __instance)
            {
                PlantMutation Ginger = __instance.AddPlantMutation(nameof(Ginger))
                    .AttributeModifier(Db.Get().PlantAttributes.MinRadiationThreshold, 250f, false)
                    .AttributeModifier(Db.Get().PlantAttributes.WiltTempRangeMod, -0.7f, true)
                    .AttributeModifier(Db.Get().PlantAttributes.HarvestTime, 1f, true)
                    .BonusCrop(GingerConfig.ID, 2f)
                    .VisualSymbolOverride("snapTo_mutate1", new_snaps_anim, "ginger_mutate1")
                    .VisualSymbolOverride("snapTo_mutate2", new_snaps_anim, "ginger_mutate2")
                    .VisualSymbolScale("snapTo_mutate1", 0.90f);

                PlantMutation Grassy = __instance.AddPlantMutation(nameof(Grassy))
                    .RestrictPrefabID(GasGrassConfig.ID)
                    .RestrictPrefabID(GasGrassConfig.SEED_ID)
                    .AttributeModifier(Db.Get().PlantAttributes.MinRadiationThreshold, 250f, false)
                    .AttributeModifier(Db.Get().PlantAttributes.FertilizerUsageMod, 0.05f, true)
                    .BonusCrop(PlantFiberConfig.ID, 20f)
                    .VisualSymbolOverride("snapTo_mutate1", new_snaps_anim, "grass_mutate1")
                    .VisualSymbolOverride("snapTo_mutate2", new_snaps_anim, "grass_mutate2")
                    .VisualSymbolScale("snapTo_mutate1", 0.75f);

                PlantMutation GlowStick = __instance.AddPlantMutation(nameof(GlowStick))
                    .AttributeModifier(Db.Get().PlantAttributes.FertilizerUsageMod, 0.25f, true)
                    .VisualSymbolOverride("snapTo_mutate1", old_snaps_anim, "rad_mutate1")
                    .VisualSymbolOverride("snapTo_mutate2", old_snaps_anim, "rad_mutate2")
                    .VisualFGFX("more_mutate_rad_fx_kanim");

                __instance.rottenHeaps
                    .VisualSymbolOverride("snapTo_mutate1", new_snaps_anim, "rot_mutate1")
                    .VisualSymbolOverride("snapTo_mutate2", new_snaps_anim, "rot_mutate2")
                    .VisualSymbolScale("snapTo_mutate1", 0.65f)
                    .VisualSymbolScale("snapTo_mutate2", 0.70f);
            }
        }
    }
}
