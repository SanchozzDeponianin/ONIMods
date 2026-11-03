using UnityEngine;
using HarmonyLib;
using PeterHan.PLib.Core;

namespace SanchozzONIMods.Shared
{
    // при уничтожении объекта содержимое хранилища - газ и жидкость
    // оставить в виде бутылок
    public static class StorageOnDestroyPatch
    {
        private const string PATCH_KEY = "Patch.Storage.OnQueueDestroyObject.DropAll";

        public static Tag DoNotDumpGas = TagManager.Create(nameof(DoNotDumpGas));
        public static Tag DoNotDumpLiquid = TagManager.Create(nameof(DoNotDumpLiquid));

        public static void Patch(Harmony harmony)
        {
            if (!PRegistry.GetData<bool>(PATCH_KEY))
            {
                var method = typeof(Storage).GetOverloadWithMostArguments(nameof(Storage.DropAll), false, typeof(Vector3));
                if (method == null)
                {
                    PUtil.LogWarning("Method not found 'Storage.DropAll(Vector3 ...)'");
                }
                else
                {
                    harmony.Patch(method, prefix: new HarmonyMethod(typeof(StorageOnDestroyPatch), nameof(DropAll)));
                    PRegistry.PutData(PATCH_KEY, true);
                }
            }
        }

        private static void DropAll(Storage __instance, ref bool vent_gas, ref bool dump_liquid)
        {
            if (__instance.IsEndOfLife())
            {
                if (vent_gas && __instance.HasTag(DoNotDumpGas))
                    vent_gas = false;
                if (dump_liquid && __instance.HasTag(DoNotDumpLiquid))
                    dump_liquid = false;
            }
        }
    }
}
