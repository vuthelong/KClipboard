#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Type = System.Type;
using Object = UnityEngine.Object;
using static Kingfisher.KClipboard.Libs.KUtils;

namespace Kingfisher.KClipboard
{
    public static class KClipboard
    {
        #region Field

        public const string DataFileName = "KClipboard Data.asset";

        private const string DebugLogPrefix = "[KCLIP-DEBUG]";

        public static KClipboardData Data;

        #endregion

        #region Context Menu

        [MenuItem("CONTEXT/Component/Copy to K-Clipboard History")]
        private static void CopyToHistory(MenuCommand command)
        {
            if (KClipboardMenu.PluginDisabled) return;
            if (command.context is not Component component) return;

            ComponentUtility.CopyComponent(component);

            var componentType = component.GetType();

            EnsureData().Push(componentType.AssemblyQualifiedName, componentType.Name, EditorJsonUtility.ToJson(component), Mathf.RoundToInt(KClipboardMenu.MaxHistoryCount));

            Libs.KData.Flush();

            if (!KClipboardMenu.DebugLoggingEnabled) return;

            Debug.Log($"{DebugLogPrefix} Copied {componentType.Name} from '{component.gameObject.name}' to history.");
        }

        #endregion

        #region Method

        public static KClipboardData EnsureData()
        {
            if (Data) return Data;

            Data = Libs.KData.Load<KClipboardData>(DataFileName) ?? Libs.KData.Create<KClipboardData>(DataFileName);

            Libs.KData.Autosave(Data, DataFileName);

            return Data;
        }

        public static void DeleteData()
        {
            Libs.KData.Delete(DataFileName);

            if (Data) Object.DestroyImmediate(Data);

            Data = null;
        }

        public static void ClearHistory()
        {
            EnsureData().Clear();

            Libs.KData.Flush();
        }

        public static bool TryPasteToSelected(KClipboardData.HistoryEntry entry, out string message)
        {
            var gameObjects = Selection.gameObjects;

            if (gameObjects.Length == 0)
            {
                message = "Select at least one GameObject to paste into.";

                return false;
            }

            var componentType = Type.GetType(entry.componentTypeName);

            if (componentType == null)
            {
                message = $"Component type '{entry.componentTypeLabel}' could not be resolved. It may belong to a script that was removed or renamed.";

                return false;
            }

            var pastedCount = 0;
            var skippedCount = 0;

            for (var i = 0; i < gameObjects.Length; i++)
            {
                var targetComponent = gameObjects[i].GetComponent(componentType);

                if (targetComponent == null)
                {
                    skippedCount++;

                    continue;
                }

                Undo.RecordObject(targetComponent, "Paste Component Values from K-Clipboard");

                EditorJsonUtility.FromJsonOverwrite(entry.json, targetComponent);

                targetComponent.Dirty();

                pastedCount++;
            }

            if (pastedCount == 0)
            {
                message = $"None of the selected GameObjects have a {entry.componentTypeLabel} component. Add the component first, then paste.";

                return false;
            }

            message = skippedCount == 0
                ? $"Pasted {entry.componentTypeLabel} values onto {pastedCount} GameObject(s)."
                : $"Pasted {entry.componentTypeLabel} values onto {pastedCount} GameObject(s), skipped {skippedCount} without the component.";

            return true;
        }

        #endregion
    }
}
#endif
