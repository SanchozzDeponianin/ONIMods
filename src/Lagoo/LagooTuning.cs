using System.Collections.Generic;

namespace Lagoo
{
    public static class LagooTuning
    {
        public const SimHashes EMIT_ELEMENT = SimHashes.ToxicSand;
        public const float DAYS_PLANT_GROWTH_EATEN_PER_CYCLE = 0.5f;
        public static float CALORIES_PER_DAY_OF_PLANT_EATEN = SquirrelTuning.STANDARD_CALORIES_PER_CYCLE / DAYS_PLANT_GROWTH_EATEN_PER_CYCLE;
        public const float KG_POOP_PER_DAY_OF_PLANT = 50f;
        public const float KG_ORE_EATEN_PER_CYCLE = 50f;
        public static float CALORIES_PER_KG_OF_ORE = SquirrelTuning.STANDARD_CALORIES_PER_CYCLE / KG_ORE_EATEN_PER_CYCLE;
        public const float MIN_POOP_SIZE_KG = 40f;
        public const int GERMS_EMMITED_PER_KG_POOPED = 100000;
        public const string GERM_ID_EMMITED_ON_POOP = "PollenGerms";

        public static List<FertilityMonitor.BreedingChance> EGG_CHANCES_LAGOO = new()
        {
            new FertilityMonitor.BreedingChance { egg = SquirrelConfig.EGG_ID,      weight = 0.25f },
            new FertilityMonitor.BreedingChance { egg = SquirrelHugConfig.EGG_ID,   weight = 0.05f },
            new FertilityMonitor.BreedingChance { egg = LagooConfig.EGG_ID, weight = 0.70f },
        };
    }
}
