using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using testclient2.League.Models;
namespace testclient2.League;

public class LeagueClient
{
    private HttpClient? httpClient;

    public bool IsConnected => httpClient != null;

    public async Task<bool> ConnectAsync()
    {
        Process? process = FindLeagueClient();

        if (process == null)
            return false;

        if (!TryGetClientArguments(
                process,
                out int port,
                out string? authToken))
        {
            return false;
        }

        httpClient = new HttpClient(
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    (_, _, _, _) => true
            });

        string credentials =
            Convert.ToBase64String(
                Encoding.ASCII.GetBytes(
                    $"riot:{authToken}"));

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Basic",
                credentials);

        httpClient.BaseAddress =
            new Uri($"https://127.0.0.1:{port}");

        try
        {
            HttpResponseMessage response =
                await httpClient.GetAsync(
                    "/lol-summoner/v1/current-summoner");

            return response.IsSuccessStatusCode;
        }
        catch
        {
            httpClient.Dispose();
            httpClient = null;

            return false;
        }
    }

    public async Task<string?> GetCurrentSummonerAsync()
    {
        return await GetAsync(
            "/lol-summoner/v1/current-summoner");
    }

    public async Task<string?> GetAsync(string endpoint)
    {
        if (httpClient == null)
        {
            Debug.WriteLine("GET failed: HTTP client is null.");
            return null;
        }

        try
        {
            Debug.WriteLine($"GET {endpoint}");

            HttpResponseMessage response =
                await httpClient.GetAsync(endpoint);

            Debug.WriteLine(
                $"Status: {(int)response.StatusCode} {response.StatusCode}");

            string content =
                await response.Content.ReadAsStringAsync();

            Debug.WriteLine(
                $"Response length: {content.Length}");

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine(
                    $"Response body: {content}");

                return null;
            }

            return content;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"GET {endpoint} threw an exception:");

            Debug.WriteLine(ex.ToString());

            return null;
        }
    }
    public async Task<string?> GetFriendMatchHistoryAsync(
        string puuid,
        int beginIndex = 0,
        int endIndex = 10)
    {
        return await GetAsync(
            $"/lol-match-history/v1/products/lol/{puuid}/matches" +
            $"?begIndex={beginIndex}&endIndex={endIndex}");
    }

    public async Task<string?> GetGameTimelineAsync(
    string gameId)
    {
        return await GetAsync(
            $"/lol-match-history/v1/game-timelines/{gameId}");
    }

    private static Process? FindLeagueClient()
    {
        Process[] processes =
            Process.GetProcessesByName(
                "LeagueClientUx");

        return processes.Length > 0
            ? processes[0]
            : null;
    }

    private static bool TryGetClientArguments(
        Process process,
        out int port,
        out string? authToken)
    {
        port = 0;
        authToken = null;

        try
        {
            string? commandLine =
                GetCommandLine(process);

            if (string.IsNullOrWhiteSpace(commandLine))
                return false;

            const string portPrefix =
                "--app-port=";

            const string tokenPrefix =
                "--remoting-auth-token=";

            int portIndex =
                commandLine.IndexOf(
                    portPrefix,
                    StringComparison.OrdinalIgnoreCase);

            if (portIndex >= 0)
            {
                int start =
                    portIndex + portPrefix.Length;

                int end =
                    commandLine.IndexOf(
                        ' ',
                        start);

                string value =
                    end >= 0
                        ? commandLine[start..end]
                        : commandLine[start..];

                int.TryParse(
                    value.Trim('"'),
                    out port);
            }

            int tokenIndex =
                commandLine.IndexOf(
                    tokenPrefix,
                    StringComparison.OrdinalIgnoreCase);

            if (tokenIndex >= 0)
            {
                int start =
                    tokenIndex + tokenPrefix.Length;

                int end =
                    commandLine.IndexOf(
                        ' ',
                        start);

                authToken =
                    (end >= 0
                        ? commandLine[start..end]
                        : commandLine[start..])
                    .Trim('"');
            }

            return port != 0 &&
                   !string.IsNullOrWhiteSpace(authToken);
        }
        catch
        {
            return false;
        }
    }

    private static string? GetCommandLine(
        Process process)
    {
        try
        {
            using ManagementObjectSearcher searcher =
                new ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");

            foreach (ManagementObject result in searcher.Get())
            {
                return result["CommandLine"]?.ToString();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Failed to get League command line: {ex}");
        }

        return null;
    }
}