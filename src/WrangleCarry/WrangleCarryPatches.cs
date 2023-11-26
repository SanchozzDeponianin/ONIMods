using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Database;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace WrangleCarry
{
    internal sealed class WrangleCarryPatches : KMod.UserMod2
    {
        private static WrangleCarryPatches @this;
        public override void OnLoad(Harmony harmony)
        {
            Utils.LogModVersion();
            @this = this;
            harmony.Patch(typeof(Db).GetMethod(nameof(Db.Initialize)), prefix: new HarmonyMethod(typeof(WrangleCarryPatches), nameof(PatchLater)));
        }

        private static void PatchLater()
        {
            @this.mod.loaded_mod_data.harmony.PatchAll(@this.assembly);
        }

        // сделать перенос жеготных равноприоритетным с поимкой
        // однако врядли сильно поможет в случае отлова нескольких жеготных одновременно
        // так как есть ещё учёт стоимости пути, а задача переноса инициируется точкой доставки а не самим жеготным
        [HarmonyPatch(typeof(ChoreTypes), "Add")]
        private static class ChoreTypes_Add
        {
            private static void Prefix(string id, ref bool skip_implicit_priority_change)
            {
                if (id == nameof(Db.ChoreTypes.CreatureFetch))
                    skip_implicit_priority_change = true;
            }
        }

        // ИИ жеготных как исвестно - подлагивает
        // например, есть лаги между:
        // а) жеготновод начал связывать жеготное, и жеготное поняло что его связывают и встало на месте
        // б) жеготновод закончил связывать жеготное, и жеготное поняло что его связали, и стало доступным для переноса
        // изза б) жеготновод убегает делать другие дела оставляя жеготное на полу
        // поэтому принудительно вправляем мозги жеготному в начале и конце связывания
        // todo: посмотреть что там с ловушками
        [HarmonyPatch]
        private static class Capturable_OnWork
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                yield return typeof(Capturable).GetMethod("OnStartWork", flags);
                yield return typeof(Capturable).GetMethod("OnStopWork", flags);
            }
            private static void Postfix(Capturable __instance)
            {
                if (__instance.TryGetComponent<CreatureBrain>(out var brain) && brain.IsRunning())
                    brain.UpdateBrain();
            }
        }

        // добавляем жеготноводу эмоцию после поимки, если жеготное неподнимаемо прямо сейчас
        // например зарытые хачи и ройки, или попотолкуходячие белочки и барашки
        // с одной стороны это даст время жеготному упасть напол
        // с другой - задачу переноса могут перетянуть на себя другие дупели
        // в любом случае это выглядит запавно
        [HarmonyPatch(typeof(Capturable), "OnCompleteWork")]
        private static class Capturable_OnCompleteWork
        {
            private const string kanim = "anim_rage_kanim";
            private static readonly HashedString[] anims = { "rage_pre", "rage_loop", "rage_loop", "rage_loop", "rage_pst" };
            private static void Postfix(Capturable __instance, Worker worker)
            {
                if (__instance.TryGetComponent<Pickupable>(out var pickupable) && pickupable.IsReachable())
                    return;
                if (worker != null && worker.TryGetComponent<ChoreProvider>(out var provider))
                    new EmoteChore(provider, Db.Get().ChoreTypes.EmoteHighPriority, kanim, anims);
            }
        }

        // косметика
        // когда дупель несет жеготное, добавляем ему анимацию мешка за спиной
        private const string chest = "snapTo_chest";
        private static void AddSackSymbolOverride(GameObject dupe, GameObject pickupable)
        {
            if (dupe != null && pickupable != null
                && pickupable.HasTag(GameTags.Creature) && !pickupable.HasTag(GameTags.Robot)
                && dupe.TryGetComponent<KAnimControllerBase>(out var kbac)
                && dupe.TryGetComponent<SymbolOverrideController>(out var syoc))
            {
                var symbol = Assets.GetAnim("creature_sack_kanim").GetData().build.GetSymbol("object");
                syoc.AddSymbolOverride(chest, symbol);
                kbac.SetSymbolVisiblity(chest, true);
            }
        }
        private static void RemoveSackSymbolOverride(GameObject dupe)
        {
            if (dupe != null
                && dupe.TryGetComponent<KAnimControllerBase>(out var kbac)
                && dupe.TryGetComponent<SymbolOverrideController>(out var syoc))
            {
                kbac.SetSymbolVisiblity(chest, false);
                syoc.RemoveSymbolOverride(chest);
            }
        }

        // обычная переноска жеготного
        [HarmonyPatch(typeof(FetchAreaChore.States), nameof(FetchAreaChore.States.InitializeStates))]
        private static class FetchAreaChore_States_InitializeStates
        {
            private static void Postfix(FetchAreaChore.States __instance)
            {
                __instance.delivering.movetostorage
                    .Enter(smi => AddSackSymbolOverride(smi.gameObject, smi.sm.deliveryObject.Get(smi)))
                    .Exit(smi => RemoveSackSymbolOverride(smi.gameObject));
            }
        }

        // новая переноска жеготного командой MoveTo
        [HarmonyPatch(typeof(MovePickupableChore.States), nameof(MovePickupableChore.States.InitializeStates))]
        private static class MovePickupableChore_States_InitializeStates
        {
            private static void Postfix(MovePickupableChore.States __instance)
            {
                __instance.approachstorage
                    .Enter(smi => AddSackSymbolOverride(smi.sm.deliverer.Get(smi), smi.sm.pickupablesource.Get(smi)))
                    .Exit(smi => RemoveSackSymbolOverride(smi.sm.deliverer.Get(smi)));
            }
        }

        // если команда MoveTo применена к жеготному, меняем ChoreType на RanchingFetch
        [HarmonyPatch(typeof(MovePickupableChore), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(IStateMachineTarget), typeof(GameObject), typeof(Action<Chore>) })]
        private static class MovePickupableChore_Constructor
        {
            private static ChoreType Inject(ChoreType original, GameObject go)
            {
                if (go != null && go.GetComponent<CreatureBrain>() != null)
                {
                    return Db.Get().ChoreTypes.RanchingFetch;
                }
                return original;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }

            private static bool transpiler(List<CodeInstruction> instructions, MethodBase original)
            {
                var pickupable = original?.GetParameters().FirstOrDefault(p =>
                    p.ParameterType == typeof(GameObject) && p.Name == "pickupable");
                var choreTypes = typeof(Db).GetField(nameof(Db.ChoreTypes));
                var inject = typeof(MovePickupableChore_Constructor).GetMethod(nameof(Inject), BindingFlags.NonPublic | BindingFlags.Static);
                if (pickupable != null && choreTypes != null && inject != null)
                {
                    int i = instructions.FindIndex(ins => ins.LoadsField(choreTypes));
                    if (i != -1)
                    {
                        i++;
                        var ins = instructions[i];
                        if (ins.opcode == OpCodes.Ldfld && ins.operand is FieldInfo field && field.FieldType == typeof(ChoreType))
                        {
                            instructions.Insert(++i, TranspilerUtils.GetLoadArgInstruction(pickupable));
                            instructions.Insert(++i, new CodeInstruction(OpCodes.Call, inject));
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // если множество объектов назначены для переноски командой MoveTo в одну целевую клетку
        // клеи по неизвестной причине прописали в MovePickupableChore.States.InitializeStates
        // вместо того чтобы просто позволить чоре успешно завершится,
        // и новая чора для следующего объекта была бы пересоздана из CancellableMove
        // они возвращают эту же чору к началу, выставляя целевой следующий объект
        // и завтавляя этого же дупликанта снова и снова носить объекты
        // и это приводит к крайне странному поведению, если для переноски
        // назначены вперемешку и жеготные и куски материалов.
        // например. если нет дупликантов с навыком отлова жеготных:
        // если первое в очереди жеготное - дупликанты также откажутся носить куски материалов
        // если первое в очереди кусок материала - дупликанты перенесут и жеготных, даже не имея навыка отлова

        // можно было бы просто позволить чоре всегда завершаться,
        // но возможно у клеев была какая то причина сделать так
        // поэтому позволим чоре завершиться,
        // если следующий объект == жеготное, а choreType не соответсвует замененному нами RanchingFetch
        // и наоборот
        [HarmonyPatch(typeof(MovePickupableChore.States), "IsDeliveryComplete")]
        private static class MovePickupableChore_States_IsDeliveryComplete
        {
            private static void Postfix(ref bool __result, MovePickupableChore.StatesInstance smi)
            {
                if (!__result)
                {
                    var go = smi.sm.deliverypoint.Get(smi);
                    if (go != null && go.TryGetComponent<CancellableMove>(out var move))
                    {
                        var next = move.GetNextTarget();
                        if (next != null)
                        {
                            if (next.TryGetComponent<CreatureBrain>(out _) !=
                                (smi.master.choreType.IdHash == Db.Get().ChoreTypes.RanchingFetch.IdHash))
                                __result = true;
                        }
                    }
                }
            }
        }
    }
}
