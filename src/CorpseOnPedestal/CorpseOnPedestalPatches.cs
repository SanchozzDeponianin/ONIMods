using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using KMod;

using SanchozzONIMods.Lib;

namespace CorpseOnPedestal
{
    internal sealed class CorpseOnPedestalPatches : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (Utils.LogModVersion()) return;
            base.OnLoad(harmony);
        }

        // разрешить дупликов и гоботов на предестал
        [HarmonyPatch]
        private static class MinionConfig_CreatePrefab
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return typeof(MinionConfig).GetMethod(nameof(MinionConfig.CreatePrefab));
                yield return typeof(BaseRoverConfig).GetMethod(nameof(BaseRoverConfig.BaseRover));
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
                    .EventHandler(GameHashes.OnStore, smi => smi.GetComponent<KAnimControllerBase>().Play("idle_dead", KAnim.PlayMode.Loop));

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
                if (smi.IsDuplicant)
                {
                    var death = smi.sm.death.Get(smi);
                    if (death == null)
                        death = Db.Get().Deaths.Generic;
                    smi.GetComponent<KAnimControllerBase>().Play(death.loopAnim, KAnim.PlayMode.Loop);
                }
            }
        }

        // так как игра не учитывает дупликов и гоботов в мировом инвентаре, посчитаем их сами
        private static Components.Cmps<RobotAi.Instance> DeadRobotsIdentities = new Components.Cmps<RobotAi.Instance>();

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
                    if (tag == GameTags.Minion)
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
                    else if (tag == ScoutRoverConfig.ID || tag == MorbRoverConfig.ID)
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
                if (prefabTag == GameTags.Minion)
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
                if (entityTag == GameTags.Minion)
                    additionalFilterTag = GameTags.Corpse;
                else if (entityTag == ScoutRoverConfig.ID || entityTag == MorbRoverConfig.ID)
                    additionalFilterTag = GameTags.Dead;
            }

            private static void Postfix(Tag entityTag, FetchChore ___fetchChore)
            {
                if (entityTag == GameTags.Minion && ___fetchChore != null)
                    ___fetchChore.AddPrecondition(ChorePreconditions.instance.IsNotARobot);
            }
        }

        // чуть пониже
        [HarmonyPatch(typeof(SingleEntityReceptacle), "PositionOccupyingObject")]
        private static class SingleEntityReceptacle_PositionOccupyingObject
        {
            private static void Postfix(SingleEntityReceptacle __instance)
            {
                if (__instance.Occupant != null)
                {
                    var id = __instance.Occupant.PrefabID();
                    if (id == GameTags.Minion || id == ScoutRoverConfig.ID || id == MorbRoverConfig.ID)
                    {
                        var pos = __instance.Occupant.transform.GetPosition();
                        float offcetY = (id == GameTags.Minion) ? 0.25f : 0.35f;
                        pos.y -= offcetY;
                        __instance.Occupant.transform.SetPosition(pos);
                    }
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
