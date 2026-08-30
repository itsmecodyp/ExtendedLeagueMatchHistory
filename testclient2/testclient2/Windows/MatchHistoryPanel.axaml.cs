using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using testclient2.League;
using testclient2.League.Models;

namespace testclient2.Windows;

public partial class MatchHistoryPanel : Window
{
    // First time we see a player, grab a big batch to seed the cache.
    private const int InitialFetchCount = 200;

    // Never ask the LCU for more than this many games in one request.
    private const int MaxFetchCount = 200;

    private const int MinFetchCount = 1;

    // Roughly how long an average game takes - used to estimate how many
    // games might have finished since we last checked.
    private const double MinutesPerGameEstimate = 30;

    public MatchHistoryPanel()
    {
        InitializeComponent();
    }

    public async Task LoadHistoryAsync(
        string puuid,
        string displayName)
    {
        PlayerNameText.Text =
            $"{displayName} - Match History";

        try
        {
            MatchHistoryCacheData cache =
                await MatchHistoryCacheService.LoadAsync(puuid);

            bool hasCachedEntries = cache.Entries.Count > 0;

            int gamesToFetch = CalculateGamesToFetch(
                hasCachedEntries ? cache.LastCheckedUtc : null);

            Debug.WriteLine(
                $"Fetching {gamesToFetch} recent game(s) for {displayName} " +
                $"(cache had {cache.Entries.Count})");

            string? json =
                await MainWindow.leagueClient.GetFriendMatchHistoryAsync(
                    puuid,
                    0,
                    gamesToFetch - 1);

            if (!string.IsNullOrWhiteSpace(json))
            {
                List<MatchHistoryEntry> freshEntries =
                    await ParseEntriesAsync(json);

                int added = MatchHistoryCacheService.MergeEntries(
                    cache,
                    freshEntries);

                Debug.WriteLine(
                    $"Added {added} new match(es) to cache for {displayName}");
            }
            else
            {
                Debug.WriteLine(
                    $"No match history response for {displayName}, " +
                    "showing cached entries only.");
            }

            cache.LastCheckedUtc = DateTime.UtcNow;

            await MatchHistoryCacheService.SaveAsync(puuid, cache);

            DisplayEntries(cache.Entries);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Failed to load match history: {ex}");
        }
    }

    /// <summary>
    /// Decides how many games to ask the LCU for. First look at a player
    /// pulls a full batch. After that we estimate how many games could
    /// plausibly have finished since the last check (elapsed minutes /
    /// ~30 min per game) instead of always re-pulling everything.
    /// </summary>
    private static int CalculateGamesToFetch(DateTime? lastCheckedUtc)
    {
        if (lastCheckedUtc is not DateTime lastChecked)
            return InitialFetchCount;

        double elapsedMinutes =
            (DateTime.UtcNow - lastChecked).TotalMinutes;

        if (elapsedMinutes <= 0)
            return MinFetchCount;

        int estimatedGames =
            (int)Math.Ceiling(elapsedMinutes / MinutesPerGameEstimate);

        return Math.Clamp(estimatedGames, MinFetchCount, MaxFetchCount);
    }

    private void DisplayEntries(List<MatchHistoryEntry> entries)
    {
        int wins = entries.Count(entry => entry.Win);
        int losses = entries.Count - wins;

        WinsText.Text = $"W {wins}";
        LossesText.Text = $"L {losses}";

        GamesList.ItemsSource = entries;
    }

    private static async Task<List<MatchHistoryEntry>> ParseEntriesAsync(
        string json)
    {
        List<MatchHistoryEntry> entries = new();

        using JsonDocument document =
            JsonDocument.Parse(json);

        JsonElement root =
            document.RootElement;

        if (!root.TryGetProperty(
                "games",
                out JsonElement gamesObject))
        {
            return entries;
        }

        if (!gamesObject.TryGetProperty(
                "games",
                out JsonElement games))
        {
            return entries;
        }

        foreach (JsonElement game in games.EnumerateArray())
        {
            if (!game.TryGetProperty(
                    "participants",
                    out JsonElement participants))
            {
                continue;
            }

            if (participants.GetArrayLength() == 0)
                continue;

            JsonElement participant =
                participants[0];

            if (!participant.TryGetProperty(
                    "stats",
                    out JsonElement stats))
            {
                continue;
            }

            string gameId = GetGameId(game);

            // No id means we can't dedupe or link to it - skip it.
            if (string.IsNullOrEmpty(gameId))
                continue;

            long gameCreation =
                game.TryGetProperty(
                    "gameCreation",
                    out JsonElement creationElement) &&
                creationElement.TryGetInt64(out long creationValue)
                    ? creationValue
                    : 0;

            int championId =
                participant.TryGetProperty(
                    "championId",
                    out JsonElement championIdElement) &&
                championIdElement.TryGetInt32(out int championIdValue)
                    ? championIdValue
                    : 0;

            string championName =
                await ChampionDataService.GetChampionNameAsync(
                    MainWindow.leagueClient,
                    championId);

            int kills =
                GetInt(stats, "kills");

            int deaths =
                GetInt(stats, "deaths");

            int assists =
                GetInt(stats, "assists");

            bool win =
                stats.TryGetProperty(
                    "win",
                    out JsonElement winElement) &&
                winElement.GetBoolean();

            double kda =
                deaths == 0
                    ? kills + assists
                    : (double)(kills + assists) / deaths;

            entries.Add(
                new MatchHistoryEntry
                {
                    GameId = gameId,
                    GameCreation = gameCreation,
                    Win = win,

                    Result = win ? "W" : "L",

                    ChampionName = championName,

                    KdaText =
                        $"{kills} / {deaths} / {assists}",

                    KdaRatioText =
                        $"KDA {kda:0.00}",

                    Background =
                        win
                            ? "#245A38"
                            : "#5A2929"
                });
        }

        return entries;
    }

    private static string GetGameId(JsonElement game)
    {
        if (!game.TryGetProperty("gameId", out JsonElement gameIdElement))
            return "";

        return gameIdElement.ValueKind switch
        {
            JsonValueKind.Number => gameIdElement.GetInt64().ToString(),
            JsonValueKind.String => gameIdElement.GetString() ?? "",
            _ => ""
        };
    }

    private static int GetInt(
        JsonElement element,
        string property)
    {
        if (element.TryGetProperty(
                property,
                out JsonElement value) &&
            value.TryGetInt32(out int result))
        {
            return result;
        }

        return 0;
    }

    // ------------------------------------------------------------
    // Match card interaction - opens the leagueofgraphs match page.
    // ------------------------------------------------------------

    private void MatchCard_PointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        if (sender is not Control control)
            return;

        if (control.Tag is not string gameId ||
            string.IsNullOrWhiteSpace(gameId))
        {
            return;
        }

        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
            return;

        OpenMatchUrl(gameId);
    }

    private static void OpenMatchUrl(string gameId)
    {
        string url =
            $"https://www.leagueofgraphs.com/match/na/{gameId}";

        try
        {
            Process.Start(
                new ProcessStartInfo(url)
                {
                    UseShellExecute = true
                });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Failed to open match URL '{url}': {ex}");
        }
    }
}
