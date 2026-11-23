using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.PatchManager;

namespace OldLiquidReservoir
{
    internal sealed class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit(Harmony harmony)
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            Assets.GetBuildingDef(LiquidReservoirConfig.ID).BuildingComplete.AddOrGet<ReservoirMeterFix>();
        }

        [HarmonyPatch(typeof(Blueprints), MethodType.Constructor)]
        private static class Blueprints_Constructor
        {
            private static void Postfix(Blueprints __instance)
            {
                __instance.skinsReleaseProviders = __instance.skinsReleaseProviders.Append(new Blueprints_OldReservoir());
            }
        }

        // хз почему но при смене скинов метеры разпидорасило
        // возможно изза несовпадения символов в анимациях
        // проще всего просто обновить кбак

        internal static readonly GameHashes BuildingFacadeChanged = (GameHashes)Hash.SDBMLower(nameof(BuildingFacadeChanged));

        [HarmonyPatch(typeof(BuildingFacade), "ChangeBuilding")]
        private static class BuildingFacade_ChangeBuilding
        {
            private static void Postfix(BuildingFacade __instance)
            {
                __instance.Trigger((int)BuildingFacadeChanged);
            }
        }

        [HarmonyPatch(typeof(BuildingFacadeAnimateIn), "OnDisable")]
        private static class BuildingFacadeAnimateIn_OnDisable
        {
            private static void Prefix(KBatchedAnimController ___sourceAnimController)
            {
                if (!___sourceAnimController.IsNullOrDestroyed())
                    ___sourceAnimController.gameObject.Trigger((int)BuildingFacadeChanged);
            }
        }
    }
}
