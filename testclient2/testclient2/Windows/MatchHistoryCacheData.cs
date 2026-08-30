using System;
using System.Collections.Generic;

namespace testclient2.League.Models
{
    /// <summary>
    /// Persisted, per-player match history cache. Serialized to disk as
    /// JSON so repeated launches don't have to re-fetch full history.
    /// </summary>
    public class MatchHistoryCacheData
    {
        public DateTime LastCheckedUtc { get; set; }

        public List<MatchHistoryEntry> Entries { get; set; } = new();
    }
}
