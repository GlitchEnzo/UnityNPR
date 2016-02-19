using UnityEngine;

// from here: http://kylehalladay.com/blog/tutorial/2014/06/27/Compute-Shaders-Are-Nifty.html
public class ComputeShaderRunner : MonoBehaviour
{
    public ComputeShader testShader;
    public RenderTexture testResult;

    public ComputeShader simpleShader;
    public RenderTexture simpleResult;

    void RunTestShader()
    {
        int kernelHandle = testShader.FindKernel("CSMain");

        testResult = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
        testResult.enableRandomWrite = true;
        //testResult.antiAliasing = 1;
        testResult.Create();

        testShader.SetTexture(kernelHandle, "Result", testResult);
        testShader.Dispatch(kernelHandle, 256 / 8, 256 / 8, 1);
    }

    void RunSimpleShader()
    {
        int kernelHandle = simpleShader.FindKernel("SimpleMain");

        simpleResult = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32);
        simpleResult.enableRandomWrite = true;
        //simpleResult.antiAliasing = 1;
        simpleResult.Create();

        simpleShader.SetTexture(kernelHandle, "Output", simpleResult);
        simpleShader.Dispatch(kernelHandle, 32, 32, 1);
    }

    void Start()
    {
        RunTestShader();
        RunSimpleShader();

        //RawImage rawImage = GetComponent<RawImage>();
        //rawImage.texture = testResult;
    }

    void OnGUI()
    {
        GUILayout.Label(testResult);
        //GUI.DrawTexture(new Rect(0, 0, 256, 256), testResult);

        if (GUILayout.Button("Save"))
        {
            SaveRenderTexture(testResult, "testResult.png");
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
