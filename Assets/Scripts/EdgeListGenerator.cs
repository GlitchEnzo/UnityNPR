using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public Dictionary<Edge, Edge> edges = new Dictionary<Edge, Edge>();

    public List<Face> faces = new List<Face>();

    public List<Edge> creases = new List<Edge>();
    public List<Edge> silhouettes = new List<Edge>();

    private Mesh mesh;
    int[] indexBuffer;
    Vector3[] vertexBuffer;

    void Start()
    {
        Debug.LogFormat("{0}.Start()", name);

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.sharedMesh;

        indexBuffer = mesh.triangles;
        vertexBuffer = mesh.vertices;

        // initialize sizes
        edges = new Dictionary<Edge, Edge>(indexBuffer.Length);
        faces = new List<Face>(indexBuffer.Length / 3);
        creases = new List<Edge>(indexBuffer.Length / 2); // estimate that half of the edges will be creases
        
        FindFacesAndEdges();
        FindCreases();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
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

        Edge temp;

        for (int i = 0; i <= indexBuffer.Length - 3; i += 3)
        {
            Face face = new Face(indexBuffer[i], indexBuffer[i + 1], indexBuffer[i + 2], vertexBuffer);
            faces.Add(face);
            int faceIndex = faces.Count - 1;

            Edge edge = new Edge(indexBuffer[i], indexBuffer[i + 1], faceIndex);
            if (!edges.TryGetValue(edge, out temp))
            {
                edges.Add(edge, edge);
            }
            else
            {
                temp.FaceB = faceIndex;
            }

            edge = new Edge(indexBuffer[i + 1], indexBuffer[i + 2], faceIndex);
            if (!edges.TryGetValue(edge, out temp))
            {
                edges.Add(edge, edge);
            }
            else
            {
                temp.FaceB = faceIndex;
            }

            edge = new Edge(indexBuffer[i + 2], indexBuffer[i], faceIndex);
            if (!edges.TryGetValue(edge, out temp))
            {
                edges.Add(edge, edge);
            }
            else
            {
                temp.FaceB = faceIndex;
            }
        }

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
            if (edge.Value.FaceA == edge.Value.FaceB)
            {
                // if the edge is only on one face, it is a crease too
                //edge.Value.IsCrease = true;
                //creases.Add(edge.Value);
            }
            else
            {
                float dotProduct = Vector3.Dot(faces[edge.Value.FaceA].normal, faces[edge.Value.FaceB].normal);
                float angle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

                if (angle > thresholdAngle)
                {
                    Debug.LogFormat("Angle = {0} degrees", angle);
                    edge.Value.IsCrease = true;
                    creases.Add(edge.Value);
                }
            }
        }

        Debug.LogFormat("{0}: There were {1} creases found.", name, creases.Count);

        stopwatch.Stop();
        Debug.LogFormat("{0}: Finding creases took {1}ms", name, stopwatch.ElapsedMilliseconds);
    }

    private void Rasterize(Camera camera)
    {
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
            Vector3 faceANormal = transform.TransformVector(faces[edge.Value.FaceA].normal);
            Vector3 faceBNormal = transform.TransformVector(faces[edge.Value.FaceB].normal);

            bool isFaceAForward = Vector3.Dot(faceANormal, camera.transform.forward) < 0;
            bool isFaceBForward = Vector3.Dot(faceBNormal, camera.transform.forward) < 0;

            if (isFaceAForward ^ isFaceBForward)
            {
                edge.Value.IsSilhouette = true;
                silhouettes.Add(edge.Value);
            }
            else
            {
                edge.Value.IsSilhouette = false;
            }
        }
    }

    public Vector3 GetVertex(int index)
    {
        return vertexBuffer[index];
    }
}
