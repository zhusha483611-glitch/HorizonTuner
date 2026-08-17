using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace MA_FH5Trainer.Cheats;

/// <summary>
/// 内存池管理器，用于优化 AoB 扫描的内存分配
/// </summary>
public unsafe class MemoryPool : IDisposable
{
    private readonly ConcurrentBag<MemoryBlock> _pool;
    private readonly int _maxPoolSize;
    private readonly int _blockSize;
    private bool _disposed;

    /// <summary>
    /// 初始化内存池
    /// </summary>
    /// <param name="blockSize">每个内存块的大小（字节）</param>
    /// <param name="maxPoolSize">最大内存块数量</param>
    public MemoryPool(int blockSize = 1024 * 1024, int maxPoolSize = 10)
    {
        _blockSize = blockSize;
        _maxPoolSize = maxPoolSize;
        _pool = new ConcurrentBag<MemoryBlock>();
    }

    /// <summary>
    /// 从池中获取内存块
    /// </summary>
    /// <param name="size">所需大小</param>
    /// <returns>内存块</returns>
    public MemoryBlock Rent(int size)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MemoryPool));
        }

        if (size > _blockSize)
        {
            // 如果请求的大小大于默认块大小，直接分配新内存
            return new MemoryBlock((IntPtr)NativeMemory.Alloc((nuint)size), size, false);
        }

        // 尝试从池中获取
        if (_pool.TryTake(out var block))
        {
            block.InUse = true;
            return block;
        }

        // 池中没有可用块，分配新内存
        return new MemoryBlock((IntPtr)NativeMemory.Alloc((nuint)_blockSize), _blockSize, true);
    }

    /// <summary>
    /// 归还内存块到池中
    /// </summary>
    /// <param name="block">要归还的内存块</param>
    public void Return(MemoryBlock block)
    {
        if (_disposed || block == null)
        {
            return;
        }

        block.InUse = false;

        // 只有来自池的块才能归还
        if (block.Pooled && _pool.Count < _maxPoolSize)
        {
            _pool.Add(block);
        }
        else
        {
            // 直接释放
            block.Dispose();
        }
    }

    /// <summary>
    /// 清空内存池
    /// </summary>
    public void Clear()
    {
        while (_pool.TryTake(out var block))
        {
            block.Dispose();
        }
    }

    /// <summary>
    /// 获取池的统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public MemoryPoolStats GetStats()
    {
        return new MemoryPoolStats
        {
            AvailableBlocks = _pool.Count,
            MaxPoolSize = _maxPoolSize,
            BlockSize = _blockSize,
            TotalAllocatedSize = _pool.Count * _blockSize
        };
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Clear();
        _disposed = true;
    }
}

/// <summary>
/// 内存块
/// </summary>
public unsafe class MemoryBlock : IDisposable
{
    /// <summary>
    /// 内存地址
    /// </summary>
    public IntPtr Pointer { get; }

    /// <summary>
    /// 内存大小
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// 是否来自池
    /// </summary>
    public bool Pooled { get; }

    /// <summary>
    /// 是否在使用
    /// </summary>
    public bool InUse { get; set; }

    /// <summary>
    /// 初始化内存块
    /// </summary>
    /// <param name="pointer">内存指针</param>
    /// <param name="size">大小</param>
    /// <param name="pooled">是否来自池</param>
    public MemoryBlock(IntPtr pointer, int size, bool pooled)
    {
        Pointer = pointer;
        Size = size;
        Pooled = pooled;
        InUse = true;
    }

    /// <summary>
    /// 获取不安全指针
    /// </summary>
    /// <returns>不安全指针</returns>
    public unsafe byte* GetPointer()
    {
        return (byte*)Pointer;
    }

    /// <summary>
    /// 释放内存
    /// </summary>
    public void Dispose()
    {
        if (Pointer != IntPtr.Zero)
        {
            NativeMemory.Free(Pointer.ToPointer());
        }
    }
}

/// <summary>
/// 内存池统计信息
/// </summary>
public class MemoryPoolStats
{
    /// <summary>
    /// 可用块数量
    /// </summary>
    public int AvailableBlocks { get; init; }

    /// <summary>
    /// 最大池大小
    /// </summary>
    public int MaxPoolSize { get; init; }

    /// <summary>
    /// 块大小
    /// </summary>
    public int BlockSize { get; init; }

    /// <summary>
    /// 总分配大小
    /// </summary>
    public long TotalAllocatedSize { get; init; }

    /// <summary>
    /// 使用率
    /// </summary>
    public double UsageRate => MaxPoolSize > 0 ? (double)AvailableBlocks / MaxPoolSize : 0;
}