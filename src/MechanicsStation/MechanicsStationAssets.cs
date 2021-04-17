using Database;
using Klei.AI;
using STRINGS;
using UnityEngine;

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
        public const float MACHINE_TINKERABLE_WORKTIME = 20f;
        public const string REQUIRED_ROLE_PERK = "CanMachineTinker";

        private static SkillPerk CanMachineTinker;
        internal static Attribute CraftingSpeed;
        private static AttributeModifier MachinerySpeedModifier;
        private static AttributeModifier CraftingSpeedModifier;
        private static Effect MachineTinkerEffect;

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

            // добавляем перк для работы на станции
            CanMachineTinker = db.SkillPerks.Add(new SimpleSkillPerk(REQUIRED_ROLE_PERK, STRINGS.PERK_CAN_MACHINE_TINKER.DESCRIPTION));
            db.Skills.Technicals1.perks.Add(CanMachineTinker);

            // добавляем модификаторы и эффекты 
            string text = DUPLICANTS.MODIFIERS.MACHINETINKER.NAME;
            string description = STRINGS.DUPLICANTS.MODIFIERS.MACHINETINKER.TOOLTIP;

            CraftingSpeed = db.Attributes.Add(new Attribute(CRAFTING_SPEED_MODIFIER_NAME, false, Attribute.Display.General, false, BASE_SPEED_VALUE));
            CraftingSpeed.SetFormatter(new PercentAttributeFormatter());

            MachinerySpeedModifier = new AttributeModifier(MACHINERY_SPEED_MODIFIER_NAME, MACHINERY_SPEED_MODIFIER, text, is_readonly: false);
            CraftingSpeedModifier = new AttributeModifier(CRAFTING_SPEED_MODIFIER_NAME, CRAFTING_SPEED_MODIFIER, text, is_readonly: false);

            MachineTinkerEffect = db.effects.Add(new Effect(MACHINE_TINKER_EFFECT_NAME, text, description, MACHINE_TINKER_EFFECT_DURATION * Constants.SECONDS_PER_CYCLE, true, true, false));
            MachineTinkerEffect.Add(MachinerySpeedModifier);
            MachineTinkerEffect.Add(CraftingSpeedModifier);
        }

        internal static void LoadOptions()
        {
            MechanicsStationOptions.Reload();
            MachinerySpeedModifier.SetValue(MechanicsStationOptions.Instance.MachinerySpeedModifier / 100);
            CraftingSpeedModifier.SetValue(MechanicsStationOptions.Instance.CraftingSpeedModifier / 100);
            MachineTinkerEffect.duration = MechanicsStationOptions.Instance.MachineTinkerEffectDuration * Constants.SECONDS_PER_CYCLE;
        }

        // сделать постройку улучшаемой
        internal static Tinkerable MakeMachineTinkerable(GameObject go)
        {
            // todo: увеличение времени эффекта в длц
            var tinkerable = Tinkerable.MakePowerTinkerable(go);
            tinkerable.tinkerMaterialTag = MechanicsStationConfig.TINKER_TOOLS;
            tinkerable.tinkerMaterialAmount = 1f;
            tinkerable.addedEffect = MACHINE_TINKER_EFFECT_NAME;
            tinkerable.requiredSkillPerk = REQUIRED_ROLE_PERK;
            tinkerable.SetWorkTime(MACHINE_TINKERABLE_WORKTIME);
            tinkerable.choreTypeTinker = Db.Get().ChoreTypes.MachineTinker.IdHash;
            tinkerable.choreTypeFetch = Db.Get().ChoreTypes.MachineFetch.IdHash;
            go.AddOrGet<RoomTracker>().requiredRoomType = Db.Get().RoomTypes.MachineShop.Id;
            // а это для корректного изменения времени работы после изменения в настройках
            go.GetComponent<KPrefabID>().prefabSpawnFn += delegate (GameObject prefab)
            {
                var _tinkerable = prefab.GetComponent<Tinkerable>();
                if (_tinkerable != null)
                {
                    _tinkerable.workTime = MechanicsStationOptions.Instance.MachineTinkerableWorkTime;
                    _tinkerable.WorkTimeRemaining = Mathf.Min(_tinkerable.WorkTimeRemaining, _tinkerable.workTime);
                }
            };
            return tinkerable;
        }
    }
}
