using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityNPR
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Edge
    {
        public Vector3 NormalA;
        public Vector3 NormalB;

        public Vector3 VertexA;
        public Vector3 VertexB;
    }

    //[StructLayout(LayoutKind.Sequential)]
    //public struct SilhouetteEdge
    //{
    //    public Vector3 VertexA;
    //    public Vector3 VertexB;
    //}
}
