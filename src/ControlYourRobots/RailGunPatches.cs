using System.Collections.Generic;
using HarmonyLib;
using SanchozzONIMods.Lib;

namespace ControlYourRobots
{
    using StorageParameter = StateMachinesExtensions.NonSerializedObjectParameter<RailGun.States, RailGun.StatesInstance, RailGun, object, Storage>;

    internal static class RailGunPatches
    {
        // запускаем отключенных роботов через рельсу
        internal static void SetupRailGun()
        {
            var railgun = Assets.GetBuildingDef(RailGunConfig.ID).BuildingComplete;
            var launcher = railgun.AddOrGet<RobotLauncher>();
            launcher.duplicantOperated = false;
            launcher.outStorage = railgun.GetComponent<Storage>();
            launcher.inStorage = railgun.AddComponent<Storage>();
            launcher.inStorage.showInUI = true;
            launcher.inStorage.SetDefaultStoredItemModifiers(Storage.StandardFabricatorStorage);
            launcher.buildStorage = railgun.AddComponent<Storage>();
            launcher.buildStorage.showInUI = true;
            launcher.buildStorage.SetDefaultStoredItemModifiers(Storage.StandardFabricatorStorage);
            railgun.AddOrGet<RobotLauncherSM>();
        }

        internal static void ConfigureRecipes()
        {
            static void AddRecipe(string robot_id)
            {
                var prefabID = Assets.GetPrefab(robot_id)?.GetComponent<KPrefabID>();
                if (prefabID == null)
                    return;
                new ComplexRecipe($"{RailGunConfig.ID}_{robot_id}_{RailGunPayloadConfig.ID}",
                    new[] { new ComplexRecipe.RecipeElement(robot_id, 1f) },
                    new[] { new ComplexRecipe.RecipeElement(RailGunPayloadConfig.ID, 1f) },
                    prefabID.requiredDlcIds, prefabID.forbiddenDlcIds)
                {
                    time = 1f,
                    description = STRINGS.UI.UISIDESCREENS.RAILGUN_SIDESCREEN.RECIPE_DESC,
                    nameDisplay = ComplexRecipe.RecipeNameDisplay.Ingredient,
                    fabricators = new List<Tag>() { TagManager.Create(RailGunConfig.ID) },
                };
            }
            AddRecipe(MorbRoverConfig.ID);
            AddRecipe(ScoutRoverConfig.ID);
            AddRecipe(FetchDroneConfig.ID);
        }

        [HarmonyPatch(typeof(ComplexFabricator), nameof(ComplexFabricator.UpdateFetches))]
        private static class ComplexFabricator_UpdateFetches
        {
            private static bool Prepare() => ModOptions.Instance.allow_railgun_launch;
            private static bool Prefix(ComplexFabricator __instance, DictionaryPool<Tag, float, ComplexFabricator>.PooledDictionary missingAmounts)
            {
                if (__instance is RobotLauncher launcher)
                {
                    launcher.UpdateFetches(missingAmounts);
                    return false;
                }
                return true;
            }
        }

        private static StorageParameter robot_storage;

        [HarmonyPatch(typeof(RailGun.States), nameof(RailGun.States.InitializeStates))]
        private static class RailGun_States_InitializeStates
        {
            private static bool Prepare() => ModOptions.Instance.allow_railgun_launch;
            private static void Postfix(RailGun.States __instance)
            {
                robot_storage = __instance.AddParameter(nameof(robot_storage), new StorageParameter());
                __instance.root.Enter(smi => robot_storage.Set(smi.GetComponent<RobotLauncher>().buildStorage, smi));
            }
        }

        // в первую очередь запускать гоботов
        [HarmonyPatch(typeof(RailGun), nameof(RailGun.LaunchProjectile))]
        private static class RailGun_LaunchProjectile
        {
            private static bool Prepare() => ModOptions.Instance.allow_railgun_launch;
            private static bool Prefix(RailGun __instance) => !LaunchRobotProjectile(__instance);
            private static bool LaunchRobotProjectile(RailGun rail_gun)
            {
                var storage = robot_storage.Get(rail_gun.smi);
                if (storage.Count == 0f)
                    return false;
                var extents = rail_gun.GetComponent<Building>().GetExtents();
                var vector = Grid.PosToXY(rail_gun.transform.position);
                vector.y += extents.height + 1;
                int cell = Grid.XYToCell(vector.x, vector.y);
                var payload = Util.KInstantiate(Assets.GetPrefab(RailGunPayloadConfig.ID), Grid.CellToPosCBC(cell, Grid.SceneLayer.Front));
                var payload_storage = payload.GetComponent<Storage>();
                storage.Transfer(storage[0], payload_storage, false, true);
                payload_storage.SetContentsDeleteOffGrid(false);
                rail_gun.particleStorage.ConsumeAndGet(rail_gun.smi.EnergyCost());
                payload.SetActive(true);
                if (rail_gun.destinationSelector.GetDestinationWorld() >= 0)
                {
                    var smi = payload.GetSMI<RailGunPayload.StatesInstance>();
                    smi.takeoffVelocity = 35f;
                    smi.StartSM();
                    smi.Launch(rail_gun.gameObject.GetMyWorldLocation(), rail_gun.destinationSelector.GetDestination());
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RailGun.StatesInstance), nameof(RailGun.StatesInstance.HasResources))]
        private static class RailGun_StatesInstance_HasResources
        {
            private static bool Prepare() => ModOptions.Instance.allow_railgun_launch;
            private static void Postfix(RailGun.StatesInstance __instance, ref bool __result)
            {
                __result = __result || (robot_storage.Get(__instance).Count > 0);
            }
        }

        // выбросить и включить робота
        [HarmonyPatch(typeof(RailGunPayload), nameof(RailGunPayload.InitializeStates))]
        private static class RailGunPayload_InitializeStates
        {
            private static bool Prepare() => ModOptions.Instance.allow_railgun_launch;
            private static void Postfix(RailGunPayload __instance)
            {
                __instance.grounded.crater.Enter(Desant);
            }
            private static void Desant(RailGunPayload.StatesInstance smi)
            {
                var storage = smi.GetComponent<Storage>();
                for (int i = storage.Count - 1; i >= 0; i--)
                {
                    var item = storage[i];
                    if (item.TryGetComponent(out RobotTurnOffOn @switch))
                    {
                        storage.Drop(item);
                        @switch.SetState(true);
                    }
                }
                if (storage.Count == 0 && storage.TryGetComponent(out DropAllWorkable dropall))
                    dropall.OnCompleteWork(null);
            }
        }
    }
}
