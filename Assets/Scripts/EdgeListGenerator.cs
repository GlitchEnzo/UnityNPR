using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(MeshFilter))]
public class EdgeListGenerator : MonoBehaviour
{
    public interface IEdgeID
    {
        int IndexA { get; set; }
        int IndexB { get; set; }
    }

    [Serializable]
    public struct Edge : IEdgeID
    {
        public int IndexA { get; set; }
        public int IndexB { get; set; }

        public int FaceA { get; set; }
        public int FaceB { get; set; }

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
            return IndexA.GetHashCode() ^ IndexB.GetHashCode();
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

        public Face(int a, int b, int c, Mesh mesh)
        {
            indexA = a;
            indexB = b;
            indexC = c;

            normal = Vector3.zero;
            Vector3 U = mesh.vertices[indexB] - mesh.vertices[indexA];
            Vector3 V = mesh.vertices[indexC] - mesh.vertices[indexA];
            normal = Vector3.Cross(U, V).normalized;

            centroid = (mesh.vertices[indexA] + mesh.vertices[indexB] + mesh.vertices[indexC]) / 3.0f;
        }

        public override string ToString()
        {
            return string.Format("Face(verts={0}, {1}, {2}; normal={3})", indexA, indexB, indexC, normal);
        }
    }

    public Dictionary<IEdgeID, Edge> edges = new Dictionary<IEdgeID, Edge>();

    public List<Face> faces = new List<Face>();

    public List<Edge> creases = new List<Edge>();

    private Mesh mesh;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;

        // initialize sizes
        edges = new Dictionary<IEdgeID, Edge>(mesh.triangles.Length);
        faces = new List<Face>(mesh.triangles.Length / 3);
        creases = new List<Edge>(mesh.triangles.Length / 2); // estimate that half of the edges will be creases
        
        FindFacesAndEdges();
        FindCreases();
    }

    private void FindFacesAndEdges()
    {
        Debug.LogFormat("{0}: There should be {1} edges or less.", name, mesh.triangles.Length);
        Debug.LogFormat("{0}: There are {1} vertices.", name, mesh.vertexCount);

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        for (int i = 0; i <= mesh.triangles.Length - 3; i += 3)
        {
            Face face = new Face(mesh.triangles[i], mesh.triangles[i + 1], mesh.triangles[i + 2], mesh);
            faces.Add(face);
            int faceIndex = faces.Count - 1;

            Edge edge = new Edge(mesh.triangles[i], mesh.triangles[i + 1], faceIndex);
            if (!edges.ContainsKey(edge))
                edges.Add(edge, edge);
            else
            {
                Edge temp = edges[edge];
                temp.FaceB = faceIndex;
                edges[edge] = temp;
            }

            edge = new Edge(mesh.triangles[i + 1], mesh.triangles[i + 2], faceIndex);
            if (!edges.ContainsKey(edge))
                edges.Add(edge, edge);
            else
            {
                Edge temp = edges[edge];
                temp.FaceB = faceIndex;
                edges[edge] = temp;
            }

            edge = new Edge(mesh.triangles[i + 2], mesh.triangles[i], faceIndex);
            if (!edges.ContainsKey(edge))
                edges.Add(edge, edge);
            else
            {
                Edge temp = edges[edge];
                temp.FaceB = faceIndex;
                edges[edge] = temp;
            }
        }

        Debug.LogFormat("{0}: There were {1} unique edges found.", name, edges.Count);
        Debug.LogFormat("{0}: There were {1} faces found.", name, faces.Count);

        stopwatch.Stop();
        Debug.LogFormat("Finding edges took {0}ms", stopwatch.ElapsedMilliseconds);
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
                //creases.Add(edge.Value);
            }
            else
            {
                float dotProduct = Vector3.Dot(faces[edge.Value.FaceA].normal, faces[edge.Value.FaceB].normal);
                float angle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

                if (angle > thresholdAngle)
                {
                    Debug.LogFormat("Angle = {0} degrees", angle);
                    creases.Add(edge.Value);
                }
            }
        }

        Debug.LogFormat("{0}: There were {1} creases found.", name, creases.Count);

        stopwatch.Stop();
        Debug.LogFormat("Finding creases took {0}ms", stopwatch.ElapsedMilliseconds);
    }

    public Vector3 GetVertex(int index)
    {
        return mesh.vertices[index];
    }
}
