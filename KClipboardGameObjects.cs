#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kingfisher.KClipboard
{
    public static class KClipboardGameObjects
    {
        #region Field

        public const string DataFileName = "KClipboard GameObjects Data.asset";

        public const string PrefabIconName = "Prefab Icon";

        private const string CopyMenuPath = "GameObject/Copy to K-Clipboard History";
        private const int CopyMenuPriority = 20;

        private const string EmptySelectionToCopyMessage = "Select at least one GameObject to copy.";
        private const string CopyProducedNoDataMessage = "Unity's GameObject copy produced no data to store.";
        private const string CopiedFormat = "Copied {0} GameObject(s) to K-Clipboard History.";

        private const string EmptyPasteboardMessage = "This entry has no stored data.";
        private const string PastedMessage = "Pasted GameObject(s).";

        private const string PasteFailureLogFormat = "K-Clipboard: {0}";

        public static KClipboardGameObjectsData Data;

        #endregion

        #region Menu

        [MenuItem(CopyMenuPath, false, CopyMenuPriority)]
        private static void CopyToHistory()
        {
            if (KClipboardMenu.PluginDisabled) return;

            if (!CopySelectionToHistory(Selection.gameObjects, out var message))
                Debug.LogError(string.Format(PasteFailureLogFormat, message));
        }

        [MenuItem(CopyMenuPath, true)]
        private static bool ValidateCopyToHistory() => !KClipboardMenu.PluginDisabled && Selection.gameObjects.Length > 0;

        #endregion

        #region Copy

        public static bool CopySelectionToHistory(GameObject[] gameObjects, out string message)
        {
            if (gameObjects == null || gameObjects.Length == 0)
            {
                message = EmptySelectionToCopyMessage;

                return false;
            }

            var previousSelection = Selection.objects;
            var previousClipboard = EditorGUIUtility.systemCopyBuffer;

            Selection.objects = gameObjects;

            Unsupported.CopyGameObjectsToPasteboard();

            var blob = EditorGUIUtility.systemCopyBuffer;

            EditorGUIUtility.systemCopyBuffer = previousClipboard;
            Selection.objects = previousSelection;

            if (string.IsNullOrEmpty(blob))
            {
                message = CopyProducedNoDataMessage;

                return false;
            }

            PushToHistory(gameObjects, blob);

            message = string.Format(CopiedFormat, gameObjects.Length);

            return true;
        }

        private static void PushToHistory(GameObject[] gameObjects, string blob)
        {
            var rootNames = new string[gameObjects.Length];

            for (var i = 0; i < gameObjects.Length; i++)
                rootNames[i] = gameObjects[i].name;

            EnsureData().Push(new KClipboardGameObjectsData.HistoryEntry
            {
                rootNames = rootNames,
                iconName = PrefabIconName,
                pasteboardBlob = blob,
            }, Mathf.RoundToInt(KClipboardMenu.MaxGameObjectHistoryCount));

            Libs.KData.Flush();
        }

        #endregion

        #region Paste

        // Placement (child of the current selection vs. scene root, and whether the original
        // local transform is preserved) is entirely Unity's own native Hierarchy-paste behavior -
        // Selection is deliberately left untouched here so a triggered paste behaves exactly like
        // a manual Ctrl+V against whatever the user currently has selected.
        //
        // Selection.gameObjects does not update synchronously within this call, so there is no
        // reliable public-API signal to confirm the paste actually created anything - this call
        // is treated as a fire-and-forget command, same as native Ctrl+V has no return value either.
        public static bool TryPaste(KClipboardGameObjectsData.HistoryEntry entry, out string message)
        {
            if (entry == null || string.IsNullOrEmpty(entry.pasteboardBlob))
            {
                message = EmptyPasteboardMessage;

                return false;
            }

            var previousClipboard = EditorGUIUtility.systemCopyBuffer;

            EditorGUIUtility.systemCopyBuffer = entry.pasteboardBlob;

            Unsupported.PasteGameObjectsFromPasteboard();

            EditorGUIUtility.systemCopyBuffer = previousClipboard;

            message = PastedMessage;

            return true;
        }

        #endregion

        #region Method

        public static KClipboardGameObjectsData EnsureData()
        {
            if (Data) return Data;

            Data = Libs.KData.Load<KClipboardGameObjectsData>(DataFileName) ?? Libs.KData.Create<KClipboardGameObjectsData>(DataFileName);

            Libs.KData.Autosave(Data, DataFileName);

            return Data;
        }

        public static void DeleteData()
        {
            Libs.KData.Delete(DataFileName);

            if (Data) Object.DestroyImmediate(Data);

            Data = null;
        }

        public static void RemoveEntry(int index)
        {
            EnsureData().RemoveAt(index);

            Libs.KData.Flush();
        }

        public static void TogglePinned(int index)
        {
            EnsureData().TogglePinned(index);

            Libs.KData.Flush();
        }

        public static void ClearHistory()
        {
            EnsureData().Clear();

            Libs.KData.Flush();
        }

        #endregion
    }
}
#endif
