using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Klei.AI;
using TUNING;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

namespace RoverRefueling
{
    internal sealed class RoverRefuelingPatches : KMod.UserMod2
    {
        public const string RefuelingEffectID = "ScoutBotRefueling";
        public static Effect RefuelingEffect;

        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary();
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(RoverRefuelingPatches));
            new POptions().RegisterOptions(this, typeof(RoverRefuelingOptions));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void Localize()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.AfterDbInit)]
        private static void AfterDbInit()
        {
            ModUtil.AddBuildingToPlanScreen(BUILD_CATEGORY.Utilities, RoverRefuelingStationConfig.ID, BUILD_SUBCATEGORY.automated, SweepBotStationConfig.ID);
            Utils.AddBuildingToTechnology("ArtificialFriends", RoverRefuelingStationConfig.ID);
            var db = Db.Get();
            var rate = ROBOTS.SCOUTBOT.BATTERY_CAPACITY / RoverRefuelingOptions.Instance.charge_time;
            var modifier = new AttributeModifier(
                attribute_id: db.Amounts.InternalChemicalBattery.deltaAttribute.Id,
                value: rate,
                description: STRINGS.DUPLICANTS.MODIFIERS.SCOUTBOTREFUELING.NAME);
            RefuelingEffect = new Effect(
                id: RefuelingEffectID,
                name: STRINGS.DUPLICANTS.MODIFIERS.SCOUTBOTREFUELING.NAME,
                description: STRINGS.DUPLICANTS.MODIFIERS.SCOUTBOTREFUELING.TOOLTIP,
                duration: 0,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: false);
            RefuelingEffect.Add(modifier);
            db.effects.Add(RefuelingEffect);
        }

        public static readonly Tag RoverNeedRefueling = TagManager.Create(nameof(RoverNeedRefueling));

        [HarmonyPatch(typeof(RobotBatteryMonitor), nameof(RobotBatteryMonitor.InitializeStates))]
        private static class RobotBatteryMonitor_InitializeStates
        {
            private static void Postfix(RobotBatteryMonitor __instance)
            {
                __instance.drainingStates.lowBattery.ToggleTag(RoverNeedRefueling);
                __instance.needsRechargeStates.lowBattery.ToggleTag(RoverNeedRefueling);
            }
        }

        // так как батарея ровера теперь заряжается, нужно показывать другой статусытем
        // но мы не будем трогать RobotBatteryMonitor.Def.canCharge чтобы не поломать его другую логику
        // вместо этого найдём и пропатчим сгенерированые делегатовые методы
        // благо конструкция встречается пару раз и с одинаковой начинкой
        // .ToggleStatusItem(smi => smi.def.canCharge ? Db.Get().RobotStatusItems.LowBattery : Db.Get().RobotStatusItems.LowBatteryNoCharge
        [HarmonyPatch]
        private static class RobotBatteryMonitor_InitializeStates_ToggleStatusItem
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (var type in typeof(RobotBatteryMonitor).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (type.IsDefined(typeof(CompilerGeneratedAttribute)))
                    {
                        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            var parameters = method.GetParameters();
                            if (method.ReturnType == typeof(StatusItem)
                                && parameters.Length == 1 && parameters[0].ParameterType == typeof(RobotBatteryMonitor.Instance))
                            {
                                yield return method;
                            }
                        }
                    }
                }
            }

            private static void Postfix(RobotBatteryMonitor.Instance smi, ref StatusItem __result)
            {
                if (smi.gameObject.PrefabID() == ScoutRoverConfig.ID)
                    __result = Db.Get().RobotStatusItems.LowBattery;
            }
        }
    }
}
