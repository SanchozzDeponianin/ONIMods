using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using STRINGS;
using UnityEngine;
using HarmonyLib;
using SanchozzONIMods.Lib;
using UnityEngine.UI;

namespace VitalsDisplayRads
{
    internal static class VitalsDisplayRadsPatches
    {
        private sealed class Mod : KMod.UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                if (Utils.LogModVersion()) return;
                base.OnLoad(harmony);
            }
        }

        // так как УИ прибит гвоздями, попробуем эмпирически уменьшить ширину столбцов чтобы все влезло и поместилось
        [HarmonyPatch]
        private static class LabelTableColumn_GetWidget
        {
            private static bool Prepare() => Sim.IsRadiationEnabled();
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return typeof(LabelTableColumn).GetMethod(nameof(LabelTableColumn.GetHeaderWidget));
                yield return typeof(LabelTableColumn).GetMethod(nameof(LabelTableColumn.GetDefaultWidget));
                yield return typeof(LabelTableColumn).GetMethod(nameof(LabelTableColumn.GetMinionWidget));
            }

            private static void Postfix(GameObject __result)
            {
                var label = __result.GetComponentInChildren<LocText>();
                if (label != null && label.TryGetComponent(out LayoutElement label_layout) && __result.TryGetComponent(out LayoutElement widget_layout))
                {
                    widget_layout.minWidth = Mathf.Min(widget_layout.minWidth, label_layout.minWidth + 32);
                    widget_layout.preferredWidth = Mathf.Min(widget_layout.preferredWidth, label_layout.preferredWidth + 32);
                }
            }
        }

        [HarmonyPatch(typeof(VitalsTableScreen), nameof(VitalsTableScreen.OnActivate))]
        private static class VitalsTableScreen_OnActivate
        {
            private static bool Prepare() => Sim.IsRadiationEnabled();

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                return TranspilerUtils.Wrap(instructions, original, transpiler);
            }
            // внедряем после столбца здоровья
            private static bool transpiler(List<CodeInstruction> instructions)
            {
                var add_column = typeof(TableScreen).GetMethod(nameof(TableScreen.AddLabelColumn), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var inject = typeof(VitalsTableScreen_OnActivate).GetMethod(nameof(Inject), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (add_column == null || inject == null)
                    return false;
                int i = instructions.FindIndex(ins => ins.LoadsConstant("Health"));
                if (i == -1)
                    return false;
                i = instructions.FindIndex(i, ins => ins.Calls(add_column));
                if (i == -1)
                    return false;
                instructions.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                instructions.Insert(++i, new CodeInstruction(OpCodes.Call, inject));
                return true;
            }

            private static void Inject(VitalsTableScreen screen)
            {
                screen.AddLabelColumn("RadTarakan", screen.on_load_radiation, screen.get_value_radiation_label,
                    compare_rows_radiation, screen.on_tooltip_radiation, screen.on_tooltip_sort_radiation, 96, true);
            }
        }

        private static string title;
        private static void on_load_radiation(this VitalsTableScreen screen, IAssignableIdentity identity, GameObject widget_go)
        {
            if (string.IsNullOrEmpty(title))
            {
                title = UI.OVERLAYS.RADIATION.NAME.ToString();
                title = string.Concat(title[0].ToString().ToUpperInvariant(), title.Substring(1).ToLowerInvariant());
            }
            var row = screen.GetWidgetRow(widget_go);
            var label = widget_go.GetComponentInChildren<LocText>(true);
            if (identity != null)
            {
                label.text = (screen.GetWidgetColumn(widget_go) as LabelTableColumn).get_value_action(identity, widget_go);
                return;
            }
            label.text = row.isDefault ? "" : title;
        }

        private static string get_value_radiation_label(this VitalsTableScreen screen, IAssignableIdentity identity, GameObject widget_go)
        {
            var row = screen.GetWidgetRow(widget_go);
            if (row.rowType == TableRow.RowType.Minion)
            {
                var minion = identity as MinionIdentity;
                if (minion != null)
                    return Db.Get().Amounts.RadiationBalance.Lookup(minion).GetValueString();
            }
            else if (row.rowType == TableRow.RowType.StoredMinon)
                return UI.TABLESCREENS.NA;
            return "";
        }

        private static int compare_rows_radiation(IAssignableIdentity identity_a, IAssignableIdentity identity_b)
        {
            var minion_a = identity_a as MinionIdentity;
            var minion_b = identity_b as MinionIdentity;
            if (minion_a == null && minion_b == null)
                return 0;
            if (minion_a == null)
                return -1;
            if (minion_b == null)
                return 1;
            float value_a = Db.Get().Amounts.RadiationBalance.Lookup(minion_a).value;
            float value_b = Db.Get().Amounts.RadiationBalance.Lookup(minion_b).value;
            return value_b.CompareTo(value_a);
        }

        private static void on_tooltip_radiation(this VitalsTableScreen screen, IAssignableIdentity identity, GameObject widget_go, ToolTip tooltip)
        {
            tooltip.ClearMultiStringTooltip();
            switch (screen.GetWidgetRow(widget_go).rowType)
            {
                case TableRow.RowType.Header:
                case TableRow.RowType.Default:
                    break;
                case TableRow.RowType.Minion:
                    MinionIdentity minion = identity as MinionIdentity;
                    if (minion != null)
                    {
                        tooltip.AddMultiStringTooltip(Db.Get().Amounts.RadiationBalance.Lookup(minion).GetTooltip(), null);
                        return;
                    }
                    break;
                case TableRow.RowType.StoredMinon:
                    screen.StoredMinionTooltip(identity, tooltip);
                    break;
                default:
                    return;
            }
        }

        private static string toltip;
        private static void on_tooltip_sort_radiation(this VitalsTableScreen screen, IAssignableIdentity minion, GameObject widget_go, ToolTip tooltip)
        {
            if (string.IsNullOrEmpty(toltip))
            {
                toltip = UI.TABLESCREENS.COLUMN_SORT_BY_FULLNESS.ToString();
                string name = DUPLICANTS.STATS.RADIATIONBALANCE.NAME.ToString();
                const string b = "<b>", _b = "</b>";
                int m = toltip.IndexOf(b), n = toltip.IndexOf(_b);
                if (m != -1 && n != -1)
                    toltip = string.Concat(toltip.Substring(0, m + b.Length), name, toltip.Substring(n));
                else
                    toltip = string.Concat(b, name, _b);
            }
            tooltip.ClearMultiStringTooltip();
            switch (screen.GetWidgetRow(widget_go).rowType)
            {
                case TableRow.RowType.Header:
                    tooltip.AddMultiStringTooltip(toltip, null);
                    break;
                case TableRow.RowType.Default:
                case TableRow.RowType.Minion:
                case TableRow.RowType.StoredMinon:
                    break;
                default:
                    return;
            }
        }
    }
}