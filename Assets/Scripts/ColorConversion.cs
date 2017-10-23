using UnityEngine;

public static class ColorConversion
{
    public static int Color32ToInt(Color32 color)
    {
        //if (color.a != 0)
        //{
        //    Debug.LogWarning("Alpha channel never used to ensure the color is always opaque");
        //}

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
            Debug.LogWarning("Maximum value allowed is 2^24 = 16,777,215.  This is because the alpha channel is forced to 127");
        }

        // [alpha] [red] [green] [blue] 
        //byte a = (byte)((value >> 24) & 0xFF);
        byte a = 0xFF;
        byte r = (byte)((value >> 16) & 0xFF);
        byte g = (byte)((value >> 8) & 0xFF);
        byte b = (byte)((value) & 0xFF);

        return new Color32(r, g, b, a);
    }
}
