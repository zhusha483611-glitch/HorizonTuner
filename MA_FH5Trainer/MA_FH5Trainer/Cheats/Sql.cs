using System.Text;
using HorizonTuner.Resources;
using Memory;
using static HorizonTuner.Resources.Memory;

namespace HorizonTuner.Cheats.ForzaHorizon5;

public class Sql : CheatsUtilities, ICheatsBase, ISqlCheat
{
    private UIntPtr _cDatabaseAddress, _ptr;
    public bool WereScansSuccessful { get; private set; }

    public async Task SqlExecAobScan()
    {
        WereScansSuccessful = false;
        _cDatabaseAddress = 0;
        _ptr = 0;

        const string sig = "0F 84 ? ? ? ? 48 8B 35 ? ? ? ? 48 85 F6 74";
        _cDatabaseAddress = await SmartAobScan(sig);

        if (_cDatabaseAddress > 0)
        {
            var relativeAddress = _cDatabaseAddress + 0x6 + 0x3;
            var relative = GetInstance().ReadMemory<int>(relativeAddress);
            var pCDataBaseAddress = _cDatabaseAddress + (nuint)relative + 0x6 + 0x7;
            _ptr = GetInstance().ReadMemory<nuint>(pCDataBaseAddress);
            WereScansSuccessful = true;
            return;
        }

        ShowError("SQL", sig);
    }
    
    private static nuint GetVirtualFunctionPtr(nuint ptr, int index)
    {
        var mem = GetInstance();
        var pVTable = mem.ReadMemory<UIntPtr>(ptr);
        var lpBaseAddress = pVTable + (nuint)nuint.Size * (nuint)index;
        var result = mem.ReadMemory<UIntPtr>(lpBaseAddress);
        return result;
    }
    
    public async Task QueryAsync(string command)
    {
        var memory = GetInstance();
        var procHandle = memory.MProc.Handle;

        if (_ptr == 0)
        {
            await SqlExecAobScan();
        }

        if (_ptr == 0)
        {
            ShowError("SQL", "_ptr == 0");
            return;
        }

        var rcx = _ptr;
        const int virtualFunctionIndex = 9;
        var callFunction = GetVirtualFunctionPtr(_ptr, virtualFunctionIndex);
        if (callFunction <= 0)
        {
            ShowError("SQL", "callFunction <= 0");
            return;
        }

        var mainMod = memory.MProc.Process.MainModule;
        if (mainMod == null)
        {
            ShowError("SQL", "mainMod == null");
            return;
        }

        // The command is written into a 0x1000-byte remote buffer (r8) as UTF-8.
        // Reject commands that would overflow it (buffer safety + defense-in-depth
        // against any future caller that builds SQL from untrusted input).
        if (Encoding.UTF8.GetByteCount(command) + 1 > 0x1000)
        {
            ShowError("SQL", "command too long for remote buffer");
            return;
        }

        using var cleanup = new CleanupScope();

        var shellCodeAddress = (UIntPtr)mainMod.BaseAddress + 0x1000;
        var jmpShellcodeaddr = Imps.VirtualAllocEx(procHandle, 0, 0x1000, 0x3000, 0x40);
        if (jmpShellcodeaddr != UIntPtr.Zero)
        {
            cleanup.Push(() => Free(jmpShellcodeaddr));
        }

        var rdx = Imps.VirtualAllocEx(procHandle, 0, 0x1000, 0x3000, 0x40);
        if (rdx != UIntPtr.Zero)
        {
            cleanup.Push(() => Free(rdx));
        }

        var r8 = Imps.VirtualAllocEx(procHandle, 0, 0x1000, 0x3000, 0x40);
        if (r8 != UIntPtr.Zero)
        {
            cleanup.Push(() => Free(r8));
        }

        var rcxBytes = BitConverter.GetBytes(rcx.ToUInt64());
        var rdxBytes = BitConverter.GetBytes(rdx.ToUInt64());
        var r8Bytes = BitConverter.GetBytes(r8.ToUInt64());
        var callBytes = BitConverter.GetBytes(callFunction.ToUInt64());

        byte[] shellCode =
        [
            0x51, 0x52, 0x41, 0x50, 0x41, 0x51, 0x48, 0x83, 0xEC, 0x28, 0x48, 0xB9, rcxBytes[0], rcxBytes[1], rcxBytes[2], rcxBytes[3], rcxBytes[4], rcxBytes[5], rcxBytes[6], rcxBytes[7], 0x48, 0xBA, rdxBytes[0], rdxBytes[1], rdxBytes[2], rdxBytes[3],
            rdxBytes[4], rdxBytes[5], rdxBytes[6], rdxBytes[7], 0x49, 0xB8, r8Bytes[0], r8Bytes[1], r8Bytes[2],
            r8Bytes[3], r8Bytes[4], r8Bytes[5], r8Bytes[6], r8Bytes[7], 0xFF, 0x15, 0x02, 0x00, 0x00, 0x00, 0xEB, 0x08, callBytes[0], callBytes[1], callBytes[2], callBytes[3], callBytes[4], callBytes[5],
            callBytes[6], callBytes[7], 0x48, 0x83, 0xC4, 0x28, 0x41, 0x59, 0x41, 0x58, 0x5A, 0x59, 0xC3
        ];

        shellCodeAddress -= (UIntPtr)shellCode.Length;
        if (!memory.ChangeProtection(shellCodeAddress, Imps.MemoryProtection.ExecuteReadWrite, out var old))
        {
            ShowError("SQL", "memory.ChangeProtection(shellCodeAddress, Imps.MemoryProtection.ExecuteReadWrite, out var old)");
            return;
        }

        cleanup.Push(() =>
        {
            _ = memory.ChangeProtection(shellCodeAddress, old, out _);
        });

        memory.WriteStringMemory(r8, command + "\0");
        memory.WriteArrayMemory(shellCodeAddress, shellCode);

        var jmpBytes = BitConverter.GetBytes(shellCodeAddress.ToUInt64());
        byte[] jmpShellcode =
        [
            0xFF, 0x25, 0x00, 0x00, 0x00, 0x00, jmpBytes[0], jmpBytes[1], jmpBytes[2], jmpBytes[3],
            jmpBytes[4], jmpBytes[5], jmpBytes[6], jmpBytes[7]
        ];

        memory.WriteArrayMemory(jmpShellcodeaddr, jmpShellcode);
        var thread = Imports.CreateRemoteThread(procHandle, 0, 0, jmpShellcodeaddr, rcx, 0, out _);
        if (thread is 0 or -1)
        {
            ShowError("SQL", "thread == 0 || thread == -1");
            return;
        }

        cleanup.Push(() => Imports.CloseHandle(thread));
        _ = Imports.WaitForSingleObject(thread, int.MaxValue);
    }

    public void Cleanup()
    {
    }

    public void Reset()
    {
        WereScansSuccessful = false;
        var fields = typeof(Sql).GetFields().Where(f => f.FieldType == typeof(UIntPtr));
        foreach (var field in fields)
        {
            field.SetValue(this, UIntPtr.Zero);
        }
    }
}