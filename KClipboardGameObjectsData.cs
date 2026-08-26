#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using static Kingfisher.KClipboard.Libs.KUtils;

namespace Kingfisher.KClipboard
{
    public class KClipboardGameObjectsData : ScriptableObject
    {
        #region Field

        private const int NoIndex = -1;

        public List<HistoryEntry> entries = new();

        private int _version;

        #endregion

        #region Property

        public int Version => this._version;

        #endregion

        #region Entry

        public void Push(HistoryEntry entry, int maxCount)
        {
            entry.timestampTicks = DateTime.UtcNow.Ticks;

            this.entries.Insert(GetPinnedCount(), entry);

            while (this.entries.Count > maxCount.Max(0))
            {
                var trimIndex = GetLastUnpinnedIndex();

                if (trimIndex == NoIndex) break;

                this.entries.RemoveAt(trimIndex);
            }

            MarkChanged();
        }

        public void TogglePinned(int index)
        {
            if (!index.IsInRangeOf(this.entries)) return;

            var entry = this.entries[index];

            entry.pinned = !entry.pinned;

            this.entries.RemoveAt(index);
            this.entries.Insert(GetSortedIndex(entry), entry);

            MarkChanged();
        }

        public void RemoveAt(int index)
        {
            if (!index.IsInRangeOf(this.entries)) return;

            this.entries.RemoveAt(index);

            MarkChanged();
        }

        public void Clear()
        {
            for (var i = this.entries.Count - 1; i >= 0; i--)
            {
                if (this.entries[i].pinned) continue;

                this.entries.RemoveAt(i);
            }

            MarkChanged();
        }

        #endregion

        #region Order

        public bool HasUnpinned() => GetLastUnpinnedIndex() != NoIndex;

        private int GetPinnedCount()
        {
            for (var i = 0; i < this.entries.Count; i++)
            {
                if (this.entries[i].pinned) continue;

                return i;
            }

            return this.entries.Count;
        }

        private int GetSortedIndex(HistoryEntry entry)
        {
            var pinnedCount = GetPinnedCount();

            if (entry.pinned) return pinnedCount;

            for (var i = pinnedCount; i < this.entries.Count; i++)
            {
                if (this.entries[i].timestampTicks > entry.timestampTicks) continue;

                return i;
            }

            return this.entries.Count;
        }

        private void MarkChanged()
        {
            this._version++;

            this.Dirty();
        }

        private int GetLastUnpinnedIndex()
        {
            for (var i = this.entries.Count - 1; i >= 0; i--)
            {
                if (this.entries[i].pinned) continue;

                return i;
            }

            return NoIndex;
        }

        #endregion

        #region Nested Type

        [Serializable]
        public class HistoryEntry
        {
            public string[] rootNames;
            public string iconName;
            public string pasteboardBlob;
            public long timestampTicks;
            public bool pinned;
        }

        #endregion
    }
}
#endif
