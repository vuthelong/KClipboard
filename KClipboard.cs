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

        private const string CopyMenuPath = "CONTEXT/Component/Copy to K-Clipboard History";

        private const string PasteUndoLabel = "Paste Component Values from K-Clipboard";

        private const string EmptySelectionMessage = "Select at least one GameObject to paste into.";
        private const string UnresolvedTypeFormat = "Component type '{0}' could not be resolved. It may belong to a script that was removed or renamed.";
        private const string NotAComponentTypeFormat = "'{0}' is not a component type that can be pasted.";
        private const string NothingPastedFormat = "Could not paste {0} onto any of the selected GameObjects.";
        private const string PastedFormat = "Pasted {0} values onto {1} GameObject(s).";
        private const string AddedSuffixFormat = " Added the component to {0} of them.";
        private const string FailedSuffixFormat = " Failed on {0}.";

        public static KClipboardData Data;

        #endregion

        #region Context Menu

        [MenuItem(CopyMenuPath)]
        private static void CopyToHistory(MenuCommand command)
        {
            if (KClipboardMenu.PluginDisabled) return;
            if (command.context is not Component component) return;

            ComponentUtility.CopyComponent(component);

            var componentType = component.GetType();

            EnsureData().Push(componentType.AssemblyQualifiedName, componentType.Name, EditorJsonUtility.ToJson(component), Mathf.RoundToInt(KClipboardMenu.MaxHistoryCount));

            Libs.KData.Flush();
        }

        #endregion

        #region Paste

        public static bool TryPasteToSelected(KClipboardData.HistoryEntry entry, out string message)
        {
            var gameObjects = Selection.gameObjects;

            if (gameObjects.Length == 0)
            {
                message = EmptySelectionMessage;

                return false;
            }

            if (!TryResolveComponentType(entry, out var componentType, out message)) return false;

            Undo.IncrementCurrentGroup();

            var undoGroup = Undo.GetCurrentGroup();
            var pastedCount = 0;
            var addedCount = 0;

            for (var i = 0; i < gameObjects.Length; i++)
            {
                var targetComponent = GetOrAddComponent(gameObjects[i], componentType, out var wasAdded);

                if (targetComponent == null) continue;

                if (wasAdded) addedCount++;

                EditorJsonUtility.FromJsonOverwrite(entry.json, targetComponent);

                targetComponent.Dirty();

                pastedCount++;
            }

            Undo.SetCurrentGroupName(PasteUndoLabel);
            Undo.CollapseUndoOperations(undoGroup);

            message = BuildPasteMessage(entry.componentTypeLabel, pastedCount, addedCount, gameObjects.Length - pastedCount);

            return pastedCount > 0;
        }

        private static bool TryResolveComponentType(KClipboardData.HistoryEntry entry, out Type componentType, out string message)
        {
            componentType = Type.GetType(entry.componentTypeName);

            if (componentType == null)
            {
                message = string.Format(UnresolvedTypeFormat, entry.componentTypeLabel);

                return false;
            }

            if (componentType.IsAbstract || !typeof(Component).IsAssignableFrom(componentType))
            {
                message = string.Format(NotAComponentTypeFormat, entry.componentTypeLabel);

                return false;
            }

            message = string.Empty;

            return true;
        }

        private static Component GetOrAddComponent(GameObject gameObject, Type componentType, out bool wasAdded)
        {
            var component = gameObject.GetComponent(componentType);

            wasAdded = component == null;

            if (!wasAdded)
            {
                Undo.RecordObject(component, PasteUndoLabel);

                return component;
            }

            return Undo.AddComponent(gameObject, componentType);
        }

        private static string BuildPasteMessage(string componentTypeLabel, int pastedCount, int addedCount, int failedCount)
        {
            if (pastedCount == 0) return string.Format(NothingPastedFormat, componentTypeLabel);

            var message = string.Format(PastedFormat, componentTypeLabel, pastedCount);

            if (addedCount > 0) message += string.Format(AddedSuffixFormat, addedCount);
            if (failedCount > 0) message += string.Format(FailedSuffixFormat, failedCount);

            return message;
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

        public static void RemoveEntry(int index)
        {
            EnsureData().RemoveAt(index);

            Libs.KData.Flush();
        }

        public static void SetPinned(int index, bool isPinned)
        {
            EnsureData().SetPinned(index, isPinned);

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
