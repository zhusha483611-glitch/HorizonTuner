namespace HorizonTuner.Services.Handling.Abstractions;

public interface IMemoryWriter
{
    bool Write<T>(UIntPtr address, T value, bool removeWriteProtection = true) where T : unmanaged;
    bool WriteArray<T>(UIntPtr address, T[] value, bool removeWriteProtection = true) where T : unmanaged;
}
