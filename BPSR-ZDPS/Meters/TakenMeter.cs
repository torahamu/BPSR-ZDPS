using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.Windows;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ZLinq;

namespace BPSR_ZDPS.Meters
{
    public class TakenMeter : MeterBase
    {
        public TakenMeter()
        {
            Name = "NPC Taken";
        }

        bool SelectableWithHint(string label, string hint)
        {
            ImGui.AlignTextToFramePadding(); // This makes the entries about 1/3 larger but keeps it nicely centered
            bool ret = ImGui.Selectable(label);
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0.0f, ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(hint).X));
            ImGui.TextUnformatted(hint);
            return ret;
        }

        public override void Draw(MainWindow mainWindow)
        {
            if (ImGui.BeginListBox("##TakenMeterList", new Vector2(-1, -1)))
            {
                if (Settings.Instance.KeepPastEncounterInMeterUntilNextDamage)
                {
                    if ((AppState.ActiveEncounter == null && EncounterManager.Current != null) || (AppState.ActiveEncounter != null && AppState.ActiveEncounter.Entities.IsEmpty))
                    {
                        AppState.ActiveEncounter = EncounterManager.Current;
                    }
                    /*else if (AppState.ActiveEncounter?.BattleId != EncounterManager.Current?.BattleId)
                    {
                        AppState.ActiveEncounter = EncounterManager.Current;
                    }*/
                    else if (AppState.ActiveEncounter?.EncounterId != EncounterManager.Current?.EncounterId)
                    {
                        if (EncounterManager.Current.HasStatsBeenRecorded())
                        {
                            AppState.ActiveEncounter = EncounterManager.Current;
                        }
                    }
                }
                else
                {
                    if (AppState.ActiveEncounter?.EncounterId != EncounterManager.Current?.EncounterId || AppState.ActiveEncounter?.BattleId != EncounterManager.Current?.BattleId)
                    {
                        AppState.ActiveEncounter = EncounterManager.Current;
                    }
                }

                var playerList = AppState.ActiveEncounter.Entities.AsValueEnumerable().Where(x => x.Value.EntityType == Zproto.EEntityType.EntMonster).OrderByDescending(x => x.Value.TotalTakenDamage);

                ulong topTotalValue = 0;

                int i = 0;
                foreach (var player in playerList)
                {
                    var entity = player.Value;

                    if (i == 0 && Settings.Instance.NormalizeMeterContributions)
                    {
                        topTotalValue = entity.TotalTakenDamage;
                    }

                    string name = "Unknown";
                    if (!string.IsNullOrEmpty(entity.Name))
                    {
                        name = entity.Name;
                    }

                    if (AppState.PlayerUUID != 0 && AppState.PlayerUUID == entity.UUID)
                    {
                        AppState.PlayerMeterPlacement = i + 1;
                        AppState.PlayerTotalMeterValue = entity.TotalTakenDamage;
                        AppState.PlayerMeterValuePerSecond = entity.TakenStats.ValuePerSecond;
                    }

                    double contribution = 0.0;
                    double contributionProgressBar = 0.0;
                    if (AppState.ActiveEncounter.TotalNpcTakenDamage != 0)
                    {
                        contribution = Math.Round(((double)entity.TotalTakenDamage / (double)AppState.ActiveEncounter.TotalNpcTakenDamage) * 100, 4);

                        if (Settings.Instance.NormalizeMeterContributions)
                        {
                            contributionProgressBar = Math.Round(((double)entity.TotalTakenDamage / (double)topTotalValue) * 100, 4);
                        }
                        else
                        {
                            contributionProgressBar = contribution;
                        }
                    }
                    string truePerSecond = "";
                    if (Settings.Instance.DisplayTruePerSecondValuesInMeters)
                    {
                        truePerSecond = $"[{Utils.NumberToShorthand(entity.TakenStats.TrueValuePerSecond)}] ";
                    }
                    string totalTaken = Utils.NumberToShorthand(entity.TotalTakenDamage);
                    string totalTps = Utils.NumberToShorthand(entity.TakenStats.ValuePerSecond);
                    StringBuilder format = new();
                    var startPoint = ImGui.GetCursorPos();
                    // ImGui.GetTextLineHeightWithSpacing();

                    ImGui.PushFont(HelperMethods.Fonts["Cascadia-Mono"], 14.0f * Settings.Instance.WindowSettings.MainWindow.MeterBarScale);

                    bool hasHpData = entity.Hp >= 0 && entity.MaxHp > 0;
                    if (hasHpData && (Settings.Instance.MeterSettingsNpcTakenShowHpData || Settings.Instance.MeterSettingsNpcTakenUseHpMeter))
                    {
                        var healthPct = MathF.Round((float)entity.Hp / (float)entity.MaxHp, 4);

                        if (Settings.Instance.MeterSettingsNpcTakenShowHpData)
                        {
                            format.Append("[ ");

                            format.Append(Utils.NumberToShorthand(entity.Hp));

                            if (!Settings.Instance.MeterSettingsNpcTakenHideMaxHp)
                            {
                                format.Append($" / {Utils.NumberToShorthand(entity.MaxHp)}");
                            }

                            format.Append($" ({(healthPct * 100).ToString("00.00")}%)");

                            format.Append(" ]");
                        }

                        if (Settings.Instance.MeterSettingsNpcTakenUseHpMeter)
                        {
                            ImGui.SetCursorPos(startPoint);
                            //ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0, 0, 0));
                            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Colors.DarkRed);

                            ImGui.ProgressBar(healthPct, new Vector2(-1, 0), $"##TakenEntryHealth_{i}");
                            ImGui.PopStyleColor();
                        }
                    }

                    if (!Settings.Instance.MeterSettingsNpcTakenUseHpMeter || !hasHpData)
                    {
                        ImGui.ProgressBar((float)contributionProgressBar / 100.0f, new Vector2(-1, 0), $"##TakenEntryContribution_{i}");
                    }

                    format.Append($" {totalTaken} {truePerSecond}({totalTps}) {contribution.ToString("F0").PadLeft(3, ' ')}%");

                    ImGui.SetCursorPos(startPoint);
                    if (SelectableWithHint($"{name} [{entity.UID.ToString()}]##TakenEntry_{i}", format.ToString()))
                    {
                        mainWindow.entityInspector = new EntityInspector();
                        mainWindow.entityInspector.LoadEntity(entity, AppState.ActiveEncounter.StartTime);
                        mainWindow.entityInspector.Open();
                    }

                    ImGui.PopFont();
                    i++;
                }

                ImGui.EndListBox();
            }
        }
    }
}
