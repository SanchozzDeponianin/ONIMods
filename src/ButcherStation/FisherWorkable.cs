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
        public CellOffset[] workOffsets;

        public override void OnPrefabInit()
        {
            base.OnPrefabInit();
            SetOffsets(workOffsets);
            autoRegisterSimRender = false;
        }

        public const string CAPTURING_SYMBOL = "creatureSymbol";
        public const string LINE_SYMBOL = "pipe";

        // для рыбовых
        private readonly static HashedString BITEHOOK_ANIM = "bitehook";
        private readonly static HashedString CAUGHT_ANIM = "caught_loop";
        private readonly static HashedString CAUGHT_PST_ANIM = "flop_loop";

        // для скрытых кбаков
        private readonly static HashedString[] LINE_ANIMS = { "line_pre", "line_loop", "line_pst" };
        private readonly static HashedString SACK_ANIM = "fish_bag";

        private HashedString capturingSymbol;
        private Vector3 capturingAnimOffcet;
        private HashedString sackSymbol;
        private HashedString caughtPstAnim;
        private int lineLength;

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
            capturingAnimOffcet = Vector3.zero;
            kbac.Play(workAnims, workAnimPlayMode);
            fishing.line.Play(LINE_ANIMS, workAnimPlayMode);
            fishing.sack.Play(SACK_ANIM, KAnim.PlayMode.Paused);
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
                fishing.line.SetSymbolVisiblity(LINE_SYMBOL + i.ToString(), i <= lineLength);
                fishing.line.SetSymbolVisiblity(CAPTURING_SYMBOL + i.ToString(), i == lineLength);
            }
            sackSymbol = (worker.transform.GetPosition().x < transform.GetPosition().x) ? "fish_bag_left" : "fish_bag_right";
            caughtPstAnim = butcher.leaveAlive ? Baggable.GetBaggedAnimName(critterAnimController.gameObject) : CAUGHT_PST_ANIM;
            SimAndRenderScheduler.instance.Add(this);
        }

        public override bool InstantlyFinish(WorkerBase worker) => false;

        public override bool OnWorkTick(WorkerBase worker, float dt)
        {
            // для рыб у кого нет bitehook
            if (fishing.line.currentAnim == LINE_ANIMS[1] && kbac.currentFrame <= 2)
            {
                if (critterAnimController != null
                    && critterAnimController.currentAnim != BITEHOOK_ANIM
                    && critterAnimController.currentAnim != CAUGHT_ANIM)
                {
                    critterAnimController.Play(CAUGHT_ANIM);
                }
            }
            // для рыб у кого нет caught_loop
            else if (fishing.line.currentAnim == LINE_ANIMS[2] && fishing.line.IsStopped())
            {
                if (critterAnimController != null
                    && critterAnimController.currentAnim != caughtPstAnim)
                {
                    StartPlaySackAnim();
                }
            }
            return base.OnWorkTick(worker, dt);
        }

        public void RenderEveryTick(float dt) => UpdateCritterPosition();

        private void UpdateCritterPosition()
        {
            if (critterAnimController != null)
            {
                if (critterAnimController.currentAnim == CAUGHT_ANIM)
                {
                    var position = critterAnimController.transform.GetPosition();
                    var column = fishing.line.GetSymbolTransform(capturingSymbol, out _).GetColumn(3);
                    var offset = (Vector3)column - position + capturingAnimOffcet;
                    offset.z = 0f;
                    if ((position + offset).y <= transform.GetPosition().y)
                    {
                        if (critterAnimController.Offset != offset)
                            critterAnimController.Offset = offset;
                    }
                    else
                        StartPlaySackAnim();
                }
                if (critterAnimController.currentAnim == caughtPstAnim)
                {
                    var position = critterAnimController.transform.GetPosition();
                    var column = fishing.sack.GetSymbolTransform(sackSymbol, out _).GetColumn(3);
                    var offset = (Vector3)column - position;
                    offset.z = 0f;
                    if (critterAnimController.Offset != offset)
                        critterAnimController.Offset = offset;
                }
            }
        }

        private void StartPlaySackAnim()
        {
            fishing.sack.Play(SACK_ANIM, KAnim.PlayMode.Once);
            critterAnimController.Play(caughtPstAnim, KAnim.PlayMode.Loop);
        }

        private void MoveCritterToPosition()
        {
            if (critterAnimController != null)
            {
                var position = critterAnimController.transform.GetPosition() + critterAnimController.Offset;
                position.z = Grid.GetLayerZ(Grid.SceneLayer.Creatures);
                critterAnimController.transform.SetPosition(position);
                critterAnimController.Offset = Vector3.zero;
            }
        }

        private void MoveCritterToPositionFast()
        {
            if (critterAnimController != null)
            {
                var offset = (worker.transform.GetPosition().x > transform.GetPosition().x) ? CellOffset.leftup : CellOffset.rightup;
                var position = Grid.CellToPosCBC(Grid.OffsetCell(Grid.PosToCell(this), offset), Grid.SceneLayer.Creatures);
                critterAnimController.transform.SetPosition(position);
                critterAnimController.Offset = Vector3.zero;
            }
        }

        public override void OnPendingCompleteWork(WorkerBase worker)
        {
            SimAndRenderScheduler.instance.Remove(this);
            if (Game.Instance.FastWorkersModeActive)
                MoveCritterToPositionFast();
            else
                MoveCritterToPosition();
            base.OnPendingCompleteWork(worker);
        }

        public override void OnAbortWork(WorkerBase worker)
        {
            SimAndRenderScheduler.instance.Remove(this);
            MoveCritterToPosition();
            PlayAbortAnim();
            base.OnAbortWork(worker);
        }

        private void PlayAbortAnim()
        {
            if (kbac.animQueue.TryPeek(out var data) && data.anim == workAnims[1])
                kbac.animQueue.TryDequeue(out _);
            if (kbac.currentAnim == workAnims[1])
                kbac.StartQueuedAnim();
            if (fishing.line.animQueue.TryPeek(out data) && data.anim == LINE_ANIMS[1])
                fishing.line.animQueue.TryDequeue(out _);
            if (fishing.line.currentAnim == LINE_ANIMS[1])
                fishing.line.StartQueuedAnim();
        }
    }
}
