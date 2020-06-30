using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TChunk : MonoBehaviour
{
    int id;
    int chunkCoordX;
    int chunkCoordZ;

    Vector3[] vertices;
    int[] triangles;
    Vector2[] uv;

    List<Vector3> sideVertices;
    List<int> sideTriangles;

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
        for (int i = 0; i < verticesFromTyle.Length; i++)
        {
            vertices[verticesStartIndex + i] = verticesFromTyle[i];
        }

        int tSI = verticesStartIndex / 4 * 6;
        triangles[tSI] = verticesStartIndex;
        triangles[tSI + 1] = verticesStartIndex + 3;
        triangles[tSI + 2] = verticesStartIndex + 2;
        triangles[tSI + 3] = verticesStartIndex;
        triangles[tSI + 4] = verticesStartIndex + 2;
        triangles[tSI + 5] = verticesStartIndex + 1;

    }

    public void CreateSidemeshFromTyles()
    {
        if (sideMesh == null) CreateSideMeshObject();

        sideVertices = new List<Vector3>();
        sideTriangles = new List<int>();

        for (int z = 0, i = 0; z < cg.tylesPerChunkZ; z++)
        {
            for (int x = 0; x < cg.tylesPerChunkX; x++, i += 4)
            {
                Tyle t = tgt.tyles[x + z * tgt.tylesX + chunkCoordX * cg.tylesPerChunkX + chunkCoordZ * tgt.tylesX * cg.tylesPerChunkZ];

                for (int ti = 0; ti < 4; ti++)
                {
                    if (t.neighbours[ti] != null)
                    {
                        if (IsDirectionStepped(t,ti))
                        {
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
            return true;
        }

        return false;
    }



    void AddSideQuad(Tyle ty, Tyle neighbourTy, int neighbourDirection)
    {
        int startVertex = sideVertices.Count;
        Vector3[] v = new Vector3[4];

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



        v[0] = ty.GetVertice(vIndeces[0]);
        v[1] = ty.GetVertice(vIndeces[1]);
        v[2] = neighbourTy.GetVertice(vIndeces[2]);
        v[3] = neighbourTy.GetVertice(vIndeces[3]);

        int[] t = new int[6];

        t[0] = startVertex;
        t[1] = startVertex + 1;
        t[2] = startVertex + 3;
        t[3] = startVertex;
        t[4] = startVertex + 3;
        t[5] = startVertex + 2;

        sideVertices.AddRange(v);
        sideTriangles.AddRange(t);
    }

    public void ReGenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = $"chunkmesh {chunkCoordX}/{chunkCoordZ}";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    void CreateSideMeshObject()
    {
        sideMesh = new GameObject($"sidemesh {chunkCoordX}/{chunkCoordZ}");
        sideMesh.AddComponent(typeof(MeshFilter));
        sideMesh.AddComponent(typeof(MeshRenderer));
        sideMesh.AddComponent(typeof(MeshCollider));
        sideMesh.transform.parent = transform;

        sideMesh.GetComponent<Renderer>().material = Resources.Load("Materials/sidemeshMaterial", typeof(Material)) as Material;

        Debug.Log($"sideMesh renderer = {sideMesh.GetComponent<Renderer>().material}");

    }

    public void ReGenerateSidemesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = sideVertices.ToArray();
        mesh.triangles = sideTriangles.ToArray();
        mesh.RecalculateNormals();
        //transform.Find($"sidemesh {chunkCoordX}/{chunkCoordZ}").GetComponent<MeshFilter>().mesh = mesh;
        sideMesh.GetComponent<MeshFilter>().mesh = mesh;
        sideMesh.GetComponent<MeshCollider>().sharedMesh = mesh;
    }
}
