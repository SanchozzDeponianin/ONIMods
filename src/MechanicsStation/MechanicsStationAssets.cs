using System.Collections.Generic;
using System.Linq;
using Database;
using Klei.AI;
using STRINGS;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Shared;
using PeterHan.PLib.Core;
using PeterHan.PLib.Detours;

namespace MechanicsStation
{
    internal class MechanicsStationAssets
    {
        public const float BASE_SPEED_VALUE = 1f;
        public const string MACHINERY_SPEED_MODIFIER_NAME = "MachinerySpeed";
        public const float MACHINERY_SPEED_MODIFIER = 0.5f;
        public const string CRAFTING_SPEED_MODIFIER_NAME = "CraftingSpeed";
        public const float CRAFTING_SPEED_MODIFIER = 1f;
        public const string MACHINE_TINKER_EFFECT_NAME = "Machine_Tinker";
        public const float MACHINE_TINKER_EFFECT_DURATION = 2f;
        public const float MACHINE_TINKER_EFFECT_DURATION_PER_SKILL = 0.05f;
        public const string REQUIRED_ROLE_PERK = "CanMachineTinker";

        internal static SkillPerk CanMachineTinker;
        internal static Attribute CraftingSpeed;
        private static AttributeModifier MachinerySpeedModifier;
        private static AttributeModifier CraftingSpeedModifier;
        private static Effect MachineTinkerEffect;
        private static AttributeConverter MachineTinkerEffectDuration;

        internal static void Init()
        {
            var db = Db.Get();
            var machineShop = db.RoomTypes.MachineShop;

            // тюнингуем и актифируем комнату "мащинэщоп"
            // подхватывать максимальный размер комнаты из тюнинга
            int maxRoomSize = TuningData<RoomProber.Tuning>.Get().maxRoomSize;
            var MAXIMUM_SIZE_MAX = new RoomConstraints.Constraint(
                building_criteria: null,
                room_criteria: (Room room) => room.cavity.numCells <= maxRoomSize,
                times_required: 1,
                name: string.Format(ROOMS.CRITERIA.MAXIMUM_SIZE.NAME, maxRoomSize),
                description: string.Format(ROOMS.CRITERIA.MAXIMUM_SIZE.DESCRIPTION, maxRoomSize),
                stomp_in_conflict: null);

            var additional_constraints = machineShop.additional_constraints;
            for (int i = 0; i < additional_constraints.Length; i++)
            {
                if (additional_constraints[i] == RoomConstraints.MAXIMUM_SIZE_96)
                {
                    additional_constraints[i] = MAXIMUM_SIZE_MAX;
                    break;
                }
            }

            db.RoomTypes.Add(machineShop);

            // модифицируем "мастерскую" чтобы она могла быть обгрейднутна до других комнат
            var upgrade_paths = new List<RoomType>() {
                db.RoomTypes.Laboratory,
                db.RoomTypes.Kitchen,
                db.RoomTypes.Farm,
                db.RoomTypes.CreaturePen,
                db.RoomTypes.PowerPlant,
                db.RoomTypes.RecRoom,
                db.RoomTypes.Hospital,
                db.RoomTypes.MassageClinic };
            if (machineShop.upgrade_paths != null)
                upgrade_paths.AddRange(machineShop.upgrade_paths);

            // детектим "Rooms Expanded"
            var RoomsExpanded = PPatchTools.GetTypeSafe("RoomsExpanded.RoomTypes_AllModded", "RoomsExpandedMerged");
            if (RoomsExpanded != null)
            {
                PUtil.LogDebug("RoomsExpanded found. Attempt to add compatibility.");
                try
                {
                    var ext_rooms = new string[] { "KitchenetteRoom", "Nursery", "GeneticNursery", "GymRoom", "MissionControlRoom" };
                    foreach (var id in ext_rooms)
                    {
                        var room = (RoomType)RoomsExpanded.GetPropertySafe<RoomType>(id, true)?.GetValue(null);
                        if (room != null)
                            upgrade_paths.Add(room);
                    }
                }
                catch (System.Exception e)
                {
                    PUtil.LogExcWarn(e);
                }
            }

            upgrade_paths.RemoveAll(room => room == null);
            var priority = System.Math.Min(machineShop.priority, upgrade_paths.Min(room => room.priority));
            var MachineShop = Traverse.Create(machineShop);
            MachineShop.Property(nameof(RoomType.upgrade_paths)).SetValue(upgrade_paths.ToArray());
            MachineShop.Property(nameof(RoomType.priority)).SetValue(priority);

            // добавляем перк для работы на станции
            CanMachineTinker = db.SkillPerks.Add(new SimpleSkillPerk(
                id: REQUIRED_ROLE_PERK,
                description: STRINGS.PERK_CAN_MACHINE_TINKER.DESCRIPTION));
            db.Skills.Technicals1.perks.Add(CanMachineTinker);

            // добавляем модификаторы и эффекты 
            string text = DUPLICANTS.MODIFIERS.MACHINETINKER.NAME;
            string description = STRINGS.DUPLICANTS.MODIFIERS.MACHINETINKER.TOOLTIP;

            CraftingSpeed = db.Attributes.Add(new Attribute(
                id: CRAFTING_SPEED_MODIFIER_NAME,
                is_trainable: false,
                show_in_ui: Attribute.Display.General,
                is_profession: false,
                base_value: BASE_SPEED_VALUE));
            CraftingSpeed.SetFormatter(new PercentAttributeFormatter());

            MachinerySpeedModifier = new AttributeModifier(
                attribute_id: MACHINERY_SPEED_MODIFIER_NAME,
                value: MACHINERY_SPEED_MODIFIER,
                description: text,
                is_readonly: false);
            CraftingSpeedModifier = new AttributeModifier(
                attribute_id: CRAFTING_SPEED_MODIFIER_NAME,
                value: CRAFTING_SPEED_MODIFIER,
                description: text,
                is_readonly: false);

            MachineTinkerEffect = db.effects.Add(new Effect(
                id: MACHINE_TINKER_EFFECT_NAME,
                name: text,
                description: description,
                duration: MACHINE_TINKER_EFFECT_DURATION * Constants.SECONDS_PER_CYCLE,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: false));
            MachineTinkerEffect.Add(MachinerySpeedModifier);
            MachineTinkerEffect.Add(CraftingSpeedModifier);

            MachineTinkerEffectDuration = db.AttributeConverters.Create(
                id: "MachineTinkerEffectDuration",
                name: "Engie's Jerry Rig Effect Duration",
                description: STRINGS.DUPLICANTS.ATTRIBUTES.MACHINERY.MACHINE_TINKER_EFFECT_MODIFIER,
                attribute: db.Attributes.Machinery,
                multiplier: MACHINE_TINKER_EFFECT_DURATION_PER_SKILL,
                base_value: 0,
                formatter: new ToPercentAttributeFormatter(1f, GameUtil.TimeSlice.None));
        }

        internal static void LoadOptions()
        {
            MechanicsStationOptions.Reload();
            MachinerySpeedModifier.SetValue(MechanicsStationOptions.Instance.machinery_speed_modifier / 100);
            CraftingSpeedModifier.SetValue(MechanicsStationOptions.Instance.crafting_speed_modifier / 100);
            MachineTinkerEffect.duration = MechanicsStationOptions.Instance.machine_tinker_effect_duration * Constants.SECONDS_PER_CYCLE;
            MachineTinkerEffectDuration.multiplier = MechanicsStationOptions.Instance.machine_tinker_effect_duration_per_skill / 100;
        }

        // сделать постройку улучшаемой
        private static IDetouredField<Tinkerable, bool> UserMenuAllowed = PDetours.DetourFieldLazy<Tinkerable, bool>("userMenuAllowed");
        private static Tinkerable MakeMachineTinkerable(GameObject go)
        {
            var tinkerable = Tinkerable.MakePowerTinkerable(go);
            tinkerable.tinkerMaterialTag = MechanicsStationConfig.TINKER_TOOLS;
            tinkerable.tinkerMaterialAmount = 1f;
            tinkerable.addedEffect = MACHINE_TINKER_EFFECT_NAME;
            tinkerable.effectAttributeId = Db.Get().Attributes.Machinery.Id;
            tinkerable.effectMultiplier = MACHINE_TINKER_EFFECT_DURATION_PER_SKILL;
            tinkerable.requiredSkillPerk = REQUIRED_ROLE_PERK;
            tinkerable.SetWorkTime(TUNING.BUILDINGS.WORK_TIME_SECONDS.SHORT_WORK_TIME);
            tinkerable.choreTypeTinker = Db.Get().ChoreTypes.MachineTinker.IdHash;
            tinkerable.choreTypeFetch = Db.Get().ChoreTypes.MachineFetch.IdHash;
            tinkerable.boostSymbolNames = null;
            go.GetComponent<KPrefabID>().prefabInitFn += delegate (GameObject prefab)
            {
                if (prefab.TryGetComponent<Tinkerable>(out var _tinkerable))
                {
                    _tinkerable.SetWorkTime(TUNING.BUILDINGS.WORK_TIME_SECONDS.SHORT_WORK_TIME);
                    _tinkerable.effectMultiplier = MechanicsStationOptions.Instance.machine_tinker_effect_duration_per_skill / 100;
                    UserMenuAllowed.Set(_tinkerable, false); // по умолчанию выключено
                }
            };
            return tinkerable;
        }

        // сделать постройку улучшаемой и восстановить оригинальные параметры RoomTracker если они были
        internal static Tinkerable MakeMachineTinkerableSave(GameObject go)
        {
            Tinkerable tinkerable;
            // проверяем был ли до нас
            var originRoomTracker = go.GetComponent<RoomTracker>();
            if (originRoomTracker != null)
            {
                var requiredRoomType = originRoomTracker.requiredRoomType;
                var requirement = originRoomTracker.requirement;
                tinkerable = MakeMachineTinkerable(go);
                originRoomTracker.requiredRoomType = requiredRoomType;
                originRoomTracker.requirement = requirement;
            }
            else
            {
                // либо добавляем компонент для работы в нескольких комнатах
                tinkerable = MakeMachineTinkerable(go);
                go.AddOrGet<RoomTracker>().requiredRoomType = Db.Get().RoomTypes.MachineShop.Id;
                go.AddOrGet<MultiRoomTracker>().allowAnyRoomType = true;
            }
            return tinkerable;
        }
    }

    // чтобы эта опция применялась без перезагрузки
    [SkipSaveFileSerialization]
    public class MachineTinkerFreezeEffectDuration : OperationalNotActiveFreezeEffectDuration
    {
        protected override bool ShouldFreeze => MechanicsStationOptions.Instance.machine_tinker_freeze_effect_duration && base.ShouldFreeze;
    }
}
