using System.Linq;
using UnityEngine;
using Klei.AI;
using PeterHan.PLib.Detours;

namespace ExoticSpices
{
    using static ModAssets;

    public class DupeEffectZombie : GameStateMachine<DupeEffectZombie, DupeEffectZombie.Instance>
    {
        private static readonly IDetouredField<UrgeMonitor.Instance, float> inScheduleThreshold =
            PDetours.DetourField<UrgeMonitor.Instance, float>("inScheduleThreshold");

        public new class Instance : GameInstance
        {
            private float StaminaThresholdNormal;
            private float StaminaThresholdZombie;
            private Effects effects;
            private KBatchedAnimController fx;

            public Instance(IStateMachineTarget master) : base(master)
            {
                StaminaThresholdNormal = Db.Get().Amounts.Stamina.maxAttribute.BaseValue;
                StaminaThresholdZombie = 0.15f * StaminaThresholdNormal;
                effects = master.GetComponent<Effects>();
            }

            private UrgeMonitor.Instance GetUrgeSMI()
            {
                return gameObject.GetAllSMI<UrgeMonitor.Instance>()?.First(smi => smi.GetUrge() == Db.Get().Urges.Sleep);
            }

            public bool ShouldBeZombie() => effects.HasEffect(ZOMBIE_SPICE);
            public void OnEnter()
            {
                // идти спать только если мало стамины
                var urgeSMI = GetUrgeSMI();
                if (urgeSMI != null)
                    inScheduleThreshold.Set(urgeSMI, StaminaThresholdZombie);
                // эффект спор
                fx = FXHelpers.CreateEffect("spore_fx_kanim", gameObject.transform.GetPosition() + new Vector3(0f, 0f, -0.1f), gameObject.transform, true, Grid.SceneLayer.Front, false);
                fx.Play("working_loop", KAnim.PlayMode.Loop);
            }

            public void OnExit()
            {
                // восстанавливаем как было
                var urgeSMI = GetUrgeSMI();
                if (urgeSMI != null)
                    inScheduleThreshold.Set(urgeSMI, StaminaThresholdNormal);
                fx?.gameObject?.DeleteObject();
                fx = null;
            }
        }

#pragma warning disable CS0649
        private State zombie_off;
        private State zombie_on;
#pragma warning restore CS0649

        public override void InitializeStates(out BaseState default_state)
        {
            default_state = zombie_off;
            root
                .EnterTransition(zombie_on, smi => smi.ShouldBeZombie());
            zombie_off
                .EventTransition(GameHashes.EffectAdded, zombie_on, smi => smi.ShouldBeZombie());
            zombie_on
                .EventTransition(GameHashes.EffectRemoved, zombie_off, smi => !smi.ShouldBeZombie())
                .ToggleExpression(smi => Db.Get().Expressions.Zombie)
                .ToggleTag(Tireless)
                .ToggleAnims(ANIM_IDLE_ZOMBIE)
                .ToggleAnims(ANIM_LOCO_ZOMBIE)
                .ToggleAnims(ANIM_LOCO_WALK_ZOMBIE)
                .Enter(smi => smi.OnEnter())
                .Exit(smi => smi.OnExit())
                .EventHandler(GameHashes.EatCompleteEater, smi => CreateEmoteChore(smi.master, ZombieControlEmote, 1f))
                .EventHandler(GameHashes.SleepFinished, smi => CreateEmoteChore(smi.master, ZombieControlEmote, 0.35f));
        }
    }
}
