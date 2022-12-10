using System;
using STRINGS;
using UnityEngine;
using SanchozzONIMods.Lib.UI;
using PeterHan.PLib.Detours;
using PeterHan.PLib.UI;

namespace AttributeRestrictions
{
    using static STRINGS.UI.UISIDESCREENS.ATTRIBUTE_RESTRICTION_SIDESCREEN;

    internal class AttributeRestrictionSideScreen : SideScreenContent
    {
        private const string prefix = "STRINGS.UI.UISIDESCREENS.ATTRIBUTE_RESTRICTION_SIDESCREEN.";
        private AttributeRestriction target;

        // каллбаки для чекбоксов и слидеров
        private Action<bool> is_enable;
        private Action<string> set_text;
        private Action<float> required_level;

        private GameObject aboveButton;
        private GameObject belowButton;

        private static ColorStyleSetting ButtonActiveStyle;
        private static ColorStyleSetting ButtonInactiveStyle;

        protected override void OnPrefabInit()
        {
            if (ButtonActiveStyle == null)
            {
                ButtonActiveStyle = ScriptableObject.CreateInstance<ColorStyleSetting>();
                ButtonActiveStyle.activeColor = new Color(0.5033521f, 0.5444419f, 0.6985294f);
                ButtonActiveStyle.inactiveColor = new Color(0.5033521f, 0.5444419f, 0.6985294f);
                ButtonActiveStyle.disabledColor = new Color(0.4156863f, 0.4117647f, 0.4f);
                ButtonActiveStyle.disabledActiveColor = new Color(0.625f, 0.6158088f, 0.5882353f);
                ButtonActiveStyle.hoverColor = new Color(0.3461289f, 0.3739619f, 0.4852941f);
                ButtonActiveStyle.disabledhoverColor = new Color(0.5f, 0.4898898f, 0.4595588f);
            }
            if (ButtonInactiveStyle == null)
            {
                ButtonInactiveStyle = ScriptableObject.CreateInstance<ColorStyleSetting>();
                ButtonInactiveStyle.activeColor = new Color(0.2431373f, 0.2627451f, 0.3411765f);
                ButtonInactiveStyle.inactiveColor = new Color(0.2431373f, 0.2627451f, 0.3411765f);
                ButtonInactiveStyle.disabledColor = new Color(0.4156863f, 0.4117647f, 0.4f);
                ButtonInactiveStyle.disabledActiveColor = new Color(0.625f, 0.6158088f, 0.5882353f);
                ButtonInactiveStyle.hoverColor = new Color(0.3461289f, 0.3739619f, 0.4852941f);
                ButtonInactiveStyle.disabledhoverColor = new Color(0.5f, 0.4898898f, 0.4595588f);
            }

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
                // чекбох
                .AddCheckBox(prefix, nameof(is_enable),
                    b => { if (target != null) target.isEnabled = b; }, out is_enable, out _, out set_text)
                // две кнопки
                .AddChild(new PPanel("Buttons")
                {
                    Alignment = TextAnchor.MiddleCenter,
                    Direction = PanelDirection.Horizontal,
                    FlexSize = Vector2.right,
                    Spacing = 10,
                }
                    // Арргхх!!! ад и израиль из кучи панелей чтобы кнопки по центру выровнять
                    .AddChild(new PPanel("Left")
                    {
                        Direction = PanelDirection.Horizontal,
                        FlexSize = Vector2.right,
                    }
                        .AddChild(new PSpacer())
                        .AddChild(new PButton()
                        {
                            Color = ButtonInactiveStyle,
                            Margin = new RectOffset(8, 8, 3, 3),
                            TextStyle = PUITuning.Fonts.TextLightStyle,
                            OnClick = go => SetBelow(false),
                            Text = UI.UISIDESCREENS.THRESHOLD_SWITCH_SIDESCREEN.ABOVE_BUTTON
                        }.AddOnRealize(go => aboveButton = go))
                    )
                    .AddChild(new PPanel("Right")
                    {
                        Direction = PanelDirection.Horizontal,
                        FlexSize = Vector2.right,
                    }
                        .AddChild(new PButton()
                        {
                            Color = ButtonInactiveStyle,
                            Margin = new RectOffset(8, 8, 3, 3),
                            TextStyle = PUITuning.Fonts.TextLightStyle,
                            OnClick = go => SetBelow(true),
                            Text = UI.UISIDESCREENS.THRESHOLD_SWITCH_SIDESCREEN.BELOW_BUTTON
                        }.AddOnRealize(go => belowButton = go))
                        .AddChild(new PSpacer())
                    )
                )
                // слайдер
                .AddSliderBox(prefix, nameof(required_level), 0f, 20f,
                    f => { if (target != null) target.requiredAttributeLevel = Mathf.RoundToInt(f); }, out required_level)
                .AddTo(gameObject);
            ContentContainer = gameObject;
            base.OnPrefabInit();
            UpdateScreen();
        }

        private void UpdateScreen()
        {
            if (target != null)
            {
                is_enable?.Invoke(target.isEnabled);
                set_text?.Invoke(string.Format(IS_ENABLE.NAME, UI.FormatAsKeyWord(target.requiredAttribute.Name)));
                required_level?.Invoke(target.requiredAttributeLevel);
                UpdateButtons();
            }
        }

        private void SetBelow(bool is_below)
        {
            if (target != null)
            {
                target.isBelow = is_below;
                UpdateButtons();
            }
        }

        private void UpdateButtons()
        {
            if (target.isBelow)
            {
                SetupButtonBackground(aboveButton, ButtonInactiveStyle);
                SetupButtonBackground(belowButton, ButtonActiveStyle);
            }
            else
            {
                SetupButtonBackground(aboveButton, ButtonActiveStyle);
                SetupButtonBackground(belowButton, ButtonInactiveStyle);
            }
        }

        // Plib не имеет готовой возможности менять цвет кнопок на лету. пришлось скопипастить некоторые куски
        private static readonly IDetouredField<KImage, ColorStyleSetting> COLOR_STYLE_SETTING = PDetours.DetourFieldLazy<KImage, ColorStyleSetting>(nameof(KImage.colorStyleSetting));

        private static readonly DetouredMethod<Action<KImage>> APPLY_COLOR_STYLE = typeof(KImage).DetourLazy<Action<KImage>>(nameof(KImage.ApplyColorStyleSetting));

        private void SetupButtonBackground(GameObject button, ColorStyleSetting color)
        {
            var bgImage = button?.GetComponent<KImage>();
            if (bgImage == null)
                return;
            COLOR_STYLE_SETTING.Set(bgImage, color);
            APPLY_COLOR_STYLE.Invoke(bgImage);
        }

        public override bool IsValidForTarget(GameObject target)
        {
            var restriction = target.GetComponent<AttributeRestriction>();
            return restriction != null && restriction.requiredAttribute != null;
        }

        public override void SetTarget(GameObject target)
        {
            this.target = target?.GetComponent<AttributeRestriction>();
            UpdateScreen();
        }

        public override void ClearTarget() => target = null;
        public override int GetSideScreenSortOrder() => -30;
        public override string GetTitle() => TITLE.text;
    }
}
