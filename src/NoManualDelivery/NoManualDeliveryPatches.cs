using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;
using TUNING;
using SanchozzONIMods.Lib;
//using PeterHan.PLib;
//using PeterHan.PLib.Datafiles;
using PeterHan.PLib.UI;
using PeterHan.PLib.Options;

namespace NoManualDelivery
{
    internal static class NoManualDeliveryPatches
    {
        public static void OnLoad(string path)
        {
            //PUtil.InitLibrary();
            //PUtil.RegisterPatchClass(typeof (NoManualDeliveryPatches));
            //PLocalization.Register();
            POptions.RegisterOptions(typeof(NoManualDeliveryOptions));

            NoManualDeliveryOptions.Reload();

            // хак для того чтобы разрешить руке хватать бутылки
            if (NoManualDeliveryOptions.Instance.AllowTransferArmPickupGasLiquid)
            {
                SolidTransferArm.tagBits = new TagBits(STORAGEFILTERS.NOT_EDIBLE_SOLIDS.Concat(STORAGEFILTERS.FOOD).Concat(STORAGEFILTERS.GASES).Concat(STORAGEFILTERS.LIQUIDS).ToArray());

                BuildingToMakeAutomatable.AddRange(BuildingToMakeAutomatableWithTransferArmPickupGasLiquid);
            }

            // подготовка хака, чтобы разрешить дупликам забирать жеготных из инкубатора и всегда хватать еду
            AlwaysCouldBePickedUpByMinionTags = new Tag[] { GameTags.Creatures.Deliverable };
            if (NoManualDeliveryOptions.Instance.AllowAlwaysPickupEdible)
            {
                AlwaysCouldBePickedUpByMinionTags = AlwaysCouldBePickedUpByMinionTags.Concat(STORAGEFILTERS.FOOD).Add(GameTags.MedicalSupplies).ToArray();
            }
        }

        [HarmonyPatch(typeof(Localization), "Initialize")]
        internal static class Localization_Initialize
        {
            private static void Postfix(Localization.Locale ___sLocale)
            {
                Utils.InitLocalization(typeof(STRINGS), ___sLocale, "", false);
                Utils.InitLocalization(typeof(PUIStrings), ___sLocale, "peterhan.plib.ui_", false);
            }
        }

        /*
        [PLibMethod(RunAt.AfterModsLoad)]
        private static void AdditionalLocalization()
        {
            Debug.Log("PLibMethod AdditionalLocalization");
            Assembly assembly = Assembly.GetCallingAssembly();
            Type type = assembly.GetType("PeterHan.PLib.UI.PUIStrings");
            if (type != null)
            {
                Debug.Log("GawGaw!!");
                Utils.InitLocalization(type, Localization.GetLocale(), "peterhan.plib.ui_", false);
            }
        }
        */

        // ручной список добавленных построек
        private static List<string> BuildingToMakeAutomatable = new List<string>()
        {
            "StorageLocker",
            "StorageLockerSmart",
            "ObjectDispenser",
            "RationBox",
            "Refrigerator",
            "FarmTile",
            "HydroponicFarm",
            "PlanterBox",
            "AquaticFarm",
            "CreatureFeeder",
            "FishFeeder",
            "EggIncubator",
            "Kiln",
            "IceCooledFan",
            "SolidBooster",
            "ResearchCenter",
            // из модов:
            // Storage Pod  https://steamcommunity.com/sharedfiles/filedetails/?id=1873476551
            "StoragePodConfig",
        };

        private static List<string> BuildingToMakeAutomatableWithTransferArmPickupGasLiquid = new List<string>()
        {
            "LiquidPumpingStation",
            "GasBottler",
            "BottleEmptier",
            "BottleEmptierGas",
            // из модов:
            // Fluid Shipping https://steamcommunity.com/sharedfiles/filedetails/?id=1794548334
            "StormShark.BottleInserter",
            "StormShark.CanisterInserter",
            // Automated Canisters https://steamcommunity.com/sharedfiles/filedetails/?id=1824410623
            "asquared31415.PipedLiquidBottler",
        };

        // добавляем компонент к постройкам
        [HarmonyPatch(typeof(Assets), "AddBuildingDef")]
        internal static class Assets_AddBuildingDef
        {
            private static void Prefix(ref BuildingDef def)
            {
                GameObject go = def.BuildingComplete;
                if (go != null)
                {
                    if (
                        BuildingToMakeAutomatable.Contains(def.PrefabID)
                        || (go.GetComponent<ManualDeliveryKG>() != null && (go.GetComponent<ElementConverter>() != null || go.GetComponent<EnergyGenerator>() != null) && go.GetComponent<ResearchCenter>() == null)
                        || (go.GetComponent<ComplexFabricatorWorkable>() != null)
                        || (go.GetComponent<TinkerStation>() != null)
                        )
                    {
                        go.AddOrGet<Automatable2>();
                    }
                }
            }
        }

        // хак для того чтобы разрешить дупликам доставку для Tinkerable объектов, типа генераторов
        [HarmonyPatch(typeof(Tinkerable), "UpdateChore")]
        internal static class Tinkerable_UpdateChore
        {
            private static void Postfix(ref Chore ___chore)
            {
                if (___chore != null && ___chore is FetchChore)
                {
                    Traverse traverse = Traverse.Create(___chore);
                    traverse.Field<bool>("arePreconditionsDirty").Value = true;
                    traverse.Field<List<Chore.PreconditionInstance>>("preconditions").Value.RemoveAll((Chore.PreconditionInstance x) => x.id == "IsAllowedByAutomation");
                    ((FetchChore)___chore).automatable = null;
                }
            }
        }

        private static Tag[] AlwaysCouldBePickedUpByMinionTags = new Tag[0];

        // хак для того чтобы разрешить дупликам забирать жеготных из инкубатора и всегда хватать еду
        [HarmonyPatch(typeof(Pickupable), "CouldBePickedUpByMinion")]
        internal static class Pickupable_CouldBePickedUpByMinion
        {
            private static bool Prefix(Pickupable __instance, GameObject carrier, ref bool __result)
            {
                if (__instance.KPrefabID.HasAnyTags(AlwaysCouldBePickedUpByMinionTags))
                {
                    __result = __instance.CouldBePickedUpByTransferArm(carrier);
                    return false;
                }
                return true;
            }
        }

        // хак - дуплы обжирающиеся от стресса, будут игнорировать установленную галку
        [HarmonyPatch(typeof(BingeEatChore.StatesInstance), "FindFood")]
        internal static class BingeEatChore_StatesInstance_FindFood
        {
            /*
            if ( блаблабла && 
                item.GetComponent<Pickupable>()
        ---         .CouldBePickedUpByMinion(base.gameObject)
        +++         .CouldBePickedUpByTransferArm(base.gameObject)
               )
			{
                блаблабла
            */
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo CouldBePickedUpByMinion = typeof(Pickupable).GetMethod("CouldBePickedUpByMinion", new Type[] { typeof(GameObject) });
                MethodInfo CouldBePickedUpByTransferArm = typeof(Pickupable).GetMethod("CouldBePickedUpByTransferArm", new Type[] { typeof(GameObject) });
                bool result = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction instruction = instructionsList[i];
                    if (instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == CouldBePickedUpByMinion)
                    {
                        yield return new CodeInstruction(OpCodes.Callvirt, CouldBePickedUpByTransferArm);
                        result = true;
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
                if (!result)
                {
                    Debug.LogError("NoManualDelivery cannot apply patch BingeEatChore.StatesInstance.FindFood");
                }
            }
        }

        // хак для того чтобы не испортить заголовок окна - сместить галку вниз
        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        internal static class DetailsScreen_OnPrefabInit
        {
            private static void Prefix(ref List<DetailsScreen.SideScreenRef> ___sideScreens)
            {
                DetailsScreen.SideScreenRef sideScreen;
                for (int i = 0; i < ___sideScreens.Count; i++)
                {
                    
                    if (___sideScreens[i].name == "Automatable Side Screen")
                    {
                        sideScreen = ___sideScreens[i];
                        ___sideScreens.RemoveAt(i);
                        ___sideScreens.Insert(0, sideScreen);
                        break;
                    }
                }
            }
        }
    }

    // нужно, чтобы установить галку по умолчанию
    public class Automatable2 : Automatable
    {
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            SetAutomationOnly(false);
        }
    }
}
