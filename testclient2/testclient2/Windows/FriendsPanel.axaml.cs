using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using testclient2.League;
using testclient2.League.Models;
using testclient2.League.Models;

namespace testclient2;

public partial class FriendsPanel : Window
{
    private const double ExpandedWidth = 200;
    private const double CollapsedWidth = 45;
    public event Action<LeagueFriend?>? HistoryTargetChanged;
    private bool isCollapsed;
    private LeagueFriend? selectedFriend;
    private async void Friend_Click(
       object? sender,
       RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not LeagueFriend friend)
            return;

        selectedFriend = friend;

        HistoryTargetChanged?.Invoke(friend);

        await ShowFriendDetailsAsync(friend);
    }
    private async Task ShowFriendDetailsAsync(
LeagueFriend friend)
    {
        Debug.WriteLine(
            $"Loading match history for {friend.DisplayName}");

        //string? json = await MainWindow.leagueClient.GetFriendMatchHistoryAsync(friend.PuuId!);

        string? json = await MainWindow.leagueClient.GetFriendMatchHistoryAsync(friend.PuuId!, 0, 10);

        Debug.WriteLine("MATCH HISTORY:");
        Debug.WriteLine(json);
    }



    private void ShowFriendDetails(
    LeagueFriend friend)
    {
        Debug.WriteLine(
            $"Selected friend: {friend.DisplayName}");

        Debug.WriteLine(
            $"PUUID: {friend.PuuId}");

        Debug.WriteLine(
            $"Summoner ID: {friend.GameName}");

        Debug.WriteLine(
            $"Level: {friend.Lol?.Level}");

        Debug.WriteLine(
            $"Champion: {friend.Lol?.Skinname}");

        Debug.WriteLine(
            $"Game ID: {friend.Lol?.GameId}");
    }

    public FriendsPanel()
    {
        InitializeComponent();

        CollapseButton.Click += CollapseButton_Click;
    }
    private List<LeagueFriend> allFriends = new();

    private bool showOffline;

    public void SetFriends(
        IEnumerable<LeagueFriend> friends)
    {
        allFriends = friends.ToList();

        UpdateFriendList();
    }

    private void UpdateFriendList()
    {
        IEnumerable<LeagueFriend> visibleFriends;

        if (showOffline)
        {
            visibleFriends = allFriends;
        }
        else
        {
            visibleFriends = allFriends.Where(friend =>
                !string.Equals(
                    friend.Availability,
                    "offline",
                    StringComparison.OrdinalIgnoreCase));
        }

        visibleFriends =
            visibleFriends
                .OrderBy(friend => friend.StatusSortOrder)
                .ThenBy(friend => friend.DisplayName);

        FriendsList.ItemsSource =
            visibleFriends.ToList();
    }

    private void OfflineToggle_Click(
    object? sender,
    RoutedEventArgs e)
    {
        showOffline = OfflineToggle.IsChecked == true;

        UpdateFriendList();
    }

    private void CollapseButton_Click(
    object? sender,
    RoutedEventArgs e)
    {
        isCollapsed = !isCollapsed;

        if (isCollapsed)
        {
            Width = CollapsedWidth;

            ExpandedContent.IsVisible = false;

            CollapseButton.Content = "+";

            // Collapsed means return history to ourselves.
            HistoryTargetChanged?.Invoke(null);
        }
        else
        {
            Width = ExpandedWidth;

            ExpandedContent.IsVisible = true;

            CollapseButton.Content = "−";

            // If a friend was selected, keep showing them.
            if (selectedFriend != null)
            {
                HistoryTargetChanged?.Invoke(
                    selectedFriend);
            }
        }
    }
}