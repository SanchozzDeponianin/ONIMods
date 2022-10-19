using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Database;
using KSerialization;
using HarmonyLib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;

namespace SanchozzONIMods.Lib
{
    /*
    компоненты для обхода проблемы отсутсвия должных проверок в игре при загрузке приправленной специями еды.
    если мод добавляющий новую специю, отключен - невозможно загрузить сейф, игра вылетает.
    для обхода проблемы сохраняем id новых специй в отдельном KMonoBehaviour.
    удаляем их из Edible перед записью сейфа и восстанавливаем после.
    пришлось вмешаться в сериализацию.
    */
    public sealed class ModdedSpicesSerializationManager : PForwardedComponent
    {
        [SerializationConfig(MemberSerialization.OptIn)]
        public class ModdedSpices : KMonoBehaviour
        {
            [Serialize]
            public HashSet<Tag> spices = new HashSet<Tag>();

            [OnDeserialized]
            private void OnDeserialized()
            {
                spices.RemoveWhere(spice => !SpiceGrinder.SettingOptions.ContainsKey(spice));
            }
        }

        private static readonly Version VERSION = new Version(1, 0, 0, 0);
        public override Version Version => VERSION;

        internal static ModdedSpicesSerializationManager Instance { get; private set; }
        private Harmony harmony;
        private HashSet<string> RegisteredSpices;
        private HashSet<string> KleiSpices;

        public void RegisterModdedSpices(params string[] spice_ids)
        {
            if (spice_ids == null)
                throw new ArgumentNullException(nameof(spice_ids));
            RegisterForForwarding();
            var shared = GetSharedData(new HashSet<string>());
            foreach (var spice_id in spice_ids)
            {
                if (!shared.Add(spice_id))
                    PUtil.LogWarning($"Registered dublicate spice id '{spice_id}'");
            }
            SetSharedData(shared);
        }

        public override void Initialize(Harmony plibInstance)
        {
            Instance = this;
            harmony = plibInstance;
            harmony.Patch(typeof(Db), nameof(Db.PostProcess), postfix: PatchMethod(nameof(Db_PostProcess_Postfix)));
        }

        private void InitializeLater()
        {
            var db_spices = Db.Get().Spices;
            KleiSpices = new HashSet<string>();
            foreach (var field in typeof(Spices).GetFields())
            {
                if (field.FieldType == typeof(Spice))
                    KleiSpices.Add(((Spice)field.GetValue(db_spices)).Id);
            }
            RegisteredSpices = GetSharedData(new HashSet<string>());
            RegisteredSpices.RemoveWhere(id => KleiSpices.Contains(id)); // на всякий
            // отложенный патчинг
            harmony.Patch(typeof(Manager).GetMethodSafe(nameof(Manager.GetType), true, typeof(string)),
                prefix: PatchMethod(nameof(Manager_GetType_Prefix)));
            harmony.Patch(typeof(SerializationTemplate), nameof(SerializationTemplate.SerializeData),
                prefix: PatchMethod(nameof(SerializationTemplate_SerializeData_Prefix)),
                postfix: PatchMethod(nameof(SerializationTemplate_SerializeData_Postfix)));
            harmony.Patch(typeof(Edible), "OnSpawn", prefix: PatchMethod(nameof(Edible_OnSpawn_Prefix)));
            harmony.Patch(typeof(Edible), nameof(Edible.SpiceEdible), prefix: PatchMethod(nameof(Edible_SpiceEdible_Prefix)));
            //harmony.Patch(typeof(Edible), nameof(Edible.CanAbsorb), prefix: PatchMethod(nameof(Edible_CanAbsorb_Prefix)));
        }

        // добавляем компонент во всю еду
        private static void Db_PostProcess_Postfix()
        {
            foreach (var go in Assets.GetPrefabsWithComponent<Edible>())
            {
                go.AddOrGet<ModdedSpices>();
            }
            Instance?.InitializeLater();
        }

        // для правильной десереализации, если есть несколько разных модов с нашим компонентом
        private static bool Manager_GetType_Prefix(ref Type __result, string type_name)
        {
            if (type_name == typeof(ModdedSpices).GetKTypeString())
            {
                __result = typeof(ModdedSpices);
                return false;
            }
            return true;
        }

        // убираем моддовые специи перед записью в сейф
        private static void SerializationTemplate_SerializeData_Prefix(SerializationTemplate __instance, object obj)
        {
            if (__instance.serializableType == typeof(Edible))
            {
                Instance?.HideModdedSpices((Edible)obj);
            }
        }

        // восстанавливаем моддовые специи после записи в сейф
        private static void SerializationTemplate_SerializeData_Postfix(SerializationTemplate __instance, object obj)
        {
            if (__instance.serializableType == typeof(Edible))
            {
                Instance?.RestoreModdedSpices((Edible)obj);
            }
        }

        private static void Edible_OnSpawn_Prefix(Edible __instance, ref List<SpiceInstance> ___spices)
        {
            if (___spices == null)
                ___spices = new List<SpiceInstance>();
            // вычищаем отсутсвующие специи
            ___spices.RemoveAll(spice => !SpiceGrinder.SettingOptions.ContainsKey(spice.Id));
            // восстанавливаем моддовые специи при загрузке сейфа
            Instance?.RestoreModdedSpices(__instance);
        }

        private static void Edible_SpiceEdible_Prefix(Edible __instance, SpiceInstance spice)
        {
            Instance?.AddModdedSpice(__instance, spice);
        }

        // дополнительный патч - переопределяем странную проверку клеев по поводу слияния кусков еды со специями
        // ляяя!!! есть второй косяк в EntitySplitter.CanFirstAbsorbSecond
        // там одна из двух Edible сравнивается сама с собой
        // пока отложим
        /*
        private static bool Edible_CanAbsorb_Prefix(Edible __instance, Edible other, ref bool __result)
        {
            PUtil.LogDebug($"Edible_CanAbsorb_Prefix {__instance} {other}");
            var spicesA = SPICES.Get(__instance);
            var spicesB = SPICES.Get(other);

            PUtil.LogDebug(__instance);
            foreach (var x in spicesA)
                PUtil.LogDebug(x.Id);
            PUtil.LogDebug(other);
            foreach (var x in spicesB)
                PUtil.LogDebug(x.Id);

            if (spicesA.Count == spicesB.Count)
                __result = spicesA.TrueForAll(spiceA => spicesB.Exists(spiceB => spiceA.Id == spiceB.Id));
            else
                __result = false;

            PUtil.LogDebug($"__result = {__result}");
            return false;
        }*/

        private static readonly IDetouredField<Edible, List<SpiceInstance>> SPICES = PDetours.DetourField<Edible, List<SpiceInstance>>("spices");

        private void AddModdedSpice(Edible edible, SpiceInstance spice)
        {
            if (edible != null && edible.TryGetComponent<ModdedSpices>(out var moddedSpices))
            {
                if (RegisteredSpices.Contains(spice.Id.Name))
                {
                    moddedSpices.spices.Add(spice.Id);
                }
            }
        }

        private void HideModdedSpices(Edible edible)
        {
            if (edible != null && edible.TryGetComponent<ModdedSpices>(out var moddedSpices))
            {
                var spices = SPICES.Get(edible);
                if (spices != null)
                {
                    for (int i = spices.Count - 1; i >= 0; i--)
                    {
                        if (RegisteredSpices.Contains(spices[i].Id.Name))
                        {
                            moddedSpices.spices.Add(spices[i].Id);
                            spices.RemoveAt(i);
                        }
                    }
                }
            }
        }

        private void RestoreModdedSpices(Edible edible)
        {
            if (edible != null && edible.TryGetComponent<ModdedSpices>(out var moddedSpices))
            {
                var spices = SPICES.Get(edible);
                if (spices != null)
                {
                    foreach (var moddedSpiceId in moddedSpices.spices)
                    {
                        if (SpiceGrinder.SettingOptions.TryGetValue(moddedSpiceId, out var option)
                            && !spices.Exists(spice => spice.Id == moddedSpiceId))
                        {
                            spices.Add(new SpiceInstance() { Id = option.Id, TotalKG = option.Spice.TotalKG });
                        }
                    }
                }
            }
        }
    }
}