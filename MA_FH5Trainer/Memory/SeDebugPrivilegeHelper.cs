using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Memory;

public interface ISeDebugPrivilegeApi
{
    bool IsAdministrator();
    IntPtr GetCurrentProcess();
    bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);
    bool LookupPrivilegeValue(string? systemName, string name, out Mem.LUID luid);
    bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAllPrivileges, ref Mem.TOKEN_PRIVILEGES newState, uint bufferLength, IntPtr previousState, IntPtr returnLength);
    bool CloseHandle(IntPtr handle);
}

public static class SeDebugPrivilegeHelper
{
    private const uint TokenAdjustPrivileges = 0x0020;
    private const uint TokenQuery = 0x0008;
    private const string SeDebugName = "SeDebugPrivilege";
    private const uint SePrivilegeEnabled = 0x00000002;

    public static bool Enable(ISeDebugPrivilegeApi api)
    {
        ArgumentNullException.ThrowIfNull(api);

        if (!api.IsAdministrator())
        {
            return false;
        }

        if (!api.OpenProcessToken(api.GetCurrentProcess(), TokenAdjustPrivileges | TokenQuery, out var tokenHandle))
        {
            return false;
        }

        try
        {
            if (!api.LookupPrivilegeValue(null, SeDebugName, out var luid))
            {
                return false;
            }

            var tp = new Mem.TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SePrivilegeEnabled
            };

            return api.AdjustTokenPrivileges(tokenHandle, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            api.CloseHandle(tokenHandle);
        }
    }

    private sealed class Win32SeDebugPrivilegeApi : ISeDebugPrivilegeApi
    {
        public bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public IntPtr GetCurrentProcess() => GetCurrentProcessNative();

        public bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle)
            => OpenProcessTokenNative(processHandle, desiredAccess, out tokenHandle);

        public bool LookupPrivilegeValue(string? systemName, string name, out Mem.LUID luid)
            => LookupPrivilegeValueNative(systemName, name, out luid);

        public bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAllPrivileges, ref Mem.TOKEN_PRIVILEGES newState, uint bufferLength, IntPtr previousState, IntPtr returnLength)
            => AdjustTokenPrivilegesNative(tokenHandle, disableAllPrivileges, ref newState, bufferLength, previousState, returnLength);

        public bool CloseHandle(IntPtr handle) => CloseHandleNative(handle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessTokenNative(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LookupPrivilegeValueNative(string? systemName, string name, out Mem.LUID luid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivilegesNative(
            IntPtr tokenHandle,
            bool disableAllPrivileges,
            ref Mem.TOKEN_PRIVILEGES newState,
            uint bufferLength,
            IntPtr previousState,
            IntPtr returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcessNative();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandleNative(IntPtr handle);
    }

    public static bool Enable()
    {
        return Enable(new Win32SeDebugPrivilegeApi());
    }
}
