using MA_FH5Trainer.Services.Handling.Abstractions;
using static MA_FH5Trainer.Resources.Memory;

namespace MA_FH5Trainer.Services.Handling.Implementations;

public sealed class DefaultMemoryWriter : IMemoryWriter
{
    public bool Write<T>(UIntPtr address, T value, bool removeWriteProtection = true) where T : unmanaged
    {
        return GetInstance().WriteMemory((nuint)address, value, removeWriteProtection);
    }

    public bool WriteArray<T>(UIntPtr address, T[] value, bool removeWriteProtection = true) where T : unmanaged
    {
        return GetInstance().WriteArrayMemory((nuint)address, value, removeWriteProtection);
    }
}
