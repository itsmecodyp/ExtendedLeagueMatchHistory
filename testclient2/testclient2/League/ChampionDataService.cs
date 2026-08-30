using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace testclient2.League;

/// <summary>
/// Resolves champion id -> champion name using the LCU's static
/// champion-summary asset, which lists every champion (not just ones the
/// current summoner owns). Fetched once per app run and cached in memory.
/// </summary>
public static class ChampionDataService
{
    private static Dictionary<int, string>? championsById;
    private static readonly SemaphoreSlim loadLock = new(1, 1);

    public static async Task<string> GetChampionNameAsync(
        LeagueClient client,
        int championId)
    {
        if (championId <= 0)
            return "Unknown";

        await EnsureLoadedAsync(client);

        if (championsById != null &&
            championsById.TryGetValue(championId, out string? name))
        {
            return name;
        }

        return "Unknown";
    }

    private static async Task EnsureLoadedAsync(LeagueClient client)
    {
        if (championsById != null)
            return;

        await loadLock.WaitAsync();

        try
        {
            // Another caller may have populated it while we waited.
            if (championsById != null)
                return;

            Dictionary<int, string> map = new();

            string? json = await client.GetAsync(
                "/lol-game-data/assets/v1/champion-summary.json");

            if (!string.IsNullOrWhiteSpace(json))
            {
                using JsonDocument document = JsonDocument.Parse(json);

                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement champion in
                             document.RootElement.EnumerateArray())
                    {
                        if (!champion.TryGetProperty(
                                "id",
                                out JsonElement idElement))
                        {
                            continue;
                        }

                        if (!champion.TryGetProperty(
                                "name",
                                out JsonElement nameElement))
                        {
                            continue;
                        }

                        int id = idElement.GetInt32();
                        string? name = nameElement.GetString();

                        // id -1 is the "None" placeholder entry.
                        if (id > 0 && !string.IsNullOrWhiteSpace(name))
                            map[id] = name;
                    }
                }
            }

            championsById = map;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load champion data: {ex}");

            // Cache an empty map so we don't hammer the endpoint on
            // every single match history entry if the LCU isn't ready.
            championsById = new Dictionary<int, string>();
        }
        finally
        {
            loadLock.Release();
        }
    }
}
