using System;
using System.Globalization;
using UnityEngine;
using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using static STRINGS.UI;

namespace MorePlantMutations
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class ModOptions : BaseOptions<ModOptions>
    {
        [JsonProperty]
        [Option]
        public Glowstick glowstick { get; set; } = new();

        [JsonProperty]
        [Option]
        public BPTintegration bpt_intergration { get; set; } = new();

        [JsonObject(MemberSerialization.OptIn)]
        public class Glowstick
        {
            [JsonProperty]
            [Option]
            public bool emit_light { get; set; } = true;

            [JsonProperty]
            [Option]
            public bool adjust_radiation_by_grow_speed { get; set; } = false;

            [JsonProperty]
            [Option]
            public bool decrease_radiation_by_wildness { get; set; } = false;
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class BPTintegration
        {
            [JsonIgnore]
            [DynamicOption(typeof(BPTLabel))]
            public LocText label => null;

            [JsonProperty]
            [Option]
            public bool enable { get; set; } = true;

            [JsonIgnore]
            [DynamicOption(typeof(BPTButtons))]
            public LocText buttons => null;
        }

        internal static Type BPTOptionsType;

        private abstract class CustomOption : IOptionsEntry
        {
            public virtual bool RestartRequired { get; set; } = false;
            public string Category => string.Empty;
            public string Format => string.Empty;
            public string Title => string.Empty;
            public string Tooltip => string.Empty;
            public abstract void CreateUIEntry(PGridPanel parent, ref int row);
            public void ReadFrom(object settings) { }
            public void WriteTo(object settings) { }
        }

        private class BPTLabel : CustomOption
        {
            public override void CreateUIEntry(PGridPanel parent, ref int row)
            {
                parent.AddChild(new PLabel("Label")
                {
                    Text = STRINGS.OPTIONS.BPT_LABEL.NAME.text,
                    ToolTip = string.Empty,
                    TextStyle = PUITuning.Fonts.TextLightStyle.DeriveStyle(0, null, null),
                }.AddOnRealize(OnRealize), new GridComponentSpec(row, 0)
                {
                    Margin = new RectOffset(0, 0, 2, 2),
                    Alignment = TextAnchor.MiddleLeft,
                    ColumnSpan = 2
                });
            }

            private void OnRealize(GameObject label)
            {
                var text = label.GetComponentInChildren<LocText>();
                if (text != null)
                    text.alignment = TMPro.TextAlignmentOptions.Left;
            }
        }

        private class BPTButtons : CustomOption
        {
            public override void CreateUIEntry(PGridPanel parent, ref int row)
            {
                var panel = new PPanel("Buttons")
                {
                    Spacing = 10,
                    Direction = PanelDirection.Horizontal,
                    Alignment = TextAnchor.MiddleCenter,
                    FlexSize = Vector2.right,
                };

                if (BPTOptionsType != null)
                    panel.AddChild(new PButton("ShowOptions")
                    {
                        Text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(FRONTEND.MAINMENU.OPTIONS.text.ToLower()),
                        ToolTip = PLibStrings.DIALOG_TITLE.text.F(FormatAsKeyWord(STRINGS.OPTIONS.BPT_TITLE.text)),
                        OnClick = ShowOptions,
                        TextAlignment = TextAnchor.MiddleCenter,
                        Margin = PDialog.BUTTON_MARGIN,
                    }.SetKleiPinkStyle());

                panel.AddChild(new PButton("ShowWorkshop")
                {
                    Text = FRONTEND.MODS.MANAGE,
                    ToolTip = FRONTEND.MODS.TOOLTIPS.MANAGE_STEAM_SUBSCRIPTION,
                    OnClick = ShowWorkshop,
                    TextAlignment = TextAnchor.MiddleCenter,
                    Margin = PDialog.BUTTON_MARGIN,
                }.SetKleiPinkStyle());

                parent.AddChild(panel, new GridComponentSpec(row, 0)
                {
                    Margin = new RectOffset(0, 0, 2, 2),
                    Alignment = TextAnchor.MiddleCenter,
                    ColumnSpan = 2,
                });
            }

            private void ShowOptions(GameObject _) => POptions.ShowDialog(BPTOptionsType);
            private void ShowWorkshop(GameObject _) => App.OpenWebURL("https://steamcommunity.com/sharedfiles/filedetails/?id=2778941969");
        }
    }
}
