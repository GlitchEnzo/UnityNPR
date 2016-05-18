using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class RenderTextureReader : MonoBehaviour
{
    public KeyCode keyToRead = KeyCode.R;

    private Camera attachedCamera;

    private RenderTexture attachedRenderTexture;

    void Start()
    {
        attachedCamera = GetComponent<Camera>();
        attachedRenderTexture = attachedCamera.targetTexture;

        foreach (TextureFormat format in Enum.GetValues(typeof(TextureFormat)))
        {
            try
            {
                Debug.LogFormat("TextureFormat.{0} supported = {1}", format, SystemInfo.SupportsTextureFormat(format));
            }
            catch
            {
                Debug.LogErrorFormat("TextureFormat.{0} was invalid!", format);
            }
        }

        Texture2D texture = new Texture2D(1024, 768, TextureFormat.RFloat, false);
        Color[] colorData = new Color[1024 * 768];
        for (int x = 0; x < 1024; x++)
        {
            for (int y = 0; y < 768; y++)
            {
                colorData[y * 1024 + x] = new Color(y * 1024 + x, 0, 0, 0);
            }
        }
        texture.SetPixels(colorData);

        Debug.Log(texture.GetPixel(0, 0).r + " should = " + (0 * 1024 + 0));
        Debug.Log(texture.GetPixel(10, 10).r + " should = " + (10 * 1024 + 10));
        Debug.Log(texture.GetPixel(1023, 767).r + " should = " + (767 * 1024 + 1023));
    }

    void Update()
    {
        if (Input.GetKeyDown(keyToRead))
        {
            if (attachedRenderTexture != null)
            {
                //RenderBuffer buffer = attachedRenderTexture.colorBuffer;
                //buffer.???
            }
        }
    }
}
