#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using static Kingfisher.KClipboard.Libs.KUtils;

namespace Kingfisher.KClipboard
{
    public class KClipboardData : ScriptableObject
    {
        #region Field

        private const int NoIndex = -1;

        public List<HistoryEntry> entries = new();

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

            this.Dirty();
        }

        public void TogglePinned(int index)
        {
            if (!index.IsInRangeOf(this.entries)) return;

            var entry = this.entries[index];

            entry.pinned = !entry.pinned;

            this.entries.RemoveAt(index);
            this.entries.Insert(GetSortedIndex(entry), entry);

            this.Dirty();
        }

        public void SetJson(HistoryEntry entry, string json)
        {
            if (entry == null) return;
            if (entry.json == json) return;

            entry.json = json;

            this.Dirty();
        }

        public void SetIconName(HistoryEntry entry, string iconName)
        {
            if (entry == null) return;
            if (entry.iconName == iconName) return;

            entry.iconName = iconName;

            this.Dirty();
        }

        public void RemoveAt(int index)
        {
            if (!index.IsInRangeOf(this.entries)) return;

            this.entries.RemoveAt(index);

            this.Dirty();
        }

        public void Clear()
        {
            for (var i = this.entries.Count - 1; i >= 0; i--)
            {
                if (this.entries[i].pinned) continue;

                this.entries.RemoveAt(i);
            }

            this.Dirty();
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
            public string componentTypeName;
            public string componentTypeLabel;
            public string iconName;
            public string json;
            public long timestampTicks;
            public bool pinned;
        }

        #endregion
    }
}
#endif
