using System.Runtime.InteropServices;
using Memory;
using static MA_FH5Trainer.Resources.Memory;

namespace MA_FH5Trainer.Cheats;

/// <summary>
/// 内存操作辅助类，提供安全的内存读写操作
/// </summary>
public class MemoryHelper
{
    private readonly Mem _mem;

    public MemoryHelper() : this(MA_FH5Trainer.Resources.Memory.GetInstance())
    {
    }

    public MemoryHelper(Mem mem)
    {
        _mem = mem;
    }

    /// <summary>
    /// 读取内存值（带地址验证和异常处理）
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="address">内存地址</param>
    /// <returns>读取的值</returns>
    public T SafeReadMemory<T>(nuint address) where T : unmanaged
    {
        if (address == 0)
        {
            throw new ArgumentException("地址不能为零", nameof(address));
        }

        if (!IsValidAddress(address))
        {
            throw new ArgumentException($"无效的内存地址: 0x{address:X}", nameof(address));
        }

        var result = _mem.ReadMemory<T>(address);

        // 检查是否读取失败（返回默认值）
        if (EqualityComparer<T>.Default.Equals(result, default))
        {
            // 尝试再次读取以确认
            var verify = _mem.ReadMemory<T>(address);
            if (EqualityComparer<T>.Default.Equals(verify, default))
            {
                throw new InvalidOperationException($"读取内存失败: 0x{address:X}");
            }
        }

        return result;
    }

    /// <summary>
    /// 写入内存值（带地址验证和异常处理）
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="address">内存地址</param>
    /// <param name="value">要写入的值</param>
    /// <param name="removeWriteProtection">是否移除写保护</param>
    /// <returns>是否写入成功</returns>
    public bool SafeWriteMemory<T>(nuint address, T value, bool removeWriteProtection = true) where T : unmanaged
    {
        if (address == 0)
        {
            throw new ArgumentException("地址不能为零", nameof(address));
        }

        if (!IsValidAddress(address))
        {
            throw new ArgumentException($"无效的内存地址: 0x{address:X}", nameof(address));
        }

        return _mem.WriteMemory(address, value, removeWriteProtection);
    }

    /// <summary>
    /// 读取内存数组（带地址验证和异常处理）
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="address">内存地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>读取的数组</returns>
    public T[] SafeReadArrayMemory<T>(nuint address, int length) where T : unmanaged
    {
        if (address == 0)
        {
            throw new ArgumentException("地址不能为零", nameof(address));
        }

        if (length <= 0)
        {
            throw new ArgumentException("长度必须大于零", nameof(length));
        }

        if (!IsValidAddress(address))
        {
            throw new ArgumentException($"无效的内存地址: 0x{address:X}", nameof(address));
        }

        var result = _mem.ReadArrayMemory<T>(address, length);

        if (result.Length == 0)
        {
            throw new InvalidOperationException($"读取内存数组失败: 0x{address:X}, 长度: {length}");
        }

        return result;
    }

    /// <summary>
    /// 写入内存数组（带地址验证和异常处理）
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="address">内存地址</param>
    /// <param name="values">要写入的数组</param>
    /// <param name="removeWriteProtection">是否移除写保护</param>
    /// <returns>是否写入成功</returns>
    public bool SafeWriteArrayMemory<T>(nuint address, T[] values, bool removeWriteProtection = true) where T : unmanaged
    {
        if (address == 0)
        {
            throw new ArgumentException("地址不能为零", nameof(address));
        }

        if (values == null || values.Length == 0)
        {
            throw new ArgumentException("数组不能为空", nameof(values));
        }

        if (!IsValidAddress(address))
        {
            throw new ArgumentException($"无效的内存地址: 0x{address:X}", nameof(address));
        }

        try
        {
            return _mem.WriteArrayMemory(address, values, removeWriteProtection);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"写入内存数组失败: 0x{address:X}", ex);
        }
    }

    /// <summary>
    /// 跟随多级指针（带地址验证和异常处理）
    /// </summary>
    /// <param name="baseAddress">基地址</param>
    /// <param name="offsets">偏移量数组</param>
    /// <returns>最终地址</returns>
    public nuint SafeFollowMultiLevelPointer(nuint baseAddress, int[] offsets)
    {
        if (baseAddress == 0)
        {
            throw new ArgumentException("基地址不能为零", nameof(baseAddress));
        }

        if (offsets == null || offsets.Length == 0)
        {
            throw new ArgumentException("偏移量数组不能为空", nameof(offsets));
        }

        var currentAddress = baseAddress;

        foreach (var offset in offsets)
        {
            currentAddress = _mem.ReadMemory<nuint>(currentAddress);
            
            if (currentAddress == 0)
            {
                throw new InvalidOperationException($"跟随多级指针失败: 地址为 0x{baseAddress:X}, 在偏移量 {offset} 处");
            }

            if (!IsValidAddress(currentAddress))
            {
                throw new InvalidOperationException($"跟随多级指针失败: 无效地址 0x{currentAddress:X}");
            }

            currentAddress += (nuint)offset;
        }

        return currentAddress;
    }

    /// <summary>
    /// 验证地址是否有效
    /// </summary>
    /// <param name="address">要验证的地址</param>
    /// <returns>地址是否有效</returns>
    private static bool IsValidAddress(nuint address)
    {
        // 检查地址是否在合理的范围内
        // Windows 64 位地址空间通常在 0x10000 到 0x7FFFFFFFFFFF 之间
        const nuint minAddress = 0x10000;
        var maxAddress = unchecked((nuint)0x7FFFFFFFFFFFUL);

        return address >= minAddress && address <= maxAddress;
    }

    /// <summary>
    /// 尝试读取内存值（不抛出异常）
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="address">内存地址</param>
    /// <param name="result">读取的值</param>
    /// <returns>是否读取成功</returns>
    public bool TryReadMemory<T>(nuint address, out T result) where T : unmanaged
    {
        result = default;
        
        try
        {
            result = SafeReadMemory<T>(address);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 尝试写入内存值（不抛出异常）
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="address">内存地址</param>
    /// <param name="value">要写入的值</param>
    /// <returns>是否写入成功</returns>
    public bool TryWriteMemory<T>(nuint address, T value) where T : unmanaged
    {
        try
        {
            return SafeWriteMemory(address, value);
        }
        catch
        {
            return false;
        }
    }
}