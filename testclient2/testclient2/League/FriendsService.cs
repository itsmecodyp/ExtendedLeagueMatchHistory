using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using testclient2.League;
using testclient2.League.Models;
using testclient2.League.Models;

namespace testclient2.League;

public class FriendsService
{
    private readonly LeagueClient client;

    public FriendsService(LeagueClient client)
    {
        this.client = client;
    }

    public async Task<List<LeagueFriend>> GetFriendsAsync()
    {
        string? json =
            await client.GetAsync(
                "/lol-chat/v1/friends");

        if (string.IsNullOrWhiteSpace(json))
            return new List<LeagueFriend>();

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(json);

            if (document.RootElement.ValueKind ==
                JsonValueKind.Array)
            {
                foreach (JsonElement friend in
                         document.RootElement.EnumerateArray())
                {
                    string? gameName = null;

                    if (friend.TryGetProperty(
                            "gameName",
                            out JsonElement gameNameElement))
                    {
                        gameName =
                            gameNameElement.GetString();
                    }

                    Debug.WriteLine(
                        $"===== {gameName ?? "Unknown"} =====");

                    foreach (JsonProperty property in
                             friend.EnumerateObject())
                    {
                        Debug.WriteLine(
                            $"{property.Name}: {property.Value}");
                    }
                }
            }

            return JsonSerializer.Deserialize<List<LeagueFriend>>(
                       json,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       })
                   ?? new List<LeagueFriend>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Friend JSON error: {ex}");

            return new List<LeagueFriend>();
        }
    }
}