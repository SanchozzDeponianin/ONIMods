using UnityEngine;
using PeterHan.PLib.UI;
using static STRINGS.UI.UISIDESCREENS.AUTOMATABLE_SIDE_SCREEN;

namespace NoManualDelivery
{
    internal class Automatable2SideScreen : SideScreenContent
    {
        private Automatable2 target;
        private GameObject toggle;

        public override void OnPrefabInit()
        {
            var star = PUITuning.Images.GetSpriteByName("icon_star");
            var tooltip = ALLOWMANUALBUTTONTOOLTIP.text;
            if (ModOptions.Instance.HoldMode.Chores)
                tooltip = $"{tooltip}\n{(star == null ? "•" : STRINGS.STAR)} {STRINGS.AUTOMATABLE_TOOLTIP.text}";

            if (gameObject.TryGetComponent(out BoxLayoutGroup baseLayout))
                baseLayout.Params.Alignment = TextAnchor.MiddleLeft;
            var cb = new PCheckBox("Automatable")
            {
                CheckColor = Instantiate(PUITuning.Colors.ComponentLightStyle),
                CheckSize = new Vector2(28f, 26f),
                Margin = new RectOffset(1, 1, 1, 1),
                ComponentBackColor = PUITuning.Colors.Transparent,
                DynamicSize = false,
                Text = ALLOWMANUALBUTTON.text,
                TextAlignment = TextAnchor.MiddleLeft,
                TextStyle = PUITuning.Fonts.TextDarkStyle,
                ToolTip = tooltip,
                OnChecked = OnChecked,
            }.AddOnRealize(realized => toggle = realized);

            var color = new Color32(79, 84, 104, 255); // как Automatable
            cb.CheckColor.activeColor = color;
            cb.CheckColor.hoverColor = color;

            var panel = new PPanel("MainPanel")
            {
                Alignment = TextAnchor.MiddleLeft,
                Direction = PanelDirection.Vertical,
                Margin = new RectOffset(14, 14, 10, 14),
                FlexSize = Vector2.right,
                BackColor = PUITuning.Colors.Transparent,
            }.AddChild(cb).AddTo(gameObject);

            if (toggle.TryGetComponent(out MultiToggle multi))
            {
                var offcet = new Vector2(4f, 2f);
                multi.states[PCheckBox.STATE_CHECKED].rect_margins = cb.CheckSize - offcet;
                multi.states[PCheckBox.STATE_CHECKED].use_rect_margins = true;
                multi.states[PCheckBox.STATE_PARTIAL].rect_margins = cb.CheckSize - offcet;
                multi.states[PCheckBox.STATE_PARTIAL].use_rect_margins = true;
                if (star != null)
                    multi.states[PCheckBox.STATE_PARTIAL].sprite = star;
            }
            ContentContainer = gameObject;
            base.OnPrefabInit();
            UpdateScreen();
        }

        private void UpdateScreen()
        {
            if (target != null && toggle != null)
            {
                int state = target.GetAutomationOnly() ? PCheckBox.STATE_UNCHECKED
                    : (target.GetAutomationHold() ? PCheckBox.STATE_PARTIAL : PCheckBox.STATE_CHECKED);
                PCheckBox.SetCheckState(toggle, state);
            }
        }

        private void OnChecked(GameObject go, int state)
        {
            if (target != null)
            {
                state++;
                if (state > (target.allowHold ? PCheckBox.STATE_PARTIAL : PCheckBox.STATE_CHECKED))
                    state = PCheckBox.STATE_UNCHECKED;
                target.SetAutomation(state == PCheckBox.STATE_UNCHECKED, state == PCheckBox.STATE_PARTIAL);
                Patches.UpdateSweepBotStationStorage(target);
                PCheckBox.SetCheckState(go, state);
                KFMOD.PlayUISound(WidgetSoundPlayer.getSoundPath(ToggleSoundPlayer.default_values[state]));
            }
        }

        public override bool IsValidForTarget(GameObject target)
        {
            return target.TryGetComponent(out Automatable2 automatable) && automatable.showInUI;
        }

        public override void SetTarget(GameObject target)
        {
            this.target = target?.GetComponent<Automatable2>();
            UpdateScreen();
        }

        public override void ClearTarget() => target = null;
        public override int GetSideScreenSortOrder() => -10;
        public override string GetTitle() => TITLE.text;
    }
}
