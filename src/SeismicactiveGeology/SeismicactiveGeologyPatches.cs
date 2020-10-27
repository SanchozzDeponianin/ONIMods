using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Database;
using Klei.AI;
using STRINGS;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace SeismicactiveGeology
{
    public class SeismicactiveGeologyPatches
    {
        public static readonly Tag GeyserTag = TagManager.Create("Geyser");

        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        public class GeneratedBuildings_LoadGeneratedBuildings
        {
            public static void Prefix()
            {
                Utils.AddBuildingToPlanScreen("Equipment", BurnerConfig.ID, "ShearingStation");  // подумать куда !!
            }
        }

        /*
        [HarmonyPatch(typeof(Db), "Initialize")]
        public class Db_Initialize
        {
            public static void Prefix()
            {
                Utils.AddBuildingToTechnology("AnimalControl", ButcherStationConfig.ID);      // подумать куда !!
            }
        }
        */

        // сделать гейзер местом присобачивания
        [HarmonyPatch(typeof(GeyserGenericConfig), "CreateGeyser")]
        public static class GeyserGenericConfig_CreateGeyser
        {
            public static void Postfix(ref GameObject __result)
            {
                __result.AddOrGet<BuildingAttachPoint>().points = new BuildingAttachPoint.HardPoint[]
                    { new BuildingAttachPoint.HardPoint(new CellOffset(0, 0), GeyserTag, null) };
            }
        }
    }
}
