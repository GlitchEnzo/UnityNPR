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

    private List<Mesh> edgeMeshes;

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

        // two vertices for each edge (line)
        int totalVertexCount = edges.Count * 2;

        const int vertexLimit = 65000;

        // Unity only allows for ~65,000 vertices [exactly 65,534] (only using 16 bit)
        // so, we must divide up the edge mesh into multiple edge meshes, in multiples of 65000
        // Note that DirectX 11+ and modern GPUs don't have this limitation since they use 32 bit
        // However, Unity targets the lowest common denominator
        int numberOfMeshes = edges.Count * 2 / vertexLimit + 1;

        edgeMeshes = new List<Mesh>(numberOfMeshes);

        Vector3[][] newVertices = new Vector3[numberOfMeshes][];
        Vector2[][] newUV = new Vector2[numberOfMeshes][];
        int[][] newTriangles = new int[numberOfMeshes][];

        for (int meshIndex = 0; meshIndex < numberOfMeshes; meshIndex++)
        {
            //int size = totalVertexCount - (meshIndex * vertexLimit) % vertexLimit;
            int size = Math.Min(vertexLimit, totalVertexCount - (meshIndex * vertexLimit));
            newVertices[meshIndex] = new Vector3[size];
            newUV[meshIndex] = new Vector2[size];
            newTriangles[meshIndex] = new int[size];
        }

        for (int i = 0; i < edges.Count; i++)
        {
            int meshIndex = i * 2 / vertexLimit;
            int vertIndex = i * 2 % vertexLimit;

            newVertices[meshIndex][vertIndex] = vertexBuffer[edges[i].IndexA];
            newVertices[meshIndex][vertIndex + 1] = vertexBuffer[edges[i].IndexB];

            // store the edge index as the UV
            newUV[meshIndex][vertIndex] = new Vector2(i + 1, 0);
            newUV[meshIndex][vertIndex + 1] = new Vector2(i + 1, 0);

            // since it's a line list, the 
            newTriangles[meshIndex][vertIndex] = vertIndex;
            newTriangles[meshIndex][vertIndex + 1] = vertIndex + 1;
        }

        for (int meshIndex = 0; meshIndex < numberOfMeshes; meshIndex++)
        {
            edgeMeshes.Add(new Mesh());
            edgeMeshes[meshIndex].vertices = newVertices[meshIndex];
            edgeMeshes[meshIndex].uv = newUV[meshIndex];
            //edgeMeshes[meshIndex].triangles = newTriangles[meshIndex];
            edgeMeshes[meshIndex].SetIndices(newTriangles[meshIndex], MeshTopology.Lines, 0);

            GameObject edgeMeshGameObject = new GameObject(name + ".Edges" + meshIndex);
            MeshFilter meshFilter = edgeMeshGameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = edgeMeshes[meshIndex];
            edgeMeshGameObject.AddComponent<MeshRenderer>();
        }

        stopwatch.Stop();
        Debug.LogFormat("{0}: Creating {1} edge mesh(es) took {1}ms", name, stopwatch.ElapsedMilliseconds);
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
