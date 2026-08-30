using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace testclient2;

using System.Text.Json;
using System.Threading.Tasks;
using testclient2.League;
using testclient2.League.Models;
using testclient2.Windows;

public partial class MainWindow : Window
{
    private ClientBorder? clientBorder;
    private FriendsPanel? friendsPanel;
    private FriendsService? friendsService;
    public static LeagueClient leagueClient;
    private MatchHistoryPanel? matchHistoryPanel;

    private string? ownPuuid;
    private string ownDisplayName = "Me";

    private readonly DispatcherTimer timer;

    private IntPtr lastForegroundWindow = IntPtr.Zero;

    private const int ExtraWidth = 400;

    public MainWindow()
    {
        InitializeComponent();
        Debug.WriteLine("MainWindow initialized");
        leagueClient = new LeagueClient();
        _ = TestLeagueConnectionAsync();
        // We don't want MainWindow itself visible.
        ShowInTaskbar = false;

        clientBorder = new ClientBorder();
        friendsPanel = new FriendsPanel();
        matchHistoryPanel = new MatchHistoryPanel();

        friendsPanel.HistoryTargetChanged +=
            FriendsPanel_HistoryTargetChanged;

        clientBorder.Show();
        friendsPanel.Show();
        matchHistoryPanel.Show();
        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };

        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private async void FriendsPanel_HistoryTargetChanged(
    LeagueFriend? friend)
    {
        if (matchHistoryPanel == null)
            return;

        if (friend != null)
        {
            if (string.IsNullOrWhiteSpace(friend.PuuId))
                return;

            await matchHistoryPanel.LoadHistoryAsync(
                friend.PuuId,
                friend.DisplayName);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(ownPuuid))
                return;

            await matchHistoryPanel.LoadHistoryAsync(
                ownPuuid,
                ownDisplayName);
        }
    }

    private async Task TestLeagueConnectionAsync()
    {
        Debug.WriteLine("Starting League connection test...");

        bool connected =
            await leagueClient.ConnectAsync();

        if (!connected)
        {
            Debug.WriteLine(
                "Could not connect to League LCU.");

            return;
        }

        Debug.WriteLine(
            "Connected to League LCU!");
        string? summonerJson =
    await leagueClient.GetCurrentSummonerAsync();

        if (!string.IsNullOrWhiteSpace(summonerJson))
        {
            using JsonDocument document =
                JsonDocument.Parse(summonerJson);

            JsonElement root =
                document.RootElement;

            if (root.TryGetProperty(
                    "puuid",
                    out JsonElement puuidElement))
            {
                ownPuuid = puuidElement.GetString();
            }

            if (root.TryGetProperty(
                    "gameName",
                    out JsonElement gameNameElement))
            {
                ownDisplayName =
                    gameNameElement.GetString() ?? "Me";
            }
        }
        friendsService = new FriendsService(
            leagueClient);

        var friends =
    await friendsService.GetFriendsAsync();

        friendsPanel?.SetFriends(friends);

        Debug.WriteLine(
            $"Friends returned: {friends.Count}");

        // Initial history target = ourselves.
        if (!string.IsNullOrWhiteSpace(ownPuuid))
        {
            await matchHistoryPanel!.LoadHistoryAsync(
                ownPuuid,
                ownDisplayName);
        }
    }
    private const int HistoryHeight = 140;
    private void Timer_Tick(object? sender, EventArgs e)
    {
        IntPtr leagueWindow = FindWindow(
            null,
            "League of Legends");

        if (leagueWindow == IntPtr.Zero)
        {
            clientBorder?.Hide();
            friendsPanel?.Hide();
            matchHistoryPanel?.Hide();
            return;
        }

        if (!GetWindowRect(
                leagueWindow,
                out RECT rect))
        {
            clientBorder?.Hide();
            friendsPanel?.Hide();
            matchHistoryPanel?.Hide();
            return;
        }

        int leagueWidth = rect.Right - rect.Left;
        int leagueHeight = rect.Bottom - rect.Top;

        // ------------------------------------------------
        // Client border
        // ------------------------------------------------

        if (clientBorder != null)
        {
            clientBorder.Position = new PixelPoint(
                rect.Left,
                rect.Top);

            clientBorder.Width = leagueWidth;
            clientBorder.Height = leagueHeight;
        }

        // ------------------------------------------------
        // Friends panel
        // ------------------------------------------------

        if (friendsPanel != null)
        {
            friendsPanel.Position = new PixelPoint(
                rect.Right,
                rect.Top);

            friendsPanel.Height = leagueHeight;
        }

        // ------------------------------------------------
        // Focus
        // ------------------------------------------------

        bool active = IsLeagueOrPanelFocused(
            leagueWindow);

        if (active)
        {
            clientBorder?.Show();
            friendsPanel?.Show();
            matchHistoryPanel?.Show();
        }
        else
        {
            clientBorder?.Hide();
            friendsPanel?.Hide();
            matchHistoryPanel?.Hide();
        }
        // ------------------------------------------------
        // Match history
        // ------------------------------------------------

        if (matchHistoryPanel != null)
        {
            int friendsWidth =
                friendsPanel?.Width > 100
                    ? 200
                    : 45;

            matchHistoryPanel.Position = new PixelPoint(
      rect.Left,
      rect.Bottom);

            matchHistoryPanel.Width =
                leagueWidth + friendsWidth;

            matchHistoryPanel.Height =
                HistoryHeight;
        }
    }


    private bool IsLeagueOrPanelFocused(
        IntPtr leagueWindow)
    {
        IntPtr foregroundWindow =
            GetForegroundWindow();

        if (foregroundWindow == lastForegroundWindow)
        {
            return clientBorder?.IsVisible == true;
        }

        lastForegroundWindow = foregroundWindow;

        // Friends panel
        if (friendsPanel != null &&
            foregroundWindow ==
                friendsPanel.TryGetPlatformHandle()?.Handle)
        {
            return true;
        }

        // Match history panel
        if (matchHistoryPanel != null &&
            foregroundWindow ==
                matchHistoryPanel.TryGetPlatformHandle()?.Handle)
        {
            return true;
        }

        GetWindowThreadProcessId(
            foregroundWindow,
            out uint foregroundProcessId);

        try
        {
            using Process process =
                Process.GetProcessById(
                    (int)foregroundProcessId);

            string processName =
                process.ProcessName;

            if (processName.Equals(
                    "LeagueClientUx",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (processName.Equals(
                    "explorer",
                    StringComparison.OrdinalIgnoreCase) ||
                processName.Equals(
                    "ShellExperienceHost",
                    StringComparison.OrdinalIgnoreCase) ||
                processName.Equals(
                    "StartMenuExperienceHost",
                    StringComparison.OrdinalIgnoreCase))
            {
                return clientBorder?.IsVisible == true;
            }
        }
        catch
        {
            return clientBorder?.IsVisible == true;
        }

        return false;
    }


    // ----------------------------------------------------
    // Win32
    // ----------------------------------------------------

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(
        string? lpClassName,
        string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(
        IntPtr hWnd,
        out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(
        IntPtr hWnd,
        out uint processId);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}