using UnityEngine;
using System.Collections;

public class SingleRenderTest : MonoBehaviour
{
    public KeyCode renderButton = KeyCode.F5;

    public Camera renderCamera;

    public Mesh renderMesh;
    public Transform meshTransform;
    public Material meshMaterial;

    public RenderTexture renderTexture;

    void Reset()
    {
        Vector3[] verts = new Vector3[] { Vector3.up, Vector3.right, Vector3.down, Vector3.left };
        int[] indicesForLineStrip = new int[] { 0, 1, 2, 3, 0 };
        //int[] indicesForLines = new int[]{0,1,1,2,2,3,3,0};
        renderMesh = new Mesh();
        renderMesh.vertices = verts;
        renderMesh.SetIndices(indicesForLineStrip, MeshTopology.LineStrip, 0);
        //renderMesh.SetIndices(indicesForLines, MeshTopology.Lines, 0);
        //renderMesh.RecalculateNormals();
        renderMesh.RecalculateBounds();

        meshTransform = transform;
    }
    
	void Start ()
    {
        Debug.LogFormat("Creating a {0}x{1} render texture.", Screen.width, Screen.height);

        renderTexture = new RenderTexture(Screen.width, Screen.height, 24);

        if (renderCamera != null)
        {
            renderCamera.targetTexture = renderTexture;
        }
    }
	
	void Update ()
    {
	    if (Input.GetKeyDown(renderButton))
        {
            if (renderCamera != null)
            {
                Graphics.DrawMesh(renderMesh, meshTransform.localToWorldMatrix, meshMaterial, 13, renderCamera);
                renderCamera.Render();
            }
        }
	}
}
