using UnityEngine;

public static class ColorConversion
{
    public static int Color32ToInt(Color32 color)
    {
        if (color.r > 127)
        {
            Debug.LogWarning("Red channel cannot be larger than 127 (7-bits) for positive integers.  8th bit is the sign bit.");
        }

        int value = color.a;
        value += color.b << 8;
        value += color.g << 16;
        value += color.r << 24;

        return value;
    }

    public static Color32 IntToColor32(int value)
    {
        // [red] [green] [blue] [alpha]
        byte r = (byte)((value >> 24) & 0xFF);
        byte g = (byte)((value >> 16) & 0xFF);
        byte b = (byte)((value >> 8) & 0xFF);
        byte a = (byte)((value) & 0xFF);

        return new Color32(r, g, b, a);
    }

    public static void Test()
    {
        Color32 color = new Color32(127, 255, 255, 255);
        int value = Color32ToInt(color);
        Debug.LogFormat("{0} = {1}", value, int.MaxValue);

        value = int.MaxValue;
        color = IntToColor32(value);
        Debug.LogFormat("{0} = {1}", color, new Color32(127, 255, 255, 255));
    }
}
