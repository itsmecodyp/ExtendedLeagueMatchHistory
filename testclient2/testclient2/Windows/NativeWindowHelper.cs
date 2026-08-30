using System;
using System.Runtime.InteropServices;

namespace testclient2.Windows;

public static class NativeWindowHelper
{
    private const int GWL_EXSTYLE = -20;

    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(
        IntPtr hWnd,
        int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(
        IntPtr hWnd,
        int nIndex,
        int dwNewLong);

    public static void MakeClickThrough(
        IntPtr handle)
    {
        int style = GetWindowLong(
            handle,
            GWL_EXSTYLE);

        SetWindowLong(
            handle,
            GWL_EXSTYLE,
            style |
            WS_EX_LAYERED |
            WS_EX_TRANSPARENT |
            WS_EX_NOACTIVATE);
    }
}