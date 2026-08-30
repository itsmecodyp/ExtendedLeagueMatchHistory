using System;
using System.Collections.Generic;
using System.Text;

namespace testclient2.League.Models
{

    public class MatchHistoryEntry
    {
        /// <summary>
        /// Unique LCU game id, used as the dedupe key when merging cached
        /// history and as the id in the leagueofgraphs match URL.
        /// </summary>
        public string GameId { get; set; } = "";

        /// <summary>
        /// Epoch-milliseconds timestamp from the LCU "gameCreation" field.
        /// Used to keep the cached entries sorted newest-first.
        /// </summary>
        public long GameCreation { get; set; }

        public bool Win { get; set; }

        public string Result { get; set; } = "";

        public string ChampionName { get; set; } = "";

        public string KdaText { get; set; } = "";

        public string KdaRatioText { get; set; } = "";

        public string Background { get; set; } = "";
    }
}
