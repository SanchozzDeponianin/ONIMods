using Klei.AI;
using STRINGS;
using TUNING;
using KSerialization;
using UnityEngine;
using SanchozzONIMods.Lib;

namespace DeathReimagined
{
    /*
        гниение трупов. основано на неиспользуемом коде из игры, но немного переписано
        добавлено: 
        * гниение трупа, визуалка гниения
        * выделение зомби микробов
        * выденение вонючих газов, визуалка, спецэффекты 
        * заражение воды
        * спавн морбов
        * спавн скелета
        * уничтожение трупа
        * дебафф на всех дуплов при уничтожении трупа
        * дебафф за вид свежего и гнилого трупов
    */
    [SerializationConfig(MemberSerialization.OptIn)]
    public class Decomposition : StateMachineComponent<Decomposition.SMInstance>
    {
        // прогресс гниения
        [Serialize]
        private float decomposition = 0;

        // сроки основаны на гниении пищи
        private static readonly float rottenTime = FOOD.SPOIL_TIME.DEFAULT;
        private static readonly float fullyDecompositionTime = FOOD.SPOIL_TIME.VERYSLOW;
        private static readonly Color rottenColor = new Color(0.51f, 0.7f, 0.51f, 1f);

        // модификаторы декора. всяты из оригинального кода
        public static readonly AttributeModifier freshDecorModifier = new AttributeModifier(Db.Get().BuildingAttributes.Decor.Id, -65, DUPLICANTS.MODIFIERS.DEAD.NAME, false, false, true);

        public static readonly AttributeModifier freshDecorRadiusModifier = new AttributeModifier(Db.Get().BuildingAttributes.DecorRadius.Id, 4, DUPLICANTS.MODIFIERS.DEAD.NAME, false, false, true);

        public static readonly AttributeModifier rottenDecorModifier = new AttributeModifier(Db.Get().BuildingAttributes.Decor.Id, -100, DUPLICANTS.MODIFIERS.ROTTING.NAME, false, false, true);

        public static readonly AttributeModifier rottenDecorRadiusModifier = new AttributeModifier(Db.Get().BuildingAttributes.DecorRadius.Id, 4, DUPLICANTS.MODIFIERS.ROTTING.NAME, false, false, true);

        // параметры выделения микробов болезни
        private static readonly string diseaseID = ZombieSpores.ID;
        private static readonly float diseaseEmitFrequency = 1f;
        private static readonly int diseaseAverageEmitPerSecond = 500;
        private static readonly int diseaseSingleEmitQuantity = 100000;

        // выделение газов
        private static readonly float gasEmitMass = 0.01f;
        private static readonly SimHashes gasEmitElement = SimHashes.ContaminatedOxygen;
        private static readonly SimHashes gasReplacedElement = SimHashes.Oxygen;
        private static readonly float gasMaxDistanceSq = 3.5f;
        private static readonly float submergedThreshold = 0.035f;

        // заражение воды
        private static readonly SimHashes waterEmitElement = SimHashes.DirtyWater;
        private static readonly SimHashes waterReplacedElement = SimHashes.Water;
        private static readonly int dirtyWaterMaxRange = 3;

        // морбы
        private static readonly int countRotMonsters = 3;

        public class SMInstance : GameStateMachine<States, SMInstance, Decomposition>.GameInstance
        {
            private KPrefabID kPrefabID;
            private PrimaryElement primaryElement;
            private float temperature { get => primaryElement.Temperature; }

            public SMInstance(Decomposition master) : base(master)
            {
                kPrefabID = smi.GetComponent<KPrefabID>();
                primaryElement = smi.GetComponent<PrimaryElement>();
            }

            public void UpdateDecomposition(float dt)
            {
                // todo: протестить при наличии нескольких трупов
                // скорость гниения в зависимости от температуры и окружения
                float rate = Mathf.InverseLerp(FOOD.DEFAULT_PRESERVE_TEMPERATURE, FOOD.DEFAULT_ROT_TEMPERATURE, temperature) + Mathf.InverseLerp(FOOD.HIGH_PRESERVE_TEMPERATURE, FOOD.HIGH_ROT_TEMPERATURE, temperature);

                switch (Rottable.AtmosphereQuality(gameObject))
                {
                    case Rottable.RotAtmosphereQuality.Contaminating:
                        rate += 1;
                        break;
                    case Rottable.RotAtmosphereQuality.Normal:
                        break;
                    case Rottable.RotAtmosphereQuality.Sterilizing:
                        rate *= 0.5f;
                        break;
                }

                master.decomposition += (rate * dt);
            }

            // свежий
            public bool IsFresh()
            {
                return master.decomposition < rottenTime;
            }

            // гнилой
            public bool IsRotten()
            {
                return master.decomposition >= rottenTime;
            }

            // полностью гнилой
            public bool IsFullyRotten()
            {
                return master.decomposition >= fullyDecompositionTime;
            }

            // при переноске дупликами есть оба тэга Preserved и Sealed, при похоронении в могилу только Preserved
            // закопан в могилу. 
            public bool IsBuried()
            {
                return kPrefabID != null && kPrefabID.HasTag(GameTags.Stored) && !kPrefabID.HasTag(GameTags.Sealed);
            }

            // труп переносят
            public bool IsCarried()
            {
                return kPrefabID != null && kPrefabID.HasTag(GameTags.Stored) && kPrefabID.HasTag(GameTags.Sealed);
            }

            // утоплен
            public bool IsSubmerged()
            {
                return PathFinder.IsSubmerged(Grid.PosToCell(this));
            }

            // выделение газов
            public void Emit()
            {
                // эффект вонизмы, применяемый к рядомнаходящимся дуплам, кроме того кто несет труп, иначе работа сбивается.
                GameObject gravedigger = null;
                if (master.HasTag(GameTags.Sealed))
                {
                    gravedigger = master.GetComponent<Pickupable>()?.storage?.gameObject;
                }
                Vector2 a = master.transform.GetPosition();
                foreach (MinionIdentity minionIdentity in Components.LiveMinionIdentities)
                {
                    if (minionIdentity.gameObject != master.gameObject)
                    {
                        Vector2 b = minionIdentity.transform.GetPosition();
                        if (Vector2.SqrMagnitude(a - b) <= gasMaxDistanceSq)
                        {
                            if (minionIdentity.gameObject != gravedigger)
                            {
                                minionIdentity.Trigger((int)GameHashes.Cringe, Strings.Get("STRINGS.DUPLICANTS.DISEASES.PUTRIDODOUR.CRINGE_EFFECT").String);
                            }
                            minionIdentity.GetSMI<ThoughtGraph.Instance>().AddThought(Db.Get().Thoughts.PutridOdour);
                        }
                    }
                }

                // выделяем газ
                int cell = Grid.PosToCell(this);
                if (Grid.IsValidCell(cell) && !Grid.Element[cell].IsSolid && !Grid.IsSubstantialLiquid(cell, submergedThreshold))
                {
                    if (Grid.Element[cell].id == gasReplacedElement)
                    {
                        SimMessages.ReplaceElement(cell, gasEmitElement, CellEventLogger.Instance.DecompositionDirtyWater, Grid.Mass[cell], Grid.Temperature[cell], Grid.DiseaseIdx[cell], Grid.DiseaseCount[cell]);
                    }
                    else
                    {
                        SimMessages.AddRemoveSubstance(cell, gasEmitElement, CellEventLogger.Instance.ElementConsumerSimUpdate, gasEmitMass, temperature, byte.MaxValue, 0);
                    }
                }
            }

            // заражение воды
            public void DirtyWater()
            {
                int cell = Grid.PosToCell(this);
                if (Grid.IsValidCell(cell))
                {
                    if (Grid.Element[cell].id == waterReplacedElement)
                    {
                        SimMessages.ReplaceElement(cell, waterEmitElement, CellEventLogger.Instance.DecompositionDirtyWater, Grid.Mass[cell], Grid.Temperature[cell], Grid.DiseaseIdx[cell], Grid.DiseaseCount[cell], -1);
                    }
                    else if (Grid.Element[cell].id == waterEmitElement)
                    {
                        int[] array = new int[4];
                        for (int i = 0; i < dirtyWaterMaxRange; i++)
                        {
                            for (int j = 0; j < dirtyWaterMaxRange; j++)
                            {
                                array[0] = Grid.OffsetCell(cell, new CellOffset(-i, j));
                                array[1] = Grid.OffsetCell(cell, new CellOffset(i, j));
                                array[2] = Grid.OffsetCell(cell, new CellOffset(-i, -j));
                                array[3] = Grid.OffsetCell(cell, new CellOffset(i, -j));
                                array.Shuffle();
                                foreach (int cell2 in array)
                                {
                                    if (Grid.GetCellDistance(cell, cell2) < dirtyWaterMaxRange - 1 && Grid.IsValidCell(cell2) && Grid.Element[cell2].id == waterReplacedElement)
                                    {
                                        SimMessages.ReplaceElement(cell2, waterEmitElement, CellEventLogger.Instance.DecompositionDirtyWater, Grid.Mass[cell2], Grid.Temperature[cell2], Grid.DiseaseIdx[cell2], Grid.DiseaseCount[cell2], -1);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // обновить визуализатор микробов
            public void UpdateDiseaseSourceVisualizer(string disease)
            {
                DiseaseSourceVisualizer diseaseSourceVisualizer = smi.GetComponent<DiseaseSourceVisualizer>();
                if (diseaseSourceVisualizer != null)
                {
                    diseaseSourceVisualizer.alwaysShowDisease = disease;
                    diseaseSourceVisualizer.UpdateVisibility();
                }
            }

            // спавн продуктов разложения
            public void SpawnDecompositionProductsSelfDestroy()
            {
                // визуалочка
                Vector3 position = master.transform.GetPosition();
                KBatchedAnimController kbacFX = FXHelpers.CreateEffect("decomposition_corpse_fx_kanim", position, null, false, Grid.SceneLayer.FXFront, false);
                kbacFX.Play("idle", KAnim.PlayMode.Once);
                kbacFX.destroyOnAnimComplete = true;


                // морбы 
                if (!IsSubmerged())
                {
                    Vector3 position_morb = Grid.CellToPos(Grid.PosToCell(position));
                    for (int i = 0; i < countRotMonsters; i++)
                    {
                        GameObject morb = GameUtil.KInstantiate(Assets.GetPrefab("Glom".ToTag()), position_morb, Grid.SceneLayer.Creatures);
                        morb.SetActive(true);
                        //PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Resource, morb.name, morb.transform);
                    }
                }

                // скелетон
                position.y += 0.5f;
                GameObject skeleton = GameUtil.KInstantiate(Assets.GetPrefab(SkeletonConfig.TAG), position, Grid.SceneLayer.Ore);
                skeleton.SetActive(true);
                skeleton.GetComponent<PrimaryElement>().Temperature = temperature;

                // эффект на всех живых дуплов
                EffectsExtensions.AddEffectToAllLiveMinions(DeathPatches.DESTROYED_CORPSE, true, true);

                // удаляем труп
                Util.KDestroyGameObject(master.gameObject);
            }
        }

        public class States : GameStateMachine<States, SMInstance, Decomposition>
        {
            public class FreshState : State
            {
                public State exposed;
                public State carried;
            }

            public class RottenExposedState : State
            {
                public State openair;
                public State submerged;
            }

            public class RottenState : State
            {
                public RottenExposedState exposed;
                public State carried;
                public State fullydecomposition;
            }

            public State alive;
            public FreshState fresh;
            public RottenState rotten;
            public State buried;

            // вонизма
            private FXLoopAnim.Instance CreateOdorFX(SMInstance smi)
            {
                return smi.isMasterNull ? null : new FXLoopAnim.Instance(smi.master, "odor_fx_kanim", new HashedString[3] { "working_pre", "working_loop", "working_pst" }, KAnim.PlayMode.Once, new Vector3(0f, 0f, -0.1f), Color.white);
            }

            // мушки
            private FliesFX.Instance CreateFliesFX(SMInstance smi)
            {
                return smi.isMasterNull ? null : new FliesFX.Instance(smi.master, new Vector3(0f, 0f, -0.1f));
            }

            // пузырики
            private FXAnim.Instance CreateBubblesFX(SMInstance smi)
            {
                return smi.isMasterNull ? null : new FXAnim.Instance(smi.master, "contaminated_building_fx_kanim", "bubble", KAnim.PlayMode.Loop, new Vector3(0f, -0.4f, -0.1f), Color.white);
            }

            public override void InitializeStates(out BaseState default_state)
            {
                default_state = alive;
                serializable = true;

                alive.TagTransition(GameTags.Dead, fresh, false);

                fresh
                    .DefaultState(fresh.exposed)
                    .ToggleAttributeModifier("Dead", (SMInstance smi) => freshDecorModifier)
                    .ToggleAttributeModifier("Dead", (SMInstance smi) => freshDecorRadiusModifier)
                    .ToggleStateMachine((SMInstance smi) => new EffectLineOfSight.Instance(smi.master, new EffectLineOfSight.Def() { effectName = DeathPatches.OBSERVED_CORPSE } ))
                    .ToggleReactable((SMInstance smi) => CreateDeadReactable(smi.gameObject));

                fresh.exposed
                    .Update((SMInstance smi, float dt) => smi.UpdateDecomposition(dt), UpdateRate.SIM_1000ms)
                    .Transition(rotten, (SMInstance smi) => smi.IsRotten(), UpdateRate.SIM_1000ms)
                    .EventTransition(GameHashes.OnStore, fresh.carried, (SMInstance smi) => smi.IsCarried())
                    .EventTransition(GameHashes.OnStore, buried, (SMInstance smi) => smi.IsBuried());

                fresh.carried
                    .TagTransition(GameTags.Stored, fresh.exposed, true)
                    .TagTransition(GameTags.Sealed, buried, true);

                rotten
                    .DefaultState(rotten.exposed)
                    .ToggleStatusItem(Db.Get().DuplicantStatusItems.Rotten)
                    .ToggleAttributeModifier("Rotten", (SMInstance smi) => rottenDecorModifier, null)
                    .ToggleAttributeModifier("Rotten", (SMInstance smi) => rottenDecorRadiusModifier, null)
                    .Enter((SMInstance smi) =>
                    {
                        smi.UpdateDiseaseSourceVisualizer(diseaseID);
                        smi.GetComponent<KBatchedAnimController>().TintColour = rottenColor;
                    })
                    .Exit((SMInstance smi) => smi.UpdateDiseaseSourceVisualizer(null))
                    .ToggleStateMachine(smi => new DiseaseDropper.Instance(smi.master, new DiseaseDropper.Def()
                    {
                        diseaseIdx = Db.Get().Diseases.GetIndex(diseaseID),
                        emitFrequency = diseaseEmitFrequency,
                        averageEmitPerSecond = diseaseAverageEmitPerSecond,
                        singleEmitQuantity = diseaseSingleEmitQuantity
                    }))
                    .ToggleStateMachine((SMInstance smi) => new EffectLineOfSight.Instance(smi.master, new EffectLineOfSight.Def() { effectName = DeathPatches.OBSERVED_ROTTEN_CORPSE } ))
                    .ToggleReactable((SMInstance smi) => CreateRottenReactable(smi.gameObject))
                    .ToggleFX((SMInstance smi) => CreateOdorFX(smi))
                    .Update((SMInstance smi, float dt) => smi.DirtyWater(), UpdateRate.SIM_4000ms)
                    .Update((SMInstance smi, float dt) => smi.Emit(), UpdateRate.SIM_4000ms);

                rotten.exposed
                    .DefaultState(rotten.exposed.openair)
                    .Update((SMInstance smi, float dt) => smi.UpdateDecomposition(dt), UpdateRate.SIM_1000ms)                   
                    .Transition(rotten.fullydecomposition, (SMInstance smi) => smi.IsFullyRotten(), UpdateRate.SIM_1000ms)
                    .EventTransition(GameHashes.OnStore, rotten.carried, (SMInstance smi) => smi.IsCarried())
                    .EventTransition(GameHashes.OnStore, buried, (SMInstance smi) => smi.IsBuried());

                rotten.exposed.openair
                    .Transition(rotten.exposed.submerged, (SMInstance smi) => smi.IsSubmerged(), UpdateRate.SIM_1000ms)
                    .ToggleFX((SMInstance smi) => CreateFliesFX(smi));

                rotten.exposed.submerged
                    .Transition(rotten.exposed.openair, (SMInstance smi) => !smi.IsSubmerged(), UpdateRate.SIM_1000ms)
                    .ToggleFX((SMInstance smi) => CreateBubblesFX(smi));

                rotten.carried
                    //.ToggleTag(GameTags.PreventEmittingDisease)
                    .TagTransition(GameTags.Stored, rotten.exposed, true)
                    .TagTransition(GameTags.Sealed, buried, true);

                rotten.fullydecomposition
                    .TriggerOnEnter(GameHashes.BurstEmitDisease)
                    .Enter((SMInstance smi) => smi.SpawnDecompositionProductsSelfDestroy());

                buried
                    .ToggleStatusItem((SMInstance smi) => smi.IsRotten() ? Db.Get().DuplicantStatusItems.Rotten : null)
                    .ToggleReactable((SMInstance smi) => CreateBuriedReactable(smi.gameObject))
                    .EventTransition(GameHashes.TagsChanged, fresh.exposed, (SMInstance smi) => !smi.IsBuried() && smi.IsFresh())
                    .EventTransition(GameHashes.TagsChanged, rotten.exposed, (SMInstance smi) => !smi.IsBuried() && smi.IsRotten());
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            smi.StartSM();
        }

        // эмоции при виде трупа
        // todo: обдумать тайминги. возможно стоит перенести в более общий класс. и сделать реакцию на саму могилу а не на труп внутри
        public static Reactable CreateDeadReactable(GameObject gameObject)
        {
            return new EmoteReactable(gameObject, "DeadCorpse", Db.Get().ChoreTypes.Emote, "anim_react_respect_kanim")
                .AddStep(new EmoteReactable.EmoteStep{ anim = "react" })
                .AddExpression(Db.Get().Expressions.Unhappy);
        }

        public static Reactable CreateRottenReactable(GameObject gameObject)
        {
            return new EmoteReactable(gameObject, "RottenCorpse", Db.Get().ChoreTypes.Emote, "anim_cringe_kanim")
                .AddStep(new EmoteReactable.EmoteStep { anim = "cringe_pre" })
                .AddStep(new EmoteReactable.EmoteStep { anim = "cringe_loop" })
                .AddStep(new EmoteReactable.EmoteStep { anim = "cringe_pst" })
                .AddExpression(Db.Get().Expressions.Unhappy);
        }

        public static Reactable CreateBuriedReactable(GameObject gameObject)
        {
            return new EmoteReactable(gameObject, "Buried", Db.Get().ChoreTypes.Emote, "anim_react_respect_kanim", 3, 3, 0, 30)
                .AddStep(new EmoteReactable.EmoteStep { anim = "react" })
                .AddExpression(Db.Get().Expressions.Unhappy);
        }
    }
}