using System;
using System.Threading.Tasks;

namespace testclient2.League.Models;

public class LeagueFriend
{

public DateTime? GameStartedAt { get; set; }

public TimeSpan GameDuration =>
    GameStartedAt.HasValue
        ? DateTime.UtcNow - GameStartedAt.Value
        : TimeSpan.Zero;
    public string GameInfoDisplay
    {
        get
        {
            if (!IsInGame)
                return StatusDisplay;

            if (!string.IsNullOrWhiteSpace(Lol?.Skinname))
                return $"In Game · {Lol.Skinname}";

            return "In Game";
        }
    }


    public string GameDurationDisplay =>
    $"{(int)GameDuration.TotalMinutes:00}:{GameDuration.Seconds:00}";
public string? GameName { get; set; }

    public string? GameTag { get; set; }

    public string? PuuId { get; set; }
    public long SummonerId { get; set; }

    public string? Availability { get; set; }

    public string? StatusMessage { get; set; }

    public string? ProductName { get; set; }
    public string? GameId { get; set; }

    public string? GameMode { get; set; }

    public string? GameQueueType { get; set; }

    public string? GameStatus { get; set; }

    public string? QueueId { get; set; }

    public string? ChampionId { get; set; }

    public string? Skinname { get; set; }

    public string? MapId { get; set; }

    public string? Level { get; set; }

    public LeagueFriendLol? Lol { get; set; }

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(GameName)
            ? $"{GameName}#{GameTag}"
            : "Unknown";

    public bool IsInGame =>
        Lol?.GameStatus?.Equals(
            "inGame",
            System.StringComparison.OrdinalIgnoreCase) == true;

    public string StatusDisplay
    {
        get
        {
            if (IsInGame)
                return "In Game";

            return Availability?.ToLowerInvariant() switch
            {
                "chat" => "Online",
                "mobile" => "Mobile",
                "away" => "Away",
                "dnd" => "Do Not Disturb",
                "offline" => "Offline",
                _ => Availability ?? "Unknown"
            };
        }
    }

    public string StatusSymbol => "●";

    public string StatusColor
    {
        get
        {
            if (IsInGame)
                return "#E05A5A";

            return Availability?.ToLowerInvariant() switch
            {
                "chat" => "#5AC878",
                "mobile" => "#5AA9E0",
                "away" => "#D0A94A",
                "dnd" => "#D0A94A",
                "offline" => "#666666",
                _ => "#666666"
            };
        }
    }

    public int StatusSortOrder
    {
        get
        {
            if (IsInGame)
                return 0;

            return Availability?.ToLowerInvariant() switch
            {
                "chat" => 1,
                "mobile" => 2,
                "away" => 3,
                "dnd" => 4,
                "offline" => 5,
                _ => 6
            };
        }
    }
}

public class LeagueFriendLol
{
    public string? GameId { get; set; }

    public string? GameMode { get; set; }

    public string? GameQueueType { get; set; }

    public string? GameStatus { get; set; }

    public string? QueueId { get; set; }

    public string? ChampionId { get; set; }

    public string? Skinname { get; set; }

    public string? MapId { get; set; }

    public string? Puuid { get; set; }

    public string? Level { get; set; }
}