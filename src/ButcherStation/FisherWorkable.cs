using UnityEngine;

namespace ButcherStation
{
    [SkipSaveFileSerialization]
    public class FisherWorkable : RancherChore.RancherWorkable
    {
        [SerializeField]
        public CellOffset workOffset;

        public override void OnPrefabInit()
        {
            base.OnPrefabInit();
            SetOffsets(new[] { workOffset });
        }
    }
}
