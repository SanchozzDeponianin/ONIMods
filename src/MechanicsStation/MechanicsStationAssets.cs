using Database;
using Klei.AI;
using STRINGS;
using Harmony;
using UnityEngine;
using PeterHan.PLib;

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
#if EXPANSION1
        private static AttributeConverter MachineTinkerEffectDuration;
#endif
        // для устранения конфликта с модом "Rooms Expanded" с комнатой "кухня"
        public static bool RoomsExpandedFound { get; private set; } = false;
        private static RoomType KitchenRoom;
        private static Tag KitchenBuildingTag = Tag.Invalid;

        internal static void Init()
        {
            var db = Db.Get();

            // тюнингуем и актифируем комнату
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

            // детектим "Rooms Expanded". модифицируем "мастерскую" чтобы она могла быть обгрейднутна до "кухни"
            var RoomsExpanded = PPatchTools.GetTypeSafe("RoomsExpanded.RoomTypes_AllModded", "RoomsExpandedMerged");
            if (RoomsExpanded != null)
            {
                PUtil.LogDebug("RoomsExpanded found. Attempt to add compatibility.");
                try
                {
                    KitchenRoom = (RoomType)RoomsExpanded.GetPropertySafe<RoomType>("KitchenRoom", true)?.GetValue(null, null);
                    if (KitchenRoom != null)
                    {
                        var upgrade_paths = db.RoomTypes.MachineShop.upgrade_paths.AddToArray(KitchenRoom);
                        Traverse.Create(db.RoomTypes.MachineShop).Property(nameof(RoomType.upgrade_paths)).SetValue(upgrade_paths);
                        Traverse.Create(db.RoomTypes.MachineShop).Property(nameof(RoomType.priority)).SetValue(KitchenRoom.priority);
                        KitchenBuildingTag = "KitchenBuildingTag".ToTag();
                        RoomsExpandedFound = true;
                    }
                }
                catch (System.Exception e)
                {
                    PUtil.LogExcWarn(e);
                }
            }

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

#if EXPANSION1
            MachineTinkerEffectDuration = db.AttributeConverters.Create(
                id: "MachineTinkerEffectDuration",
                name: "Engie's Jerry Rig Effect Duration",
                description: STRINGS.DUPLICANTS.ATTRIBUTES.MACHINERY.MACHINE_TINKER_EFFECT_MODIFIER,
                attribute: db.Attributes.Machinery,
                multiplier: MACHINE_TINKER_EFFECT_DURATION_PER_SKILL,
                base_value: 0,
                formatter: new ToPercentAttributeFormatter(1f, GameUtil.TimeSlice.None));
#endif
        }

        internal static void LoadOptions()
        {
            MechanicsStationOptions.Reload();
            MachinerySpeedModifier.SetValue(MechanicsStationOptions.Instance.MachinerySpeedModifier / 100);
            CraftingSpeedModifier.SetValue(MechanicsStationOptions.Instance.CraftingSpeedModifier / 100);
            MachineTinkerEffect.duration = MechanicsStationOptions.Instance.MachineTinkerEffectDuration * Constants.SECONDS_PER_CYCLE;
#if EXPANSION1
            MachineTinkerEffectDuration.multiplier = MechanicsStationOptions.Instance.MachineTinkerEffectDurationPerSkill / 100;
#endif
        }

        // сделать постройку улучшаемой
        internal static Tinkerable MakeMachineTinkerable(GameObject go)
        {
            var tinkerable = Tinkerable.MakePowerTinkerable(go);
            tinkerable.tinkerMaterialTag = MechanicsStationConfig.TINKER_TOOLS;
            tinkerable.tinkerMaterialAmount = 1f;
            tinkerable.addedEffect = MACHINE_TINKER_EFFECT_NAME;
            tinkerable.requiredSkillPerk = REQUIRED_ROLE_PERK;
            tinkerable.SetWorkTime(MACHINE_TINKERABLE_WORKTIME);
            tinkerable.choreTypeTinker = Db.Get().ChoreTypes.MachineTinker.IdHash;
            tinkerable.choreTypeFetch = Db.Get().ChoreTypes.MachineFetch.IdHash;
            // увеличение времени эффекта в длц
#if EXPANSION1
            tinkerable.effectAttributeId = Db.Get().Attributes.Machinery.Id;
            tinkerable.effectMultiplier = MACHINE_TINKER_EFFECT_DURATION_PER_SKILL;
#endif
            go.AddOrGet<RoomTracker>().requiredRoomType = Db.Get().RoomTypes.MachineShop.Id;
            // а это для корректного изменения времени работы после изменения в настройках
            go.GetComponent<KPrefabID>().prefabSpawnFn += delegate (GameObject prefab)
            {
                var _tinkerable = prefab.GetComponent<Tinkerable>();
                if (_tinkerable != null)
                {
                    _tinkerable.workTime = MechanicsStationOptions.Instance.MachineTinkerableWorkTime;
                    _tinkerable.WorkTimeRemaining = Mathf.Min(_tinkerable.WorkTimeRemaining, _tinkerable.workTime);
#if EXPANSION1
                    _tinkerable.effectMultiplier = MechanicsStationOptions.Instance.MachineTinkerEffectDurationPerSkill / 100;
#endif
                }
            };
            // если "Rooms Expanded" найден, добавляем в кухонные постройки компонент для работы в нескольких комнатах.
            if (RoomsExpandedFound && go.HasTag(KitchenBuildingTag))
            {
                var multiRoomTracker = go.AddOrGet<MultiRoomTracker>();
                multiRoomTracker.possibleRoomTypes = new string[] { Db.Get().RoomTypes.MachineShop.Id, KitchenRoom.Id };
            }
            return tinkerable;
        }
    }
}
