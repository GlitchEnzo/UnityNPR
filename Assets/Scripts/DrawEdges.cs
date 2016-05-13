using UnityEngine;

[RequireComponent(typeof(EdgeListGenerator))]
public class DrawEdges : MonoBehaviour
{
    public enum LineType
    {
        Edges,
        Creases,
        EdgesAndCreases
    }

    static Material lineMaterial;
    static void CreateLineMaterial()
    {
        if (!lineMaterial)
        {
            var shader = Shader.Find("Unlit/CustomUnlit");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;

            // Turn on alpha blending
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            // Turn backface culling off
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

            // Turn off depth writes
            lineMaterial.SetInt("_ZWrite", 0);

            //lineMaterial.SetColor("_Color", Color.blue);
        }
    }

    public LineType lineType;

    private EdgeListGenerator edgeList;

    private void Start()
    {
        edgeList = GetComponent<EdgeListGenerator>();
    }

    // Will be called after all regular rendering is done
    public void OnRenderObject()
    {
        CreateLineMaterial();

        // Apply the line material
        //lineMaterial.SetPass(0);

        GL.PushMatrix();

        // TODO: Use the transform scale.

        // Set transformation matrix for drawing to match our transform
        GL.MultMatrix(transform.localToWorldMatrix);

        // Draw lines
        GL.Begin(GL.LINES);
        if (lineType == LineType.Edges || lineType == LineType.EdgesAndCreases)
        {
            lineMaterial.SetColor("_Color", Color.blue);
            lineMaterial.SetPass(0);

            foreach (var keyValuePair in edgeList.edges)
            {
                var edgeVertexA = edgeList.GetVertex(keyValuePair.Value.IndexA);
                GL.Vertex3(edgeVertexA.x, edgeVertexA.y, edgeVertexA.z);

                var edgeVertexB = edgeList.GetVertex(keyValuePair.Value.IndexB);
                GL.Vertex3(edgeVertexB.x, edgeVertexB.y, edgeVertexB.z);
            }
        }
        GL.End();

        GL.Begin(GL.LINES);
        if (lineType == LineType.Creases || lineType == LineType.EdgesAndCreases)
        {
            lineMaterial.SetColor("_Color", Color.red);
            lineMaterial.SetPass(0);

            foreach (var crease in edgeList.creases)
            {
                var edgeVertexA = edgeList.GetVertex(crease.IndexA);
                GL.Vertex3(edgeVertexA.x, edgeVertexA.y, edgeVertexA.z);

                var edgeVertexB = edgeList.GetVertex(crease.IndexB);
                GL.Vertex3(edgeVertexB.x, edgeVertexB.y, edgeVertexB.z);
            }
        }
        GL.End();
        GL.PopMatrix();
    }
}