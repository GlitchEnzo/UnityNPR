using UnityEngine;

public static class ColorConversion
{
    public static int Color32ToInt(Color32 color)
    {
        if (color.a != 0xFF)
        {
            Debug.LogWarning("Alpha channel is expected to always be 256");
        }

        int value = color.b;
        value += color.g << 8;
        value += color.r << 16;
        //value += color.a << 24;

        return value;
    }

    public static Color32 IntToColor32(int value)
    {
        if (value > System.Math.Pow(2, 24))
        {
            Debug.LogWarning("Maximum value allowed is 2^24 = 16,777,216.  This is because the alpha channel is forced to 256");
        }

        // [alpha] [red] [green] [blue] 
        //byte a = (byte)((value >> 24) & 0xFF);
        byte a = 0xFF;
        byte r = (byte)((value >> 16) & 0xFF);
        byte g = (byte)((value >> 8) & 0xFF);
        byte b = (byte)((value) & 0xFF);

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
