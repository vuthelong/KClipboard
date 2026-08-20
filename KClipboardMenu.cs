#if UNITY_EDITOR
using UnityEngine;
using static Kingfisher.KClipboard.Libs.KUtils;

namespace Kingfisher.KClipboard
{
    public class KClipboardMenu
    {
        #region Field

        private const string KeyPrefix = "KClipboard-kingfisher-";

        private const string MaxHistoryCountKey = KeyPrefix + "maxHistoryCount";
        private const string PluginDisabledKey = KeyPrefix + "pluginDisabled";
        private const string DebugLoggingKey = KeyPrefix + "debugLoggingEnabled";

        private const int DefaultMaxHistoryCount = 20;
        private const int MinHistoryCount = 1;
        private const int MaxHistoryCountLimit = 200;

        public static readonly string[] SettingsLayout =
        {
            "# History",
            "~MaxHistoryCount|Max history entries|" + MinHistoryCount + "|" + MaxHistoryCountLimit,

            "# Debug",
            "DebugLoggingEnabled|Enable debug logging",
        };

        #endregion

        #region Property

        public static float MaxHistoryCount
        {
            get => EditorPrefsCached.GetInt(MaxHistoryCountKey, DefaultMaxHistoryCount);
            set => EditorPrefsCached.SetInt(MaxHistoryCountKey, Mathf.RoundToInt(value).Clamp(MinHistoryCount, MaxHistoryCountLimit));
        }

        public static bool DebugLoggingEnabled { get => EditorPrefsCached.GetBool(DebugLoggingKey, false); set => EditorPrefsCached.SetBool(DebugLoggingKey, value); }

        public static bool PluginDisabled
        {
            get => EditorPrefsCached.GetBool(PluginDisabledKey, false);
            set
            {
                EditorPrefsCached.SetBool(PluginDisabledKey, value);

                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            }
        }

        #endregion

        #region Method

        public static void DeleteData() => KClipboard.DeleteData();

        public static void OpenTool() => KClipboardWindow.Open();

        #endregion
    }
}
#endif
