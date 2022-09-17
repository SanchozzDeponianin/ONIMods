using System.Collections.Generic;
using Klei;
using KSerialization;
using TUNING;
using UnityEngine;
using PeterHan.PLib.Detours;

namespace BuildableGeneShuffler
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class BuildableGeneShuffler : StateMachineComponent<BuildableGeneShuffler.StatesInstance>
    {
        public class StatesInstance : GameStateMachine<States, StatesInstance, BuildableGeneShuffler, object>.GameInstance
        {
            public StatesInstance(BuildableGeneShuffler smi) : base(smi) { }

            public GameObject GetMorb() => master.storage.FindFirst(GlomConfig.ID);

            public bool HasMorb() => GetMorb() != null;

            public bool HasEnoughBrine() => master.storage.GetMassAvailable(SimHashes.Brine) >= BuildableGeneShufflerConfig.brine_mass;

            public bool IsReady() => HasEnoughBrine() && HasMorb();

            private static readonly IDetouredField<FetchOrder2, Tag> RequiredFetchTag
                = PDetours.DetourFieldLazy<FetchOrder2, Tag>(nameof(FetchOrder2.RequiredTag));

            public FetchList2 CreateFetchList()
            {
                var fetchList = new FetchList2(master.storage, Db.Get().ChoreTypes.DoctorFetch);
                fetchList.Add(GlomConfig.ID, null, 1f, Operational.State.Functional);
                RequiredFetchTag.Set(fetchList.FetchOrders[0], GameTags.Creatures.Deliverable);
                return fetchList;
            }

            public Chore CreateChore()
            {
                var workChore = new WorkChore<GeneShufflerPrepare>(
                    chore_type: Db.Get().ChoreTypes.GeneShuffle,
                    target: master.workable,
                    schedule_block: Db.Get().ScheduleBlockTypes.Work,
                    only_when_operational: true);
                return workChore;
            }
        }

        public class States : GameStateMachine<States, StatesInstance, BuildableGeneShuffler>
        {
            public class AwaitingState : State
            {
                public State brine;
                public State morb;
            }
            public class PreparingState : State
            {
                public State waiting;
                public State working;
                public State working_pst;
            }
            public AwaitingState awaiting;
            public PreparingState preparing;
            public State spawn_geneshuffler;

            public override void InitializeStates(out BaseState default_state)
            {
                default_state = awaiting;
                awaiting
                    .DefaultState(awaiting.brine)
                    .EnterTransition(preparing, smi => smi.IsReady())
                    .EnterTransition(awaiting.morb, smi => smi.HasEnoughBrine());
                awaiting.brine
                    .PlayAnim("empty")
                    .EventTransition(GameHashes.OnStorageChange, awaiting.morb, smi => smi.HasEnoughBrine());
                awaiting.morb
                    .PlayAnim("filled")
                    .ToggleFetch(smi => smi.CreateFetchList(), preparing);
                preparing
                    .DefaultState(preparing.waiting)
                    .EventTransition(GameHashes.OnStorageChange, awaiting, smi => !smi.IsReady())
                    .ToggleChore(smi => smi.CreateChore(), preparing.working_pst, preparing)
                    .Enter(smi => smi.GetMorb()?.AddTag(GameTags.Trapped))
                    .Exit(smi => smi.GetMorb()?.RemoveTag(GameTags.Trapped));
                preparing.waiting
                    .PlayAnim("morbed")
                    .Enter(smi => smi.master.meter.meterController.Queue("dirty", KAnim.PlayMode.Loop))
                    .WorkableStartTransition(smi => smi.master.workable, preparing.working);
                preparing.working
                    .PlayAnim("preparing_pre")
                    .QueueAnim("preparing_loop", true)
                    .Enter(smi => smi.master.meter.meterController.Queue("idle_loop", KAnim.PlayMode.Loop))
                    .WorkableStopTransition(smi => smi.master.workable, preparing.waiting);
                preparing.working_pst
                    .QueueAnim("preparing_pst")
                    .Enter(smi => smi.master.meter.meterController.Queue("death", KAnim.PlayMode.Once))
                    .EventTransition(GameHashes.AnimQueueComplete, smi => smi.master.meter.gameObject.GetComponent<KPrefabID>(), spawn_geneshuffler);
                spawn_geneshuffler
                    .Enter(smi => smi.master.SpawnGeneShuffler());
            }
        }

        public class GeneShufflerPrepare : Workable
        {
            protected override void OnPrefabInit()
            {
                base.OnPrefabInit();
                overrideAnims = new KAnimFile[] { Assets.GetAnim("anim_interacts_medical_bed_doctor_kanim") };
                workAnims = new HashedString[] { "working_loop" };
                faceTargetWhenWorking = true;
                synchronizeAnims = false;
                resetProgressOnStop = false;
                SetWorkTime(BuildableGeneShufflerOptions.Instance.manipulationTime);
                attributeConverter = Db.Get().AttributeConverters.CompoundingSpeed;
                skillExperienceSkillGroup = Db.Get().SkillGroups.MedicalAid.Id;
                skillExperienceMultiplier = SKILLS.ALL_DAY_EXPERIENCE;
                requiredSkillPerk = Db.Get().SkillPerks.CanCompound.Id;
            }

            public override Vector3 GetFacingTarget() => transform.GetPosition() + Vector3.left;
            public override bool InstantlyFinish(Worker worker) => false;
        }

#pragma warning disable CS0649
        [MyCmpAdd]
        private GeneShufflerPrepare workable;

        [MyCmpReq]
        private Building building;

        [MyCmpReq]
        private Deconstructable deconstructable;

        [MyCmpReq]
        private Storage storage;
#pragma warning restore CS0649

        private MeterController meter;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            meter = new MeterController(GetComponent<KBatchedAnimController>(), "snapto_morb", "meter", Meter.Offset.Behind, Grid.SceneLayer.NoLayer, "snapto_morb");
            meter.meterController.SwapAnims(new KAnimFile[] { Assets.GetAnim("glom_kanim") });
            smi.StartSM();
        }

        protected override void OnCleanUp()
        {
            smi.StopSM("");
            base.OnCleanUp();
        }

        private static readonly IDetouredField<LoreBearer, bool> BeenClicked =
                PDetours.DetourFieldLazy<LoreBearer, bool>("BeenClicked");

        private void SpawnGeneShuffler()
        {
            // sacrifice morb
            var morb = smi.GetMorb();
            if (morb != null)
            {
                storage.Drop(morb);
                morb.DeleteObject();
            }
            // спавним новый калибратор но без заряда и без лора
            var geneShuffler = GameUtil.KInstantiate(Assets.GetPrefab("GeneShuffler"), gameObject.transform.GetPosition(), Grid.SceneLayer.Building);
            geneShuffler.GetComponent<GeneShuffler>().IsConsumed = true;
            var loreBearer = geneShuffler.GetComponent<LoreBearer>();
            if (loreBearer != null)
                BeenClicked.Set(loreBearer, true);
            var builded = geneShuffler.GetComponent<BuildedGeneShuffler>();
            builded.isBuilded = true;
            // список конструкционных материалов
            var tag_list = new List<Tag>();
            var mass_list = new List<float>();
            for (int i = 0; i < deconstructable.constructionElements.Length; i++)
            {
                tag_list.Add(deconstructable.constructionElements[i]);
                mass_list.Add(building.Def.Mass[i]);
            }
            // вычисляем конечную температуру и микробов, с учетом самой постройки и хранилищща
            var geneShufflerPE = geneShuffler.GetComponent<PrimaryElement>();
            var MyPE = GetComponent<PrimaryElement>();
            geneShufflerPE.SetElement(MyPE.ElementID);
            geneShufflerPE.AddDisease(MyPE.DiseaseIdx, MyPE.DiseaseCount, "");
            float mass = MyPE.Mass;
            float temp = MyPE.Temperature;
            for (int i = 0; i < storage.Count; i++)
            {
                var itemPE = storage.items[i].GetComponent<PrimaryElement>();
                tag_list.Add(itemPE.Element.tag);
                mass_list.Add(itemPE.Mass);
                temp = SimUtil.CalculateFinalTemperature(mass * MyPE.Element.specificHeatCapacity, temp, itemPE.Mass * itemPE.Element.specificHeatCapacity, itemPE.Temperature);
                mass += itemPE.Mass;
                geneShufflerPE.AddDisease(itemPE.DiseaseIdx, itemPE.DiseaseCount, "");
            }
            geneShufflerPE.Temperature = temp;
            geneShuffler.GetComponent<Deconstructable>().constructionElements = tag_list.ToArray();
            builded.constructionMass = mass_list.ToArray();
            storage.ConsumeAllIgnoringDisease();
            geneShuffler.SetActive(true);
            gameObject.DeleteObject();
        }
    }
}
