using STRINGS;

namespace DeathReimagined
{
    //TODO: дописать правильные названия и описания
    public class STRINGS
    {
        public class DUPLICANTS
        {
            public class ATTRIBUTES
            {
                public class HEARTATTACKSUSCEPTIBILITY
                {
                    public static LocString NAME = "Susceptibility to Heart Attack"; // Восприимчивость к сердечному приступу
                    public static LocString DESC = string.Concat(new string[] {
                        "Duplicants with a higher ",
                        UI.PRE_KEYWORD,
                        "Susceptibility",
                        UI.PST_KEYWORD,
                        " are more likely to develop a ",
                        UI.PRE_KEYWORD,
                        "Heart attack",
                        UI.PST_KEYWORD,
                        " at ",
                        UI.PRE_KEYWORD,
                        "High Stress",
                        UI.PST_KEYWORD } ); // Дупликанты с более высокой восприимчивостью более склонны к развитию сердечного приступа при высоком стрессе
                    public static LocString AGE_MODIFIER = "Age factor"; //  Фактор возраста
                }

                public class HEARTATTACKSICKNESSCURESPEED
                {
                    public static LocString NAME = "Heart Attack Cure Speed"; // Скорость лечения сердечного приступа
                }
            }

            public class DISEASES
            {
                public class HEARTATTACKSICKNESS
                {
                    public static LocString NAME = "Heart Attack"; // Сердечный приступ
                    public static LocString EXPOSURE = "High stress"; // Сильный стресс
//                    public static LocString DESCRIPTIVE_SYMPTOMS = "DESCRIPTIVE_SYMPTOMS"; // 
//                    public static LocString DESCRIPTION = "DESCRIPTION"; // 
//                    public static LocString LEGEND_HOVERTEXT = "LEGEND_HOVERTEXT"; // 
                    public static LocString SYMPTOMS = "Causes " + UI.PRE_KEYWORD + "Incapacitation" + UI.PST_KEYWORD; // Вызывает Недееспособность
                }
            }

            public class MODIFIERS
            {
                /*
                public class _
                {
                    public static LocString NAME = "";
                    public static LocString TOOLTIP = "";
                }
                */

                public class FUNERAL
                {
                    public static LocString NAME = "Funeral"; // Похороны
                    public static LocString TOOLTIP = "This Duplicant attended the funeral of a friend. It made him feel a little better."; // Этот Дупликант присутствовал на похоронах друга. Это заставило его почувствовать себя немного лучше.
                }

                public class MELANCHOLY
                {
                    public static LocString NAME = "Existential Melancholy"; // Экзистенциальная меланхолия
                    public static LocString TOOLTIP = "";
                }

                public class UNBURIED_CORPSE
                {
                    public static LocString NAME = "Duplicant left unburied";  // Дупликант не похоронен
                    public static LocString TOOLTIP = "One of us died and we're just leaving them lying in the open. Nobody should be left like that. Duplicantes deserve a decent burial"; // Один из нас умер и мы бросили его под открытым небом. Так нельзя. Дупликанты заслуживают достойных похорон
                }

                public class OBSERVED_CORPSE
                {
                    public static LocString NAME = "Observed Corpse"; // Вид трупа
                    public static LocString TOOLTIP = "This Duplicant saw the corpse of a friend"; // Этот Дупликант видел труп друга
                }

                public class OBSERVED_ROTTEN_CORPSE
                {
                    public static LocString NAME = "Observed Rotten Corpse"; // Вид гниющего трупа
                    public static LocString TOOLTIP = "This Duplicant saw a rotten decaying corpse. Yuck!"; // Этот Дупликант видел гнилой разлагающийся труп. Фу!
                }

                public class DESTROYED_CORPSE  // todo: тут должно быть чтото про феерический кошмар
                {
                    public static LocString NAME = "***Existential Angst";
                    public static LocString TOOLTIP = "";
                }

                public class GRAVE_DESECRATION
                {
                    public static LocString NAME = "Desecration of Grave"; // Осквернение могилы
                    public static LocString TOOLTIP = "This Duplicant is saddened that grave of а friend has been destroyed"; // Этот Дупликант опечален тем, что могила друга была разрушена
                }

            }
        }

        public class ITEMS
        {
            public class INDUSTRIAL_PRODUCTS
            {
                public class SKELETON
                {
                    public static LocString NAME = "Skeleton"; // Скелет
                    public static LocString DESC = "Completely decomposed remains of a deceased Duplicant.\n\n{crushed}"; // Полностью разложившиеся останки умершего дупликанта.
                }
            }
        }
    }
}
