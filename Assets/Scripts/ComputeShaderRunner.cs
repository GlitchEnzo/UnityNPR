﻿using UnityEngine;

// from here: http://kylehalladay.com/blog/tutorial/2014/06/27/Compute-Shaders-Are-Nifty.html
public class ComputeShaderRunner : MonoBehaviour
{
    public ComputeShader testShader;
    public RenderTexture testResult;

    public ComputeShader simpleShader;
    public RenderTexture simpleResult;

    public ComputeShader copyShader;
    public RenderTexture copyResult;

    public ComputeShader sobelShader;
    public Texture2D sobelInput;
    public RenderTexture sobelResult;

    private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

    void RunTestShader()
    {
        stopwatch.Reset();
        stopwatch.Start();

        int kernelHandle = testShader.FindKernel("CSMain");

        testResult = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
        testResult.enableRandomWrite = true;
        testResult.Create();

        testShader.SetTexture(kernelHandle, "Result", testResult);
        testShader.Dispatch(kernelHandle, 256 / 8, 256 / 8, 1);

        stopwatch.Stop();
        Debug.LogFormat("TestShader took {0}ms", stopwatch.ElapsedMilliseconds);
    }

    void RunSimpleShader()
    {
        stopwatch.Reset();
        stopwatch.Start();

        int kernelHandle = simpleShader.FindKernel("SimpleMain");

        simpleResult = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32);
        simpleResult.enableRandomWrite = true;
        simpleResult.Create();

        simpleShader.SetTexture(kernelHandle, "Output", simpleResult);
        simpleShader.Dispatch(kernelHandle, 32, 32, 1);

        stopwatch.Stop();
        Debug.LogFormat("SimpleShader took {0}ms", stopwatch.ElapsedMilliseconds);
    }

    void RunCopyShader()
    {
        stopwatch.Reset();
        stopwatch.Start();

        int kernelHandle = copyShader.FindKernel("Copy");

        copyResult = new RenderTexture(simpleResult.width, simpleResult.height, 0, RenderTextureFormat.ARGB32);
        copyResult.enableRandomWrite = true;
        copyResult.Create();

        copyShader.SetTexture(kernelHandle, "source", simpleResult);
        copyShader.SetTexture(kernelHandle, "dest", copyResult);
        copyShader.SetInt("width", simpleResult.width);
        copyShader.SetInt("height", simpleResult.height);
        copyShader.Dispatch(kernelHandle, (simpleResult.width + 32 - 1) / 32, (simpleResult.height + 32 - 1) / 32, 1);

        stopwatch.Stop();
        Debug.LogFormat("CopyShader took {0}ms", stopwatch.ElapsedMilliseconds);
    }

    void RunSobelShader()
    {
        stopwatch.Reset();
        stopwatch.Start();

        int kernelHandle = sobelShader.FindKernel("Sobel");

        sobelResult = new RenderTexture(sobelInput.width, sobelInput.height, 0, RenderTextureFormat.ARGB32);
        sobelResult.enableRandomWrite = true;
        sobelResult.Create();

        sobelShader.SetTexture(kernelHandle, "Input", sobelInput);
        sobelShader.SetTexture(kernelHandle, "Output", sobelResult);
        sobelShader.Dispatch(kernelHandle, 32, 32, 1);

        stopwatch.Stop();
        Debug.LogFormat("SobelShader took {0}ms", stopwatch.ElapsedMilliseconds);
    }

    void Start()
    {
        RunTestShader();
        RunSimpleShader();

        RunCopyShader();

        RunSobelShader();
    }

    void OnGUI()
    {
        GUILayout.Label(testResult);

        if (GUILayout.Button("Save"))
        {
            stopwatch.Reset();
            stopwatch.Start();

            SaveRenderTexture(testResult, "testResult.png");

            stopwatch.Stop();
            Debug.LogFormat("Saving took {0} ms", stopwatch.ElapsedMilliseconds);
        }
    }

    private void SaveRenderTexture(RenderTexture renderTexture, string filepath)
    {
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);

        var oldRenderTexture = RenderTexture.active;
        RenderTexture.active = renderTexture;

        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        RenderTexture.active = oldRenderTexture;

        byte[] data = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(filepath, data);
    }
}
