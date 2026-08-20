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
                this.entries.RemoveLast();

            this.Dirty();
        }

        public void Clear()
        {
            this.entries.Clear();

            this.Dirty();
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
        }

        #endregion
    }
}
#endif
