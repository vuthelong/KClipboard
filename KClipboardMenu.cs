#if UNITY_EDITOR
using UnityEngine;
using static Kingfisher.KClipboard.Libs.KUtils;

namespace Kingfisher.KClipboard
{
    public class KClipboardMenu
    {
        #region Field

        private const string KeyPrefix = "KClipboard-kingfisher-";

        private const string MaxComponentHistoryCountKey = KeyPrefix + "maxComponentHistoryCount";
        private const string MaxGameObjectHistoryCountKey = KeyPrefix + "maxGameObjectHistoryCount";
        private const string PluginDisabledKey = KeyPrefix + "pluginDisabled";

        private const int DefaultMaxHistoryCount = 20;
        private const int MinHistoryCount = 1;
        private const int MaxHistoryCountLimit = 200;

        public static readonly string[] SettingsLayout =
        {
            "# Component History",
            "~MaxComponentHistoryCount|Max history entries|" + MinHistoryCount + "|" + MaxHistoryCountLimit,
            "# GameObject History",
            "~MaxGameObjectHistoryCount|Max history entries|" + MinHistoryCount + "|" + MaxHistoryCountLimit,
        };

        #endregion

        #region Property

        public static float MaxComponentHistoryCount
        {
            get => EditorPrefsCached.GetInt(MaxComponentHistoryCountKey, DefaultMaxHistoryCount);
            set => EditorPrefsCached.SetInt(MaxComponentHistoryCountKey, Mathf.RoundToInt(value).Clamp(MinHistoryCount, MaxHistoryCountLimit));
        }

        public static float MaxGameObjectHistoryCount
        {
            get => EditorPrefsCached.GetInt(MaxGameObjectHistoryCountKey, DefaultMaxHistoryCount);
            set => EditorPrefsCached.SetInt(MaxGameObjectHistoryCountKey, Mathf.RoundToInt(value).Clamp(MinHistoryCount, MaxHistoryCountLimit));
        }

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

        public static void DeleteData()
        {
            KClipboardComponents.DeleteData();
            KClipboardGameObjects.DeleteData();
        }

        public static void OpenTool() => KClipboardComponentsWindow.Open();

        #endregion
    }
}
#endif
