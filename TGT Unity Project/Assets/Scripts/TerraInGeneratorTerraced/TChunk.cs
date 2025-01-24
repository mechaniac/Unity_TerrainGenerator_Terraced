using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TChunk : MonoBehaviour
{
    int id;
    int chunkCoordX;
    int chunkCoordZ;

    int quadCountHelper;
    int chunkCountHelper;

    Vector3[] vertices;
    int[] triangles;
    Vector2[] uv;

    List<Vector3> sideVertices;
    List<int> sideTriangles;
    List<Vector2> sideUv;

    TGeneratorT tgt;
    ChunkGenerator cg;

    GameObject sideMesh;

    public void InitializeChunk(int x, int z, int i, TGeneratorT tgt, ChunkGenerator cg)
    {
        chunkCoordX = x;
        chunkCoordZ = z;
        id = i;

        this.tgt = tgt;
        this.cg = cg;

        vertices = new Vector3[cg.tylesPerChunkX * cg.tylesPerChunkZ * 4];
        triangles = new int[cg.tylesPerChunkX * cg.tylesPerChunkZ * 6];

        uv = new Vector2[vertices.Length];
    }

    public void CreateMeshFromTyles()
    {
        for (int z = 0, i = 0; z < cg.tylesPerChunkZ; z++)
        {
            for (int x = 0; x < cg.tylesPerChunkX; x++, i += 4)
            {
                Tyle t = tgt.tyles[x + z * tgt.tylesX + chunkCoordX * cg.tylesPerChunkX + chunkCoordZ * tgt.tylesX * cg.tylesPerChunkZ];
                AddTopQuad(i, t.GetVertices());
            }
        }
    }

    void AddTopQuad(int verticesStartIndex, Vector3[] verticesFromTyle)
    {
        Vector3 chunkOffset = new Vector3(chunkCoordX * cg.tylesPerChunkX * tgt.widthPerPixel, 0, chunkCoordZ * cg.tylesPerChunkZ * tgt.widthPerPixel);

        for (int i = 0; i < verticesFromTyle.Length; i++)
        {

            vertices[verticesStartIndex + i] = verticesFromTyle[i] - chunkOffset;
            //uv[verticesStartIndex + i] = new Vector2( verticesFromTyle[i].x, verticesFromTyle[i].z);
        }

        int tSI = verticesStartIndex / 4 * 6;
        triangles[tSI] = verticesStartIndex;
        triangles[tSI + 1] = verticesStartIndex + 3;
        triangles[tSI + 2] = verticesStartIndex + 2;
        triangles[tSI + 3] = verticesStartIndex;
        triangles[tSI + 4] = verticesStartIndex + 2;
        triangles[tSI + 5] = verticesStartIndex + 1;

        Vector2[] uvPerQuad = new Vector2[4];

        uvPerQuad[0] = new Vector2(0, 0);
        uvPerQuad[1] = new Vector2(1, 0);
        uvPerQuad[2] = new Vector2(1, 1);
        uvPerQuad[3] = new Vector2(0, 1);

        for (int i = 0; i < uvPerQuad.Length; i++)
        {
            uv[verticesStartIndex + i] = uvPerQuad[i];
        }


    }

    public void CreateSidemeshFromTyles()
    {
        Debug.Log($"ChunkCountHelper ChunkCountHelper ChunkCountHelper ChunkCountHelper ChunkCountHelper ChunkCountHelper: {chunkCountHelper ++}");
        Debug.Log("SideMeshCreation");
        if (sideMesh == null) CreateSideMeshObject();

        sideVertices = new List<Vector3>();
        sideTriangles = new List<int>();
        sideUv = new List<Vector2>();

        for (int z = 0, i = 0; z < cg.tylesPerChunkZ; z++)
        {
            
            for (int x = 0; x < cg.tylesPerChunkX; x++, i += 4)
            {
                Tyle t = tgt.tyles[x + z * tgt.tylesX + chunkCoordX * cg.tylesPerChunkX + chunkCoordZ * tgt.tylesX * cg.tylesPerChunkZ];

                for (int ti = 0; ti < 4; ti++)
                {
                    if (t.neighbours[ti] != null)
                    {
                        if (IsDirectionStepped(t, ti))
                        {
                            Debug.Log($"QuadCountHelper: {quadCountHelper++}");
                            Debug.Log($"sideMesh add Quad, {z}, {i}, {x}, {ti}");
                            AddSideQuad(t, t.neighbours[ti], ti);
                        }
                    }
                }
            }
        }
    }


    bool IsDirectionStepped(Tyle t, int neighbourDirection)
    {

        int[] vI;
        switch (neighbourDirection)
        {
            default:
                vI = new int[] { 1, 2, 0, 3 };
                break;
            case 1:
                vI = new int[] { 2, 3, 1, 0 };
                break;
            case 2:
                vI = new int[] { 3, 0, 2, 1 };
                break;
            case 3:
                vI = new int[] { 0, 1, 3, 2 };
                break;
        }


        if (t.vPillars[vI[0]].vertexHeights[vI[0]] != t.vPillars[vI[0]].vertexHeights[vI[2]] ||
            t.vPillars[vI[1]].vertexHeights[vI[0]] != t.vPillars[vI[1]].vertexHeights[vI[3]]
            )
        {
            // Debug.Log("Step Found!");
            return true;
        }

        return false;
    }



    void AddSideQuad(Tyle ty, Tyle neighbourTy, int neighbourDirection)
    {
        //OffsetFromCenter Prep

        float chunkOffsetX = 0f;
        float chunkOffsetZ = 0f;

        if (tgt.setMapToSceneCenter)
        {
            chunkOffsetX = -cg.tylesPerChunkX * cg.chunkCountX * tgt.widthPerPixel / 2f;
            chunkOffsetZ = -cg.tylesPerChunkZ * cg.chunkCountZ * tgt.widthPerPixel / 2f;
        }
        Vector3 offsetVector = new Vector3(chunkOffsetX, 0, chunkOffsetZ);

        // Begin Code


        int startVertex = sideVertices.Count;   //continuing the list

        Vector3[] v = new Vector3[4];   //the VERTICES

        int[] vIndeces;

        switch (neighbourDirection)
        {
            default:
                vIndeces = new int[] { 1, 2, 0, 3 };
                break;
            case 1:
                vIndeces = new int[] { 2, 3, 1, 0 };
                break;
            case 2:
                vIndeces = new int[] { 3, 0, 2, 1 };
                break;
            case 3:
                vIndeces = new int[] { 0, 1, 3, 2 };
                break;
        }



        v[0] = ty.GetVertex(vIndeces[0]) + offsetVector;
        v[1] = ty.GetVertex(vIndeces[1]) + offsetVector;
        v[2] = neighbourTy.GetVertex(vIndeces[2]) + offsetVector;
        v[3] = neighbourTy.GetVertex(vIndeces[3]) + offsetVector;

        int[] t = new int[6];

        t[0] = startVertex;
        t[1] = startVertex + 1;
        t[2] = startVertex + 3;
        t[3] = startVertex;
        t[4] = startVertex + 3;
        t[5] = startVertex + 2;

        Vector2[] uv = new Vector2[4];

        uv[0] = new Vector2(0, v[0].y / tgt.widthPerPixel);
        uv[1] = new Vector2(1, v[1].y / tgt.widthPerPixel);
        uv[2] = new Vector2(0, v[2].y / tgt.widthPerPixel);
        uv[3] = new Vector2(1, v[3].y / tgt.widthPerPixel);


        sideVertices.AddRange(v);
        sideTriangles.AddRange(t);
        sideUv.AddRange(uv);
    }

    public void ReGenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = $"chunkmesh {chunkCoordX}/{chunkCoordZ}";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        // mesh.RecalculateNormals();
        RecalculateNormalsSeamless(mesh);

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    public void ReGenerateSidemesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = sideVertices.ToArray();
        mesh.triangles = sideTriangles.ToArray();
        mesh.uv = sideUv.ToArray();

        // mesh.RecalculateNormals();
        RecalculateNormalsSeamless(mesh);
        //transform.Find($"sidemesh {chunkCoordX}/{chunkCoordZ}").GetComponent<MeshFilter>().mesh = mesh;
        sideMesh.GetComponent<MeshFilter>().mesh = mesh;
        sideMesh.GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    static void RecalculateNormalsSeamless(Mesh mesh)
    {
        var trianglesOriginal = mesh.triangles;
        var triangles = trianglesOriginal.ToArray();

        var vertices = mesh.vertices;

        var mergeIndices = new Dictionary<int, int>();

        for (int i = 0; i < vertices.Length; i++)
        {
            var vertexHash = vertices[i].GetHashCode();

            if (mergeIndices.TryGetValue(vertexHash, out var index))
            {
                for (int j = 0; j < triangles.Length; j++)
                    if (triangles[j] == i)
                        triangles[j] = index;
            }
            else
                mergeIndices.Add(vertexHash, i);
        }

        mesh.triangles = triangles;

        var normals = new Vector3[vertices.Length];

        mesh.RecalculateNormals();
        var newNormals = mesh.normals;

        for (int i = 0; i < vertices.Length; i++)
            if (mergeIndices.TryGetValue(vertices[i].GetHashCode(), out var index))
                normals[i] = newNormals[index];

        mesh.triangles = trianglesOriginal;
        mesh.normals = normals;
    }

    void CreateSideMeshObject()
    {
        sideMesh = new GameObject($"{tgt.mapName}_sidemesh_{chunkCoordX}/{chunkCoordZ}");
        sideMesh.AddComponent(typeof(MeshFilter));
        sideMesh.AddComponent(typeof(MeshRenderer));
        sideMesh.AddComponent(typeof(MeshCollider));
        sideMesh.transform.parent = transform;

        // Ensure the material is shared across all meshes

        var renderer = sideMesh.GetComponent<Renderer>();

        if (renderer.sharedMaterial != tgt.sideMeshMat)
        {
            renderer.sharedMaterial = tgt.sideMeshMat; // Use sharedMaterial to avoid instances
        }
        // Debug.Log($"Material Instance ID: {sideMesh.GetComponent<Renderer>().sharedMaterial.GetInstanceID()}");

    }


}
