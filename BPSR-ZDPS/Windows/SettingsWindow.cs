using BPSR_ZDPSLib;
using BPSR_ZDPS.DataTypes;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.Windows
{
    public static class SettingsWindow
    {
        public const string LAYER = "SettingsWindowLayer";
        public static string TITLE_ID = "###SettingsWindow";

        static int PreviousSelectedNetworkDeviceIdx = -1;
        static int SelectedNetworkDeviceIdx = -1;
        static bool normalizeMeterContributions;
        static bool useShortWidthNumberFormatting;
        static bool showClassIconsInMeters;
        static bool colorClassIconsByRole;
        static bool showSkillIconsInDetails;
        static bool onlyShowDamageContributorsInMeters;
        static bool onlyShowPartyMembersInMeters;
        static bool showAbilityScoreInMeters;
        static bool showSeasonStrengthInMeters;
        static bool showSubProfessionNameInMeters;
        static bool useAutomaticWipeDetection;
        static bool skipTeleportStateCheckInAutomaticWipeDetection;
        static bool disableWipeRecalculationOverwriting;
        static bool splitEncountersOnNewPhases;
        static bool displayTruePerSecondValuesInMeters;
        static bool allowGamepadNavigationInputInZDPS;
        static bool keepPastEncounterInMeterUntilNextDamage;
        static bool useDatabaseForEncounterHistory;
        static int databaseRetentionPolicyDays;
        static bool limitEncounterBuffTrackingWithoutDatabase;
        static bool allowEncounterSavingPausingInOpenWorld;

        static bool meterSettingsTankingShowDeaths;
        static bool meterSettingsNpcTakenShowHpData;
        static bool meterSettingsNpcTakenHideMaxHp;
        static bool meterSettingsNpcTakenUseHpMeter;

        static bool playNotificationSoundOnMatchmake;
        static string matchmakeNotificationSoundPath;
        static bool loopNotificationSoundOnMatchmake;
        static float matchmakeNotificationVolume;
        static bool playNotificationSoundOnReadyCheck;
        static string readyCheckNotificationSoundPath;
        static bool loopNotificationSoundOnReadyCheck;
        static float readyCheckNotificationVolume;

        static bool logToFile;

        static bool IsBindingEncounterResetKey = false;
        static uint EncounterResetKey;
        static string EncounterResetKeyName = "";
        static bool IsBindingPinnedWindowClickthroughKey = false;
        static uint PinnedWindowClickthroughKey;
        static string PinnedWindowClickthroughKeyName = "";

        static SharpPcap.LibPcap.LibPcapLiveDeviceList? NetworkDevices;
        static EGameCapturePreference GameCapturePreference;
        static string gameCaptureCustomExeName;

        static bool saveEncounterReportToFile;
        static int reportFileRetentionPolicyDays;
        static int minimumPlayerCountToCreateReport;
        static bool alwaysCreateReportAtDungeonEnd;

        static bool webhookReportsEnabled;
        static EWebhookReportsMode webhookReportsMode;
        static string webhookReportsDeduplicationServerUrl;
        static string webhookReportsDiscordUrl;
        static string webhookReportsCustomUrl;

        static bool checkForZDPSUpdatesOnStartup;
        static string latestZDPSVersionCheckURL;

        static bool lowPerformanceMode;

        // External Settings
        static bool externalBPTimerEnabled;
        static bool externalBPTimerIncludeCharacterId;
        static bool externalBPTimerFieldBossHpReportsEnabled;

        static WindowSettings windowSettings;

        static bool IsDiscordWebhookUrlValid = true;

        static int RunOnceDelayed = 0;

        static bool IsElevated = false;

        public static void Open()
        {
            RunOnceDelayed = 0;

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup(TITLE_ID);

            NetworkDevices = SharpPcap.LibPcap.LibPcapLiveDeviceList.Instance;

            Load();

            LoadHotkeys();

            // Set selection to matching device name (the index could have changed since last time we were here)
            if (!string.IsNullOrEmpty(Settings.Instance.NetCaptureDeviceName))
            {
                for (int i = 0; i < NetworkDevices.Count; i++)
                {
                    if (NetworkDevices[i].Name == Settings.Instance.NetCaptureDeviceName)
                    {
                        SelectedNetworkDeviceIdx = i;
                        if (PreviousSelectedNetworkDeviceIdx == -1)
                        {
                            // This is the first time we're opening the menu, so let's set the default previous value as well
                            // Doing so prevents the capture from being restarted on first save
                            PreviousSelectedNetworkDeviceIdx = i;
                        }
                    }
                }
            }

            // Default to first device in list as fallback, if there are any
            if (SelectedNetworkDeviceIdx == -1 && NetworkDevices?.Count > 0)
            {
                SelectedNetworkDeviceIdx = 0;
            }

            // Disable all HotKeys while we're in the Settings menu to prevent unexpected behavior when rebinding
            HotKeyManager.UnregisterAllHotKeys();

            ImGui.PopID();
        }

        public static void Draw(MainWindow mainWindow)
        {
            var io = ImGui.GetIO();
            var main_viewport = ImGui.GetMainViewport();

            // TODO: Open window at center of current active monitor
            // Will need to use GLFW to figure out monitors/sizes/positions/etc

            //ImGui.SetNextWindowPos(new Vector2(main_viewport.WorkPos.X + 200, main_viewport.WorkPos.Y + 120), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(550, 350), new Vector2(ImGui.GETFLTMAX()));
            //ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X, io.DisplaySize.Y), ImGuiCond.Appearing);

            ImGui.SetNextWindowSize(new Vector2(650, 680), ImGuiCond.FirstUseEver);
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));

            if (ImGui.BeginPopupModal($"Settings{TITLE_ID}"))
            {
                if (RunOnceDelayed == 0)
                {
                    RunOnceDelayed++;
                    using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
                    {
                        IsElevated = new System.Security.Principal.WindowsPrincipal(identity).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                    }
                }
                else if (RunOnceDelayed == 2)
                {
                    RunOnceDelayed++;
                    Utils.SetCurrentWindowIcon();
                    Utils.BringWindowToFront();
                }
                else if (RunOnceDelayed < 3)
                {
                    RunOnceDelayed++;
                }

                ImGuiTabBarFlags tabBarFlags = ImGuiTabBarFlags.FittingPolicyScroll | ImGuiTabBarFlags.NoTooltip | ImGuiTabBarFlags.NoCloseWithMiddleMouseButton;
                if (ImGui.BeginTabBar("##SettingsTabs", tabBarFlags))
                {
                    if (ImGui.BeginTabItem("一般"))
                    {
                        var contentRegionAvail = ImGui.GetContentRegionAvail();
                        ImGui.BeginChild("##GeneralTabContent", new Vector2(contentRegionAvail.X, contentRegionAvail.Y - 56), ImGuiChildFlags.Borders);

                        ImGui.SeparatorText("ネットワークデバイス");
                        ImGui.Text("取得に使用するネットワークデバイスを選択してください:");

                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

                        string network_device_preview = "";
                        if (SelectedNetworkDeviceIdx > -1 && NetworkDevices?.Count > 0)
                        {
                            network_device_preview = NetworkDevices[SelectedNetworkDeviceIdx].Description;
                        }

                        if (ImGui.BeginCombo("##NetworkDeviceCombo", network_device_preview, ImGuiComboFlags.HeightLarge))
                        {
                            for (int i = 0; i < NetworkDevices?.Count; i++)
                            {
                                bool isSelected = (SelectedNetworkDeviceIdx == i);
                                var device = NetworkDevices[i];

                                string friendlyName = "";
                                if (!string.IsNullOrEmpty(device.Interface?.FriendlyName))
                                {
                                    friendlyName = $"{device.Interface?.FriendlyName}\n";
                                }

                                if (ImGui.Selectable($"{friendlyName}{device.Description}\n{device.Name}", isSelected))
                                {
                                    SelectedNetworkDeviceIdx = i;
                                }

                                if (isSelected)
                                {
                                    ImGui.SetItemDefaultFocus();
                                }

                                ImGui.Separator();
                            }

                            if (NetworkDevices == null || NetworkDevices?.Count == 0)
                            {
                                ImGui.Selectable("ネットワークデバイスが見つかりません");
                            }

                            ImGui.EndCombo();
                        }

                        ImGui.AlignTextToFramePadding();
                        ImGui.TextUnformatted("ゲームキャプチャ設定: ");
                        ImGui.SameLine();

                        var gamePrefName = Utils.GameCapturePreferenceToName(GameCapturePreference);
                        ImGui.SetNextItemWidth(150);
                        if (ImGui.BeginCombo("##EGameCapturePreference", gamePrefName))
                        {
                            if (ImGui.Selectable("自動"))
                            {
                                GameCapturePreference = EGameCapturePreference.Auto;
                            }
                            else if (ImGui.Selectable("スタンドアロン"))
                            {
                                GameCapturePreference = EGameCapturePreference.Standalone;
                            }
                            else if (ImGui.Selectable("Steam"))
                            {
                                GameCapturePreference = EGameCapturePreference.Steam;
                            }
                            else if (ImGui.Selectable("Epic"))
                            {
                                GameCapturePreference = EGameCapturePreference.Epic;
                            }
                            else if (ImGui.Selectable("HaoPlay SEA"))
                            {
                                GameCapturePreference = EGameCapturePreference.HaoPlaySea;
                            }
                            else if (ImGui.Selectable("XDG"))
                            {
                                GameCapturePreference = EGameCapturePreference.XDG;
                            }
                            else if (ImGui.Selectable("カスタム"))
                            {
                                GameCapturePreference = EGameCapturePreference.Custom;
                            }
                            ImGui.SetItemTooltip(
                                "一覧にないゲームバージョンを使用する場合に選択してください。\n" +
                                "※ この機能を使うには、ゲーム実行ファイル名の入力が必要です。\n" +
                                "'GameAssembly.dll' と同じ場所にあります。"
                            );

                            ImGui.EndCombo();
                        }

                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped(
                            "ZDPS がどのゲームバージョンからデータを取得するか選択します。\n" +
                            "Auto は現在起動中のバージョンを自動検出して使用します。\n" +
                            "同時に複数クライアントを起動している場合、Auto ではデータ不整合が発生します。\n" +
                            "Steam と Standalone はそれぞれのバージョンのみを監視するため、同時起動が可能で、DPSは片方のみ取得されます。"
                        );
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        if (GameCapturePreference == EGameCapturePreference.Custom)
                        {
                            ImGui.Indent();

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("カスタムBPSR実行ファイル名: ");
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(-1);
                            if (ImGui.InputText("##GameCaptureCustomExeName", ref gameCaptureCustomExeName, 512))
                            {
                                gameCaptureCustomExeName = Path.GetFileNameWithoutExtension(gameCaptureCustomExeName);
                            }
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("監視するゲーム実行ファイル名（例: BPSR_STEAM）");
                            ImGui.EndDisabled();
                            ImGui.Unindent();

                            ImGui.Unindent();
                        }

                        ImGui.SeparatorText("キー設定");

                        if (IsElevated == false)
                        {
                            ImGui.PushStyleColor(ImGuiCol.ChildBg, Colors.Red_Transparent);
                            ImGui.BeginChild("##KeybindsNotice", new Vector2(0, 0), ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.Borders);
                            ImGui.TextWrapped("重要なお知らせ:");
                            ImGui.TextWrapped("ZDPS を管理者として実行していない場合、キー設定はゲームにフォーカスがある時のみ動作します。これはゲーム側の制約です。");
                            ImGui.EndChild();
                            ImGui.PopStyleColor();
                        }

                        ImGui.TextWrapped("以下はアプリのグローバルホットキー設定です。ボックスをクリックしてキーを押すと割り当てられます。修飾キー（Ctrl/Alt/Shift）は対応していません。");
                        ImGui.TextWrapped("Escapeキーで再割り当てをキャンセルできます。");

                        ImGui.Indent();

                        RebindKeyButton("エンカウントリセット", ref EncounterResetKey, ref EncounterResetKeyName, ref IsBindingEncounterResetKey);
                        if (splitEncountersOnNewPhases)
                        {
                            ImGui.Indent();
                            ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red_Transparent);
                            ImGui.TextWrapped(
                                "［フェーズ毎にエンカウントを分割］が有効になっています。\n" +
                                "通常、このキー設定で手動リセットを行う必要はありません。\n" +
                                "ZDPS が自動的にエンカウントの分割を処理します。"
                            );
                            ImGui.PopStyleColor();
                            ImGui.Unindent();
                        }
                        RebindKeyButton("固定ウィンドウのクリック透過", ref PinnedWindowClickthroughKey, ref PinnedWindowClickthroughKeyName, ref IsBindingPinnedWindowClickthroughKey);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("固定（最前面）ウィンドウを無視して、マウス入力をその背後（ゲームや他のアプリなど）に通します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.Unindent();

                        ImGui.SeparatorText("ZDPSアップデート確認");

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("起動時にZDPSの更新を確認: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##CheckForZDPSUpdatesOnStartup", ref checkForZDPSUpdatesOnStartup);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、起動時にオンラインで更新を確認します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("最新ZDPSバージョン確認URL: ");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(-1);
                        if (ImGui.InputText("##LatestZDPSVersionCheckURL", ref latestZDPSVersionCheckURL, 512))
                        {
                            // If the value was empty, revert back to the default URL
                            if (string.IsNullOrEmpty(latestZDPSVersionCheckURL))
                            {
                                latestZDPSVersionCheckURL = "https://raw.githubusercontent.com/Blue-Protocol-Source/BPSR-ZDPS-Metadata/master/LatestVersion.txt";
                            }
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("ZDPSの更新確認に使用するURLです。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.SeparatorText("データベース");

                        ShowRestartRequiredNotice(Settings.Instance.UseDatabaseForEncounterHistory != useDatabaseForEncounterHistory, "Use Database For Encounter History");

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("エンカウント履歴にデータベースを使用: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##UseDatabaseForEncounterHistory", ref useDatabaseForEncounterHistory);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、全エンカウントデータをローカルDB（ZDatabase.db）に保存し、メモリ使用量を減らしてセッション間でも閲覧できるようにします。ZDPS再起動後に適用されます。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.BeginDisabled(!useDatabaseForEncounterHistory);
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("エンカウント履歴の保持期間: ");
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                        ImGui.SetNextItemWidth(-1);
                        ImGui.SliderInt("##DatabaseRetentionPolicyDays", ref databaseRetentionPolicyDays, 0, 30, databaseRetentionPolicyDays == 0 ? "無期限" : $"{databaseRetentionPolicyDays} Days");
                        ImGui.PopStyleColor(2);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("過去のエンカウント履歴を保持する期間です。「無期限」以外の場合、期限切れデータはアプリ終了時に自動削除されます。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();
                        ImGui.EndDisabled();

                        ImGui.BeginDisabled(useDatabaseForEncounterHistory);
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("DBなし時のバフ追跡を制限: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##LimitEncounterBuffTrackingWithoutDatabase", ref limitEncounterBuffTrackingWithoutDatabase);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、バフ履歴をエンティティごとに最新100件までに制限します（無制限ではありません）。DBが無効な場合のみ適用され、メモリ使用量を抑えます。この設定は過去データには遡って適用されません。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();
                        ImGui.EndDisabled();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("オープンワールドでエンカウント保存の一時停止を許可: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##AllowEncounterSavingPausingInOpenWorld", ref allowEncounterSavingPausingInOpenWorld);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped(
                            "有効にすると、現在のエンカウントをデータベースに保存しないようにするボタンがメインウィンドウ上部に表示されます。\n" +
                            "この機能はオープンワールド中のみ使用でき、マップ移動時に自動的に無効になります。\n" +
                            "一時停止中は、ベンチマークおよび手動での新規エンカウント作成は無効になります。\n" +
                            "※ ZDPS起動後、このボタンが表示されるまでに最低1回のマップ移動が必要です。"
                        );
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.EndChild();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("戦闘"))
                    {
                        var contentRegionAvail = ImGui.GetContentRegionAvail();
                        ImGui.BeginChild("##CombatTabContent", new Vector2(contentRegionAvail.X, contentRegionAvail.Y - 56), ImGuiChildFlags.Borders);

                        ImGui.SeparatorText("戦闘");

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("メーター貢献度バーを正規化: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##NormalizeMeterContributions", ref normalizeMeterContributions);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、各プレイヤーのバーは全体貢献ではなくトッププレイヤー基準になります。");
                        ImGui.TextWrapped("つまりトッププレイヤーが常に「100%」として扱われます。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("数値を短縮表記で表示: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##UseShortWidthNumberFormatting", ref useShortWidthNumberFormatting);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、1000を超える値などで短い数値表記を使用します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("自動全滅検出を使用: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##UseAutomaticWipeDetection", ref useAutomaticWipeDetection);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、ボス戦でのパーティ全滅を検出して自動的に新しいエンカウントを開始します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("自動全滅検出でテレポート状態確認を省略: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##SkipTeleportStateCheckInAutomaticWipeDetection", ref skipTeleportStateCheckInAutomaticWipeDetection);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、自動全滅検出における「Teleport」状態の確認を行いません。通常は無効のままが推奨です。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("全滅再計算の上書きを許可: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##DisableWipeRecalculationOverwriting", ref disableWipeRecalculationOverwriting);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        //ImGui.TextWrapped("When enabled, the internal process of checking the Dead status of all players in the Encounter is allowed to overwrite the detected wipe status from the normal automatic detector.\nAllowing this to overturn results is experimental so only enable it if you run into incorrect wipe reporting.");
                        ImGui.TextWrapped("有効にすると、新しいワイプ再計算ロジックは無効になり、「自動ワイプ検出を使用」が有効な場合は従来の方式が使用されます。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("新フェーズでエンカウントを分割: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##SplitEncountersOnNewPhases", ref splitEncountersOnNewPhases);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、フェーズ変更ごとにエンカウントを自動分割します。ダンジョン内のボス部分を分けたり、レイドボスのフェーズを分割できます。基本的に有効推奨です。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("メーターに真の毎秒値を表示: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##DisplayTruePerSecondValuesInMeters", ref displayTruePerSecondValuesInMeters);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped(
                            "有効にすると、メーターに表示されるダメージ・回復・被ダメージの毎秒値に、通常の「アクティブ毎秒値」に加えて、角括弧で表示される「真の毎秒値」が表示されます。\n" +
                            "真の毎秒値は、実際の戦闘参加時間ではなく、毎秒ごとに再計算された値です。\n" +
                            "※ どちらの値も正確であり、計算方式が異なるだけです。\n" +
                            "この設定は次のエンカウントからのみ有効になります。過去のエンカウントには反映されず、現在はメーターUI上のみ表示されます。"
                        );
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.EndChild();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("ユーザーインターフェース"))
                    {
                        var contentRegionAvail = ImGui.GetContentRegionAvail();
                        ImGui.BeginChild("##UserInterfaceTabContent", new Vector2(contentRegionAvail.X, contentRegionAvail.Y - 56), ImGuiChildFlags.Borders);

                        ImGui.SeparatorText("ユーザーインターフェース");

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("メーターにクラスアイコンを表示: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##ShowClassIconsInMeters", ref showClassIconsInMeters);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、メーター上でプレイヤー名の横にクラスアイコンを表示します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("ロール別にクラスアイコンを色分け: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##ColorClassIconsByRole", ref colorClassIconsByRole);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、クラスアイコンを白一色ではなくロール別の色で表示します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("詳細にスキルアイコンを表示: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##ShowSkillIconsInDetails", ref showSkillIconsInDetails);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、可能な場合に詳細パネルでスキル名の横にスキルアイコンを表示します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("メーターにダメージ貢献者のみ表示: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##OnlyShowContributorsInMeters", ref onlyShowDamageContributorsInMeters);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、ダメージを与えたプレイヤーのみDPSメーターに表示します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("メーターにアビリティスコアを表示: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##ShowAbilityScoreInMeters", ref showAbilityScoreInMeters);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、プレイヤーのアビリティスコアをメーターに表示します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("メーターにサブ職業名を表示: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##ShowSubProfessionNameInMeters", ref showSubProfessionNameInMeters);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、検出されたサブ職業名をメーターに表示します。サブ職業が検出できない場合は基本クラス名のみ表示します。基本クラスも不明な場合は『不明』と表示します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("ZDPSでゲームパッド操作を許可: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##AllowGamepadNavigationInputInZDPS", ref allowGamepadNavigationInputInZDPS);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped(
                            "有効にすると、ゲームパッド入力で ZDPS のウィンドウ操作やナビゲーションが可能になります。\n" +
                            "※ ウィンドウにフォーカスが無い状態でも、ゲームパッド入力が反映される場合があります。"
                        );
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("次のダメージまで前回エンカウントを表示: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##KeepPastEncounterInMeterUntilNextDamage", ref keepPastEncounterInMeterUntilNextDamage);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped(
                            "有効にすると、現在のエンカウントでダメージが発生するまで、前回のエンカウント内容がメーターUIに表示され続けます。\n" +
                            "ただし、戦闘切り替えイベント（通常はマップ移動）が発生した場合は、自動的に現在のエンカウントへ切り替わります。"
                        );
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        if (ImGui.CollapsingHeader("固定（最前面）ウィンドウの不透明度"))
                        {
                            ImGui.Indent();

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("メインウィンドウ: ");
                            ImGui.SetNextItemWidth(-1);
                            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                            if (ImGui.SliderInt("##MainWindowOpacity", ref windowSettings.MainWindow.Opacity, 20, 100, $"{windowSettings.MainWindow.Opacity}%%", ImGuiSliderFlags.ClampOnInput))
                            {
                                windowSettings.MainWindow.Opacity = windowSettings.MainWindow.Opacity;
                            }
                            ImGui.PopStyleColor(2);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("固定中のメインウィンドウの透明度です。");
                            ImGui.EndDisabled();
                            ImGui.Unindent();

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("クールダウン優先度トラッカー: ");
                            ImGui.SetNextItemWidth(-1);
                            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                            if (ImGui.SliderInt("##CooldownPriorityTrackerWindowOpacity", ref windowSettings.RaidManagerCooldowns.Opacity, 20, 100, $"{windowSettings.RaidManagerCooldowns.Opacity}%%", ImGuiSliderFlags.ClampOnInput))
                            {
                                windowSettings.RaidManagerCooldowns.Opacity = windowSettings.RaidManagerCooldowns.Opacity;
                            }
                            ImGui.PopStyleColor(2);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("固定中のクールダウントラッカーの透明度です。");
                            ImGui.EndDisabled();
                            ImGui.Unindent();

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("エンティティキャッシュ表示: ");
                            ImGui.SetNextItemWidth(-1);
                            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                            if (ImGui.SliderInt("##EntityCacheViewerWindowOpacity", ref windowSettings.EntityCacheViewer.Opacity, 20, 100, $"{windowSettings.EntityCacheViewer.Opacity}%%", ImGuiSliderFlags.ClampOnInput))
                            {
                                windowSettings.EntityCacheViewer.Opacity = windowSettings.EntityCacheViewer.Opacity;
                            }
                            ImGui.PopStyleColor(2);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("固定中のエンティティキャッシュ表示の透明度です。");
                            ImGui.EndDisabled();
                            ImGui.Unindent();

                            ImGui.SeparatorText("連携");

                            if (ImGui.CollapsingHeader("BPTimer##BPTimerOpacitySection", ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                ImGui.Indent();

                                ImGui.AlignTextToFramePadding();
                                ImGui.Text("スポーントラッカー: ");
                                ImGui.SetNextItemWidth(-1);
                                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                                if (ImGui.SliderInt("##BPTimerSpawnTrackerWindowOpacity", ref windowSettings.SpawnTracker.Opacity, 20, 100, $"{windowSettings.SpawnTracker.Opacity}%%"))
                                {
                                    windowSettings.SpawnTracker.Opacity = windowSettings.SpawnTracker.Opacity;
                                }
                                ImGui.PopStyleColor(2);
                                ImGui.Indent();
                                ImGui.BeginDisabled(true);
                                ImGui.TextWrapped("固定中のスポーントラッカーの透明度です。");
                                ImGui.EndDisabled();
                                ImGui.Unindent();

                                ImGui.Unindent();
                            }

                            ImGui.Unindent();
                        }

                        if (ImGui.CollapsingHeader("ウィンドウスケール"))
                        {
                            ImGui.Indent();

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("メーターバーのスケール: ");
                            ImGui.SetNextItemWidth(-1);
                            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                            if (ImGui.SliderFloat("##MeterBarScale", ref windowSettings.MainWindow.MeterBarScale, 0.80f, 2.0f, $"{(int)(windowSettings.MainWindow.MeterBarScale * 100)}%%"))
                            {
                                windowSettings.MainWindow.MeterBarScale = MathF.Round(windowSettings.MainWindow.MeterBarScale, 2);
                            }
                            ImGui.PopStyleColor(2);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("メーターのバーの大きさ（スケール）です。100%が標準です。");
                            ImGui.EndDisabled();
                            ImGui.Unindent();

                            ImGui.SeparatorText("連携");

                            if (ImGui.CollapsingHeader("BPTimer##BPTimerScaleSection", ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                ImGui.Indent();

                                ImGui.AlignTextToFramePadding();
                                ImGui.Text("スポーントラッカー文字スケール: ");
                                ImGui.SetNextItemWidth(-1);
                                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                                if (ImGui.SliderFloat("##BPTimerSpawnTrackerTextScale", ref windowSettings.SpawnTracker.TextScale, 0.80f, 3.0f, $"{(int)(windowSettings.SpawnTracker.TextScale * 100)}%%"))
                                {
                                    windowSettings.SpawnTracker.TextScale = MathF.Round(windowSettings.SpawnTracker.TextScale, 2);
                                }
                                ImGui.PopStyleColor(2);
                                ImGui.Indent();
                                ImGui.BeginDisabled(true);
                                ImGui.TextWrapped("スポーントラッカーの文字サイズ（スケール）です。100%が標準です。");
                                ImGui.EndDisabled();
                                ImGui.Unindent();

                                ImGui.AlignTextToFramePadding();
                                ImGui.Text("スポーントラッカーラインスケール: ");
                                ImGui.SetNextItemWidth(-1);
                                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                                if (ImGui.SliderFloat("##BPTimerSpawnTrackerLineScale", ref windowSettings.SpawnTracker.LineScale, 0.80f, 3.0f, $"{(int)(windowSettings.SpawnTracker.LineScale * 100)}%%"))
                                {
                                    windowSettings.SpawnTracker.LineScale = MathF.Round(windowSettings.SpawnTracker.LineScale, 2);
                                }
                                ImGui.PopStyleColor(2);
                                ImGui.Indent();
                                ImGui.BeginDisabled(true);
                                ImGui.TextWrapped("スポーントラッカーのライン（チャンネル）バーの大きさです。100%が標準です。");
                                ImGui.EndDisabled();
                                ImGui.Unindent();

                                ImGui.Unindent();
                            }

                            ImGui.Unindent();
                        }

                        if (ImGui.CollapsingHeader("メーター設定"))
                        {
                            ImGui.SeparatorText("タンク");

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("死亡数を表示: ");
                            ImGui.SameLine();
                            ImGui.Checkbox("##MeterSettingsTankingShowDeaths", ref meterSettingsTankingShowDeaths);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("有効にすると、タンクメーターの各項目に死亡数カウンターを表示します。");
                            ImGui.EndDisabled();
                            ImGui.Unindent();

                            ImGui.SeparatorText("NPC被ダメ");

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("HPデータを表示: ");
                            ImGui.SameLine();
                            ImGui.Checkbox("##MeterSettingsNpcTakenShowHpData", ref meterSettingsNpcTakenShowHpData);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("有効にすると、NPC被ダメメーターの各項目に現在HP/最大HP/HP%を追加表示します。");
                            ImGui.EndDisabled();
                            ImGui.Unindent();

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("最大HPを非表示: ");
                            ImGui.SameLine();
                            ImGui.Checkbox("##MeterSettingsNpcTakenHideMaxHp", ref meterSettingsNpcTakenHideMaxHp);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("有効にすると、最大HPの表示を消します。");
                            ImGui.EndDisabled();
                            ImGui.Unindent();

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("HP%バーを表示: ");
                            ImGui.SameLine();
                            ImGui.Checkbox("##MeterSettingsNpcTakenUseHpMeter", ref meterSettingsNpcTakenUseHpMeter);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("有効にすると、通常はNPCの総被ダメ量を示す青バーの代わりに、現在HP%を赤バーで表示します。");
                            ImGui.EndDisabled();
                            ImGui.Unindent();
                        }

                        ImGui.SeparatorText("ウィンドウ設定のリセット");

                        if (ImGui.Button("メインウィンドウ位置をリセット"))
                        {
                            var glfwMonitor = Hexa.NET.GLFW.GLFW.GetPrimaryMonitor();
                            var glfwVidMode = Hexa.NET.GLFW.GLFW.GetVideoMode(glfwMonitor);
                            mainWindow.NextWindowPosition = new Vector2(glfwVidMode.Width, glfwVidMode.Height);
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("メインウィンドウを初期のデフォルト位置（プライマリモニター中央）に戻します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        if (ImGui.Button("メインウィンドウサイズをリセット"))
                        {
                            mainWindow.NextWindowSize = mainWindow.DefaultWindowSize;
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("メインウィンドウを初期サイズに戻します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        if (ImGui.Button("レイドマネージャーCDトラッカーサイズをリセット"))
                        {
                            RaidManagerCooldownsWindow.ResetWindowSize = true;
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("レイドマネージャーCDトラッカーを初期サイズに戻します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        if (ImGui.Button("エンティティキャッシュ表示サイズをリセット"))
                        {
                            EntityCacheViewerWindow.ResetWindowSize = true;
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("エンティティキャッシュ表示を初期サイズに戻します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        if (ImGui.Button("BPTimerスポーントラッカーサイズをリセット"))
                        {
                            SpawnTrackerWindow.ResetWindowSize = true;
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("BPTimerスポーントラッカーを初期サイズに戻します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.SeparatorText("低負荷モード");

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("低負荷モード: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##LowPerformanceMode", ref lowPerformanceMode);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、ZDPSの更新レートを下げます。ウィンドウ移動時にUIがカクつく可能性があります。ZDPSのCPU使用率が非常に高い場合のみ有効にしてください。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.EndChild();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("マッチメイキング"))
                    {
                        var contentRegionAvail = ImGui.GetContentRegionAvail();
                        ImGui.BeginChild("##MatchmakingTabContent", new Vector2(contentRegionAvail.X, contentRegionAvail.Y - 56), ImGuiChildFlags.Borders);

                        ImGui.SeparatorText("マッチメイキング");
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("マッチ成立時に通知音を再生: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##PlayNotificationSoundOnMatchmake", ref playNotificationSoundOnMatchmake);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、マッチメイカーが成立して承認待ちになった時に通知音を再生します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.BeginDisabled(!playNotificationSoundOnMatchmake);
                        ImGui.Indent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("通知音ファイルパス: ");
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 140 - ImGui.GetStyle().ItemSpacing.X);
                        ImGui.InputText("##MatchmakeNotificationSoundPath", ref matchmakeNotificationSoundPath, 1024);
                        ImGui.SameLine();
                        if (ImGui.Button("Browse...##MatchmakeSoundPathBrowseBtn", new Vector2(140, 0)))
                        {
                            string defaultDir = File.Exists(matchmakeNotificationSoundPath) ? Path.GetDirectoryName(matchmakeNotificationSoundPath) : "";

                            ImFileBrowser.OpenFile((selectedFilePath) =>
                            {
                                System.Diagnostics.Debug.WriteLine($"MatchmakeNotificationSoundPath = {selectedFilePath}");
                                matchmakeNotificationSoundPath = selectedFilePath;
                            },
                            "音声ファイルを選択...", defaultDir, "MP3 (*.mp3)|*.mp3|WAV (*.wav)|*.wav|All Files (*.*)|*.*", 0);
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped(
                            "マッチング通知時に再生するカスタム音声ファイルのパスを指定します。\n" +
                            "未設定、またはファイルが無効な場合は、デフォルトの通知音が使用されます。\n" +
                            "※ 対応している形式は MP3 および WAV のみです。"
                        );
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("マッチ成立通知音をループ再生: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##loopNotificationSoundOnMatchmake", ref loopNotificationSoundOnMatchmake);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、承認するかキャンセルされるまで通知音をループ再生します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("通知音量: ");
                        ImGui.SetNextItemWidth(-1);
                        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                        if (ImGui.SliderFloat("##MatchmakeNotificationVolume", ref matchmakeNotificationVolume, 0.10f, 3.0f, $"{(int)(matchmakeNotificationVolume * 100)}%%"))
                        {
                            matchmakeNotificationVolume = MathF.Round(matchmakeNotificationVolume, 2);
                        }
                        ImGui.PopStyleColor(2);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("通知音の音量スケールです。100%が元の音量です。100%を超えても必ずしも大きくなるとは限りません。さらに大きくしたい場合は外部ツールで音量を上げてください。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.Unindent();
                        ImGui.EndDisabled();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("レディチェック時に通知音を再生: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##PlayNotificationSoundOnReadyCheck", ref playNotificationSoundOnReadyCheck);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、レディチェックが行われ承認待ちになった時に通知音を再生します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.BeginDisabled(!playNotificationSoundOnReadyCheck);
                        ImGui.Indent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("レディチェック通知音パス: ");
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 140 - ImGui.GetStyle().ItemSpacing.X);
                        ImGui.InputText("##ReadyCheckNotificationSoundPath", ref readyCheckNotificationSoundPath, 1024);
                        ImGui.SameLine();
                        if (ImGui.Button("Browse...##ReadyCheckSoundPathBrowseBtn", new Vector2(140, 0)))
                        {
                            string defaultDir = File.Exists(readyCheckNotificationSoundPath) ? Path.GetDirectoryName(readyCheckNotificationSoundPath) : "";

                            ImFileBrowser.OpenFile((selectedFilePath) =>
                            {
                                System.Diagnostics.Debug.WriteLine($"ReadyCheckNotificationSoundPath = {selectedFilePath}");
                                readyCheckNotificationSoundPath = selectedFilePath;
                            },
                            "音声ファイルを選択...", defaultDir, "MP3 (*.mp3)|*.mp3|WAV (*.wav)|*.wav|All Files (*.*)|*.*", 0);
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped(
                            "レディチェック通知時に再生するカスタム音声ファイルのパスを指定します。\n" +
                            "未設定、またはファイルが無効な場合は、デフォルトの通知音が使用されます。\n" +
                            "※ 対応している形式は MP3 および WAV のみです。"
                        );
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("レディチェック通知音をループ再生: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##loopNotificationSoundOnReadyCheck", ref loopNotificationSoundOnReadyCheck);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、レディチェックに応答するまで通知音をループ再生します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("レディチェック通知音量: ");
                        ImGui.SetNextItemWidth(-1);
                        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                        if (ImGui.SliderFloat("##ReadyCheckNotificationVolume", ref readyCheckNotificationVolume, 0.10f, 3.0f, $"{(int)(readyCheckNotificationVolume * 100)}%%"))
                        {
                            readyCheckNotificationVolume = MathF.Round(readyCheckNotificationVolume, 2);
                        }
                        ImGui.PopStyleColor(2);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("再生する通知音の音量スケールです。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.Unindent();
                        ImGui.EndDisabled();

                        ImGui.EndChild();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("連携"))
                    {
                        var contentRegionAvail = ImGui.GetContentRegionAvail();
                        ImGui.BeginChild("##IntegrationsTabContent", new Vector2(contentRegionAvail.X, contentRegionAvail.Y - 56), ImGuiChildFlags.Borders);

                        ImGui.SeparatorText("連携");

                        ShowGenericImportantNotice(!useAutomaticWipeDetection, "AutoWipeDetectionDisabled", "[Use Automatic Wipe Detection] is currently Disabled. Reports may be incorrect until it is Enabled again.");
                        ShowGenericImportantNotice(skipTeleportStateCheckInAutomaticWipeDetection, "SkipTeleportStateCheckInAutomaticWipeDetectionEnabled", "[Skip Teleport State Check In Automatic Wipe Detection] is currently Enabled. Reports may be incorrect until it is Disabled again.");
                        ShowGenericImportantNotice(!splitEncountersOnNewPhases, "SplitEncountersOnNewPhasesDisabled", "[Split Encounters On New Phases] is currently Disabled. Reports may be incorrect until it is Enabled again.");

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("エンカウントレポートをファイル保存: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##SaveEncounterReportToFile", ref saveEncounterReportToFile);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、ZDPSと同じ場所のReportsフォルダにレポートファイルを書き込みます。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.BeginDisabled(!saveEncounterReportToFile);
                        ImGui.Indent();
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("レポートファイル保持期間: ");
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                        ImGui.SetNextItemWidth(-1);
                        ImGui.SliderInt("##ReportFileRetentionPolicyDays", ref reportFileRetentionPolicyDays, 0, 30, reportFileRetentionPolicyDays == 0 ? "無期限" : $"{reportFileRetentionPolicyDays} Days");
                        ImGui.PopStyleColor(2);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("ローカル保存したレポートファイルを保持する期間です。「無期限」以外の場合、期限切れはアプリ終了時に自動削除されます。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();
                        ImGui.Unindent();
                        ImGui.EndDisabled();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("レポート作成に必要な最小プレイヤー数: ");
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered, 0.55f));
                        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive, 0.55f));
                        ImGui.SetNextItemWidth(-1);
                        ImGui.SliderInt("##MinimumPlayerCountToCreateReport", ref minimumPlayerCountToCreateReport, 0, 20, minimumPlayerCountToCreateReport == 0 ? "任意" : $"{minimumPlayerCountToCreateReport} Players");
                        ImGui.PopStyleColor(2);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("レポートを作成するために必要なエンカウント内プレイヤー数です。ローカル保存とWebhook送信の両方に適用されます。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("ダンジョン終了時に常にレポートを作成: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##AlwaysCreateReportAtDungeonEnd", ref alwaysCreateReportAtDungeonEnd);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped(
                            "有効にすると、ダンジョン終了時にレポートが未作成の場合でも自動的にレポートが作成されます。\n" +
                            "この設定が無効の場合、ボス戦で終了しなかったダンジョンではレポートが作成されないことがあります。"
                        );
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.SeparatorText("ZDPSレポートWebhook");

                        ImGui.AlignTextToFramePadding();
                        ImGui.TextUnformatted("Webhookモード: ");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(-1);

                        string reportsModeName = "";
                        switch (webhookReportsMode)
                        {
                            case EWebhookReportsMode.DiscordDeduplication:
                                reportsModeName = "Discord重複防止";
                                break;
                            case EWebhookReportsMode.Discord:
                                reportsModeName = "Discord Webhook";
                                break;
                            case EWebhookReportsMode.Custom:
                                reportsModeName = "カスタムURL";
                                break;
                            case EWebhookReportsMode.FallbackDiscordDeduplication:
                                reportsModeName = "代替Discord重複防止";
                                break;
                        }

                        if (ImGui.BeginCombo("##WebhookMode", $"{reportsModeName}", ImGuiComboFlags.None))
                        {
                            if (ImGui.Selectable("Discord重複防止"))
                            {
                                webhookReportsMode = EWebhookReportsMode.DiscordDeduplication;
                            }
                            ImGui.SetItemTooltip("外部サーバーで短時間内の重複送信を確認した後、Discord Webhookへ送信します。");
                            if (ImGui.Selectable("Discord Webhook"))
                            {
                                webhookReportsMode = EWebhookReportsMode.Discord;
                            }
                            ImGui.SetItemTooltip("Discord Webhookへ直接送信します。");
                            if (ImGui.Selectable("カスタムURL"))
                            {
                                webhookReportsMode = EWebhookReportsMode.Custom;
                            }
                            ImGui.SetItemTooltip("指定したカスタムURLへ直接送信します。");
                            if (ImGui.Selectable("代替Discord重複防止"))
                            {
                                webhookReportsMode = EWebhookReportsMode.FallbackDiscordDeduplication;
                            }
                            ImGui.SetItemTooltip("外部サーバーで短時間内の重複送信を確認した後、外部サーバーがDiscord Webhookへ転送します。");
                            ImGui.EndCombo();
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped(
                            "ZDPS レポート送信に使用する Webhook モードを選択します。\n" +
                            "複数のユーザーが同じエンカウントレポートを同じ Discord チャンネルへ送信する可能性がある場合は、重複防止のため「Discord Deduplication」の使用を推奨します。"
                        );
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        // TODO: Maybe allow adding multiple Webhooks and toggling the enabled state of each one (should allow entering a friendly name next to them too)

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text($"エンカウントレポートを {reportsModeName} に送信: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##WebhookReportsEnabled", ref webhookReportsEnabled);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped($"有効にすると、指定した {reportsModeName} へエンカウントレポートを送信します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.BeginDisabled(!webhookReportsEnabled);
                        ImGui.Indent();

                        switch (webhookReportsMode)
                        {
                            case EWebhookReportsMode.DiscordDeduplication:
                            case EWebhookReportsMode.Discord:
                            case EWebhookReportsMode.FallbackDiscordDeduplication:
                                if (webhookReportsMode == EWebhookReportsMode.DiscordDeduplication || webhookReportsMode == EWebhookReportsMode.FallbackDiscordDeduplication)
                                {
                                    ImGui.AlignTextToFramePadding();
                                    ImGui.Text("重複防止サーバーURL: ");
                                    ImGui.SameLine();
                                    ImGui.SetNextItemWidth(-1);
                                    ImGui.InputText("##WebhookReportsDeduplicationServerHost", ref webhookReportsDeduplicationServerUrl, 512);
                                    ImGui.Indent();
                                    ImGui.BeginDisabled(true);
                                    ImGui.TextWrapped("重複レポート送信を防ぐためのDiscord重複防止サーバーURLです。");
                                    if (webhookReportsMode == EWebhookReportsMode.FallbackDiscordDeduplication)
                                    {
                                        ImGui.TextWrapped("注意: 期待通り動作させるには、サーバー側でFallback対応を有効にする必要があります（Discordへの送信はサーバーが行います）。");
                                    }
                                    ImGui.EndDisabled();
                                    ImGui.Unindent();
                                }

                                ImGui.AlignTextToFramePadding();
                                ImGui.Text("Webhook URL: ");
                                ImGui.SameLine();
                                ImGui.SetNextItemWidth(-1);
                                if (ImGui.InputText("##WebhookReportsDiscordUrl", ref webhookReportsDiscordUrl, 512))
                                {
                                    if (Utils.SplitAndValidateDiscordWebhook(webhookReportsDiscordUrl) != null)
                                    {
                                        IsDiscordWebhookUrlValid = true;
                                    }
                                    else
                                    {
                                        IsDiscordWebhookUrlValid = false;
                                    }
                                }

                                if (!IsDiscordWebhookUrlValid)
                                {
                                    ImGui.Indent();
                                    ImGui.BeginDisabled(true);
                                    ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red);
                                    ImGui.TextWrapped("入力されたURLが正しくありません。");
                                    ImGui.PopStyleColor();
                                    ImGui.EndDisabled();
                                    ImGui.Unindent();
                                }

                                ImGui.Indent();
                                ImGui.BeginDisabled(true);
                                ImGui.TextWrapped("レポート送信先のDiscord Webhook URLです。");
                                ImGui.EndDisabled();
                                ImGui.Unindent();
                                break;
                            case EWebhookReportsMode.Custom:
                                ImGui.AlignTextToFramePadding();
                                ImGui.Text("Webhook URL: ");
                                ImGui.SameLine();
                                ImGui.SetNextItemWidth(-1);
                                ImGui.InputText("##WebhookReportsCustomUrl", ref webhookReportsCustomUrl, 512);
                                ImGui.Indent();
                                ImGui.BeginDisabled(true);
                                ImGui.TextWrapped("レポート送信先のカスタムURLです。");
                                ImGui.EndDisabled();
                                ImGui.Unindent();
                                break;
                        }

                        ImGui.Unindent();
                        ImGui.EndDisabled();

                        if (ImGui.CollapsingHeader("BPTimer", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("BPTimerを有効化: ");
                            ImGui.SameLine();
                            ImGui.Checkbox("##ExternalBPTimerEnabled", ref externalBPTimerEnabled);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("有効にすると、BPTimer.comへレポート送信できるようになります。");
                            bool hasBPTimerReports = externalBPTimerFieldBossHpReportsEnabled;
                            if (!hasBPTimerReports)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, Colors.Red);
                            }
                            else
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, Colors.Green);
                            }
                            ImGui.TextWrapped("注意: この設定だけではレポート送信は有効になりません。下の項目で個別に有効化してください。");
                            ImGui.PopStyleColor();

                            ImGui.EndDisabled();
                            if (ImGui.CollapsingHeader("Data Collection##BPTimerDataCollectionSection"))
                            {
                                ImGui.Indent();
                                ImGui.TextUnformatted("BPTimerは次のデータを収集します:");
                                ImGui.BulletText("ボスID / HP / 位置");
                                ImGui.BulletText("キャラクター行番号");
                                ImGui.BulletText("アカウントID");
                                ImGui.SetItemTooltip("プレイしているゲームリージョン判定に使用されます。");
                                ImGui.BulletText("キャラクターUID（下で同意した場合）");
                                ImGui.BulletText("あなたのIPアドレス");
                                ImGui.Unindent();
                            }
                            ImGui.Unindent();

                            ImGui.BeginDisabled(!externalBPTimerEnabled);
                            ImGui.Indent();

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("自分のキャラクターデータをレポートに含める: ");
                            ImGui.SameLine();
                            ImGui.Checkbox("##ExternalBPTimerIncludeCharacterId", ref externalBPTimerIncludeCharacterId);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("有効にすると、あなたのキャラクターUIDが送信データに含まれます。");
                            ImGui.EndDisabled();
                            ImGui.Unindent();

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("BPTimer フィールドボスHPレポート: ");
                            ImGui.SameLine();
                            ImGui.Checkbox("##ExternalBPTimerFieldBossHpReportsEnabled", ref externalBPTimerFieldBossHpReportsEnabled);
                            ImGui.Indent();
                            ImGui.BeginDisabled(true);
                            ImGui.TextWrapped("有効にすると、フィールドボス（および魔獣）のHPデータをBPTimer.comへ送信します。");
                            ImGui.EndDisabled();
                            ImGui.Unindent();

                            ImGui.Unindent();
                            ImGui.EndDisabled();
                        }

                        ImGui.EndChild();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("開発"))
                    {
                        var contentRegionAvail = ImGui.GetContentRegionAvail();
                        ImGui.BeginChild("##DevelopmentTabContent", new Vector2(contentRegionAvail.X, contentRegionAvail.Y - 56), ImGuiChildFlags.Borders);

                        ImGui.SeparatorText("開発");
                        if (ImGui.Button("データテーブル再読み込み"))
                        {
                            AppState.LoadDataTables();
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("既存の多くの値は更新されません（新規エンカウントで設定されるデータが主対象です）。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        if (ImGui.Button("キャプチャ再起動"))
                        {
                            MessageManager.StopCapturing();
                            MessageManager.InitializeCapturing();
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("データ取得が止まった問題を解決するため、MessageManagerを再起動します。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        if (ImGui.Button("モジュール保存を再読み込み"))
                        {
                            ModuleSolver.Init();
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("'ModulesSaveData.json' からモジュール所持状況を再読み込みします。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ShowRestartRequiredNotice(Settings.Instance.LogToFile != logToFile, "Write Debug Log To File");
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("デバッグログをファイル出力: ");
                        ImGui.SameLine();
                        ImGui.Checkbox("##LogToFile", ref logToFile);
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped("有効にすると、ZDPSのデバッグログ（ZDPS_log.txt）を出力します。ZDPS再起動後に適用されます。");
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        if (ImGui.Button("GitHubプロジェクトページを開く"))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                            {
                                FileName = Settings.Instance.ZDPSWebsiteURL,
                                UseShellExecute = true,
                            });
                        }
                        ImGui.Indent();
                        ImGui.BeginDisabled(true);
                        ImGui.TextWrapped(
                            $"以下の URL にある GitHub プロジェクトページを開きます。\n{Settings.Instance.ZDPSWebsiteURL}"
                        );
                        ImGui.EndDisabled();
                        ImGui.Unindent();

                        ImGui.EndChild();
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.NewLine();
                float buttonWidth = 120;
                if (ImGui.Button("保存", new Vector2(buttonWidth, 0)))
                {
                    Save(mainWindow);

                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - buttonWidth);
                if (ImGui.Button("閉じる", new Vector2(buttonWidth, 0)))
                {
                    SelectedNetworkDeviceIdx = PreviousSelectedNetworkDeviceIdx;

                    Load();

                    EncounterResetKey = Settings.Instance.HotkeysEncounterReset;
                    if (EncounterResetKey == 0)
                    {
                        EncounterResetKeyName = "[UNBOUND]";
                    }
                    else
                    {
                        EncounterResetKeyName = ImGui.GetKeyNameS(HotKeyManager.VirtualKeyToImGuiKey((int)EncounterResetKey));
                    }

                    PinnedWindowClickthroughKey = Settings.Instance.HotkeysPinnedWindowClickthrough;
                    if (PinnedWindowClickthroughKey == 0)
                    {
                        PinnedWindowClickthroughKeyName = "[UNBOUND]";
                    }
                    else
                    {
                        PinnedWindowClickthroughKeyName = ImGui.GetKeyNameS(HotKeyManager.VirtualKeyToImGuiKey((int)PinnedWindowClickthroughKey));
                    }

                    RegisterAllHotkeys(mainWindow);

                    ImGui.CloseCurrentPopup();
                }

                ImFileBrowser.Draw();

                ImGui.EndPopup();
            }

            ImGui.PopID();
        }

        private static void Load()
        {
            normalizeMeterContributions = Settings.Instance.NormalizeMeterContributions;
            useShortWidthNumberFormatting = Settings.Instance.UseShortWidthNumberFormatting;
            showClassIconsInMeters = Settings.Instance.ShowClassIconsInMeters;
            colorClassIconsByRole = Settings.Instance.ColorClassIconsByRole;
            showSkillIconsInDetails = Settings.Instance.ShowSkillIconsInDetails;
            onlyShowDamageContributorsInMeters = Settings.Instance.OnlyShowDamageContributorsInMeters;
            onlyShowPartyMembersInMeters = Settings.Instance.OnlyShowPartyMembersInMeters;
            showAbilityScoreInMeters = Settings.Instance.ShowAbilityScoreInMeters;
            showSeasonStrengthInMeters = Settings.Instance.ShowSeasonStrengthInMeters;
            showSubProfessionNameInMeters = Settings.Instance.ShowSubProfessionNameInMeters;
            useAutomaticWipeDetection = Settings.Instance.UseAutomaticWipeDetection;
            skipTeleportStateCheckInAutomaticWipeDetection = Settings.Instance.SkipTeleportStateCheckInAutomaticWipeDetection;
            disableWipeRecalculationOverwriting = Settings.Instance.DisableWipeRecalculationOverwriting;
            splitEncountersOnNewPhases = Settings.Instance.SplitEncountersOnNewPhases;
            displayTruePerSecondValuesInMeters = Settings.Instance.DisplayTruePerSecondValuesInMeters;
            allowGamepadNavigationInputInZDPS = Settings.Instance.AllowGamepadNavigationInputInZDPS;
            keepPastEncounterInMeterUntilNextDamage = Settings.Instance.KeepPastEncounterInMeterUntilNextDamage;

            useDatabaseForEncounterHistory = Settings.Instance.UseDatabaseForEncounterHistory;
            databaseRetentionPolicyDays = Settings.Instance.DatabaseRetentionPolicyDays;
            limitEncounterBuffTrackingWithoutDatabase = Settings.Instance.LimitEncounterBuffTrackingWithoutDatabase;
            allowEncounterSavingPausingInOpenWorld = Settings.Instance.AllowEncounterSavingPausingInOpenWorld;

            meterSettingsTankingShowDeaths = Settings.Instance.MeterSettingsTankingShowDeaths;
            meterSettingsNpcTakenShowHpData = Settings.Instance.MeterSettingsNpcTakenShowHpData;
            meterSettingsNpcTakenHideMaxHp = Settings.Instance.MeterSettingsNpcTakenHideMaxHp;
            meterSettingsNpcTakenUseHpMeter = Settings.Instance.MeterSettingsNpcTakenUseHpMeter;

            GameCapturePreference = Settings.Instance.GameCapturePreference;
            gameCaptureCustomExeName = Settings.Instance.GameCaptureCustomExeName;

            playNotificationSoundOnMatchmake = Settings.Instance.PlayNotificationSoundOnMatchmake;
            matchmakeNotificationSoundPath = Settings.Instance.MatchmakeNotificationSoundPath;
            loopNotificationSoundOnMatchmake = Settings.Instance.LoopNotificationSoundOnMatchmake;
            matchmakeNotificationVolume = Settings.Instance.MatchmakeNotificationVolume;

            playNotificationSoundOnReadyCheck = Settings.Instance.PlayNotificationSoundOnReadyCheck;
            readyCheckNotificationSoundPath = Settings.Instance.ReadyCheckNotificationSoundPath;
            loopNotificationSoundOnReadyCheck = Settings.Instance.LoopNotificationSoundOnReadyCheck;
            readyCheckNotificationVolume = Settings.Instance.ReadyCheckNotificationVolume;

            saveEncounterReportToFile = Settings.Instance.SaveEncounterReportToFile;
            reportFileRetentionPolicyDays = Settings.Instance.ReportFileRetentionPolicyDays;
            minimumPlayerCountToCreateReport = Settings.Instance.MinimumPlayerCountToCreateReport;
            alwaysCreateReportAtDungeonEnd = Settings.Instance.AlwaysCreateReportAtDungeonEnd;
            webhookReportsEnabled = Settings.Instance.WebhookReportsEnabled;
            webhookReportsMode = Settings.Instance.WebhookReportsMode;
            webhookReportsDeduplicationServerUrl = Settings.Instance.WebhookReportsDeduplicationServerHost;
            webhookReportsDiscordUrl = Settings.Instance.WebhookReportsDiscordUrl;
            webhookReportsCustomUrl = Settings.Instance.WebhookReportsCustomUrl;

            checkForZDPSUpdatesOnStartup = Settings.Instance.CheckForZDPSUpdatesOnStartup;
            latestZDPSVersionCheckURL = Settings.Instance.LatestZDPSVersionCheckURL;

            windowSettings = (WindowSettings)Settings.Instance.WindowSettings.Clone();

            logToFile = Settings.Instance.LogToFile;

            lowPerformanceMode = Settings.Instance.LowPerformanceMode;

            // External
            externalBPTimerEnabled = Settings.Instance.External.BPTimerSettings.ExternalBPTimerEnabled;
            externalBPTimerIncludeCharacterId = Settings.Instance.External.BPTimerSettings.ExternalBPTimerIncludeCharacterId;
            externalBPTimerFieldBossHpReportsEnabled = Settings.Instance.External.BPTimerSettings.ExternalBPTimerFieldBossHpReportsEnabled;
        }

        private static void Save(MainWindow mainWindow)
        {
            var io = ImGui.GetIO();
            if (SelectedNetworkDeviceIdx != PreviousSelectedNetworkDeviceIdx || GameCapturePreference != Settings.Instance.GameCapturePreference)
            {
                PreviousSelectedNetworkDeviceIdx = SelectedNetworkDeviceIdx;

                MessageManager.StopCapturing();

                Settings.Instance.NetCaptureDeviceName = NetworkDevices[SelectedNetworkDeviceIdx].Name;
                MessageManager.NetCaptureDeviceName = NetworkDevices[SelectedNetworkDeviceIdx].Name;

                Settings.Instance.GameCapturePreference = GameCapturePreference;
                Settings.Instance.GameCaptureCustomExeName = gameCaptureCustomExeName;

                MessageManager.InitializeCapturing();
            }
            if (allowGamepadNavigationInputInZDPS)
            {
                io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;
            }
            else
            {
                io.ConfigFlags &= ~ImGuiConfigFlags.NavEnableGamepad;
            }

            Settings.Instance.AllowEncounterSavingPausingInOpenWorld = allowEncounterSavingPausingInOpenWorld;
            if (!allowEncounterSavingPausingInOpenWorld)
            {
                AppState.IsEncounterSavingPaused = false;
            }

            Settings.Instance.NormalizeMeterContributions = normalizeMeterContributions;
            Settings.Instance.UseShortWidthNumberFormatting = useShortWidthNumberFormatting;
            Settings.Instance.ShowClassIconsInMeters = showClassIconsInMeters;
            Settings.Instance.ColorClassIconsByRole = colorClassIconsByRole;
            Settings.Instance.ShowSkillIconsInDetails = showSkillIconsInDetails;
            Settings.Instance.OnlyShowDamageContributorsInMeters = onlyShowDamageContributorsInMeters;
            Settings.Instance.OnlyShowPartyMembersInMeters = onlyShowPartyMembersInMeters;
            Settings.Instance.ShowAbilityScoreInMeters = showAbilityScoreInMeters;
            Settings.Instance.ShowSeasonStrengthInMeters = showSeasonStrengthInMeters;
            Settings.Instance.ShowSubProfessionNameInMeters = showSubProfessionNameInMeters;
            Settings.Instance.UseAutomaticWipeDetection = useAutomaticWipeDetection;
            Settings.Instance.SkipTeleportStateCheckInAutomaticWipeDetection = skipTeleportStateCheckInAutomaticWipeDetection;
            Settings.Instance.DisableWipeRecalculationOverwriting = disableWipeRecalculationOverwriting;
            Settings.Instance.SplitEncountersOnNewPhases = splitEncountersOnNewPhases;
            Settings.Instance.DisplayTruePerSecondValuesInMeters = displayTruePerSecondValuesInMeters;
            Settings.Instance.AllowGamepadNavigationInputInZDPS = allowGamepadNavigationInputInZDPS;
            Settings.Instance.KeepPastEncounterInMeterUntilNextDamage = keepPastEncounterInMeterUntilNextDamage;

            Settings.Instance.UseDatabaseForEncounterHistory = useDatabaseForEncounterHistory;
            Settings.Instance.DatabaseRetentionPolicyDays = databaseRetentionPolicyDays;
            Settings.Instance.LimitEncounterBuffTrackingWithoutDatabase = limitEncounterBuffTrackingWithoutDatabase;

            Settings.Instance.MeterSettingsTankingShowDeaths = meterSettingsTankingShowDeaths;
            Settings.Instance.MeterSettingsNpcTakenShowHpData = meterSettingsNpcTakenShowHpData;
            Settings.Instance.MeterSettingsNpcTakenHideMaxHp = meterSettingsNpcTakenHideMaxHp;
            Settings.Instance.MeterSettingsNpcTakenUseHpMeter = meterSettingsNpcTakenUseHpMeter;

            Settings.Instance.PlayNotificationSoundOnMatchmake = playNotificationSoundOnMatchmake;
            Settings.Instance.MatchmakeNotificationSoundPath = matchmakeNotificationSoundPath;
            Settings.Instance.LoopNotificationSoundOnMatchmake = loopNotificationSoundOnMatchmake;
            Settings.Instance.MatchmakeNotificationVolume = matchmakeNotificationVolume;

            Settings.Instance.PlayNotificationSoundOnReadyCheck = playNotificationSoundOnReadyCheck;
            Settings.Instance.ReadyCheckNotificationSoundPath = readyCheckNotificationSoundPath;
            Settings.Instance.LoopNotificationSoundOnReadyCheck = loopNotificationSoundOnReadyCheck;
            Settings.Instance.ReadyCheckNotificationVolume = readyCheckNotificationVolume;

            Settings.Instance.SaveEncounterReportToFile = saveEncounterReportToFile;
            Settings.Instance.ReportFileRetentionPolicyDays = reportFileRetentionPolicyDays;
            Settings.Instance.MinimumPlayerCountToCreateReport = minimumPlayerCountToCreateReport;
            Settings.Instance.AlwaysCreateReportAtDungeonEnd = alwaysCreateReportAtDungeonEnd;
            Settings.Instance.WebhookReportsEnabled = webhookReportsEnabled;
            Settings.Instance.WebhookReportsMode = webhookReportsMode;
            Settings.Instance.WebhookReportsDeduplicationServerHost = webhookReportsDeduplicationServerUrl;
            Settings.Instance.WebhookReportsDiscordUrl = webhookReportsDiscordUrl;
            Settings.Instance.WebhookReportsCustomUrl = webhookReportsCustomUrl;

            Settings.Instance.CheckForZDPSUpdatesOnStartup = checkForZDPSUpdatesOnStartup;
            Settings.Instance.LatestZDPSVersionCheckURL = latestZDPSVersionCheckURL;

            Settings.Instance.WindowSettings = (WindowSettings)windowSettings.Clone();

            Settings.Instance.LogToFile = logToFile;

            Settings.Instance.LowPerformanceMode = lowPerformanceMode;

            // External
            Settings.Instance.External.BPTimerSettings.ExternalBPTimerEnabled = externalBPTimerEnabled;
            Settings.Instance.External.BPTimerSettings.ExternalBPTimerIncludeCharacterId = externalBPTimerIncludeCharacterId;
            Settings.Instance.External.BPTimerSettings.ExternalBPTimerFieldBossHpReportsEnabled = externalBPTimerFieldBossHpReportsEnabled;

            RegisterAllHotkeys(mainWindow);

            DB.Init();

            // Write out the new settings to file now that they've been applied
            Settings.Save();

            if (externalBPTimerEnabled && externalBPTimerFieldBossHpReportsEnabled)
            {
                // Attempt to update our supported mob list with data from the BPTimer server
                Managers.External.BPTimerManager.FetchSupportedMobList();
            }
        }

        static void ShowRestartRequiredNotice(bool showCondition, string settingName)
        {
            if (showCondition)
            {
                ImGui.PushStyleColor(ImGuiCol.ChildBg, Colors.Red_Transparent);
                ImGui.BeginChild($"##RestartRequiredNotice_{settingName}", new Vector2(0, 0), ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.Borders);
                ImGui.PushFont(HelperMethods.Fonts["Segoe-Bold"], ImGui.GetFontSize());
                ImGui.TextUnformatted("重要なお知らせ:");
                ImGui.PopFont();
                ImGui.TextWrapped($"[{settingName}] の変更を反映するにはZDPSの再起動が必要です。");
                ImGui.EndChild();
                ImGui.PopStyleColor();
            }
        }

        static void ShowGenericImportantNotice(bool showCondition, string uniqueName, string text)
        {
            if (showCondition)
            {
                ImGui.PushStyleColor(ImGuiCol.ChildBg, Colors.Red_Transparent);
                ImGui.BeginChild($"##GenericImportantNotice_{uniqueName}", new Vector2(0, 0), ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.Borders);
                ImGui.PushFont(HelperMethods.Fonts["Segoe-Bold"], ImGui.GetFontSize());
                ImGui.TextUnformatted("重要なお知らせ:");
                ImGui.PopFont();
                ImGui.TextWrapped($"{text}");
                ImGui.EndChild();
                ImGui.PopStyleColor();
            }
        }

        static void LoadHotkeys()
        {
            EncounterResetKey = Settings.Instance.HotkeysEncounterReset;
            if (EncounterResetKey == 0)
            {
                EncounterResetKeyName = "[UNBOUND]";
            }
            else
            {
                EncounterResetKeyName = ImGui.GetKeyNameS(HotKeyManager.VirtualKeyToImGuiKey((int)EncounterResetKey));
            }

            PinnedWindowClickthroughKey = Settings.Instance.HotkeysPinnedWindowClickthrough;
            if (PinnedWindowClickthroughKey == 0)
            {
                PinnedWindowClickthroughKeyName = "[UNBOUND]";
            }
            else
            {
                PinnedWindowClickthroughKeyName = ImGui.GetKeyNameS(HotKeyManager.VirtualKeyToImGuiKey((int)PinnedWindowClickthroughKey));
            }
        }

        static void RegisterAllHotkeys(MainWindow mainWindow)
        {
            if (EncounterResetKey != 0)// && EncounterResetKey != Settings.Instance.HotkeysEncounterReset)
            {
                HotKeyManager.RegisterKey("EncounterReset", mainWindow.CreateNewEncounter, EncounterResetKey);
            }
            Settings.Instance.HotkeysEncounterReset = EncounterResetKey;

            if (PinnedWindowClickthroughKey != 0)
            {
                HotKeyManager.RegisterKey("PinnedWindowClickthrough", mainWindow.ToggleMouseClickthrough, PinnedWindowClickthroughKey);
            }
            Settings.Instance.HotkeysPinnedWindowClickthrough = PinnedWindowClickthroughKey;
        }

        public static void RebindKeyButton(string bindingName, ref uint bindingVariable, ref string bindingVariableName, ref bool bindingState)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"{bindingName}:");

            string bindDisplay = "[UNBOUND]";

            if (bindingState == true)
            {
                for (uint key = (uint)ImGuiKey.NamedKeyBegin; key < (uint)ImGuiKey.NamedKeyEnd; key++)
                {
                    if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                    {
                        bindingState = false;
                    }
                    else if (ImGui.IsKeyPressed((ImGuiKey)key))
                    {
                        ImGuiKey[] blacklistedKeys =
                            [
                            ImGuiKey.ModAlt, ImGuiKey.LeftAlt, ImGuiKey.RightAlt, ImGuiKey.ReservedForModAlt,
                            ImGuiKey.ModCtrl, ImGuiKey.LeftCtrl, ImGuiKey.RightCtrl, ImGuiKey.ReservedForModCtrl,
                            ImGuiKey.ModShift, ImGuiKey.LeftShift, ImGuiKey.RightShift, ImGuiKey.ReservedForModShift,
                            ImGuiKey.ModMask, ImGuiKey.ModSuper, ImGuiKey.LeftSuper, ImGuiKey.RightSuper, ImGuiKey.ReservedForModSuper,
                            ImGuiKey.MouseLeft, ImGuiKey.MouseMiddle, ImGuiKey.MouseRight, ImGuiKey.MouseWheelX, ImGuiKey.MouseWheelY,
                            ImGuiKey.Escape, ImGuiKey.F12
                            ];

                        if (!blacklistedKeys.Contains((ImGuiKey)key))
                        {
                            string keyName = ImGui.GetKeyNameS((ImGuiKey)key);
                            bindingVariable = (uint)HotKeyManager.ImGuiKeyToVirtualKey((ImGuiKey)key);
                            bindingVariableName = keyName;
                            bindingState = false;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(bindingVariableName))
            {
                bindDisplay = bindingVariableName;
            }
            ImGui.SameLine();
            bool isInBindingState = bindingState;

            if (isInBindingState)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered]);
            }
            if (ImGui.Button($"{bindDisplay}##BindBtn_{bindingName}", new Vector2(120, 0)))
            {
                bindingState = true;
            }
            if (isInBindingState)
            {
                ImGui.PopStyleColor();
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(bindingVariable == 0);
            if (ImGui.Button($"X##ClearBindingBtn_{bindingName}"))
            {
                bindingVariable = 0;
                bindingVariableName = "";
                bindingState = false;
            }
            ImGui.EndDisabled();
            ImGui.SetItemTooltip("キー割り当てを解除");
        }
    }
}
