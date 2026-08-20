#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Kingfisher.KClipboard
{
    public class KClipboardWindow : EditorWindow
    {
        #region Field

        private const string MenuPath = "Tools/Kingfisher/K-Clipboard/History";
        private const string WindowTitle = "K-Clipboard History";
        private const int MenuPriority = 912;

        private const string ClearHistoryTitle = "Clear K-Clipboard history?";
        private const string ClearHistoryBody = "This permanently removes every copied component from the history stack. It cannot be undone.";
        private const string ClearConfirmLabel = "Clear";
        private const string CancelLabel = "Cancel";
        private const string OkLabel = "OK";

        private const string EmptyHistoryLabel = "Nothing copied yet. Right-click a component and choose \"Copy to K-Clipboard History\".";
        private const string PasteButtonLabel = "Paste to Selected";
        private const string ClearHistoryButtonLabel = "Clear history";

        private const string JustNowLabel = "just now";
        private const string MinutesAgoFormat = "{0}m ago";
        private const string HoursAgoFormat = "{0}h ago";
        private const string DaysAgoFormat = "{0}d ago";

        private const float PasteButtonWidth = 130f;
        private const float MinWindowWidth = 360f;
        private const float MinWindowHeight = 240f;

        private Vector2 _scroll;

        #endregion

        #region Unity Lifecycle

        private void OnEnable() => KClipboard.EnsureData();

        private void OnGUI()
        {
            DrawToolbar();

            var entries = KClipboard.EnsureData().entries;

            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox(EmptyHistoryLabel, MessageType.Info);

                return;
            }

            this._scroll = EditorGUILayout.BeginScrollView(this._scroll);

            for (var i = 0; i < entries.Count; i++)
                DrawRow(entries[i]);

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Drawing

        private static void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(KClipboard.EnsureData().entries.Count == 0);

            if (GUILayout.Button(ClearHistoryButtonLabel, EditorStyles.toolbarButton))
                ConfirmClearHistory();

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawRow(KClipboardData.HistoryEntry entry)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(entry.componentTypeLabel, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(GetRelativeLabel(entry.timestampTicks), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(Selection.gameObjects.Length == 0);

            if (GUILayout.Button(PasteButtonLabel, GUILayout.Width(PasteButtonWidth)))
                PasteEntry(entry);

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Actions

        private static void PasteEntry(KClipboardData.HistoryEntry entry)
        {
            KClipboard.TryPasteToSelected(entry, out var message);

            EditorUtility.DisplayDialog(WindowTitle, message, OkLabel);
        }

        private static void ConfirmClearHistory()
        {
            if (!EditorUtility.DisplayDialog(ClearHistoryTitle, ClearHistoryBody, ClearConfirmLabel, CancelLabel)) return;

            KClipboard.ClearHistory();
        }

        private static string GetRelativeLabel(long timestampTicks)
        {
            var elapsed = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - timestampTicks);

            if (elapsed.TotalSeconds < 60) return JustNowLabel;
            if (elapsed.TotalMinutes < 60) return string.Format(MinutesAgoFormat, (int)elapsed.TotalMinutes);
            if (elapsed.TotalHours < 24) return string.Format(HoursAgoFormat, (int)elapsed.TotalHours);

            return string.Format(DaysAgoFormat, (int)elapsed.TotalDays);
        }

        #endregion

        #region Method

        [MenuItem(MenuPath, false, MenuPriority)]
        public static void Open()
        {
            var window = GetWindow<KClipboardWindow>(utility: false, title: WindowTitle, focus: true);

            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
        }

        #endregion
    }
}
#endif
