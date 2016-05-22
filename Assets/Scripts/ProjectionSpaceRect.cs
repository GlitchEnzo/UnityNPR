using UnityEngine;
using System.Collections;

public class ProjectionSpaceRect : MonoBehaviour
{
    public Mesh rectangle;
    public Material material;
    
    void Start()
    {
        // 2 --- 3
        // |     |
        // |     |
        // 0-----1
        
        Vector3[] vertexBuffer = new Vector3[] { new Vector3(-0.5f, -0.5f, 0),
                                                 new Vector3( 0.5f, -0.5f, 0),
                                                 new Vector3(-0.5f,  0.5f, 0),
                                                 new Vector3( 0.5f,  0.5f, 0) };

        Color32[] colorBuffer = new Color32[] { new Color32(255,   0,   0, 255),
                                                new Color32(  0, 255,   0, 255),
                                                new Color32(  0,   0, 255, 255),
                                                new Color32(255, 255, 255, 255) };

        Vector2[] uvBuffer = new Vector2[] { new Vector2(0, 0),
                                             new Vector2(1, 0),
                                             new Vector2(0, 1),
                                             new Vector2(1, 1) };

        int[] indexBuffer = new int[] { 1, 0, 2, 2, 3, 1};

        rectangle = new Mesh();
        rectangle.vertices = vertexBuffer;
        rectangle.colors32 = colorBuffer;
        rectangle.uv = uvBuffer;
        rectangle.triangles = indexBuffer;

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = rectangle;

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
    }
}
