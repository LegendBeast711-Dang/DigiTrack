using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace DigiTrack.Helpers;

/// <summary>
/// Captures system-wide keyboard input using a low-level Windows hook.
/// Only activated with explicit user consent.
/// </summary>
public sealed class GlobalKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private IntPtr _hookHandle = IntPtr.Zero;
    private readonly LowLevelKeyboardProc _proc;
    private bool _disposed;

    public event EventHandler<GlobalKeyEventArgs>? KeyCaptured;
    public bool IsActive => _hookHandle != IntPtr.Zero;

    public GlobalKeyboardHook()
    {
        _proc = HookCallback;
    }

    public void Start()
    {
        if (_hookHandle != IntPtr.Zero) return;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
            GetModuleHandle(curModule.ModuleName), 0);

        if (_hookHandle == IntPtr.Zero)
            throw new InvalidOperationException(
                $"Failed to install keyboard hook. Error: {Marshal.GetLastWin32Error()}");
    }

    public void Stop()
    {
        if (_hookHandle == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var vkCode = (Keys)hookStruct.vkCode;

            // GetKeyboardState() is thread-local and returns wrong results when another
            // application has focus. Use GetAsyncKeyState for pressed keys and GetKeyState
            // for toggle keys (CapsLock/NumLock) to get accurate cross-window translation.
            var keyboardState = new byte[256];
            if ((GetAsyncKeyState((int)Keys.ShiftKey) & 0x8000) != 0)
                keyboardState[(int)Keys.ShiftKey] = 0x80;
            if ((GetAsyncKeyState((int)Keys.LShiftKey) & 0x8000) != 0)
                keyboardState[(int)Keys.LShiftKey] = 0x80;
            if ((GetAsyncKeyState((int)Keys.RShiftKey) & 0x8000) != 0)
                keyboardState[(int)Keys.RShiftKey] = 0x80;
            if ((GetAsyncKeyState((int)Keys.ControlKey) & 0x8000) != 0)
                keyboardState[(int)Keys.ControlKey] = 0x80;
            if ((GetAsyncKeyState((int)Keys.Menu) & 0x8000) != 0)
                keyboardState[(int)Keys.Menu] = 0x80;
            if ((GetKeyState((int)Keys.CapsLock) & 1) != 0)
                keyboardState[(int)Keys.CapsLock] = 0x01;
            if ((GetKeyState((int)Keys.NumLock) & 1) != 0)
                keyboardState[(int)Keys.NumLock] = 0x01;

            var sb = new StringBuilder(4);
            int result = ToUnicode(hookStruct.vkCode, hookStruct.scanCode,
                keyboardState, sb, sb.Capacity, 0);

            string? character = result > 0 ? sb.ToString() : null;
            KeyCaptured?.Invoke(this, new GlobalKeyEventArgs(vkCode, character));
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }

    // ── Win32 P/Invoke ────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
        IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
        StringBuilder pwszBuff, int cchBuff, uint wFlags);
}

public class GlobalKeyEventArgs : EventArgs
{
    public Keys Key { get; }
    public string? Character { get; }

    public GlobalKeyEventArgs(Keys key, string? character)
    {
        Key = key;
        Character = character;
    }
}
