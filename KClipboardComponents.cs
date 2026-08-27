#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Type = System.Type;
using Object = UnityEngine.Object;
using static Kingfisher.KClipboard.Libs.KUtils;

namespace Kingfisher.KClipboard
{
    public static class KClipboardComponents
    {
        #region Field

        public const string DataFileName = "KClipboard Components Data.asset";

        private const string CopyMenuPath = "CONTEXT/Component/Copy to K-Clipboard History";

        private const string DarkIconPrefix = "d_";

        private const string PasteUndoLabel = "Paste Component Values from K-Clipboard";

        private const string EmptySelectionMessage = "Select at least one GameObject to paste into.";
        private const string UnresolvedTypeFormat = "Component type '{0}' could not be resolved. It may belong to a script that was removed or renamed.";
        private const string NotAComponentTypeFormat = "'{0}' is not a component type that can be pasted.";
        private const string NothingPastedFormat = "Could not paste {0} onto any of the selected GameObjects.";
        private const string PastedFormat = "Pasted {0} values onto {1} GameObject(s).";
        private const string AddedSuffixFormat = " Added the component to {0} of them.";
        private const string FailedSuffixFormat = " Failed on {0}.";

        public static KClipboardComponentsData Data;

        #endregion

        #region Context Menu

        [MenuItem(CopyMenuPath)]
        private static void CopyToHistory(MenuCommand command)
        {
            if (KClipboardMenu.PluginDisabled) return;
            if (command.context is not Component component) return;

            CopyComponentToHistory(component);
        }

        public static void CopyComponentToHistory(Component component)
        {
            if (component == null) return;

            ComponentUtility.CopyComponent(component);

            PushToHistory(component);
        }

        private static void PushToHistory(Component component)
        {
            var componentType = component.GetType();

            EnsureData().Push(new KClipboardComponentsData.HistoryEntry
            {
                componentTypeName = componentType.AssemblyQualifiedName,
                componentTypeLabel = componentType.Name,
                iconName = GetIconName(component),
                json = EditorJsonUtility.ToJson(component),
            }, Mathf.RoundToInt(KClipboardMenu.MaxComponentHistoryCount));

            Libs.KData.Flush();
        }

        public static string GetIconName(Component component)
        {
            if (component == null) return string.Empty;

            var icon = EditorGUIUtility.ObjectContent(component, component.GetType()).image;

            if (icon == null) return string.Empty;

            return icon.name.StartsWith(DarkIconPrefix) ? icon.name.Substring(DarkIconPrefix.Length) : icon.name;
        }

        #endregion

        #region Paste

        public static bool TryPasteToSelected(KClipboardComponentsData.HistoryEntry entry, out string message)
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

        private static bool TryResolveComponentType(KClipboardComponentsData.HistoryEntry entry, out Type componentType, out string message)
        {
            componentType = string.IsNullOrEmpty(entry.componentTypeName) ? null : Type.GetType(entry.componentTypeName);

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

        public static KClipboardComponentsData EnsureData()
        {
            if (Data) return Data;

            Data = Libs.KData.Load<KClipboardComponentsData>(DataFileName) ?? Libs.KData.Create<KClipboardComponentsData>(DataFileName);

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

        public static KClipboardComponentsData.HistoryEntry DetachEntryForReorder(int index)
        {
            var entry = EnsureData().DetachEntry(index);

            Libs.KData.Flush();

            return entry;
        }

        public static void InsertEntryForReorder(KClipboardComponentsData.HistoryEntry entry, int index)
        {
            EnsureData().InsertEntry(entry, index);

            Libs.KData.Flush();
        }

        public static void UpdateEntry(KClipboardComponentsData.HistoryEntry entry, Component component)
        {
            var data = EnsureData();

            data.SetJson(entry, EditorJsonUtility.ToJson(component));
            data.SetIconName(entry, GetIconName(component));
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
