using STRINGS;

namespace AquaticFarm
{
    public class STRINGS
    {
        public static LocString MOD_TITLE = "";
        public static LocString MOD_DESCRIPTION = "";

        public class BUILDINGS
        {
            public class PREFABS
            {
                public class AQUATICFARM
                {
                    public static LocString NAME = UI.FormatAsLink("Aquatic Farm", "AQUATICFARM");
                    public static LocString DESC = "Aquatic farms reduce Duplicant traffic by automating irrigating crops.";
                    public static LocString EFFECT = string.Concat(new string[]                   
                    {
                       "Grows one ",
                        UI.FormatAsLink("Plant", "PLANTS"),
                        " from a ",
                        UI.FormatAsLink("Seed", "PLANTS"),
                        ".\n\nCan be used as floor tile and rotated before construction.\n\nAbsorbs ",
                        UI.FormatAsLink("Liquids", "ELEMENTS_LIQUID"),
                        " from the world for irrigation. Does not require ",
                        UI.FormatAsLink("Liquid Piping", "LIQUIDPIPING"),
                        "."
                    });
                }
            }
        }
    }
}
