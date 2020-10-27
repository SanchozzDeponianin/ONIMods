using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using Harmony;
using UnityEngine;
using TUNING;
using SanchozzONIMods.Lib;

//using PeterHan.PLib;
//using PeterHan.PLib.Datafiles;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;

namespace Test
{
    public static class Patches
    {
        public static void OnLoad(string path)
        {
//            PUtil.InitLibrary(true);
//            PLocalization.Register();
            POptions.RegisterOptions(typeof(Options));

            //Localization.GenerateStringsTemplate(typeof(PUIStrings), Path.Combine(POptions.GetModDir(Assembly.GetExecutingAssembly()) , PLocalization.TRANSLATIONS_DIR));
        }


        
        [HarmonyPatch(typeof(Localization), "Initialize")]
        internal static class Localization_Initialize
        {
            private static void Postfix(Localization.Locale ___sLocale)
            {
                //Utils.InitLocalization(typeof(STRINGS), ___sLocale, true);
                Utils.InitLocalization(typeof(PUIStrings), ___sLocale, "peterhan.plib.ui_", false);
                //LocString.CreateLocStringKeys(typeof(STRINGS.BUILDINGS));
            }
        }


        [HarmonyPatch(typeof(Game), "OnSpawn")]
        internal static class Game_OnSpawn
        {
            private static void Postfix()
            {
                Debug.Log("Game_OnSpawn");
                Options options = POptions.ReadSettings<Options>() ?? new Options();
                Debug.Log("Watts = " + options.Watts);
            }
        }

    }


    /*
    [HarmonyPatch(typeof(LogicDuplicantSensor), "RefreshReachableCells")]
    public class LogicDuplicantSensor_RefreshReachableCells
    {
        public static void Prefix()
        {
            Debug.Log("LogicDuplicantSensor_RefreshReachableCells");
        }
    }

    [HarmonyPatch(typeof(LogicDuplicantSensor), "RefreshPickupables")]
    public class LogicDuplicantSensor_RefreshPickupables
    {
        public static void Prefix(bool ___pickupablesDirty)
        {
            if (___pickupablesDirty)
            {
                Debug.Log("LogicDuplicantSensor_RefreshPickupables");
            }
        }
    }

    [HarmonyPatch(typeof(LogicDuplicantSensor), "OnPickupablesChanged")]
    public class LogicDuplicantSensor_OnPickupablesChanged
    {
        public static void Prefix()
        {
            Debug.Log("LogicDuplicantSensor_OnPickupablesChanged");
        }
    }
    */

    /*
    [HarmonyPatch(typeof(Diet.Info), MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(HashSet<Tag>), typeof(Tag), typeof(float), typeof(float), typeof(string), typeof(float), typeof(bool), typeof(bool) } )]
    class Patch
    {
        static void Postfix (Diet.Info __instance, float disease_per_kg_produced)
        {
            Traverse.Create(__instance).Property("diseasePerKgProduced").SetValue(disease_per_kg_produced);
        }
    }
    */
    /*
    [HarmonyPatch(typeof(PickledMealConfig), "CreatePrefab")]
    public class Patch_Pickled_Meal
    {
        public static void Prefix()
        {
            FOOD.FOOD_TYPES.PICKLEDMEAL.Quality = 0;
        }
    }
    */


}
