using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using KMod;

using SanchozzONIMods.Lib;

namespace CorpseOnPedestal
{
    internal sealed class Patches : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
        }

        // разрешить дупликов и гоботов на предестал
        [HarmonyPatch]
        private static class MinionConfig_CreatePrefab
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return new MethodBase[] {
                    typeof(BaseMinionConfig).GetMethod(nameof(BaseMinionConfig.BaseMinion)),
                    typeof(BaseRoverConfig).GetMethod(nameof(BaseRoverConfig.BaseRover)),
                };
            }

            private static void Postfix(GameObject __result)
            {
                __result.AddOrGet<KPrefabID>().AddTag(GameTags.PedestalDisplayable, false);
                __result.AddOrGet<Pickupable>().sortOrder = -3;
            }
        }

        // "обнаружить" мёртвых дуплов и гоботов. 
        // на пьедестале отоброжать обычную анимацию на спине
        [HarmonyPatch(typeof(DeathMonitor), nameof(DeathMonitor.InitializeStates))]
        private static class DeathMonitor_InitializeStates
        {
            private static void Postfix(DeathMonitor __instance)
            {
                __instance.dead_creature
                    .Enter(Discover)
                    .EventHandler(GameHashes.OnStore, PlayDeathAnim);

                DeathMonitor.State pedestal = __instance.CreateState(nameof(pedestal), __instance.dead);
                __instance.dead
                    .Enter(Discover);

                __instance.dead.carried
                    .EnterTransition(pedestal, IsOnPedestal)
                    .EventTransition(GameHashes.OnStore, pedestal, IsOnPedestal);

                pedestal
                    .Enter(PlayDeathAnim)
                    .EventTransition(GameHashes.OnStore, __instance.dead.ground, smi => !smi.HasTag(GameTags.Stored));
            }

            private static void Discover(DeathMonitor.Instance smi)
            {
                DiscoveredResources.Instance.GetDiscovered().Add(smi.PrefabID());
            }

            private static bool IsOnPedestal(DeathMonitor.Instance smi)
            {
                return smi.HasTag(GameTags.Stored) && smi.gameObject.TryGetComponent(out Pickupable pickupable)
                    && pickupable.storage.TryGetComponent(out SingleEntityReceptacle _);
            }

            private static void PlayDeathAnim(DeathMonitor.Instance smi)
            {
                string anim;
                if (smi.IsDuplicant)
                {
                    var death = smi.sm.death.Get(smi);
                    if (death == null)
                        death = Db.Get().Deaths.Generic;
                    anim = death.loopAnim;
                }
                else
                    anim = "idle_dead";
                smi.GetComponent<KAnimControllerBase>().Play(anim, KAnim.PlayMode.Loop);
            }
        }

        // ровер не должен заряжаться кроме как унутре ракеты
        [HarmonyPatch]
        private static class ScoutRoverConfig_OnSpawn
        {
            private static bool Prepare() => DlcManager.IsExpansion1Active();

            private static IEnumerable<MethodBase> TargetMethods()
            {
                var list = new List<MethodBase>();
                list.Add(typeof(ScoutRoverConfig).GetMethod(nameof(ScoutRoverConfig.OnSpawn)));
                foreach (var nested in typeof(ScoutRoverConfig).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    foreach (var method in nested.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (method.Name.Contains(nameof(ScoutRoverConfig.OnSpawn)))
                            list.Add(method);
                    }
                }
                return list;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return instructions.Transpile(original, transpiler);
            }

            private static Transform IsInsideCargoModule(Transform transform)
            {
                if (transform != null)
                {
                    var id = transform.PrefabID();
                    if (id == ScoutModuleConfig.ID || id == ScoutLanderConfig.ID)
                        return transform;
                }
                return null;
            }

            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var parent = typeof(Transform).GetProperty(nameof(Transform.parent))?.GetGetMethod();
                var test = typeof(ScoutRoverConfig_OnSpawn).GetMethod(nameof(IsInsideCargoModule),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                if (parent != null && test != null)
                {
                    int i = instructions.FindIndex(inst => inst.Calls(parent));
                    if (i != -1)
                    {
                        instructions.Insert(++i, new CodeInstruction(OpCodes.Call, test));
                        return true;
                    }
                }
                return false;
            }
        }

        // учтём все виды дупликов и гоботов
        private static HashSet<Tag> AllMinions = new();
        private static HashSet<Tag> AllRobots = new();
        private static bool IsMinion(Tag tag) => AllMinions.Contains(tag);
        private static bool IsRobot(Tag tag) => AllRobots.Contains(tag);
        private static bool IsMinionOrRobot(Tag tag) => IsMinion(tag) || IsRobot(tag);

        [HarmonyPatch(typeof(BuildingConfigManager), nameof(BuildingConfigManager.ConfigurePost))]
        private static class BuildingConfigManager_ConfigurePost
        {
            private static void Postfix()
            {
                foreach (var minion in Assets.GetPrefabsWithTag(GameTags.BaseMinion))
                    AllMinions.Add(minion.PrefabID());
                foreach (var robot in Assets.GetPrefabsWithTag(GameTags.Robot))
                    AllRobots.Add(robot.PrefabID());
            }
        }

        // так как игра не учитывает дупликов и гоботов в мировом инвентаре, посчитаем их сами
        private static Components.Cmps<RobotAi.Instance> DeadRobotsIdentities = new();

        [HarmonyPatch(typeof(RobotAi), nameof(RobotAi.InitializeStates))]
        private static class RobotAi_InitializeStates
        {
            private static void Postfix(RobotAi __instance)
            {
                __instance.dead.Toggle("ToggleRegistration", smi => ToggleRegistration(smi, true), smi => ToggleRegistration(smi, false));
            }

            private static void ToggleRegistration(RobotAi.Instance smi, bool register)
            {
                if (register)
                    DeadRobotsIdentities.Add(smi);
                else
                    DeadRobotsIdentities.Remove(smi);
            }
        }

        [HarmonyPatch(typeof(ReceptacleSideScreen), "GetAvailableAmount")]
        private static class ReceptacleSideScreen_GetAvailableAmount
        {
            private static bool Prefix(Tag tag, ref float __result, SingleEntityReceptacle ___targetReceptacle)
            {
                if (___targetReceptacle != null)
                {
                    float amount = 0f;
                    if (IsMinion(tag))
                    {
                        int world_id = ___targetReceptacle.GetMyParentWorldId();
                        foreach (var minion in Components.MinionIdentities.Items)
                        {
                            if (!minion.IsNullOrDestroyed() && minion.TryGetComponent(out KPrefabID prefabID)
                                && prefabID.IsPrefabID(tag) && prefabID.HasTag(GameTags.Corpse) && !prefabID.HasTag(GameTags.StoredPrivate)
                                && world_id == minion.GetMyParentWorldId())
                            {
                                amount += 1;
                            }
                        }
                        __result = amount;
                        return false;
                    }
                    else if (IsRobot(tag))
                    {
                        int world_id = ___targetReceptacle.GetMyParentWorldId();
                        foreach (var robot in DeadRobotsIdentities.Items)
                        {
                            if (!robot.IsNullOrDestroyed() && robot.gameObject.TryGetComponent(out KPrefabID prefabID)
                                && prefabID.IsPrefabID(tag) && prefabID.HasTag(GameTags.Dead) && !prefabID.HasTag(GameTags.StoredPrivate)
                                && world_id == robot.GetMyParentWorldId())
                            {
                                amount += 1;
                            }
                        }
                        __result = amount;
                        return false;
                    }
                }
                return true;
            }
        }

        // иконка дуплика
        // todo: может быть нарисовать иконку помёршего дуплика ?
        [HarmonyPatch(typeof(ReceptacleSideScreen), "GetEntityIcon")]
        private static class ReceptacleSideScreen_GetEntityIcon
        {
            private static bool Prefix(Tag prefabTag, ref Sprite __result)
            {
                if (IsMinion(prefabTag))
                {
                    __result = Assets.GetSprite("sadDupe");
                    return false;
                }
                return true;
            }
        }

        // просто предосторожность
        [HarmonyPatch(typeof(SingleEntityReceptacle), nameof(SingleEntityReceptacle.CreateOrder))]
        private static class SingleEntityReceptacle_CreateOrder
        {
            private static void Prefix(Tag entityTag, ref Tag additionalFilterTag)
            {
                if (IsMinion(entityTag))
                    additionalFilterTag = GameTags.Corpse;
                else if (IsRobot(entityTag))
                    additionalFilterTag = GameTags.Dead;
            }

            private static void Postfix(Tag entityTag, FetchChore ___fetchChore)
            {
                if (___fetchChore != null && IsMinion(entityTag))
                    ___fetchChore.AddPrecondition(ChorePreconditions.instance.IsNotARobot);
            }
        }

        // разместить чуть пониже
        [HarmonyPatch(typeof(SingleEntityReceptacle), "PositionOccupyingObject")]
        private static class SingleEntityReceptacle_PositionOccupyingObject
        {
            private static void Postfix(SingleEntityReceptacle __instance)
            {
                if (__instance.Occupant != null)
                {
                    var tag = __instance.Occupant.PrefabID();
                    if (IsMinionOrRobot(tag))
                    {
                        var pos = __instance.Occupant.transform.GetPosition();
                        float offcetY = IsMinion(tag) ? 0.25f : 0.35f;
                        pos.y -= offcetY;
                        __instance.Occupant.transform.SetPosition(pos);
                    }
                }
            }
        }

        // для красивого падения при освобождении
        [HarmonyPatch(typeof(SingleEntityReceptacle), "ClearOccupant")]
        private static class SingleEntityReceptacle_ClearOccupant
        {
            private class PositionInfo
            {
                public GameObject go;
                public Vector3 position;
            }

            private static void Prefix(SingleEntityReceptacle __instance, ref PositionInfo __state)
            {
                if (__instance.Occupant != null && IsMinionOrRobot(__instance.Occupant.PrefabID()))
                {
                    __state = new PositionInfo() { go = __instance.Occupant, position = __instance.Occupant.transform.GetPosition() };
                    __state.position.z = Grid.GetLayerZ(Grid.SceneLayer.Ore);
                }
                else
                    __state = null;
            }

            private static void Postfix(SingleEntityReceptacle __instance, PositionInfo __state)
            {
                if (__state != null && __state.go != null)
                {
                    __state.go.transform.SetPosition(__state.position);
                    var smi = __state.go.GetSMI<FallWhenDeadMonitor.Instance>();
                    if (!smi.IsNullOrStopped() && smi.IsInsideState(smi.sm.standing))
                        smi.GoTo(smi.sm.falling);
                }
            }
        }

        // не падать когда несут
        [HarmonyPatch(typeof(FallWhenDeadMonitor.Instance), nameof(FallWhenDeadMonitor.Instance.IsFalling))]
        private static class FallWhenDeadMonitor_Instance_IsFalling
        {
            private static void Postfix(FallWhenDeadMonitor.Instance __instance, ref bool __result)
            {
                if (__result && __instance.HasTag(GameTags.Stored))
                    __result = false;
            }
        }
    }
}
