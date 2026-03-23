using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.Managers;
using Hexa.NET.ImGui;
using System.Numerics;

namespace BPSR_ZDPS.Windows
{
    public class CharacterStatusWindow
    {
        public const string LAYER = "CharacterStatusWindowLayer";
        public static string TITLE_ID = "###CharacterStatusWindow";
        public static bool IsOpened = false;

        public static Vector2 DefaultWindowSize = new(420, 320);
        public static bool ResetWindowSize = false;

        static ImGuiWindowClassPtr WindowClass = ImGui.ImGuiWindowClass();

        public static void Open()
        {
            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.OpenPopup(TITLE_ID);
            IsOpened = true;

            WindowClass.ClassId = ImGuiP.ImHashStr("CharacterStatusWindowClass");
            WindowClass.ViewportFlagsOverrideSet = ImGuiViewportFlags.None;

            ImGui.PopID();
        }

        public static void Draw(MainWindow mainWindow)
        {
            if (!IsOpened) return;

            var windowSettings = Settings.Instance.WindowSettings.CharacterStatusWindow;
            PreDraw(windowSettings);
            InnerDraw(windowSettings);
            ImGui.PopID();
        }

        private static void PreDraw(CharacterStatusWindowSettings windowSettings)
        {
            ImGui.SetNextWindowSize(DefaultWindowSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(300, 220), new Vector2(ImGui.GETFLTMAX()));

            if (windowSettings.WindowPosition != new Vector2())
                ImGui.SetNextWindowPos(windowSettings.WindowPosition, ImGuiCond.FirstUseEver);

            if (windowSettings.WindowSize != new Vector2())
                ImGui.SetNextWindowSize(windowSettings.WindowSize, ImGuiCond.FirstUseEver);

            if (ResetWindowSize)
            {
                ImGui.SetNextWindowSize(DefaultWindowSize, ImGuiCond.Always);
                ResetWindowSize = false;
            }

            ImGuiP.PushOverrideID(ImGuiP.ImHashStr(LAYER));
            ImGui.SetNextWindowClass(WindowClass);
        }

        private static void InnerDraw(CharacterStatusWindowSettings windowSettings)
        {
            // タイトルは後でAppStrings化してOK。まずは固定文字で土台を出す。
            bool opened = IsOpened;

            if (ImGui.Begin("Character Status", ref opened,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
            {
                // ここに後でステータス描画を足していく
                ImGui.TextUnformatted("Coming soon...");

                // 設定保存（位置/サイズ）
                windowSettings.WindowPosition = ImGui.GetWindowPos();
                windowSettings.WindowSize = ImGui.GetWindowSize();
            }

            ImGui.End();

            // 閉じた状態を反映
            if (!opened)
            {
                IsOpened = false;
            }
        }
    }

    public class CharacterStatusWindowSettings : WindowSettingsBase
    {
        // 将来的に表示切り替え項目が増えたらここへ
        public bool ShowBasic { get; set; } = true;
    }
}
