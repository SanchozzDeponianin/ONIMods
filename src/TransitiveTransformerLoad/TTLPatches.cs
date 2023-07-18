using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using KMod;
using SanchozzONIMods.Lib;

namespace TTL
{
    internal sealed class TTLPatches : UserMod2
    {
        private static bool FastTrack = false;
        public override void OnLoad(Harmony harmony)
        {
            Utils.LogModVersion();
        }

        public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<Mod> mods)
        {
            base.OnAllModsLoaded(harmony, mods);
            try
            {
                if (Type.GetType("PeterHan.FastTrack.FastTrackMod, FastTrack", false) != null)
                    FastTrack = true;
            }
            catch { }
            harmony.PatchAll(assembly);
        }

        // кеш информации о подсетях
        // для каждой подсети храним список нижних подсетей подключенных через трансы, а также суммарную мощность этих трансов
        // key = nested circuitID
        // value = Sum Transformer.BaseWattageRating
        private static List<List<KeyValuePair<ushort, float>>> CachedNestedCircuitInfo = new List<List<KeyValuePair<ushort, float>>>();

        // прибавляем к требуемой мощности
        // мощность всех подключенных через трансы нижних подсетей
        // с учетом ограничения по мощности самих трансов
        // рекурсивно
        [HarmonyPatch(typeof(CircuitManager), nameof(CircuitManager.GetWattsNeededWhenActive))]
        private static class CircuitManager_GetWattsNeededWhenActive
        {
            private static readonly Stack<ushort> call_stack = new Stack<ushort>();
            private static void Postfix(CircuitManager __instance, ushort circuitID, ref float __result)
            {
                if (circuitID == CircuitManager.INVALID_ID)
                    return;
                // защита от зацикливания рекурсии в зацикленных сетях
                // возвращаем бесконечность, чтобы в предыдущей итерации значение окуклилось до макс. мощности трансов
                if (call_stack.Contains(circuitID))
                {
                    __result = float.PositiveInfinity;
                }
                else
                {
                    var nested_circuit_info = CachedNestedCircuitInfo[circuitID];
                    if (nested_circuit_info.Count == 0)
                    {
                        // если в кешэ не было информации о нижних подсетях - пробуем собирать её
                        var transformers = __instance.GetTransformersOnCircuit(circuitID);
                        if (transformers.Count > 0)
                        {
                            var nested_circuits = DictionaryPool<ushort, float, CircuitManager>.Allocate();
                            foreach (var transformer_battery in transformers)
                            {
                                ushort nested_circuitID = transformer_battery.powerTransformer.CircuitID;
                                float wattage_rating = transformer_battery.powerTransformer.BaseWattageRating;
                                if (!nested_circuits.ContainsKey(nested_circuitID))
                                    nested_circuits[nested_circuitID] = wattage_rating;
                                else
                                    nested_circuits[nested_circuitID] += wattage_rating;
                            }
                            nested_circuit_info.AddRange(nested_circuits.AsEnumerable());
                            nested_circuits.Recycle();
                        }
                    }
                    if (nested_circuit_info.Count > 0)
                    {
                        foreach (var nested_circuit in nested_circuit_info)
                        {
                            // для всех нижних подсетей, прибавляем к результату 
                            // минимум из а) мощность нижней подсети б) мощность трансформаторов
                            ushort nested_circuitID = nested_circuit.Key;
                            float nested_transformers_power = nested_circuit.Value;
                            float nested_power = 0f;
                            if (nested_circuitID != CircuitManager.INVALID_ID)
                            {
                                // рекурсия с защитой
                                // TODO: нужно ли делать блокировку многопоточности ?
                                call_stack.Push(circuitID);
                                nested_power = __instance.GetWattsNeededWhenActive(nested_circuitID);
                                if (call_stack.Pop() != circuitID)
                                    Debug.LogWarning("Unexpected Mismatch circuitID !!!");
                            }
                            __result += Mathf.Min(nested_power, nested_transformers_power);

                            // если есть FastTrack, он уже прибавил мощность трансформаторов, придётся отнять для правильного результата
                            if (FastTrack)
                                __result -= nested_transformers_power;
                        }
                    }
                }
            }
        }

        // при перестроении сетей сбрасываем кеш и при необходимости расширяем
        private static void Rebuild(int new_count)
        {
            int old_count = CachedNestedCircuitInfo.Count;
            for (int i = 0; i < old_count; i++)
                CachedNestedCircuitInfo[i].Clear();
            while (CachedNestedCircuitInfo.Count < new_count)
                CachedNestedCircuitInfo.Add(new List<KeyValuePair<ushort, float>>());
        }

        [HarmonyPatch(typeof(CircuitManager), "Rebuild")]
        private static class CircuitManager_Rebuild
        {
            private static bool Prepare() => !FastTrack;
            private static void Postfix(ICollection ___circuitInfo)
            {
                Rebuild(___circuitInfo.Count);
            }
        }

        // FastTrack блокирует вызов CircuitManager.Rebuild
        // придётся подлазить вот так
        [HarmonyPatch(typeof(CircuitManager), "Refresh")]
        private static class CircuitManager_Refresh
        {
            private static bool Prepare() => FastTrack;

            private static bool dirty;
            [HarmonyPriority(Priority.HigherThanNormal)]
            [HarmonyBefore("PeterHan.FastTrack")]
            private static void Prefix(bool ___dirty)
            {
                if (Game.Instance.electricalConduitSystem != null)
                    dirty = Game.Instance.electricalConduitSystem.IsDirty || ___dirty;
            }
            private static void Postfix(ICollection ___circuitInfo)
            {
                if (dirty)
                {
                    Rebuild(___circuitInfo.Count);
                    dirty = false;
                }
            }
        }

        // в окне энергия игра показывает макс мощность трансов как 0
        // заменим на минимум из а) мощность нижней подсети б) мощность трансформатора
        [HarmonyPatch(typeof(EnergyInfoScreen), "AddConsumerInfo")]
        private static class EnergyInfoScreen_AddConsumerInfo
        {
            private static float GetNestedCircuitWattageOrMaxWattageRating(IEnergyConsumer consumer)
            {
                var battery = consumer as Battery;
                if (battery != null && battery.powerTransformer != null)
                {
                    ushort circuitID = battery.powerTransformer.CircuitID;
                    float nested_power = circuitID == CircuitManager.INVALID_ID
                        ? 0f : Game.Instance.circuitManager.GetWattsNeededWhenActive(circuitID);
                    return Mathf.Min(nested_power, battery.powerTransformer.BaseWattageRating);
                }
                else
                    return consumer?.WattsNeededWhenActive ?? 0f;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var watts_needed = typeof(IEnergyConsumer).GetProperty(nameof(IEnergyConsumer.WattsNeededWhenActive)).GetGetMethod(true);
                var insert = typeof(EnergyInfoScreen_AddConsumerInfo).GetMethod(nameof(GetNestedCircuitWattageOrMaxWattageRating),
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (watts_needed != null && insert != null)
                {
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].Calls(watts_needed))
                        {
                            instructions[i].operand = insert;
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}
