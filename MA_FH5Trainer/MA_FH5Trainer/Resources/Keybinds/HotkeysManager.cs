using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace HorizonTuner.Resources.Keybinds;

public static partial class HotkeysManager
{
    private static readonly HotkeyRegistry s_hotkeys = new();
    private static readonly AsyncSingleRunner s_checkRunner = new();
    private static readonly LowLevelKeyboardProc s_lowLevelProc = HookCallback;
    private static IntPtr s_hookId = IntPtr.Zero;
    private static readonly object s_hookLock = new object();
    private const int MAX_HOOK_RETRIES = 3;

    public static void SaveAll()
    {
        foreach (var hotkey in s_hotkeys.Snapshot())
        {
            hotkey.Save();
        }
    }

    /// <summary>
    /// Sets up the system hook to capture keyboard events
    /// </summary>
    /// <returns>True if the hook was successfully set up, false otherwise</returns>
    public static bool SetupSystemHook()
    {
        lock (s_hookLock)
        {
            if (s_hookId != IntPtr.Zero)
            {
                return true;
            }

            return AttemptHookSetup();
        }
    }

    private static bool AttemptHookSetup()
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MAX_HOOK_RETRIES; attempt++)
        {
            try
            {
                if (s_hookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(s_hookId);
                    s_hookId = IntPtr.Zero;
                }

                s_hookId = SetHook(s_lowLevelProc);
                if (s_hookId != IntPtr.Zero)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        if (lastException != null)
        {
            MessageBox.Show($"Exception setting up hotkeys: {lastException.Message}",
                "HorizonTuner - Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        MessageBox.Show($"Failed to setup hotkeys after {MAX_HOOK_RETRIES} attempts. Hotkeys will not work!\n\nLast error: {GetLastWin32ErrorMessage()}",
            "HorizonTuner - Error", MessageBoxButton.OK, MessageBoxImage.Error);
        return false;
    }

    /// <summary>
    /// Gets a human-readable message for the last Win32 error
    /// </summary>
    private static string GetLastWin32ErrorMessage()
    {
        int error = Marshal.GetLastWin32Error();
        if (error == 0)
        {
            return "No error";
        }

        return new Win32Exception(error).Message;
    }

    /// <summary>
    /// Shuts down the system hook and releases resources
    /// </summary>
    public static bool ShutdownSystemHook()
    {
        lock (s_hookLock)
        {
            if (s_hookId == IntPtr.Zero)
            {
                return true;
            }

            try
            {
                bool result = UnhookWindowsHookEx(s_hookId);
                int error = Marshal.GetLastWin32Error();
                    
                if (!result && error != 0)
                {
                    return false;
                }
                    
                s_hookId = IntPtr.Zero;
                return true;
            }
            catch (Exception)
            {
                s_hookId = IntPtr.Zero;
                return false;
            }
        }
    }

    public static void Register(GlobalHotkey hotkey)
    {
        ArgumentNullException.ThrowIfNull(hotkey);
        if (s_hotkeys.Register(hotkey))
        {
            hotkey.Load();
        }
    }

    public static bool CheckExists(Key key, ModifierKeys modifierKeys)
    {
        return s_hotkeys.CheckExists(key, modifierKeys);
    }

    private static Task CheckHotkeysAsync()
    {
        return s_checkRunner.RunAsync(async () =>
        {
            if (Application.Current == null)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(ExecuteHotkeysOnUiAsync).Task.Unwrap();
        });
    }

    private static async Task ExecuteHotkeysOnUiAsync()
    {
        foreach (var hotkey in s_hotkeys.Snapshot())
        {
            if (Keyboard.Modifiers != hotkey.Modifier || hotkey.Key == Key.None || !hotkey.CanExecute)
            {
                continue;
            }

            if (hotkey.IsPressed)
            {
                continue;
            }

            hotkey.IsPressed = true;
            try
            {
                while (Keyboard.IsKeyDown(hotkey.Key))
                {
                    hotkey.Callback();
                    await Task.Delay(hotkey.Interval);
                }
            }
            finally
            {
                hotkey.IsPressed = false;
            }
        }
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
            
        if (curModule == null)
        {
            return IntPtr.Zero;
        }
            
        IntPtr moduleHandle = GetModuleHandle(curModule.ModuleName);
        if (moduleHandle == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }
            
        const int WH_KEYBOARD_LL = 13;
        IntPtr hookId = SetWindowsHookEx(WH_KEYBOARD_LL, proc, moduleHandle, 0);
        return hookId;
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
        {
            return CallNextHookEx(s_hookId, nCode, wParam, lParam);
        }
            
        const int WM_KEYDOWN = 0x0100;
        const int WM_SYSKEYDOWN = 0x0104;
                
        int msgType = wParam.ToInt32();
        if (msgType != WM_KEYDOWN && msgType != WM_SYSKEYDOWN)
        {
            return CallNextHookEx(s_hookId, nCode, wParam, lParam);
        }

        _ = CheckHotkeysAsync();

        return CallNextHookEx(s_hookId, nCode, wParam, lParam);
    }
        
    #region Native Methods

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
    private static partial IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnhookWindowsHookEx(IntPtr hhk);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr GetModuleHandle(string lpModuleName);

    #endregion
}