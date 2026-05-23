using System;
using UnityEngine;
using SanchozzONIMods.Lib.UI;
using PeterHan.PLib.UI;

namespace SuitRecharger
{
    internal class SuitBionicSideScreen : SideScreenContent
    {
        private const string prefix = "STRINGS.UI.UISIDESCREENS.SUITRECHARGERSIDESCREEN.";
        private SuitRecharger target;
        private Action<bool> fill_bionic_suit;
        private Action<bool> fill_bionic_internal;

        protected override void OnPrefabInit()
        {
            if (gameObject.TryGetComponent(out BoxLayoutGroup baseLayout))
                baseLayout.Params.Alignment = TextAnchor.MiddleLeft;
            var panel = new PPanel("MainPanel")
            {
                Alignment = TextAnchor.MiddleLeft,
                Direction = PanelDirection.Vertical,
                Margin = new RectOffset(6, 6, 4, 4),
                Spacing = 8,
                FlexSize = Vector2.right,
                BackColor = PUITuning.Colors.Transparent,
            }
                .AddCheckBox(prefix, nameof(fill_bionic_suit),
                    b => { if (target != null) target.fillBionicSuitTank = b; }, out fill_bionic_suit, out _)
                .AddCheckBox(prefix, nameof(fill_bionic_internal),
                    b => { if (target != null) target.fillBionicInternalTank = b; }, out fill_bionic_internal, out _)
                .AddTo(gameObject);
            ContentContainer = gameObject;
            base.OnPrefabInit();
            UpdateScreen();
        }

        private void UpdateScreen()
        {
            if (target != null)
            {
                fill_bionic_suit?.Invoke(target.fillBionicSuitTank);
                fill_bionic_internal?.Invoke(target.fillBionicInternalTank);
            }
        }

        public override bool IsValidForTarget(GameObject target) =>
            SuitRecharger.BionicMode && target.TryGetComponent<SuitRecharger>(out _);

        public override void SetTarget(GameObject target)
        {
            target.TryGetComponent(out this.target);
            UpdateScreen();
        }

        public override void ClearTarget() => target = null;
        public override string GetTitle() => STRINGS.UI.UISIDESCREENS.SUITRECHARGERSIDESCREEN.TITLE_BIONIC.text;
        public override int GetSideScreenSortOrder() => 400;
    }
}
