using System;
using System.Runtime.InteropServices;
 
namespace MA_FH5Trainer.Resources.Input;
 
[Flags]
public enum XInputButtons : ushort
{
    DPadUp = 0x0001,
    DPadDown = 0x0002,
    DPadLeft = 0x0004,
    DPadRight = 0x0008,
    Start = 0x0010,
    Back = 0x0020,
    LeftThumb = 0x0040,
    RightThumb = 0x0080,
    LeftShoulder = 0x0100,
    RightShoulder = 0x0200,
    A = 0x1000,
    B = 0x2000,
    X = 0x4000,
    Y = 0x8000
}
 
[StructLayout(LayoutKind.Sequential)]
public struct XInputGamepad
{
    public XInputButtons wButtons;
    public byte bLeftTrigger;
    public byte bRightTrigger;
    public short sThumbLX;
    public short sThumbLY;
    public short sThumbRX;
    public short sThumbRY;
}
 
[StructLayout(LayoutKind.Sequential)]
public struct XInputState
{
    public uint dwPacketNumber;
    public XInputGamepad Gamepad;
}
 
public static class XInput
{
    private const uint ErrorSuccess = 0;
 
    private delegate uint XInputGetStateDelegate(uint dwUserIndex, out XInputState pState);
 
    private static readonly object LockObj = new();
    private static nint _libraryHandle;
    private static XInputGetStateDelegate? _getState;
    private static int? _cachedUserIndex;
 
    static XInput()
    {
        TryLoadXInput();
    }
 
    public static bool IsAvailable
    {
        get
        {
            lock (LockObj)
            {
                return _getState != null;
            }
        }
    }
 
    public static int? CurrentUserIndex
    {
        get
        {
            lock (LockObj)
            {
                return _cachedUserIndex;
            }
        }
    }
 
    public static bool TryGetState(out XInputState state)
    {
        lock (LockObj)
        {
            state = default;
            if (_getState == null)
            {
                return false;
            }
 
            if (_cachedUserIndex.HasValue)
            {
                var idx = (uint)_cachedUserIndex.Value;
                if (_getState(idx, out state) == ErrorSuccess)
                {
                    return true;
                }
 
                _cachedUserIndex = null;
            }
 
            for (var i = 0u; i < 4; i++)
            {
                if (_getState(i, out state) == ErrorSuccess)
                {
                    _cachedUserIndex = (int)i;
                    return true;
                }
            }
 
            return false;
        }
    }
 
    public static bool IsButtonDown(XInputState state, XInputButtons button)
    {
        return (state.Gamepad.wButtons & button) != 0;
    }
 
    private static void TryLoadXInput()
    {
        lock (LockObj)
        {
            if (_getState != null)
            {
                return;
            }
 
            var candidates = new[]
            {
                "xinput1_4.dll",
                "xinput1_3.dll",
                "xinput9_1_0.dll"
            };
 
            foreach (var dll in candidates)
            {
                if (!NativeLibrary.TryLoad(dll, out var handle))
                {
                    continue;
                }
 
                if (!NativeLibrary.TryGetExport(handle, "XInputGetState", out var export))
                {
                    NativeLibrary.Free(handle);
                    continue;
                }
 
                _libraryHandle = handle;
                _getState = Marshal.GetDelegateForFunctionPointer<XInputGetStateDelegate>(export);
                return;
            }
 
            _libraryHandle = nint.Zero;
            _getState = null;
        }
    }
}
