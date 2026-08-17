using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;

namespace ControlYourRobots
{
    [HarmonyPatch]
    public class RobotLauncher : ComplexFabricator
    {
        public override void OnPrefabInit()
        {
            base.OnPrefabInit();
            // сделаем вызовы модифицированных ISim*** через вспомогательный класс
            autoRegisterSimRender = false;
            fetchChoreTypeIdHash = Db.Get().ChoreTypes.FetchCritical.IdHash;
            SideScreenRecipeScreenTitle = STRINGS.UI.UISIDESCREENS.RAILGUN_SIDESCREEN.RECIPE_DETAILS;
            SideScreenSubtitleLabel = STRINGS.UI.UISIDESCREENS.RAILGUN_SIDESCREEN.SUBTITLE;
        }

        // всю работу делает RailGun
        public override List<GameObject> SpawnOrderProduct(ComplexRecipe recipe) => null;

        // если RailGun сделал работу, то прогресс 100%
        public override float ComputeWorkProgress(float dt, ComplexRecipe recipe)
        {
            return buildStorage.Count > 0 ? 0f : 1f;
        }

        // не дёргать операцыональ
        [HarmonyReversePatch(HarmonyReversePatchType.Original)]
        [HarmonyPatch(typeof(ComplexFabricator), nameof(ComplexFabricator.Sim200ms))]
        public new void Sim200ms(float dt)
        {
#pragma warning disable CS8321
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
                PPatchTools.RemoveMethodCall(instructions,
                    PPatchTools.GetMethodSafe(typeof(Operational), nameof(Operational.SetActive), false, typeof(bool), typeof(bool)));
#pragma warning restore CS8321
            base.Sim200ms(dt);
        }

        // требовать тэг спящего гобота
        [HarmonyReversePatch(HarmonyReversePatchType.Original)]
        [HarmonyPatch(typeof(ComplexFabricator), nameof(ComplexFabricator.UpdateFetches))]
        internal new void UpdateFetches(DictionaryPool<Tag, float, ComplexFabricator>.PooledDictionary missingAmounts)
        {
#pragma warning disable CS8321
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) =>
                    instructions.Transpile(original, RequireTag);
#pragma warning restore CS8321
            base.UpdateFetches(missingAmounts);
        }

        private static bool RequireTag(ref List<CodeInstruction> instructions)
        {
            var newobj = typeof(FetchList2).GetConstructors()[0];
            var submit = typeof(FetchList2).GetMethodSafe(nameof(FetchList2.Submit), false, PPatchTools.AnyArguments);
            var inject = typeof(RobotLauncher).GetMethodSafe(nameof(RequireTag), true, typeof(FetchList2));
            if (newobj == null || submit == null || inject == null)
                return false;
            int i = instructions.FindIndex(inst => inst.Is(OpCodes.Newobj, newobj));
            if (i == -1 || !instructions[i + 1].IsStloc())
                return false;
            int j = instructions.FindIndex(inst => inst.Calls(submit));
            if (j == -1)
                return false;
            instructions.Insert(j++, instructions[i + 1].GetMatchingLoadInstruction());
            instructions.Insert(j++, new CodeInstruction(OpCodes.Call, inject));
            return true;
        }

        private static void RequireTag(FetchList2 fetchList)
        {
            for (int i = 0; i < fetchList.FetchOrders.Count; i++)
                fetchList.FetchOrders[i].RequiredTag = Patches.RobotSuspend;
        }
    }

    public class RobotLauncherSM : ComplexFabricatorSM, ISim200ms, ISim1000ms
    {
        public override void OnPrefabInit()
        {
            base.OnPrefabInit();
            simRenderLoadBalance = true;
        }

        // подавить smi чтобы не дрюкать анимации
        public override void OnSpawn() { }

        public void Sim1000ms(float dt) => fabricator.Sim1000ms(dt);

        public void Sim200ms(float dt) => (fabricator as RobotLauncher).Sim200ms(dt);
    }
}
