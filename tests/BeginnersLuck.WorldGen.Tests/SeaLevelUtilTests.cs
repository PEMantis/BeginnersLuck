using BeginnersLuck.WorldGen.Util;

namespace BeginnersLuck.WorldGen.Tests;

public class SeaLevelUtilTests
{
    [Theory]
    [InlineData(-1.0f, (byte)0)]
    [InlineData(0.0f, (byte)0)]
    [InlineData(1.0f, (byte)255)]
    [InlineData(2.5f, (byte)255)]
    public void ToByte_ClampsToByteRange(float input, byte expected)
    {
        var actual = SeaLevelUtil.ToByte(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void To01_RoundTripBoundsRemainStable()
    {
        Assert.Equal(0f, SeaLevelUtil.To01(SeaLevelUtil.ToByte(0f)));
        Assert.Equal(1f, SeaLevelUtil.To01(SeaLevelUtil.ToByte(1f)));
    }

    [Theory]
    [InlineData((byte)0, 0f)]
    [InlineData((byte)128, 128f / 255f)]
    [InlineData((byte)255, 1f)]
    public void To01_ConvertsByteToNormalizedFloat(byte input, float expected)
    {
        var actual = SeaLevelUtil.To01(input);
        Assert.Equal(expected, actual, precision: 6);
    }
}
