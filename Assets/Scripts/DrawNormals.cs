using UnityEngine;

[RequireComponent(typeof(EdgeListGenerator))]
public class DrawNormals : MonoBehaviour
{
    private EdgeListGenerator edgeList;

    private void Start()
    {
        edgeList = GetComponent<EdgeListGenerator>();
    }

    void Update()
    {
        foreach (var face in edgeList.faces)
        {
            Debug.DrawRay(transform.position + face.centroid, face.normal, Color.green);
        }
    }

    //void OnDrawGizmosSelected()
    //{
    //    if (edgeList != null)
    //    {
    //        Gizmos.color = Color.red;
    //        foreach (var face in edgeList.faces)
    //        {
    //            Gizmos.DrawRay(transform.position + face.centroid, face.normal);
    //        }
    //    }
    //}
}
