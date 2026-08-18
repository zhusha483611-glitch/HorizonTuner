using Memory;

namespace HorizonTuner.Tests;

public class MemoryEdgeCaseTests
{
    [Fact]
    public void ReadArrayMemory_ZeroLength_ReturnsEmptyArray()
    {
        var mem = new Mem();

        var result = mem.ReadArrayMemory<byte>(0, 0);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(0x1000L, 0x2000L, 0x3000L)]
    [InlineData(long.MaxValue - 4, 8, long.MaxValue)]
    [InlineData(0x1000L, long.MaxValue, long.MaxValue)]
    public void AdvanceAddress_ClampsOnOverflow(long baseAddress, long regionSize, long expected)
    {
        Assert.Equal(expected, Mem.AdvanceAddress(baseAddress, regionSize));
    }
}
