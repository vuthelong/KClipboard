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

        #region Method

        public void Push(string componentTypeName, string componentTypeLabel, string json, int maxCount)
        {
            this.entries.Insert(0, new HistoryEntry
            {
                componentTypeName = componentTypeName,
                componentTypeLabel = componentTypeLabel,
                json = json,
                timestampTicks = DateTime.UtcNow.Ticks,
            });

            while (this.entries.Count > maxCount.Max(0))
            {
                var trimIndex = GetLastUnpinnedIndex();

                if (trimIndex == NoIndex) break;

                this.entries.RemoveAt(trimIndex);
            }

            this.Dirty();
        }

        public void SetPinned(int index, bool isPinned)
        {
            if (!index.IsInRangeOf(this.entries)) return;
            if (this.entries[index].pinned == isPinned) return;

            this.entries[index].pinned = isPinned;

            this.Dirty();
        }

        public bool HasUnpinned() => GetLastUnpinnedIndex() != NoIndex;

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
            public string json;
            public long timestampTicks;
            public bool pinned;
        }

        #endregion
    }
}
#endif
