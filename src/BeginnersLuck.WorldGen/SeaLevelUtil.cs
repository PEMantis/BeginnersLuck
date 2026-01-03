namespace BeginnersLuck.WorldGen.Util;

public static class SeaLevelUtil
{
    // Converts 0..1 to 0..255
    public static byte ToByte(float seaLevel01)
    {
        if (seaLevel01 < 0f) seaLevel01 = 0f;
        if (seaLevel01 > 1f) seaLevel01 = 1f;
        return (byte)(seaLevel01 * 255f);
    }

    // Converts 0..255 to 0..1
    public static float To01(byte seaLevelByte) => seaLevelByte / 255f;
}
