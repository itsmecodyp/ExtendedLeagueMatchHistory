using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using testclient2.League.Models;

namespace testclient2.League;

/// <summary>
/// Reads/writes the per-player match history cache and merges freshly
/// fetched entries into it, skipping anything already on disk (by GameId).
/// </summary>
public static class MatchHistoryCacheService
{
    private static readonly string CacheDirectory =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "testclient2",
            "MatchHistoryCache");

    private static string GetCachePath(string puuid) =>
        Path.Combine(CacheDirectory, $"{puuid}.json");

    public static async Task<MatchHistoryCacheData> LoadAsync(string puuid)
    {
        string path = GetCachePath(puuid);

        if (!File.Exists(path))
            return new MatchHistoryCacheData();

        try
        {
            string json = await File.ReadAllTextAsync(path);

            return JsonSerializer.Deserialize<MatchHistoryCacheData>(
                       json,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       })
                   ?? new MatchHistoryCacheData();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Failed to load match history cache for {puuid}: {ex}");

            // Corrupt/unreadable cache shouldn't crash the app - start fresh.
            return new MatchHistoryCacheData();
        }
    }

    public static async Task SaveAsync(string puuid, MatchHistoryCacheData data)
    {
        try
        {
            Directory.CreateDirectory(CacheDirectory);

            string json = JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(GetCachePath(puuid), json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Failed to save match history cache for {puuid}: {ex}");
        }
    }

    /// <summary>
    /// Adds any entries from <paramref name="newEntries"/> that aren't
    /// already present (matched by GameId), then re-sorts the cache
    /// newest-first. Returns the number of entries actually added.
    /// </summary>
    public static int MergeEntries(
        MatchHistoryCacheData cache,
        IEnumerable<MatchHistoryEntry> newEntries)
    {
        HashSet<string> existingIds = cache.Entries
            .Where(entry => !string.IsNullOrEmpty(entry.GameId))
            .Select(entry => entry.GameId)
            .ToHashSet();

        int added = 0;

        foreach (MatchHistoryEntry entry in newEntries)
        {
            if (string.IsNullOrEmpty(entry.GameId))
                continue;

            if (!existingIds.Add(entry.GameId))
                continue;

            cache.Entries.Add(entry);
            added++;
        }

        cache.Entries = cache.Entries
            .OrderByDescending(entry => entry.GameCreation)
            .ToList();

        return added;
    }
}
