using UnityEngine;

namespace SanchozzONIMods.Shared
{
	[SkipSaveFileSerialization]
	public class LightSymbolMeterTracker : KMonoBehaviour, IRenderEveryTick
	{
		[SerializeField]
		public HashedString targetSymbol;

		public KBatchedAnimController kbac;

#pragma warning disable CS0649
		[MyCmpReq]
		private Light2D light2D;
#pragma warning restore CS0649

		protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
			autoRegisterSimRender = false;
        }

        protected override void OnCmpEnable()
        {
            base.OnCmpEnable();
			SimAndRenderScheduler.instance.Add(this);
        }

        protected override void OnCmpDisable()
        {
            base.OnCmpDisable();
			SimAndRenderScheduler.instance.Remove(this);
			light2D.enabled = false;
		}

        public bool IsEnableAndVisible()
		{
			return CameraController.Instance.VisibleArea.CurrentAreaExtended.Contains(Grid.PosToCell(this)) && enabled && kbac != null;
		}

		public void RenderEveryTick(float dt)
		{
			if (!IsEnableAndVisible())
				return;
            Vector3 vector = (kbac.GetTransformMatrix() * kbac.GetSymbolLocalTransform(targetSymbol, out bool visible)).MultiplyPoint(Vector3.zero) - transform.position;
            light2D.Offset = vector;
			if (light2D.enabled != visible)
				light2D.enabled = visible;
		}
	}
}
