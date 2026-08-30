using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using testclient2.Windows;
using System;
using testclient2.Windows;

namespace testclient2;

public partial class ClientBorder : Window
{
    public ClientBorder()
    {
        InitializeComponent();

        Opened += ClientBorder_Opened;
    }

    private void ClientBorder_Opened(
        object? sender,
        EventArgs e)
    {
        var handle = TryGetPlatformHandle();

        if (handle != null)
        {
            NativeWindowHelper.MakeClickThrough(
                handle.Handle);
        }
    }
}