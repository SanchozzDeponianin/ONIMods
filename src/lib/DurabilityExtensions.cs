using TUNING;
using UnityEngine;
using PeterHan.PLib.Detours;

namespace SanchozzONIMods.Lib
{
    public static class DurabilityExtensions
    {
        // оригинальные функции дают неправильное значение пока костюм надет
        // там в формуле перепутан плюс на минус, а также не учитываются кофициенты сложности игры и навыка дупеля

        public static IDetouredField<Durability, bool> isEquipped = PDetours.DetourFieldLazy<Durability, bool>("isEquipped");
        public static IDetouredField<Durability, float> durability = PDetours.DetourFieldLazy<Durability, float>("durability");
        public static IDetouredField<Durability, float> difficultySettingMod = PDetours.DetourFieldLazy<Durability, float>("difficultySettingMod");

        public static float GetTrueDurability(this Durability @this, MinionResume resume = null)
        {
            if (isEquipped.Get(@this))
            {
                float delta = GameClock.Instance.GetTimeInCycles() - @this.TimeEquipped;
                delta *= @this.durabilityLossPerCycle * difficultySettingMod.Get(@this);
                if (resume != null && resume.HasPerk(Db.Get().SkillPerks.ExosuitDurability.Id))
                {
                    delta *= 1f - EQUIPMENT.SUITS.SUIT_DURABILITY_SKILL_BONUS;
                }
                return Mathf.Clamp01(durability.Get(@this) + delta);
            }
            return durability.Get(@this);
        }

        public static bool IsTrueWornOut(this Durability @this, MinionResume resume = null)
        {
            return @this.GetTrueDurability(resume) <= 0f;
        }
    }
}
