using System.Linq;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.PatchManager;

namespace DontCrabWildCritters
{
    internal class Patches : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            if (this.LogModVersion()) return;
            base.OnLoad(harmony);
            new PPatchManager(harmony).RegisterPatchClass(typeof(Patches));
        }

        [PLibMethod(RunAt.BeforeDbInit)]
        private static void BeforeDbInit()
        {
            Utils.InitLocalization(typeof(STRINGS));
        }

        [PLibMethod(RunAt.BeforeDbPostProcess)]
        private static void BeforeDbPostProcess()
        {
            var UnTameable = new[] { GameTags.Creatures.Species.GlomSpecies,
                GameTags.Creatures.Species.BeetaSpecies,
                GameTags.Creatures.Species.MosquitoSpecies };
            foreach (var brain in Assets.GetPrefabsWithComponentAsListOfComponents<CreatureBrain>())
            {
                var egg = brain.GetDef<EggProtectionMonitor.Def>();
                var threat = brain.GetDef<ThreatMonitor.Def>();
                if (egg != null && threat != null)
                {
                    egg.allyTags = (egg.allyTags ?? new Tag[0]).Append(GameTags.Creatures.Wild);
                    threat.friendlyCreatureTags = (threat.friendlyCreatureTags ?? new Tag[0]).Append(GameTags.Creatures.Wild);
                }
                if (UnTameable.Contains(brain.species))
                    brain.AddTag(GameTags.Creatures.Wild);
            }
        }
    }
}
