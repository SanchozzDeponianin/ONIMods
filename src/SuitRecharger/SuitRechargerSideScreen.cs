using System;
using UnityEngine;
using SanchozzONIMods.Lib.UI;
using PeterHan.PLib.UI;

namespace SuitRecharger
{
    internal class SuitRechargerSideScreen : SideScreenContent
    {
        private const string prefix = "STRINGS.UI.UISIDESCREENS.SUITRECHARGERSIDESCREEN.";
        private SuitRecharger target;
        // каллбаки для чекбоксов и слидеров
        private Action<bool> enable_repair;
        private Action<float> durability_threshold;

        protected override void OnPrefabInit()
        {
            var margin = new RectOffset(6, 6, 6, 6);
            var baseLayout = gameObject.GetComponent<BoxLayoutGroup>();
            if (baseLayout != null)
                baseLayout.Params = new BoxLayoutParams()
                {
                    Alignment = TextAnchor.MiddleLeft,
                    Margin = margin,
                };
            var panel = new PPanel("MainPanel")
            {
                Alignment = TextAnchor.MiddleLeft,
                Direction = PanelDirection.Vertical,
                Margin = margin,
                Spacing = 8,
                FlexSize = Vector2.right,
            }
                // ползун прочности
                .AddSliderBox(prefix, nameof(durability_threshold), 0f, 100f,
                    f => { if (target != null) target.DurabilityThreshold = f / 100f; }, out durability_threshold)

                // включить ремонт
                .AddCheckBox(prefix, nameof(enable_repair),
                    b => { if (target != null) target.EnableRepair = b; }, out enable_repair, out _)
                .AddTo(gameObject);
            ContentContainer = gameObject;
            base.OnPrefabInit();
            UpdateScreen();
        }

        private void UpdateScreen()
        {
            if (target != null)
            {
                enable_repair?.Invoke(target.EnableRepair);
                durability_threshold?.Invoke(target.DurabilityThreshold * 100f);
            }
        }

        public override bool IsValidForTarget(GameObject target) => SuitRecharger.durabilityEnabled && target.GetComponent<SuitRecharger>() != null;

        public override void SetTarget(GameObject target)
        {
            this.target = target?.GetComponent<SuitRecharger>();
            UpdateScreen();
        }

        public override void ClearTarget() => target = null;

        public override string GetTitle() => STRINGS.UI.UISIDESCREENS.SUITRECHARGERSIDESCREEN.TITLE.text;
    }
}
