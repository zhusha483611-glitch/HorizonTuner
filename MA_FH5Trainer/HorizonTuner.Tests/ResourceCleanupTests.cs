using HorizonTuner.Cheats;
using HorizonTuner.Resources;
using Memory;

namespace HorizonTuner.Tests;

public class ResourceCleanupTests
{
    [Fact]
    public void EnableSeDebugPrivilegeCore_LookupFailure_ClosesTokenHandle()
    {
        var api = new FakePrivilegeApi
        {
            IsAdministratorResult = true,
            OpenProcessTokenResult = true,
            TokenHandle = new IntPtr(123),
            LookupPrivilegeValueResult = false
        };

        var ok = SeDebugPrivilegeHelper.Enable(api);

        Assert.False(ok);
        Assert.Equal(1, api.CloseHandleCalls);
        Assert.Equal(new IntPtr(123), api.LastClosedHandle);
    }

    [Fact]
    public void EnableSeDebugPrivilegeCore_Success_ClosesTokenHandle()
    {
        var api = new FakePrivilegeApi
        {
            IsAdministratorResult = true,
            OpenProcessTokenResult = true,
            TokenHandle = new IntPtr(456),
            LookupPrivilegeValueResult = true,
            AdjustTokenPrivilegesResult = true
        };

        var ok = SeDebugPrivilegeHelper.Enable(api);

        Assert.True(ok);
        Assert.Equal(1, api.CloseHandleCalls);
        Assert.Equal(new IntPtr(456), api.LastClosedHandle);
    }

    [Fact]
    public void CleanupAndResetAll_CallsCleanupThenResetForCheats()
    {
        var cheat = new FakeCheat();

        CheatLifecycle.CleanupAndResetAll([cheat, new object()]);

        Assert.Equal(["Cleanup", "Reset"], cheat.Calls);
    }

    private sealed class FakePrivilegeApi : ISeDebugPrivilegeApi
    {
        public bool IsAdministratorResult { get; set; }
        public bool OpenProcessTokenResult { get; set; }
        public bool LookupPrivilegeValueResult { get; set; }
        public bool AdjustTokenPrivilegesResult { get; set; }
        public IntPtr TokenHandle { get; set; }
        public int CloseHandleCalls { get; private set; }
        public IntPtr LastClosedHandle { get; private set; }

        public bool IsAdministrator() => IsAdministratorResult;
        public IntPtr GetCurrentProcess() => IntPtr.Zero;

        public bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle)
        {
            tokenHandle = TokenHandle;
            return OpenProcessTokenResult;
        }

        public bool LookupPrivilegeValue(string? systemName, string name, out Mem.LUID luid)
        {
            luid = default;
            return LookupPrivilegeValueResult;
        }

        public bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAllPrivileges, ref Mem.TOKEN_PRIVILEGES newState, uint bufferLength, IntPtr previousState, IntPtr returnLength)
        {
            return AdjustTokenPrivilegesResult;
        }

        public bool CloseHandle(IntPtr handle)
        {
            CloseHandleCalls++;
            LastClosedHandle = handle;
            return true;
        }
    }

    private sealed class FakeCheat : ICheatsBase
    {
        public List<string> Calls { get; } = [];

        public void Cleanup() => Calls.Add("Cleanup");
        public void Reset() => Calls.Add("Reset");
    }
}
