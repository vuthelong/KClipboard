#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Kingfisher.KClipboard.Libs
{
    public static class KData
    {
        #region Field

        private const double SaveDelay = .5;

        private const string FolderName = ".KData";

        private const string GitIgnoreFileName = ".gitignore";

        private const string GitIgnoreContents = "# Kingfisher Tools editor data - local to this machine.\n" +
                                                 "# Delete this file to commit the folder instead.\n" +
                                                 "*\n";

        private static readonly List<Entry> Entries = new();

        private static string _folderPath;

        #endregion

        #region Property

        public static string FolderPath => _folderPath ??= Application.dataPath.GetParentPath().CombinePath(FolderName);

        #endregion

        #region Asset Storage

        public static T Load<T>(string fileName) where T : ScriptableObject
        {
            var filePath = GetFilePath(fileName);

            if (!File.Exists(filePath)) return null;

            var loaded = InternalEditorUtility.LoadSerializedFileAndForget(filePath);

            for (var i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] is not T typed) continue;

                typed.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

                return typed;
            }

            return null;
        }

        public static T Create<T>(string fileName) where T : ScriptableObject
        {
            var created = ScriptableObject.CreateInstance<T>();

            created.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

            Save(created, fileName);
            Autosave(created, fileName);

            return created;
        }

        public static void Save(ScriptableObject asset, string fileName)
        {
            if (!asset) return;

            EnsureFolder();

            if (!EditorUtility.IsPersistent(asset))
            {
                InternalEditorUtility.SaveToSerializedFileAndForget(new[] { asset }, GetFilePath(fileName), allowTextSerialization: true);

                return;
            }

            var copy = Object.Instantiate(asset);

            copy.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

            InternalEditorUtility.SaveToSerializedFileAndForget(new[] { copy }, GetFilePath(fileName), allowTextSerialization: true);

            Object.DestroyImmediate(copy);
        }

        public static void Delete(string fileName)
        {
            // Drop the autosave entry first, or the next flush writes the file
            // straight back out of the copy still held in memory.
            for (var i = Entries.Count - 1; i >= 0; i--)
            {
                if (Entries[i].FileName != fileName) continue;

                Entries.RemoveAt(i);
            }

            var filePath = GetFilePath(fileName);

            if (!File.Exists(filePath)) return;

            File.Delete(filePath);
        }

        #endregion

        #region Autosave

        public static void Autosave(ScriptableObject asset, string fileName)
        {
            if (!asset) return;

            for (var i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Asset == asset) return;
            }

            Entries.Add(new Entry { Asset = asset, FileName = fileName });

            EditorApplication.update -= Update;
            EditorApplication.update += Update;

            EditorApplication.quitting -= Flush;
            EditorApplication.quitting += Flush;

            AssemblyReloadEvents.beforeAssemblyReload -= Flush;
            AssemblyReloadEvents.beforeAssemblyReload += Flush;

            Undo.undoRedoPerformed -= MarkAllPending;
            Undo.undoRedoPerformed += MarkAllPending;
        }

        public static void Flush()
        {
            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                if (!entry.Asset) continue;

                if (EditorUtility.IsDirty(entry.Asset))
                {
                    EditorUtility.ClearDirty(entry.Asset);

                    entry.SavePending = true;
                }

                if (!entry.SavePending) continue;

                Save(entry.Asset, entry.FileName);

                entry.SavePending = false;
            }
        }

        private static void MarkAllPending()
        {
            for (var i = 0; i < Entries.Count; i++)
            {
                Entries[i].SavePending = true;
                Entries[i].LastDirtyTime = EditorApplication.timeSinceStartup;
            }
        }

        private static void Update()
        {
            var now = EditorApplication.timeSinceStartup;

            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                if (!entry.Asset) continue;

                if (EditorUtility.IsDirty(entry.Asset))
                {
                    EditorUtility.ClearDirty(entry.Asset);

                    entry.SavePending = true;
                    entry.LastDirtyTime = now;
                }

                if (!entry.SavePending) continue;

                if (now - entry.LastDirtyTime < SaveDelay) continue;

                Save(entry.Asset, entry.FileName);

                entry.SavePending = false;
            }
        }

        #endregion

        #region File Access

        public static string GetFilePath(string fileName) => FolderPath.CombinePath(fileName);

        public static bool Exists(string fileName) => File.Exists(GetFilePath(fileName));

        private static void EnsureFolder()
        {
            Directory.CreateDirectory(FolderPath);

            var gitIgnorePath = FolderPath.CombinePath(GitIgnoreFileName);

            if (File.Exists(gitIgnorePath)) return;

            File.WriteAllText(gitIgnorePath, GitIgnoreContents);
        }

        #endregion

        #region Nested Type

        private class Entry
        {
            public ScriptableObject Asset;
            public string FileName;

            public bool SavePending;
            public double LastDirtyTime;
        }

        #endregion
    }
}
#endif
