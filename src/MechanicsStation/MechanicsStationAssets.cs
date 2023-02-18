using Database;
using Klei.AI;
using STRINGS;
using HarmonyLib;
using UnityEngine;
using SanchozzONIMods.Shared;
using PeterHan.PLib.Core;

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
        public const float MACHINE_TINKERABLE_WORKTIME = 20f;
        public const string REQUIRED_ROLE_PERK = "CanMachineTinker";

        private static SkillPerk CanMachineTinker;
        internal static Attribute CraftingSpeed;
        private static AttributeModifier MachinerySpeedModifier;
        private static AttributeModifier CraftingSpeedModifier;
        private static Effect MachineTinkerEffect;
        private static AttributeConverter MachineTinkerEffectDuration;

        internal static void Init()
        {
            var db = Db.Get();

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

            var additional_constraints = db.RoomTypes.MachineShop.additional_constraints;
            for (int i = 0; i < additional_constraints.Length; i++)
            {
                if (additional_constraints[i] == RoomConstraints.MAXIMUM_SIZE_96)
                {
                    additional_constraints[i] = MAXIMUM_SIZE_MAX;
                    break;
                }
            }

            db.RoomTypes.Add(db.RoomTypes.MachineShop);

            // модифицируем "мастерскую" чтобы она могла быть обгрейднутна до "кухни"
            var upgrade_paths = db.RoomTypes.MachineShop.upgrade_paths.AddToArray(db.RoomTypes.Kitchen);
            var priority = System.Math.Min(db.RoomTypes.MachineShop.priority, db.RoomTypes.Kitchen.priority);
            // детектим "Rooms Expanded"
            var RoomsExpanded = PPatchTools.GetTypeSafe("RoomsExpanded.RoomTypes_AllModded", "RoomsExpandedMerged");
            if (RoomsExpanded != null)
            {
                PUtil.LogDebug("RoomsExpanded found. Attempt to add compatibility.");
                try
                {
                    var KitchenetteRoom = (RoomType)RoomsExpanded.GetPropertySafe<RoomType>("KitchenetteRoom", true)?.GetValue(null);
                    if (KitchenetteRoom != null)
                    {
                        upgrade_paths = upgrade_paths.AddToArray(KitchenetteRoom);
                        priority = System.Math.Min(priority, KitchenetteRoom.priority);
                    }
                }
                catch (System.Exception e)
                {
                    PUtil.LogExcWarn(e);
                }
            }
            var MachineShop = Traverse.Create(db.RoomTypes.MachineShop);
            MachineShop.Property(nameof(RoomType.upgrade_paths)).SetValue(upgrade_paths);
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
                formatter: new ToPercentAttributeFormatter(1f, GameUtil.TimeSlice.None),
                available_dlcs: DlcManager.AVAILABLE_ALL_VERSIONS);
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
        private static Tinkerable MakeMachineTinkerable(GameObject go)
        {
            var tinkerable = Tinkerable.MakePowerTinkerable(go);
            tinkerable.tinkerMaterialTag = MechanicsStationConfig.TINKER_TOOLS;
            tinkerable.tinkerMaterialAmount = 1f;
            tinkerable.addedEffect = MACHINE_TINKER_EFFECT_NAME;
            tinkerable.requiredSkillPerk = REQUIRED_ROLE_PERK;
            tinkerable.SetWorkTime(MACHINE_TINKERABLE_WORKTIME);
            tinkerable.choreTypeTinker = Db.Get().ChoreTypes.MachineTinker.IdHash;
            tinkerable.choreTypeFetch = Db.Get().ChoreTypes.MachineFetch.IdHash;
            // увеличение времени эффекта
            tinkerable.effectAttributeId = Db.Get().Attributes.Machinery.Id;
            tinkerable.effectMultiplier = MACHINE_TINKER_EFFECT_DURATION_PER_SKILL;

            // а это для корректного изменения времени работы после изменения в настройках
            go.GetComponent<KPrefabID>().prefabSpawnFn += delegate (GameObject prefab)
            {
                if (prefab.TryGetComponent<Tinkerable>(out var _tinkerable))
                {
                    _tinkerable.workTime = MechanicsStationOptions.Instance.machine_tinkerable_worktime;
                    _tinkerable.WorkTimeRemaining = Mathf.Min(_tinkerable.WorkTimeRemaining, _tinkerable.workTime);
                    _tinkerable.effectMultiplier = MechanicsStationOptions.Instance.machine_tinker_effect_duration_per_skill / 100;
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
}
