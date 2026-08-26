#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
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

        private const string DarkIconPrefix = "d_";

        public const string DefaultGameObjectIconName = "GameObject Icon";

        private const string CopyMenuPath = "GameObject/Copy to K-Clipboard History";
        private const int CopyMenuPriority = 20;

        private const string EmptySelectionToCopyMessage = "Select at least one GameObject to copy.";
        private const string CopyProducedNoDataMessage = "Unity's GameObject copy produced no data to store.";
        private const string CopiedFormat = "Copied {0} GameObject(s) to K-Clipboard History.";

        private const string EmptyBlobMessage = "This entry has no stored data.";
        private const string PasteFailedMessage = "Could not paste these GameObject(s).";
        private const string PastedMessage = "Pasted GameObject(s).";

        private const string PasteFailureLogFormat = "K-Clipboard: {0}";

        private const string TempFolderPath = "Assets/KClipboardTemp";
        private const string TempAssetNameFormat = "KClipboard_{0:N}.prefab";
        private const string WrapperName = "KClipboard Copy Wrapper";
        private const string PasteUndoLabel = "Paste GameObjects from K-Clipboard";
        private const string AssetsFolderName = "Assets";

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

            if (!TryCaptureBlob(gameObjects, out var blob))
            {
                message = CopyProducedNoDataMessage;

                return false;
            }

            PushToHistory(gameObjects, blob);

            message = string.Format(CopiedFormat, gameObjects.Length);

            return true;
        }

        // Unity's GameObject copy/paste (Unsupported.CopyGameObjectsToPasteboard) writes to an
        // internal native buffer, NOT EditorGUIUtility.systemCopyBuffer (that's the OS text
        // clipboard, a separate and unrelated store) - there is no public API to read that native
        // buffer's contents back out for persistence. A temporary .prefab asset is Unity's own
        // hierarchy+reference serializer exposed through fully public, inspectable APIs
        // (PrefabUtility/AssetDatabase), so it's used here instead, purely as a storage mechanism -
        // the asset is written, read back as bytes, and deleted within this one call.
        private static bool TryCaptureBlob(GameObject[] gameObjects, out string blob)
        {
            blob = null;

            EnsureTempFolder();

            var assetPath = GetUniqueTempAssetPath();
            var wrapper = new GameObject(WrapperName);

            try
            {
                for (var i = 0; i < gameObjects.Length; i++)
                {
                    var clone = Object.Instantiate(gameObjects[i]);

                    clone.transform.SetParent(wrapper.transform, worldPositionStays: false);
                }

                var saved = PrefabUtility.SaveAsPrefabAsset(wrapper, assetPath);

                if (saved == null) return false;

                blob = Convert.ToBase64String(File.ReadAllBytes(GetDiskPath(assetPath)));

                return true;
            }
            finally
            {
                Object.DestroyImmediate(wrapper);
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static void PushToHistory(GameObject[] gameObjects, string blob)
        {
            var previewRoots = new KClipboardGameObjectsData.PreviewNode[gameObjects.Length];

            for (var i = 0; i < gameObjects.Length; i++)
                previewRoots[i] = BuildPreviewNode(gameObjects[i]);

            EnsureData().Push(new KClipboardGameObjectsData.HistoryEntry
            {
                previewRoots = previewRoots,
                iconName = PrefabIconName,
                prefabBlob = blob,
            }, Mathf.RoundToInt(KClipboardMenu.MaxGameObjectHistoryCount));

            Libs.KData.Flush();
        }

        // Captured up-front, before the prefab blob is ever decoded - this is what makes a
        // read-only preview possible without instantiating the stored data just to look at it.
        // Unity's own ObjectContent icon for a GameObject already accounts for a custom icon
        // override or a single distinguishing component (Camera, Light, ...), so no per-component
        // heuristic is needed here.
        private static KClipboardGameObjectsData.PreviewNode BuildPreviewNode(GameObject gameObject)
        {
            var node = new KClipboardGameObjectsData.PreviewNode
            {
                name = gameObject.name,
                iconName = GetGameObjectIconName(gameObject),
                componentIconNames = GetComponentIconNames(gameObject),
            };

            var transform = gameObject.transform;

            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;

                // Matches KHierarchy's own IsHiddenInHierarchy check - objects some tools attach as
                // internal markers (e.g. KInspector's rename marker) set this to stay out of the
                // real Hierarchy, so the preview should hide them too. This only affects the
                // preview: TryCaptureBlob clones the real GameObject and still copies them.
                if ((child.hideFlags & HideFlags.HideInHierarchy) != 0) continue;

                node.children.Add(BuildPreviewNode(child));
            }

            return node;
        }

        // Always falls back to the generic GameObject icon rather than returning empty, so every
        // preview tree row shows something before its name - matching every row Unity's own
        // Hierarchy ever draws, which never leaves a GameObject's icon slot blank either.
        private static string GetGameObjectIconName(GameObject gameObject)
        {
            if (gameObject == null) return DefaultGameObjectIconName;

            var icon = EditorGUIUtility.ObjectContent(gameObject, typeof(GameObject)).image;

            if (icon == null) return DefaultGameObjectIconName;

            var name = icon.name.StartsWith(DarkIconPrefix) ? icon.name.Substring(DarkIconPrefix.Length) : icon.name;

            return string.IsNullOrEmpty(name) ? DefaultGameObjectIconName : name;
        }

        // Transform is skipped - every GameObject has one, so showing it in the minimap would just
        // be visual noise. Components hidden from the Inspector (e.g. internal marker components
        // some tools attach to themselves, like KInspector's rename marker) are skipped here too -
        // this only affects what the preview shows, not what gets copied: the actual clone in
        // TryCaptureBlob operates on the real GameObject and includes every component regardless.
        // Reuses the Component sub-tool's own icon derivation for consistency.
        private static string[] GetComponentIconNames(GameObject gameObject)
        {
            var components = gameObject.GetComponents<Component>();
            var iconNames = new List<string>(components.Length);

            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] == null) continue;
                if (components[i] is Transform) continue;
                if ((components[i].hideFlags & HideFlags.HideInInspector) != 0) continue;

                iconNames.Add(KClipboardComponents.GetIconName(components[i]));
            }

            return iconNames.ToArray();
        }

        #endregion

        #region Paste

        // Placement mirrors the current Hierarchy selection: pasted roots land as children of
        // Selection.activeTransform, or at the active scene's root when nothing is selected -
        // there is no original local transform to "preserve" beyond what was captured into the
        // prefab, since this no longer round-trips through Unity's own native paste command.
        public static bool TryPaste(KClipboardGameObjectsData.HistoryEntry entry, out string message)
        {
            if (entry == null || string.IsNullOrEmpty(entry.prefabBlob))
            {
                message = EmptyBlobMessage;

                return false;
            }

            if (!TryInstantiateFromBlob(entry.prefabBlob, out var pastedRoots))
            {
                message = PasteFailedMessage;

                return false;
            }

            Selection.objects = pastedRoots.ToArray();

            message = PastedMessage;

            return true;
        }

        private static bool TryInstantiateFromBlob(string blob, out List<GameObject> pastedRoots)
        {
            pastedRoots = new List<GameObject>();

            EnsureTempFolder();

            var assetPath = GetUniqueTempAssetPath();

            File.WriteAllBytes(GetDiskPath(assetPath), Convert.FromBase64String(blob));

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab == null)
            {
                AssetDatabase.DeleteAsset(assetPath);

                return false;
            }

            Undo.IncrementCurrentGroup();

            var undoGroup = Undo.GetCurrentGroup();
            GameObject wrapper = null;

            try
            {
                wrapper = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

                Undo.RegisterCreatedObjectUndo(wrapper, PasteUndoLabel);

                PrefabUtility.UnpackPrefabInstance(wrapper, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                var targetParent = Selection.activeTransform;
                var wrapperTransform = wrapper.transform;

                while (wrapperTransform.childCount > 0)
                {
                    var child = wrapperTransform.GetChild(0);

                    Undo.SetTransformParent(child, targetParent, PasteUndoLabel);

                    pastedRoots.Add(child.gameObject);
                }
            }
            finally
            {
                if (wrapper) Undo.DestroyObjectImmediate(wrapper);

                Undo.SetCurrentGroupName(PasteUndoLabel);
                Undo.CollapseUndoOperations(undoGroup);

                AssetDatabase.DeleteAsset(assetPath);
            }

            return pastedRoots.Count > 0;
        }

        #endregion

        #region Temp Asset

        private static void EnsureTempFolder()
        {
            if (AssetDatabase.IsValidFolder(TempFolderPath)) return;

            var lastSlash = TempFolderPath.LastIndexOf('/');

            AssetDatabase.CreateFolder(TempFolderPath.Substring(0, lastSlash), TempFolderPath.Substring(lastSlash + 1));
        }

        private static string GetUniqueTempAssetPath() => $"{TempFolderPath}/{string.Format(TempAssetNameFormat, Guid.NewGuid())}";

        private static string GetDiskPath(string assetPath) => Application.dataPath.Substring(0, Application.dataPath.Length - AssetsFolderName.Length) + assetPath;

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
