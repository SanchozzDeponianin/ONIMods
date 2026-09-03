using HarmonyLib;
using KMod;
using SanchozzONIMods.Lib;

namespace FabricateItNow
{
    internal sealed class Patches : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            Utils.RegisterLocalization(typeof(STRINGS));
        }

        [HarmonyPatch(typeof(ComplexFabricator), "OnFetchComplete")]
        private static class ComplexFabricator_OnFetchComplete
        {
            private static void Postfix(ComplexFabricator __instance, bool ___queueDirty)
            {
                if (___queueDirty && __instance.duplicantOperated && !__instance.HasWorker)
                    __instance.Sim1000ms(0f);
            }
        }

        [HarmonyPatch(typeof(ResearchCenter), "OnSpawn")]
        private static class ResearchCenter_OnSpawn
        {
            private static EventSystem.IntraObjectHandler<ResearchCenter> OnStorageInteracted = new((component, data) => component.Sim200ms(0f));

            private static void Postfix(ResearchCenter __instance)
            {
                __instance.Subscribe((int)GameHashes.OnStorageInteracted, OnStorageInteracted);
            }
        }
    }
}
