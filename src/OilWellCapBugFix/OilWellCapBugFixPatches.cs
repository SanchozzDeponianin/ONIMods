using KMod;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Options;

namespace OilWellCapBugFix
{
    public sealed class OilWellCapBugFixPatches : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PPatchManager(harmony).RegisterPatchClass(typeof(OilWellCapBugFixPatches));
            new POptions().RegisterOptions(this, typeof(OilWellCapBugFixOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.OnStartGame)]
        private static void OnStartGame()
        {
            OilWellCapBugFixOptions.Reload();
        }

        [HarmonyPatch(typeof(OilWellCap), "OnPrefabInit")]
        internal static class OilWellCap_OnPrefabInit
        {
            private static void Postfix(OilWellCap __instance)
            {
                if (OilWellCapBugFixOptions.Instance.AllowDepressurizeWhenOutOfWater)
                {
                    var consumer = __instance.GetComponent<ConduitConsumer>();
                    if (consumer != null)
                        consumer.forceAlwaysSatisfied = true;
                }
            }
        }

        // модифицируем задачу, так как будто бы она создавалась с параметром only_when_operational = true
        [HarmonyPatch(typeof(OilWellCap), "CreateWorkChore")]
        internal static class OilWellCap_CreateWorkChore
        {
            private static readonly IDetouredField<WorkChore<OilWellCap>, bool> OnlyWhenOperational =
                PDetours.DetourFieldLazy<WorkChore<OilWellCap>, bool>(nameof(WorkChore<OilWellCap>.onlyWhenOperational));

            private static void Postfix(WorkChore<OilWellCap> __result, Operational ___operational)
            {
                if (!__result.onlyWhenOperational)
                {
                    __result.AddPrecondition(ChorePreconditions.instance.IsOperational, ___operational);
                    var deconstructable = __result.GetComponent<Deconstructable>();
                    if (deconstructable != null)
                        __result.AddPrecondition(ChorePreconditions.instance.IsNotMarkedForDeconstruction, deconstructable);
                    var enabledButton = __result.GetComponent<BuildingEnabledButton>();
                    if (enabledButton != null)
                        __result.AddPrecondition(ChorePreconditions.instance.IsNotMarkedForDisable, enabledButton);
                    OnlyWhenOperational.Set(__result, true);
                }
            }
        }
    }
}
