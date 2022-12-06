using System;
using UnityEngine;
using SanchozzONIMods.Lib.UI;
using PeterHan.PLib.UI;

namespace SmartLogicDoors
{
    public class SmartLogicDoorSideScreen : SideScreenContent
    {
        private const string prefix = "STRINGS.UI.UISIDESCREENS.SMARTLOGICDOOR_SIDESCREEN.";
        private SmartLogicDoor target;
        // каллбаки для чекбоксов
        private Action<bool> opened_locked;
        private Action<bool> opened_auto;
        private Action<bool> auto_locked;

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
                .AddChild(new PLabel("Label")
                {
                    TextAlignment = TextAnchor.MiddleLeft,
                    Text = Strings.Get($"{prefix}TITLE"),
                    TextStyle = PUITuning.Fonts.TextDarkStyle
                })
                .AddCheckBox(prefix, nameof(opened_locked),
                    b => { OnChecked(b, Door.ControlState.Opened, Door.ControlState.Locked); }, out opened_locked, out _)
                .AddCheckBox(prefix, nameof(opened_auto),
                    b => { OnChecked(b, Door.ControlState.Opened, Door.ControlState.Auto); }, out opened_auto, out _)
                .AddCheckBox(prefix, nameof(auto_locked),
                    b => { OnChecked(b, Door.ControlState.Auto, Door.ControlState.Locked); }, out auto_locked, out _)
                .AddTo(gameObject);
            ContentContainer = gameObject;
            base.OnPrefabInit();
            UpdateScreen();
        }

        private void OnChecked(bool @checked, Door.ControlState green_state, Door.ControlState red_state)
        {
            if (@checked && target != null)
            {
                target.GreenState = green_state;
                target.RedState = red_state;
                target.ApplyControlState();
            }
            UpdateScreen();
        }

        private void UpdateScreen()
        {
            if (target != null)
            {
                opened_locked?.Invoke(target.GreenState == Door.ControlState.Opened && target.RedState == Door.ControlState.Locked);
                opened_auto?.Invoke(target.GreenState == Door.ControlState.Opened && target.RedState == Door.ControlState.Auto);
                auto_locked?.Invoke(target.GreenState == Door.ControlState.Auto && target.RedState == Door.ControlState.Locked);
            }
        }

        public override bool IsValidForTarget(GameObject target)
        {
            var door = target.GetComponent<SmartLogicDoor>();
            return door != null && door.IsLogicPortConnected;
        }

        public override void SetTarget(GameObject target)
        {
            this.target = target?.GetComponent<SmartLogicDoor>();
            UpdateScreen();
        }

        public override void ClearTarget() => target = null;
        public override int GetSideScreenSortOrder() => 10;
        public override string GetTitle() => STRINGS.UI.UISIDESCREENS.SMARTLOGICDOOR_SIDESCREEN.TITLE.text;
    }
}
