using System.Linq;
using Database;
using Klei.AI;
using TUNING;
using HarmonyLib;
using UnityEngine;
using PeterHan.PLib.UI;

namespace ExoticSpices
{
    internal static class ExoticSpicesAssets
    {
        public const string FLATULENCE = "Flatulence";

        public const float FLOWER_PER_1000KKAL = 0.2f;
        public const float GRASS_PER_1000KKAL = 0.1f;
        public const float EVIL_SEED_PER_1000KKAL = 0.01f;
        public const float MASS_PER_1000KKAL = 3f;

        public const string PHOSPHO_RUFUS_SPICE = "PHOSPHO_RUFUS_SPICE";
        public const string GASSY_MOO_SPICE = "MOO_COSPLAY_SPICE";
        public const string ZOMBIE_SPICE = "ZOMBIE_COSPLAY_SPICE";

        private const string SPRITE_PHOSPHO_RUFUS = "spice_recipe_phospho_rufus";
        private const string SPRITE_GASSYMOO = "spice_recipe_gassy_moo";
        private const string SPRITE_ZOMBIE_SPORES = "spice_recipe_zombie_spores";

        public const string ANIM_IDLE_ZOMBIE = "anim_idle_zombie_kanim";
        public const string ANIM_LOCO_ZOMBIE = "anim_loco_zombie_kanim";
        public const string ANIM_LOCO_WALK_ZOMBIE = "anim_loco_walk_zombie_kanim";
        public const string ANIM_REACT_BUTT_SCRATCH = "anim_react_butt_scratch_happy_kanim";

        public static readonly Tag Tireless = TagManager.Create(nameof(Tireless));

        public static Attribute JoyReactionExtraChance;
        private static Spice PhosphoRufusSpice;
        private static Spice GassyMooSpice;
        private static Spice ZombieSpice;

        public static Emote ButtScratchEmote;
        public static Emote ZombieControlEmote;

        public static float GassyMooSpiceEmitMass = TRAITS.FLATULENCE_EMIT_MASS;
        public static SimHashes GassyMooSpiceEmitElement = SimHashes.Methane;

        internal static void LoadSprites()
        {
            foreach (var name in new string[] { SPRITE_PHOSPHO_RUFUS, SPRITE_GASSYMOO, SPRITE_ZOMBIE_SPORES })
            {
                var sprite = PUIUtils.LoadSprite($"sprites/{name}.png");
                sprite.name = name;
                Assets.Sprites.Add(name, sprite);
            }
        }

        internal static void InitStage1()
        {
            ExoticSpicesOptions.Reload();
            var db = Db.Get();
            // модификаторы доп шанса на реакцию радовасти
            JoyReactionExtraChance = db.Attributes.Add(new Attribute(
                id: "Joy_Extra_Chance",
                is_trainable: false,
                show_in_ui: Attribute.Display.General,
                is_profession: false,
                base_value: 0));
            JoyReactionExtraChance.SetFormatter(new PercentAttributeFormatter());

            const string desc = "Spices";
            var PhosphoRufusExtraChance = new AttributeModifier(
                attribute_id: JoyReactionExtraChance.Id,
                value: ExoticSpicesOptions.Instance.phospho_rufus_spice.joy_reaction_chance / 100f,
                description: desc,
                is_readonly: false);
            var GassyMooExtraChance = new AttributeModifier(
                attribute_id: JoyReactionExtraChance.Id,
                value: ExoticSpicesOptions.Instance.gassy_moo_spice.joy_reaction_chance / 100f,
                description: desc,
                is_readonly: false);
            var ZombieExtraChance = new AttributeModifier(
                attribute_id: JoyReactionExtraChance.Id,
                value: ExoticSpicesOptions.Instance.zombie_spice.joy_reaction_chance / 100f,
                description: desc,
                is_readonly: false);

            // новые специи
            PhosphoRufusSpice = new Spice(
                parent: db.Spices,
                id: PHOSPHO_RUFUS_SPICE,
                ingredients: new Spice.Ingredient[] {
                    new Spice.Ingredient { IngredientSet = new Tag[] { SwampLilyFlowerConfig.ID }, AmountKG = FLOWER_PER_1000KKAL },
                    new Spice.Ingredient { IngredientSet = new Tag[] { SimHashes.Phosphorus.CreateTag() }, AmountKG = MASS_PER_1000KKAL }
                },
                primaryColor: new Color(0.255f, 0.573f, 0.204f),
                secondaryColor: Color.white,
                statBonus: PhosphoRufusExtraChance,
                imageName: SPRITE_PHOSPHO_RUFUS);

            GassyMooSpice = new Spice(
                parent: db.Spices,
                id: GASSY_MOO_SPICE,
                ingredients: new Spice.Ingredient[] {
                    new Spice.Ingredient { IngredientSet = new Tag[] { GasGrassHarvestedConfig.ID }, AmountKG = GRASS_PER_1000KKAL },
                    new Spice.Ingredient { IngredientSet = new Tag[] { SimHashes.Sulfur.CreateTag() }, AmountKG = MASS_PER_1000KKAL }
                },
                primaryColor: new Color(0.796f, 0.443f, 0.455f),
                secondaryColor: Color.white,
                statBonus: GassyMooExtraChance,
                imageName: SPRITE_GASSYMOO);

            ZombieSpice = new Spice(
                parent: db.Spices,
                id: ZOMBIE_SPICE,
                ingredients: new Spice.Ingredient[] {
                    new Spice.Ingredient { IngredientSet = new Tag[] { EvilFlowerConfig.SEED_ID }, AmountKG = EVIL_SEED_PER_1000KKAL },
                    new Spice.Ingredient { IngredientSet = new Tag[] { SimHashes.Naphtha.CreateTag() }, AmountKG = MASS_PER_1000KKAL }
                },
                primaryColor: new Color(0.616f, 0.220f, 0.243f),
                secondaryColor: Color.white,
                statBonus: ZombieExtraChance,
                imageName: SPRITE_ZOMBIE_SPORES);
            // todo: научную специю

            // эмоции
            ButtScratchEmote = new Emote(db.Emotes.Minion, "ButtScratch", new EmoteStep[] { new EmoteStep { anim = "react" } }, ANIM_REACT_BUTT_SCRATCH);

            ZombieControlEmote = new Emote(db.Emotes.Minion, "ZombieControl", new EmoteStep[] {
                new EmoteStep { anim = "trans_idle_zombidle" },
                new EmoteStep { anim = "zombidle" },
                new EmoteStep { anim = "trans_zombidle_idle" },
                }, "anim_zombie_control_kanim");

            // морда лица
            db.Faces.Zombie.headFXHash = db.Faces.SickSpores.headFXHash;
        }

        internal static void InitStage2()
        {
            // эффекты игра сама создает при инициализации гриндера
            // добавляем в них доп. модификаторы
            var db = Db.Get();

            var PhosphoRufusEffect = db.effects.TryGet(PHOSPHO_RUFUS_SPICE);
            if (PhosphoRufusEffect != null)
            {
                PhosphoRufusEffect.Add(new AttributeModifier(
                    attribute_id: db.Attributes.ToiletEfficiency.Id,
                    value: 1f,
                    description: PhosphoRufusEffect.Name,
                    is_multiplier: true,
                    uiOnly: false,
                    is_readonly: true));
            }

            var GassyMooEffect = db.effects.TryGet(GASSY_MOO_SPICE);
            if (GassyMooEffect != null)
            {
                GassyMooEffect.Add(new AttributeModifier(
                    attribute_id: db.Attributes.Athletics.Id,
                    value: ExoticSpicesOptions.Instance.gassy_moo_spice.attribute_buff,
                    description: GassyMooEffect.Name,
                    is_multiplier: false,
                    uiOnly: false,
                    is_readonly: true));
                GassyMooEffect.Add(new AttributeModifier(
                    attribute_id: db.Attributes.Strength.Id,
                    value: ExoticSpicesOptions.Instance.gassy_moo_spice.attribute_buff,
                    description: GassyMooEffect.Name,
                    is_multiplier: false,
                    uiOnly: false,
                    is_readonly: true));
            }

            var ZombieEffect = db.effects.TryGet(ZOMBIE_SPICE);
            if (ZombieEffect != null)
            {
                ZombieEffect.Add(new AttributeModifier(
                    attribute_id: db.Amounts.Stamina.deltaAttribute.Id,
                    value: db.Amounts.Stamina.maxAttribute.BaseValue * ExoticSpicesOptions.Instance.zombie_spice.stamina_buff / 100f / Constants.SECONDS_PER_CYCLE,
                    description: ZombieEffect.Name,
                    is_multiplier: false,
                    uiOnly: false,
                    is_readonly: true));
            }

            // посчитаем массу газа
            var moo = Traverse.Create<MooConfig>();
            var grass = CROPS.CROP_TYPES.First(crop => crop.cropId == GasGrassHarvestedConfig.ID);
            var mass_per_day = GRASS_PER_1000KKAL * moo.Field<float>("DAYS_PLANT_GROWTH_EATEN_PER_CYCLE").Value
                * moo.Field<float>("KG_POOP_PER_DAY_OF_PLANT").Value * (grass.cropDuration / Constants.SECONDS_PER_CYCLE) / grass.numProduced;
            GassyMooSpiceEmitMass = mass_per_day * (TRAITS.FLATULENCE_EMIT_INTERVAL_MAX + TRAITS.FLATULENCE_EMIT_INTERVAL_MIN) / 2f / Constants.SECONDS_PER_CYCLE;
            switch (ExoticSpicesOptions.Instance.gassy_moo_spice.emit_mass)
            {
                case EmitMass.x0_25:
                    GassyMooSpiceEmitMass *= 0.25f;
                    break;
                case EmitMass.x0_5:
                    GassyMooSpiceEmitMass *= 0.5f;
                    break;
                case EmitMass.x2:
                    GassyMooSpiceEmitMass *= 2f;
                    break;
                case EmitMass.x4:
                    GassyMooSpiceEmitMass *= 4f;
                    break;
            }
            GassyMooSpiceEmitElement = (ExoticSpicesOptions.Instance.gassy_moo_spice.emit_gas == EmitGas.Methane) ? SimHashes.Methane : SimHashes.ContaminatedOxygen;
        }

        public static void CreateEmoteChore(IStateMachineTarget target, Emote emote, float probability)
        {
            if (probability > Random.value)
                new EmoteChore(target.GetComponent<ChoreProvider>(), Db.Get().ChoreTypes.EmoteHighPriority, emote);
        }
    }
}
