using UnityEngine;

namespace ButcherStation
{
    [SkipSaveFileSerialization]
    public class FisherWorkable : RancherChore.RancherWorkable, IRenderEveryTick
    {
#pragma warning disable CS0649
        [MyCmpReq]
        private KBatchedAnimController kbac;

        [MyCmpReq]
        private ButcherStation butcher;

        [MyCmpReq]
        private FishingStation fishing;
#pragma warning restore CS0649


        [SerializeField]
        public CellOffset workOffset;

        public override void OnPrefabInit()
        {
            base.OnPrefabInit();
            SetOffsets(new[] { workOffset });
            autoRegisterSimRender = false;
        }

        public const string CAPTURING_SYMBOL = "creatureSymbol";
        public const string LINE_SYMBOL = "pipe";

        // pre: 36 кадров
        // loop:  начало вытаскивания на 31 кадре, конец на 41
        private const float CAUGHT_BEGIN = (36 + 31) / 30f;
        private const float CAUGHT_END = (36 + 41) / 30f;

        private readonly static HashedString BITEHOOK_ANIM = "bitehook";
        private readonly static HashedString CAUGHT_ANIM = "caught_loop";
        private readonly static HashedString CAUGHT_PST_ANIM = "flop_loop";

        private HashedString capturingSymbol;
        private Vector3 capturingAnimOffcet;
        private int lineLength;
        private float elaspedTime;

        public enum State { Idle, Start, Caught, Finish }
        private State state;

        // рыбовые имеют тотальный разнобой в наличии анимаций "bitehook" и "caught_loop"
        // и позиционировании "caught_loop" относительно "приманки"
        // например паку и зеленая рыба - как будно бы "приманка" должна быть на клетку выше
        // todo: анимации многих жеготных явно не доделаны, надо следить за ситуацией
        // todo: рассмотреть возможность сделать кастомные смещения для разных рыбовых, в тч для большых
        // todo: рассмотреть возможность иные анимации, мб "eat_pre" ?

        // вместо того чтобы менять позицию рыбы - будем просто смещать анимацию
        // потому что жеготные слишком чуствительны к смене "пещеры" при изменении позиции

        public override void OnStartWork(WorkerBase worker)
        {
            elaspedTime = 0f;
            state = State.Start;
            capturingAnimOffcet = Vector3.zero;
            base.OnStartWork(worker);

            int ranch_cell;
            if (!ranch.IsNullOrStopped())
            {
                if (!ranch.ActiveRanchable.IsNullOrStopped())
                {
                    ranch_cell = Grid.PosToCell(ranch.ActiveRanchable);
                    if (ranch.ActiveRanchable.gameObject.TryGetComponent(out CreatureBrain brain)
                        && (brain.species == GameTags.Creatures.Species.PacuSpecies || brain.species == GameTags.Creatures.Species.SeaFairySpecies))
                    {
                        capturingAnimOffcet = Vector3.down * Grid.CellSizeInMeters;
                        if (critterAnimController != null)
                        {
                            critterAnimController.Play(BITEHOOK_ANIM);
                            critterAnimController.Queue(CAUGHT_ANIM);
                        }
                    }
                }
                else
                    ranch_cell = ranch.GetRanchNavTarget();
            }
            else
                ranch_cell = fishing.TargetRanchCell;

            lineLength = Grid.GetOffset(ranch_cell, Grid.PosToCell(this)).y - 1;
            lineLength = System.Math.Clamp(lineLength, 1, 4);
            capturingSymbol = CAPTURING_SYMBOL + lineLength.ToString();
            for (int i = 1; i <= 4; i++)
            {
                kbac.SetSymbolVisiblity(LINE_SYMBOL + i.ToString(), i <= lineLength);
                kbac.SetSymbolVisiblity(CAPTURING_SYMBOL + i.ToString(), i == lineLength);
            }
        }

        public override bool OnWorkTick(WorkerBase worker, float dt)
        {
            elaspedTime += dt;
            if (state == State.Start && elaspedTime >= CAUGHT_BEGIN)
            {
                state = State.Caught;
                if (critterAnimController != null && critterAnimController.currentAnim != CAUGHT_ANIM)
                    critterAnimController.Play(CAUGHT_ANIM);
                UpdateCritterCaughtPosition();
                SimAndRenderScheduler.instance.Add(this);
            }
            else if (state == State.Caught && elaspedTime >= CAUGHT_END)
            {
                state = State.Finish;
                SimAndRenderScheduler.instance.Remove(this);
                if (critterAnimController != null)
                {
                    var pst_anim = butcher.leaveAlive ? Baggable.GetBaggedAnimName(critterAnimController.gameObject) : CAUGHT_PST_ANIM;
                    critterAnimController.Play(pst_anim, KAnim.PlayMode.Loop);
                    var offset = GetCritterFinalPosition() - critterAnimController.transform.GetPosition();
                    offset.z = 0f;
                    critterAnimController.Offset = offset;
                }
            }
            return base.OnWorkTick(worker, dt);
        }

        public void RenderEveryTick(float dt) => UpdateCritterCaughtPosition();

        private void UpdateCritterCaughtPosition()
        {
            if (state == State.Caught && critterAnimController != null)
            {
                var column = kbac.GetSymbolTransform(capturingSymbol, out _).GetColumn(3);
                var offset = (Vector3)column - critterAnimController.transform.GetPosition() + capturingAnimOffcet;
                offset.z = 0f;
                critterAnimController.Offset = offset;
            }
        }

        private Vector3 GetCritterFinalPosition() => Grid.CellToPosCCC(Grid.CellAbove(Grid.PosToCell(this)), Grid.SceneLayer.Creatures);

        public override void OnPendingCompleteWork(WorkerBase work)
        {
            state = State.Idle;
            SimAndRenderScheduler.instance.Remove(this);
            if (critterAnimController != null)
            {
                critterAnimController.Offset = Vector3.zero;
                critterAnimController.transform.SetPosition(GetCritterFinalPosition());
            }
            base.OnPendingCompleteWork(work);
        }

        public override void OnAbortWork(WorkerBase worker)
        {
            state = State.Idle;
            SimAndRenderScheduler.instance.Remove(this);
            if (critterAnimController != null)
                critterAnimController.Offset = Vector3.zero;
            base.OnAbortWork(worker);
        }
    }
}
