using UnityEngine;
using System.Collections.Generic;

public class Segment
{
    public Vector3 EndpointA { get; set; }

    public Vector3 EndpointB { get; set; }

    public int EdgeIndex { get; set; }

    public List<int> NeighborEdges { get; set; }

    public Segment()
    {
        NeighborEdges = new List<int>();
    }
}
