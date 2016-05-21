using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(Camera))]
public class RenderTextureReader : MonoBehaviour
{
    public KeyCode keyToRead = KeyCode.R;

    private Camera attachedCamera;

    private Texture2D storageTexture;
    private Rect textureRect;

    void Start()
    {
        ColorConversion.Test();

        attachedCamera = GetComponent<Camera>();

        //foreach (TextureFormat format in Enum.GetValues(typeof(TextureFormat)))
        //{
        //    try
        //    {
        //        Debug.LogFormat("TextureFormat.{0} supported = {1}", format, SystemInfo.SupportsTextureFormat(format));
        //    }
        //    catch
        //    {
        //        Debug.LogErrorFormat("TextureFormat.{0} was invalid!", format);
        //    }
        //}

        storageTexture = new Texture2D(1024, 768, TextureFormat.ARGB32, false);
        textureRect = new Rect(0, 0, 1024, 768);

        //Color[] colorData = new Color[1024 * 768];
        //for (int x = 0; x < 1024; x++)
        //{
        //    for (int y = 0; y < 768; y++)
        //    {
        //        colorData[y * 1024 + x] = new Color(y * 1024 + x, 0, 0, 0);
        //    }
        //}
        //storageTexture.SetPixels(colorData);

        //Debug.Log(storageTexture.GetPixel(0, 0).r + " should = " + (0 * 1024 + 0));
        //Debug.Log(storageTexture.GetPixel(10, 10).r + " should = " + (10 * 1024 + 10));
        //Debug.Log(storageTexture.GetPixel(1023, 767).r + " should = " + (767 * 1024 + 1023));
    }

    void Update()
    {
        if (Input.GetKeyDown(keyToRead))
        {
            if (attachedCamera != null)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                attachedCamera.Render();

                RenderTexture.active = attachedCamera.targetTexture;
                
                storageTexture.ReadPixels(textureRect, 0, 0, false);
                storageTexture.Apply();

                RenderTexture.active = null;

                //Color32[] data = storageTexture.GetPixels32();
                //foreach (var piece in data)
                //{
                //    if (piece.a != 0)
                //    {
                //        Debug.LogFormat("Data = {0}", ColorConversion.Color32ToInt(piece));
                //    }
                //}

                Debug.Log("Saving edges.png");
                File.WriteAllBytes("edges.png", storageTexture.EncodeToPNG());

                stopwatch.Stop();
                Debug.LogFormat("{0}: Reading edges took {1}ms", name, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
