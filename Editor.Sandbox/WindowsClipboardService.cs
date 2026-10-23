using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Engine.Editor.UI;

namespace Editor.Sandbox;

internal sealed class WindowsClipboardService : IClipboardService
{
    private const uint CfUnicodeText = 13;
    private const uint GmemMoveable = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr owner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(
        uint format,
        IntPtr memory);

    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(
        uint flags,
        UIntPtr bytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr memory);

    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var bytes = Encoding.Unicode.GetBytes(text + '\0');

        var memory = GlobalAlloc(
            GmemMoveable,
            new UIntPtr((uint)bytes.Length));

        if (memory == IntPtr.Zero)
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error());
        }

        var lockedMemory = GlobalLock(memory);

        if (lockedMemory == IntPtr.Zero)
        {
            GlobalFree(memory);

            throw new Win32Exception(
                Marshal.GetLastWin32Error());
        }

        try
        {
            Marshal.Copy(
                bytes,
                0,
                lockedMemory,
                bytes.Length);
        }
        finally
        {
            GlobalUnlock(memory);
        }

        if (!OpenClipboard(IntPtr.Zero))
        {
            GlobalFree(memory);

            throw new Win32Exception(
                Marshal.GetLastWin32Error());
        }

        try
        {
            if (!EmptyClipboard())
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }

            if (SetClipboardData(CfUnicodeText, memory)
                == IntPtr.Zero)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }

            // После успеха память принадлежит Windows.
            memory = IntPtr.Zero;
        }
        finally
        {
            CloseClipboard();

            if (memory != IntPtr.Zero)
            {
                GlobalFree(memory);
            }
        }
    }
}