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

        public Edge(int a, int b, int face) //: this()
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

    // the original mesh data
    private Mesh mesh;
    int[] indexBuffer;
    Vector3[] vertexBuffer;

    public bool alwaysFindSilhouettes;
    public KeyCode createSilhouetteKey = KeyCode.Return;

    public Material edgeMaterial;

    // a mesh consisting of all of the edge lines (divided into groups of 65,000 verts)
    private List<Mesh> edgeMeshes;

    // a mesh of the current silhouette
    private Mesh silhouetteMesh;
    private int[] silhouetteIndexBuffer;
    private Color32[] silhouetteColorBuffer;
    private Vector3[] silhouetteVertexBuffer;
    private MeshFilter silhouetteMeshFilter;

    public Camera edgeCamera;
    private RenderTexture renderTexture;
    private Texture2D storageTexture;
    private Color32[] pixelData;
    private Rect textureRect;

    public Dictionary<int, Edge> visibleSilhouettes = new Dictionary<int, Edge>();
    public Dictionary<int, Segment> silhouetteSegments = new Dictionary<int, Segment>();

    void Reset()
    {
        // TODO: Load the pre-made .mat
        edgeMaterial = new Material(Shader.Find("NPR/EdgeLines"));
    }

    void Start()
    {
        Debug.LogFormat("{0}.Start()", name);

        // Terminology:
        // Edge = the line between two vertices in the mesh
        // Segment = a visible piece of an edge
        // Chain = a set of segments connected together to create a single stroke

        // 0) [ONE-TIME] CPU: Find all edges in mesh
        // 1) GPU: Find all silhouette edges (visible and occluded)
        // 2) GPU: Render mesh as solid color and silhouette edges as lines
        //         Store all visible silhouette edge pixels (edge id, screenspace position) in append buffer
        // 3) CPU: Sort the buffer of visible edges pixels by edge id and screenspace position (can this be done on the GPU?)
        // 4) CPU: Step through the each pixel and determine start and end points of edge segments
        // 5) CPU: Step through each segment and chain them together
        // 6) GPU: Draw the chains as textured triangle lists [or line lists expanded in the geometry shader]

        GameObject silhouetteGameObject = new GameObject(name + ".Silhouette");
        silhouetteMeshFilter = silhouetteGameObject.AddComponent<MeshFilter>();
        silhouetteMeshFilter.sharedMesh = silhouetteMesh;
        MeshRenderer meshRenderer = silhouetteGameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = edgeMaterial;

        silhouetteGameObject.transform.position = transform.position;
        silhouetteGameObject.transform.rotation = transform.rotation;
        silhouetteGameObject.transform.parent = transform;
        silhouetteGameObject.transform.localScale = Vector3.one;

        //foreach (RenderTextureFormat format in Enum.GetValues(typeof(RenderTextureFormat)))
        //{
        //    Debug.LogFormat("{0} supported = {1}", format, SystemInfo.SupportsRenderTextureFormat(format));
        //}

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.sharedMesh;

        indexBuffer = mesh.triangles;
        vertexBuffer = mesh.vertices;

        // initialize sizes
        edges = new List<Edge>(indexBuffer.Length);
        faces = new List<Face>(indexBuffer.Length / 3);
        creases = new List<Edge>(indexBuffer.Length / 2); // estimate that half of the edges will be creases

        // default the silhouette to 1000 verts
        silhouetteMesh = new Mesh();
        silhouetteIndexBuffer = new int[1000];
        silhouetteColorBuffer = new Color32[1000];
        silhouetteVertexBuffer = new Vector3[1000];

        Debug.LogFormat("Creating a {0}x{1} RenderTexture", Screen.width, Screen.height);
        renderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        storageTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);
        textureRect = new Rect(0, 0, Screen.width, Screen.height);
        edgeCamera.targetTexture = renderTexture;

        FindFacesAndEdges();
        FindCreases();

        //CreateEdgeMesh();
    }

    void Update()
    {
        if (alwaysFindSilhouettes || Input.GetKeyDown(createSilhouetteKey))
        {
            GenerateSilhouetteMesh(edgeCamera);
            GetVisibleSilhouettes();
            GetSegments();
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
                    Debug.LogFormat("Crease Angle = {0} degrees", angle);
                    Edge creaseEdge = edge;
                    creaseEdge.IsCrease = true;
                    creases.Add(creaseEdge);
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
        Color32[][] newColors = new Color32[numberOfMeshes][];
        int[][] newTriangles = new int[numberOfMeshes][];

        for (int meshIndex = 0; meshIndex < numberOfMeshes; meshIndex++)
        {
            //int size = totalVertexCount - (meshIndex * vertexLimit) % vertexLimit;
            int size = Math.Min(vertexLimit, totalVertexCount - (meshIndex * vertexLimit));
            newVertices[meshIndex] = new Vector3[size];
            newColors[meshIndex] = new Color32[size];
            newTriangles[meshIndex] = new int[size];
        }

        for (int i = 0; i < edges.Count; i++)
        {
            int meshIndex = i * 2 / vertexLimit;
            int vertIndex = i * 2 % vertexLimit;

            newVertices[meshIndex][vertIndex] = vertexBuffer[edges[i].IndexA];
            newVertices[meshIndex][vertIndex + 1] = vertexBuffer[edges[i].IndexB];

            // store the edge index as the vertex color (perform a +1 in order to ensure it is 1-based, not 0-based - this allows 0s in the render texture to be ignored)
            newColors[meshIndex][vertIndex] = ColorConversion.IntToColor32(i + 1);
            newColors[meshIndex][vertIndex + 1] = ColorConversion.IntToColor32(i + 1);

            // since it's a line list, the 
            newTriangles[meshIndex][vertIndex] = vertIndex;
            newTriangles[meshIndex][vertIndex + 1] = vertIndex + 1;
        }

        for (int meshIndex = 0; meshIndex < numberOfMeshes; meshIndex++)
        {
            edgeMeshes.Add(new Mesh());
            edgeMeshes[meshIndex].vertices = newVertices[meshIndex];
            edgeMeshes[meshIndex].colors32 = newColors[meshIndex];
            //edgeMeshes[meshIndex].triangles = newTriangles[meshIndex];
            edgeMeshes[meshIndex].SetIndices(newTriangles[meshIndex], MeshTopology.Lines, 0);

            GameObject edgeMeshGameObject = new GameObject(name + ".Edges" + meshIndex);
            MeshFilter meshFilter = edgeMeshGameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = edgeMeshes[meshIndex];
            MeshRenderer meshRenderer = edgeMeshGameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = edgeMaterial;

            edgeMeshGameObject.transform.position = transform.position;
            edgeMeshGameObject.transform.rotation = transform.rotation;
            edgeMeshGameObject.transform.parent = transform;
            edgeMeshGameObject.transform.localScale = Vector3.one;
        }

        stopwatch.Stop();
        Debug.LogFormat("{0}: Creating {1} edge mesh(es) took {2}ms", name, edgeMeshes.Count, stopwatch.ElapsedMilliseconds);
    }

    private void GenerateSilhouetteMesh(Camera camera)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        silhouettes.Clear();

        stopwatch.Stop();
        Debug.LogFormat("{0}: Clearing silhouettes took {1}ms", name, stopwatch.ElapsedMilliseconds);
        stopwatch.Reset();

        // transform all verts
        //Vector3[] transformedVertexBuffer = new Vector3[vertexBuffer.Length];
        //for (int i = 0; i < vertexBuffer.Length; i++)
        //{
        //    //transformedVertexBuffer[i] = vertexBuffer[i] * transform.localToWorldMatrix;
        //    transformedVertexBuffer[i] = transform.TransformPoint(vertexBuffer[i]);
        //}

        stopwatch.Start();

        //foreach (var edge in edges)
        for (int i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];

            // TransformPoint = position, rotation, and scale used
            // TransformDirection = rotation only used
            // TransformVector = rotation and scale used
            Vector3 faceANormal = transform.TransformDirection(faces[edge.FaceA].normal);
            Vector3 faceBNormal = transform.TransformDirection(faces[edge.FaceB].normal);

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
        Debug.LogFormat("{0}: Finding {1} silhouette edges took {2}ms", name, silhouettes.Count, stopwatch.ElapsedMilliseconds);
        stopwatch.Reset();

        if (silhouettes.Count * 2 > silhouetteVertexBuffer.Length)
        {
            Debug.LogWarningFormat("Length = {0}.  Need = {1}. Growing...", silhouetteVertexBuffer.Length, silhouettes.Count * 2);

            silhouetteIndexBuffer = new int[silhouettes.Count * 2];
            silhouetteColorBuffer = new Color32[silhouettes.Count * 2];
            silhouetteVertexBuffer = new Vector3[silhouettes.Count * 2];
        }

        stopwatch.Start();

        int vertIndex;
        for (int i = 0; i < silhouettes.Count; i++)
        {
            vertIndex = i * 2;
            silhouetteVertexBuffer[vertIndex] = vertexBuffer[silhouettes[i].IndexA];
            silhouetteVertexBuffer[vertIndex + 1] = vertexBuffer[silhouettes[i].IndexB];

            // store the silhouette index as the vertex color (perform a +1 in order to ensure it is 1-based, not 0-based - this allows 0s in the render texture to be ignored)
            silhouetteColorBuffer[vertIndex] = ColorConversion.IntToColor32(i + 1);
            silhouetteColorBuffer[vertIndex + 1] = ColorConversion.IntToColor32(i + 1);

            // since it's a line list, the 
            silhouetteIndexBuffer[vertIndex] = vertIndex;
            silhouetteIndexBuffer[vertIndex + 1] = vertIndex + 1;
        }

        silhouetteMesh.vertices = silhouetteVertexBuffer;
        silhouetteMesh.colors32 = silhouetteColorBuffer;
        //silhouetteMesh.triangles = silhouetteIndexBuffer;
        silhouetteMesh.SetIndices(silhouetteIndexBuffer, MeshTopology.Lines, 0);
        silhouetteMeshFilter.sharedMesh = silhouetteMesh;

        stopwatch.Stop();
        Debug.LogFormat("{0}: Building silhouette mesh took {1}ms", name, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Gets all of the visible silhouettes by rendering out the scene into a render texture storing the edge IDs.
    /// </summary>
    private void GetVisibleSilhouettes()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        visibleSilhouettes.Clear();

        edgeCamera.Render();

        RenderTexture.active = edgeCamera.targetTexture;

        storageTexture.ReadPixels(textureRect, 0, 0, false);
        storageTexture.Apply();

        RenderTexture.active = null;

        pixelData = storageTexture.GetPixels32();
        foreach (var piece in pixelData)
        {
            if (piece.a != 0)
            {
                //Debug.LogFormat("Data = {0}", ColorConversion.Color32ToInt(piece));

                // get the color as an integer index into the edges.  perform a -1 since it was stored with a +1 to allow 0s to be ignored.
                int edgeIndex = ColorConversion.Color32ToInt(piece) - 1;
                if (!visibleSilhouettes.ContainsKey(edgeIndex))
                {
                    visibleSilhouettes.Add(edgeIndex, silhouettes[edgeIndex]);
                }
            }
        }

        stopwatch.Stop();
        Debug.LogFormat("{0}: Getting {1} visible silhouettes took {2}ms", name, visibleSilhouettes.Count, stopwatch.ElapsedMilliseconds);
        stopwatch.Reset();

        Debug.Log("Saving edges.png");
        stopwatch.Start();
        System.IO.File.WriteAllBytes("edges.png", storageTexture.EncodeToPNG());
        stopwatch.Stop();
        Debug.LogFormat("{0}: Saving edges.png took {1}ms", name, stopwatch.ElapsedMilliseconds);
    }

    private void GetSegments()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        Texture2D softwareRasterization = new Texture2D(storageTexture.width, storageTexture.height);

        silhouetteSegments.Clear();

        Debug.LogFormat("{0}: edgeCamera.pixelWidth={1}, storageTexture.width={2}", name, edgeCamera.pixelWidth, storageTexture.width);

        foreach (var edgeKVP in visibleSilhouettes)
        {
            Edge edge = edgeKVP.Value;
            int edgeIndex = edgeKVP.Key;

            // convert from model space to world space
            var vertexA = transform.TransformPoint(vertexBuffer[edge.IndexA]);
            var vertexB = transform.TransformPoint(vertexBuffer[edge.IndexB]);

            // convert from world space to screen space
            // bottom-left is (0,0) and top-right is (pixelWidth, pixelHeight)
            vertexA = edgeCamera.WorldToScreenPoint(vertexA);
            vertexB = edgeCamera.WorldToScreenPoint(vertexB);

            // ensure that vertex A is the left-most vertex
            if (vertexA.x > vertexB.x)
            {
                var temp = vertexA;
                vertexA = vertexB;
                vertexB = temp;
            }

            // "scan-convert" along the line in the render texture color data to see which part of the edge is actually present (a segment)
            // save off any neighboring edges encountered
            Segment segment = new Segment();
            segment.EdgeIndex = edgeIndex;
            int textureWidth = storageTexture.width;
            bool segmentStarted = false;
            int slope = Mathf.RoundToInt((vertexB.y - vertexA.y) / (vertexB.x - vertexA.x));
            int y = Mathf.RoundToInt(vertexA.y);
            int endX = Mathf.RoundToInt(vertexB.x);
            for (int x = Mathf.RoundToInt(vertexA.x); x <= endX; x++)
            {
                softwareRasterization.SetPixel(x, y, ColorConversion.IntToColor32(edgeIndex));

                // TODO: Add range checks
                // read pixel and surrounding pixels
                int center    = ColorConversion.Color32ToInt(pixelData[y * textureWidth + x]) - 1;
                int upLeft    = ColorConversion.Color32ToInt(pixelData[(y - 1) * textureWidth + x - 1]) - 1;
                int up        = ColorConversion.Color32ToInt(pixelData[(y - 1) * textureWidth + x]) - 1;
                int upRight   = ColorConversion.Color32ToInt(pixelData[(y - 1) * textureWidth + x + 1]) - 1;
                int left      = ColorConversion.Color32ToInt(pixelData[y * textureWidth + x - 1]) - 1;
                int right     = ColorConversion.Color32ToInt(pixelData[y * textureWidth + x + 1]) - 1;
                int downLeft  = ColorConversion.Color32ToInt(pixelData[(y + 1) * textureWidth + x - 1]) - 1;
                int down      = ColorConversion.Color32ToInt(pixelData[(y + 1) * textureWidth + x]) - 1;
                int downRight = ColorConversion.Color32ToInt(pixelData[(y + 1) * textureWidth + x + 1]) - 1;

                //if (upLeft != -1 || up != -1 || upRight != -1 ||
                //    left != -1 || center != -1 || right != -1 ||
                //    downLeft != -1 || down != -1 || downRight != -1)
                //{
                //    Debug.LogFormat("Found actual edge!");
                //}

                // store any neighboring edges we encounter along this edge
                if (upLeft > 0 && upLeft != edgeIndex && !segment.NeighborEdges.Contains(upLeft))
                {
                    segment.NeighborEdges.Add(upLeft);
                }
                if (up > 0 && up != edgeIndex && !segment.NeighborEdges.Contains(up))
                {
                    segment.NeighborEdges.Add(up);
                }
                if (upRight > 0 && upRight != edgeIndex && !segment.NeighborEdges.Contains(upRight))
                {
                    segment.NeighborEdges.Add(upRight);
                }
                if (left > 0 && left != edgeIndex && !segment.NeighborEdges.Contains(left))
                {
                    segment.NeighborEdges.Add(left);
                }
                if (right > 0 && right != edgeIndex && !segment.NeighborEdges.Contains(right))
                {
                    segment.NeighborEdges.Add(right);
                }
                if (downLeft > 0 && downLeft != edgeIndex && !segment.NeighborEdges.Contains(downLeft))
                {
                    segment.NeighborEdges.Add(downLeft);
                }
                if (down > 0 && down != edgeIndex && !segment.NeighborEdges.Contains(down))
                {
                    segment.NeighborEdges.Add(upLeft);
                }
                if (downRight > 0 && downRight != edgeIndex && !segment.NeighborEdges.Contains(downRight))
                {
                    segment.NeighborEdges.Add(downRight);
                }

                // start or end the segment if the edge was detected or not
                if (upLeft == edgeIndex || up == edgeIndex || upRight == edgeIndex ||
                    left == edgeIndex || center == edgeIndex || right == edgeIndex ||
                    downLeft == edgeIndex || down == edgeIndex || downRight == edgeIndex)
                {
                    if (!segmentStarted)
                    {
                        segmentStarted = true;
                        segment.EndpointA = new Vector3(x, y);
                    }
                }
                else
                {
                    if (segmentStarted)
                    {
                        // the segment is now ended
                        // TODO: Add support for multiple segments formed from the same edge
                        segment.EndpointB = new Vector3(x, y);
                        silhouetteSegments.Add(edgeIndex, segment);
                        break;
                    }
                }

                // increment y
                y = y + slope;
            }
        }

        System.IO.File.WriteAllBytes("softwareRasterization.png", softwareRasterization.EncodeToPNG());

        stopwatch.Stop();
        Debug.LogFormat("{0}: Getting {1} segments took {2}ms", name, silhouetteSegments.Count, stopwatch.ElapsedMilliseconds);
    }

    public Vector3 GetVertex(int index)
    {
        return vertexBuffer[index];
    }
}
