using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(MeshFilter))]
public class EdgeListGenerator : MonoBehaviour
{
    [Serializable]
    public class Edge
    {
        public int IndexA { get; set; }
        public int IndexB { get; set; }

        public int FaceA { get; set; }
        public int FaceB { get; set; }

        public bool IsCrease { get; set; }
        public bool IsSilhouette { get; set; }

        public Edge(int a, int b, int face)
        {
            if (a < b)
            {
                IndexA = a;
                IndexB = b;
            }
            else
            {
                IndexA = b;
                IndexB = a;
            }

            FaceA = FaceB = face;

            // TODO: Check case where they are equal?
        }

        public override bool Equals(object obj)
        {
            Edge other = (Edge)obj;
            return other.IndexA == IndexA && other.IndexB == IndexB;
        }

        public override int GetHashCode()
        {
            //return IndexA.GetHashCode() ^ IndexB.GetHashCode();

            // Szudzik's function:
            // http://stackoverflow.com/questions/919612/mapping-two-integers-to-one-in-a-unique-and-deterministic-way
            return IndexA >= IndexB ? IndexA * IndexA + IndexA + IndexB : IndexA + IndexB * IndexB;
        }

        public override string ToString()
        {
            return string.Format("Edge({0}, {1})", IndexA, IndexB);
        }
    }

    [Serializable]
    public struct Face
    {
        public int indexA;
        public int indexB;
        public int indexC;

        public Vector3 normal;

        public Vector3 centroid;

        public Face(int a, int b, int c, Vector3[] vertexBuffer)
        {
            indexA = a;
            indexB = b;
            indexC = c;

            normal = Vector3.zero;
            Vector3 U = vertexBuffer[indexB] - vertexBuffer[indexA];
            Vector3 V = vertexBuffer[indexC] - vertexBuffer[indexA];
            normal = Vector3.Cross(U, V).normalized;

            centroid = (vertexBuffer[indexA] + vertexBuffer[indexB] + vertexBuffer[indexC]) / 3.0f;
        }

        public override string ToString()
        {
            return string.Format("Face(verts={0}, {1}, {2}; normal={3})", indexA, indexB, indexC, normal);
        }
    }

    public List<Edge> edges = new List<Edge>();

    public List<Face> faces = new List<Face>();

    public List<Edge> creases = new List<Edge>();
    public List<Edge> silhouettes = new List<Edge>();

    private Mesh mesh;
    int[] indexBuffer;
    Vector3[] vertexBuffer;

    public bool alwaysFindSilhouettes;

    private Mesh edgeMesh;

    void Start()
    {
        Debug.LogFormat("{0}.Start()", name);

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.sharedMesh;

        indexBuffer = mesh.triangles;
        vertexBuffer = mesh.vertices;

        // initialize sizes
        edges = new List<Edge>(indexBuffer.Length);
        faces = new List<Face>(indexBuffer.Length / 3);
        creases = new List<Edge>(indexBuffer.Length / 2); // estimate that half of the edges will be creases

        FindFacesAndEdges();
        FindCreases();

        CreateEdgeMesh();
    }

    void Update()
    {
        if (alwaysFindSilhouettes || Input.GetKeyDown(KeyCode.Return))
        {
            Rasterize(Camera.main);
        }
    }

    private void FindFacesAndEdges()
    {
        Debug.LogFormat("{0}: There should be {1} edges or less.", name, indexBuffer.Length);
        Debug.LogFormat("{0}: There should be {1} faces.", name, indexBuffer.Length / 3);
        Debug.LogFormat("{0}: There are {1} vertices.", name, mesh.vertexCount);

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        Dictionary<Edge, Edge> edgesDictionary = new Dictionary<Edge, Edge>(indexBuffer.Length);
        Edge temp;

        for (int i = 0; i <= indexBuffer.Length - 3; i += 3)
        {
            Face face = new Face(indexBuffer[i], indexBuffer[i + 1], indexBuffer[i + 2], vertexBuffer);
            faces.Add(face);
            int faceIndex = faces.Count - 1;

            Edge edge = new Edge(indexBuffer[i], indexBuffer[i + 1], faceIndex);
            if (!edgesDictionary.TryGetValue(edge, out temp))
            {
                edgesDictionary.Add(edge, edge);
            }
            else
            {
                temp.FaceB = faceIndex;
            }

            edge = new Edge(indexBuffer[i + 1], indexBuffer[i + 2], faceIndex);
            if (!edgesDictionary.TryGetValue(edge, out temp))
            {
                edgesDictionary.Add(edge, edge);
            }
            else
            {
                temp.FaceB = faceIndex;
            }

            edge = new Edge(indexBuffer[i + 2], indexBuffer[i], faceIndex);
            if (!edgesDictionary.TryGetValue(edge, out temp))
            {
                edgesDictionary.Add(edge, edge);
            }
            else
            {
                temp.FaceB = faceIndex;
            }
        }

        edges = edgesDictionary.Values.ToList();

        Debug.LogFormat("{0}: There were {1} unique edges found.", name, edges.Count);
        Debug.LogFormat("{0}: There were {1} faces found.", name, faces.Count);

        stopwatch.Stop();
        Debug.LogFormat("{0}: Finding edges took {1}ms", name, stopwatch.ElapsedMilliseconds);
    }

    private void FindCreases()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        float thresholdAngle = 90.0f;

        creases.Clear();

        foreach (var edge in edges)
        {
            if (edge.FaceA == edge.FaceB)
            {
                // if the edge is only on one face, it is a crease too
                //edge.IsCrease = true;
                //creases.Add(edge);
            }
            else
            {
                float dotProduct = Vector3.Dot(faces[edge.FaceA].normal, faces[edge.FaceB].normal);
                float angle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

                if (angle > thresholdAngle)
                {
                    Debug.LogFormat("Angle = {0} degrees", angle);
                    edge.IsCrease = true;
                    creases.Add(edge);
                }
            }
        }

        Debug.LogFormat("{0}: There were {1} creases found.", name, creases.Count);

        stopwatch.Stop();
        Debug.LogFormat("{0}: Finding creases took {1}ms", name, stopwatch.ElapsedMilliseconds);
    }

    private void CreateEdgeMesh()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        Vector3[] newVertices = new Vector3[edges.Count * 2];
        Vector2[] newUV = new Vector2[edges.Count * 2];
        int[] newTriangles = new int[edges.Count * 2];

        for (int i = 0; i < edges.Count; i++)
        {
            newVertices[i * 2] = vertexBuffer[edges[i].IndexA];
            newVertices[i * 2 + 1] = vertexBuffer[edges[i].IndexB];

            // store the edge index as the UV
            newUV[i * 2] = new Vector2(i + 1, 0);
            newUV[i * 2 + 1] = new Vector2(i + 1, 0);

            // since it's a line list, the 
            newTriangles[i * 2] = i * 2;
            newTriangles[i * 2 + 1] = i * 2 + 1;
        }

        edgeMesh = new Mesh();
        edgeMesh.vertices = newVertices;
        edgeMesh.uv = newUV;
        //edgeMesh.triangles = newTriangles;
        edgeMesh.SetIndices(newTriangles, MeshTopology.Lines, 0);

        GameObject edgeMeshGameObject = new GameObject(name + ".Edges");
        MeshFilter meshFilter = edgeMeshGameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = edgeMesh;
        edgeMeshGameObject.AddComponent<MeshRenderer>();

        stopwatch.Stop();
        Debug.LogFormat("{0}: Creating the edge mesh took {1}ms", name, stopwatch.ElapsedMilliseconds);
    }

    private void Rasterize(Camera camera)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        silhouettes.Clear();

        // transform all verts
        Vector3[] transformedVertexBuffer = new Vector3[vertexBuffer.Length];
        for (int i = 0; i < vertexBuffer.Length; i++)
        {
            //transformedVertexBuffer[i] = vertexBuffer[i] * transform.localToWorldMatrix;
            transformedVertexBuffer[i] = transform.TransformPoint(vertexBuffer[i]);
        }

        foreach (var edge in edges)
        {
            Vector3 faceANormal = transform.TransformVector(faces[edge.FaceA].normal);
            Vector3 faceBNormal = transform.TransformVector(faces[edge.FaceB].normal);

            bool isFaceAForward = Vector3.Dot(faceANormal, camera.transform.forward) < 0;
            bool isFaceBForward = Vector3.Dot(faceBNormal, camera.transform.forward) < 0;

            // forward + forward = skip, unless showing creases
            // forward + backward = silhouette
            // backward + forward = silhouette
            // backward + backward = skip, unless showing hidden creases
            if (isFaceAForward ^ isFaceBForward)
            {
                edge.IsSilhouette = true;
                silhouettes.Add(edge);
            }
            else
            {
                edge.IsSilhouette = false;
            }
        }

        stopwatch.Stop();
        Debug.LogFormat("{0}: Finding silhouettes took {1}ms", name, stopwatch.ElapsedMilliseconds);
    }

    public Vector3 GetVertex(int index)
    {
        return vertexBuffer[index];
    }
}
