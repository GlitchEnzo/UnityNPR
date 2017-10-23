using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class ColorConversionTests
{
    [Test]
    [TestCase(0, 0, 0, 0, 255)]
    [TestCase(1, 0, 0, 1, 255)]
    [TestCase(2, 0, 0, 2, 255)]
    [TestCase(255, 0, 0, 255, 255)]
    [TestCase(256, 0, 1, 0, 255)]
    [TestCase(65535, 0, 255, 255, 255)]
    [TestCase(65536, 1, 0, 0, 255)]
    [TestCase(16777215, 255, 255, 255, 255)] // 2^24 = 16,777,216
    public void ValuesEqualTest(int value, params int[] colorBytes)
    {
        // pass in as byte params to allow for the TestCase to be a constant expression
        // https://stackoverflow.com/questions/19479817/how-do-i-put-new-listint-1-in-an-nunit-testcase
        Color32 color = new Color32((byte)colorBytes[0], (byte)colorBytes[1], (byte)colorBytes[2], (byte)colorBytes[3]);
        int calcValue = ColorConversion.Color32ToInt(color);
        Assert.AreEqual(value, calcValue, "{0} was NOT equal to {1}", value, calcValue);

        var calcColor = ColorConversion.IntToColor32(value);
        Assert.AreEqual(color, calcColor, "{0} was NOT equal to {1}", color, calcColor);
    }
}
