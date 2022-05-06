using System;
using UnityEngine;
using TMPro;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;

namespace SanchozzONIMods.Lib.UI
{
    public static class SideScreenUtils
    {
        // штуки для создания сидескреенов
        // чекбокс и слидер с обвязкой и подгрузкой строк, добавляемые на панельку
        // установка и считывание значений реализованы через акции-каллбаки
        public static PPanel AddCheckBox(this PPanel parent, string prefix, string name, Action<bool> onChecked, out Action<bool> setChecked, out Action<bool> setActive)
        {
            prefix = (prefix + name).ToUpperInvariant();
            GameObject cb_go = null;
            var cb = new PCheckBox(name)
            {
                CheckColor = PUITuning.Colors.ComponentLightStyle,
                CheckSize = new Vector2(26f, 26f),
                Text = Strings.Get(prefix + ".NAME"),
                TextAlignment = TextAnchor.MiddleLeft,
                TextStyle = PUITuning.Fonts.TextDarkStyle,
                ToolTip = Strings.Get(prefix + ".TOOLTIP"),
                OnChecked = (go, state) =>
                {
                    // переворачиваем предыдующее значение
                    bool @checked = state == PCheckBox.STATE_UNCHECKED;
                    PCheckBox.SetCheckState(go, @checked ? PCheckBox.STATE_CHECKED : PCheckBox.STATE_UNCHECKED);
                    onChecked?.Invoke(@checked);
                    // внесапно, численное значение состояния чекбокса совпало с индексом таблицы звуков
                    KFMOD.PlayUISound(WidgetSoundPlayer.getSoundPath(ToggleSoundPlayer.default_values[state]));
                },
            }.AddOnRealize(realized => cb_go = realized);
            setChecked = @checked =>
            {
                if (cb_go != null)
                    PCheckBox.SetCheckState(cb_go, @checked ? PCheckBox.STATE_CHECKED : PCheckBox.STATE_UNCHECKED);
            };
            setActive = on => cb_go?.SetActive(on);
            return parent.AddChild(cb);
        }

        public static PPanel AddSliderBox(this PPanel parent, string prefix, string name, float min, float max, Action<float> onValueUpdate, out Action<float> setValue, Func<float, string> customTooltip = null)
        {
            float value = 0;
            GameObject text_go = null;
            GameObject slider_go = null;
            setValue = newValue =>
            {
                value = newValue;
                Update();
            };
            if (!(min > float.NegativeInfinity && max < float.PositiveInfinity && min < max))
            {
                PUtil.LogError("Invalid min max parameters");
                return parent;
            }
            prefix = (prefix + name).ToUpperInvariant();

            void Update()
            {
                var field = text_go?.GetComponentInChildren<TMP_InputField>();
                if (field != null)
                    field.text = value.ToString("F0");
                if (slider_go != null)
                    PSliderSingle.SetCurrentValue(slider_go, value);
                onValueUpdate?.Invoke(value);
            }

            void OnTextChanged(GameObject _, string text)
            {
                if (float.TryParse(text, out float newValue))
                    value = Mathf.Clamp(newValue, min, max);
                Update();
            }

            void OnSliderChanged(GameObject _, float newValue)
            {
                value = Mathf.Clamp(Mathf.Round(newValue), min, max);
                Update();
            }

            var small = PUITuning.Fonts.TextDarkStyle.DeriveStyle(size: 12);
            var minLabel = new PLabel("min_" + name)
            {
                TextStyle = small,
                Text = string.Format(Strings.Get(prefix + ".MIN_MAX"), min),
            };
            var maxLabel = new PLabel("max_" + name)
            {
                TextStyle = small,
                Text = string.Format(Strings.Get(prefix + ".MIN_MAX"), max),
            };
            var preLabel = new PLabel("pre_" + name)
            {
                TextStyle = PUITuning.Fonts.TextDarkStyle,
                Text = Strings.Get(prefix + ".PRE"),
            };
            var pstLabel = new PLabel("pst_" + name)
            {
                TextStyle = PUITuning.Fonts.TextDarkStyle,
                Text = Strings.Get(prefix + ".PST"),
            };

            var textField = new PTextField("text_" + name)
            {
                MinWidth = 40,
                Type = PTextField.FieldType.Integer,
                OnTextChanged = OnTextChanged,
            }.AddOnRealize(realized => text_go = realized);

            var margin = new RectOffset(12, 12, 2, 2);
            var panel_top = new PPanel("slider_top_" + name)
            {
                Alignment = TextAnchor.MiddleCenter,
                Direction = PanelDirection.Horizontal,
                FlexSize = Vector2.right,
                Margin = margin,
                Spacing = 4,
            };
            panel_top.AddChild(minLabel).AddChild(new PSpacer()).AddChild(preLabel)
                .AddChild(textField).AddChild(pstLabel).AddChild(new PSpacer()).AddChild(maxLabel);

            var slider = new PSliderSingle("slider_" + name)
            {
                MinValue = min,
                MaxValue = max,
                IntegersOnly = true,
                Direction = UnityEngine.UI.Slider.Direction.LeftToRight,
                FlexSize = Vector2.right,
                HandleSize = 24,
                TrackSize = 16,
                ToolTip = Strings.Get(prefix + ".TOOLTIP"),
                OnDrag = OnSliderChanged,
                OnValueChanged = OnSliderChanged,
            }.AddOnRealize(realized =>
            {
                slider_go = realized;
                if (customTooltip != null)
                {
                    var ks = slider_go.GetComponent<KSlider>();
                    var toolTip = slider_go.GetComponent<ToolTip>();
                    if (ks != null && toolTip != null)
                    {
                        toolTip.OnToolTip = () => customTooltip(ks.value);
                        toolTip.refreshWhileHovering = true;
                    }
                }
            });

            var panel_bottom = new PPanel("slider_bottom_" + name)
            {
                Alignment = TextAnchor.MiddleCenter,
                Direction = PanelDirection.Horizontal,
                FlexSize = Vector2.right,
                Margin = margin,
                Spacing = 4,
            };
            panel_bottom.AddChild(slider);
            return parent.AddChild(panel_top).AddChild(panel_bottom);
        }
    }
}