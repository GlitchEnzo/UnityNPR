using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class EdgeListGenerator : MonoBehaviour
{
    [Serializable]
    public struct Edge
    {
        public int indexA;
        public int indexB;

        public Edge(int a, int b)
        {
            indexA = a;
            indexB = b;
        }
    }

    public List<Edge> edges = new List<Edge>(); 

    private Mesh mesh;

    // Use this for initialization
    void Start()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;

        Debug.LogFormat("There should be {0} edges or less.", mesh.triangles.Length);

        for (int i = 0; i <= mesh.triangles.Length - 3; i+=3)
        {
            // TODO: Check if already in list
            edges.Add(new Edge(mesh.triangles[i], mesh.triangles[i+1]));
            edges.Add(new Edge(mesh.triangles[i+1], mesh.triangles[i+2]));
            edges.Add(new Edge(mesh.triangles[i+2], mesh.triangles[i]));
        }
    }
}
