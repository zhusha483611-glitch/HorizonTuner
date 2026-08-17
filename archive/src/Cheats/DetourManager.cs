using System.Collections.Concurrent;
using Memory;
using static MA_FH5Trainer.Resources.Memory;

namespace MA_FH5Trainer.Cheats;

/// <summary>
/// Detour 管理器，用于管理内存 hook 和 detour 操作
/// </summary>
public class DetourManager
{
    private readonly ConcurrentDictionary<string, DetourInfo> _detours;
    private readonly Mem _mem;

    /// <summary>
    /// 初始化 Detour 管理器
    /// </summary>
    public DetourManager() : this(MA_FH5Trainer.Resources.Memory.GetInstance())
    {
    }

    public DetourManager(Mem mem)
    {
        _mem = mem;
        _detours = new ConcurrentDictionary<string, DetourInfo>();
    }

    /// <summary>
    /// 注册 detour
    /// </summary>
    /// <param name="name">Detour 名称</param>
    /// <param name="address">目标地址</param>
    /// <param name="detourAddress">Detour 地址</param>
    /// <param name="originalBytes">原始字节</param>
    /// <param name="detourSize">Detour 大小</param>
    public void Register(string name, UIntPtr address, UIntPtr detourAddress, byte[] originalBytes, int detourSize)
    {
        _detours.TryAdd(name, new DetourInfo
        {
            Name = name,
            Address = address,
            DetourAddress = detourAddress,
            OriginalBytes = originalBytes,
            DetourSize = detourSize
        });
    }

    /// <summary>
    /// 获取 detour 信息
    /// </summary>
    /// <param name="name">Detour 名称</param>
    /// <returns>Detour 信息</returns>
    public DetourInfo? GetDetour(string name)
    {
        return _detours.TryGetValue(name, out var detour) ? detour : null;
    }

    /// <summary>
    /// 清理所有 detour（恢复原始字节并释放内存）
    /// </summary>
    /// <param name="mem">内存对象</param>
    public void CleanupAll(Memory.Mem mem)
    {
        foreach (var detour in _detours.Values)
        {
            if (detour.DetourAddress > 0)
            {
                mem.WriteArrayMemory(detour.Address, detour.OriginalBytes);
                Free(detour.DetourAddress);
            }
        }
    }

    /// <summary>
    /// 恢复所有 detour（恢复原始字节）
    /// </summary>
    /// <param name="mem">内存对象</param>
    public void RevertAll(Memory.Mem mem)
    {
        foreach (var detour in _detours.Values)
        {
            if (detour.DetourAddress > 0)
            {
                mem.WriteArrayMemory(detour.Address, detour.OriginalBytes);
            }
        }
    }

    /// <summary>
    /// 继续所有 detour（应用 detour）
    /// </summary>
    /// <param name="mem">内存对象</param>
    public void ContinueAll(Memory.Mem mem)
    {
        foreach (var detour in _detours.Values)
        {
            if (detour.DetourAddress > 0)
            {
                var detourBytes = CalculateDetour(detour.Address, detour.DetourAddress, detour.DetourSize);
                mem.WriteArrayMemory(detour.Address, detourBytes);
            }
        }
    }

    /// <summary>
    /// 重置所有 detour
    /// </summary>
    public void ResetAll()
    {
        _detours.Clear();
    }

    /// <summary>
    /// 计算 detour 字节
    /// </summary>
    /// <param name="address">目标地址</param>
    /// <param name="detourAddress">Detour 地址</param>
    /// <param name="size">大小</param>
    /// <returns>Detour 字节</returns>
    private byte[] CalculateDetour(UIntPtr address, UIntPtr detourAddress, int size)
    {
        var bytes = new byte[size];
        long offset = (long)detourAddress - (long)address - 5;

        bytes[0] = 0xE9; // JMP
        BitConverter.GetBytes(offset).CopyTo(bytes, 1);

        return bytes;
    }

    /// <summary>
    /// 释放内存
    /// </summary>
    /// <param name="address">内存地址</param>
    private void Free(UIntPtr address)
    {
        try
        {
            var handle = _mem.MProc.Handle;
            Imps.VirtualFreeEx(handle, address, 0, Imps.MemRelease);
        }
        catch
        {
            // 忽略释放错误
        }
    }
}

/// <summary>
/// Detour 信息
/// </summary>
public record DetourInfo
{
    /// <summary>
    /// Detour 名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 目标地址
    /// </summary>
    public UIntPtr Address { get; init; }

    /// <summary>
    /// Detour 地址
    /// </summary>
    public UIntPtr DetourAddress { get; init; }

    /// <summary>
    /// 原始字节
    /// </summary>
    public byte[] OriginalBytes { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// Detour 大小
    /// </summary>
    public int DetourSize { get; init; }
}