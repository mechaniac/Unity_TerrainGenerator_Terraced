using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TChunk : MonoBehaviour
{
    int id;
    int chunkCoordX;
    int chunkCoordZ;

    int quadCountHelper;
    int chunkCountHelper;

    Vector3 chunkOffset;

    //TEMP Variables

    List<Vector3> vertices_temp;
    List<int> triangles_temp;
    List<Vector2> uv_temp;

    List<Vector3> normals_temp;

    // ------------

    Vector3[] vertices;
    int[] triangles;
    Vector2[] uv;
    Vector3[] normals;

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

        // vertices = new Vector3[cg.tylesPerChunkX * cg.tylesPerChunkZ * 4];
        // triangles = new int[cg.tylesPerChunkX * cg.tylesPerChunkZ * 6];

        // uv = new Vector2[vertices.Length];
    }

    public void GenerateMeshes()
    {
        InitializeTempLists();

        SetVerticesFromPillars();

        SetMeshesFromTyles();



        DistortVerticesSine(tgt.sineAmplitude, tgt.sineFrequency, tgt.sinePhase);


        FinalizeListsToArrays();
        // ChunkLog();

        GenerateMesh();
        // ChunkLog();
        // SetSideVerticesFromTyles();
        // GenerateSidemesh();
    }

    void ChunkLog()
    {
        Debug.Log($"Chunk ------------------------------------------------------------------------------------------------------------- {chunkCoordX}, {chunkCoordZ}");
        Debug.Log($"Generated Vertices {vertices.Length}, temp: {vertices_temp.Count}");

        Debug.Log($"Generated Triangles {triangles.Length / 3}, temp: {triangles_temp.Count / 3}");

        Debug.Log($"Generated UVs: {uv.Length}, temp: {uv_temp.Count}");

        for (int i = 0; i < triangles.Length; i++)
        {
            if (i % 6 == 0) { Debug.Log($"{i / 6} next quad"); }

            Debug.Log($"triangle {i} points to vertex {triangles[i]}, sitting at {vertices[triangles[i]]}");
        }
    }


    public void DistortVerticesSine(float amplitude, float frequency, float phase)
    {
        if (vertices_temp == null || vertices_temp.Count == 0)
        {
            throw new InvalidOperationException("Vertices list is null or empty");
        }

        for (int i = 0; i < vertices_temp.Count; i++)
        {
            Vector3 vertex = vertices_temp[i];
            float sineOffset = Mathf.Sin(frequency * (vertex.x + vertex.z) + phase) * amplitude;
            vertices_temp[i] = new Vector3(vertex.x, vertex.y + sineOffset, vertex.z);
        }
    }

    public void InitializeTempLists()
    {
        triangles_temp = new List<int>();
        uv_temp = new List<Vector2>();
        vertices_temp = new List<Vector3>();
        normals_temp = new List<Vector3>();

        chunkOffset = new Vector3(chunkCoordX * cg.tylesPerChunkX * tgt.widthPerPixel, 0, chunkCoordZ * cg.tylesPerChunkZ * tgt.widthPerPixel);
    }

    public void FinalizeListsToArrays()
    {


        vertices = vertices_temp.ToArray();
        triangles = triangles_temp.ToArray();
        uv = uv_temp.ToArray();
        normals = normals_temp.ToArray(); // Add this line

        // Clear and destroy temporary lists
        // triangles_temp.Clear();
        // uv_temp.Clear();
        // vertices_temp.Clear();
        // normals_temp.Clear();

        // triangles_temp = null;
        // uv_temp = null;
        // vertices_temp = null;
        // normals_temp = null;
    }

    public void SetVerticesFromPillars()
    {
        int i = 0;
        for (int z = 0; z < cg.tylesPerChunkZ + 1; z++)
        {
            for (int x = 0; x < cg.tylesPerChunkX + 1; x++, i++)
            {
                // int pillarIndex = x + z * (tgt.tylesX + 1) + chunkCoordX * (cg.tylesPerChunkX + 1) + chunkCoordZ * (tgt.tylesX + 1) * (cg.tylesPerChunkZ + 1);

                // int pillarIndex = x
                // + z * (tgt.tylesX + 1)  // Local offset within the chunk
                // + chunkCoordX * (cg.tylesPerChunkX + 1)  // Horizontal chunk offset
                // + chunkCoordZ * (cg.tylesPerChunkX + 1) * (cg.tylesPerChunkZ + 1);  // Vertical chunk offset

                int pillarIndex = x 
                + z * (tgt.tylesX + 1)  // Local offset within the global grid
                + chunkCoordX * cg.tylesPerChunkX  // Global horizontal chunk offset
                + chunkCoordZ * (tgt.tylesX + 1) * cg.tylesPerChunkZ;  // Global vertical chunk offset


                Debug.Log($"Pillar {i}, {pillarIndex} : x: {x}, z: {z}, tyles per chunkx {cg.tylesPerChunkX}, position: {tgt.vPillars[pillarIndex].transform.position}, pillarOriginal ID: {tgt.vPillars[pillarIndex].name}");

                if (pillarIndex < tgt.vPillars.Length && tgt.vPillars[pillarIndex] != null)
                {
                    Debug.Log("added!");
                    VPillar p = tgt.vPillars[pillarIndex];

                    vertices_temp.Add(p.transform.position);


                    // Calculate normal (assuming quad is planar)
                    normals_temp.Add(Vector3.zero);


                    // Add UV coordinates
                    uv_temp.Add(new Vector2(x, z));
                }
            }

        }
        // Debug.Log($"vertext GENERATION DONE. number of vertices added: {i} vertext GENERATION DONE. number of vertices added: {i} vertext GENERATION DONE. number of vertices added: {i} COUNT of vertices Temp {vertices_temp.Count}");
    }

    public void SetMeshesFromTyles()
    {
        for (int z = 0, i = 0; z < cg.tylesPerChunkZ; z++)
        {
            for (int x = 0; x < cg.tylesPerChunkX; x++, i += 4)
            {
                int insideChunkOffset = x + z * tgt.tylesX;
                int chunkXOffset = chunkCoordX * cg.tylesPerChunkX;
                int chunkZOffset = chunkCoordZ * cg.tylesPerChunkZ * cg.tylesPerChunkX * cg.chunkCountX;
                int tyleIndex = insideChunkOffset + chunkXOffset + chunkZOffset;

                // Debug.Log($"insideChunkOffsett {insideChunkOffset}, chunkxOffset: {chunkXOffset}, chunkZOffset {chunkZOffset}");

                if (tgt.tyles[tyleIndex] != null)
                {
                    Tyle t = tgt.tyles[tyleIndex];
                    // Debug.Log($"Tyle {t.name}, index: {tyleIndex}");
                    AddTopQuad(x, z);
                }

            }
        }

    }


    void AddTopQuad(int x, int z)
    {
        //vertices already set from pillars    
        // int c = vertices_temp.Count;

        // Add triangles
        int oA = x + z * cg.vertPerChunkX; //the OFFSET MULTIPLIER

        int[] t = new int[6];
        t[0] = oA;
        t[1] = cg.vertPerChunkX + oA;
        t[2] = cg.vertPerChunkX + 1 + oA;
        t[3] = cg.vertPerChunkX + 1 + oA;
        t[4] = 1 + oA;
        t[5] = oA;

        foreach (var index in t)
        {
            triangles_temp.Add(index);
        }


        // LOGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGING
        // for (int i = 0; i < t.Length; i++)
        // {
        //     Debug.Log($"triangle {i} is set to vertex {t[i]}");
        //     Debug.Log($"vertices length: {vertices.Length}");
        //     Debug.Log($"triangle {i} sits at {vertices_temp[t[i]]}");
        // }





        // Log triangle indices and check validity
        int hardcodedVertexCount = 121; // Example hardcoded vertex count

        for (int i = 0; i < t.Length; i++)
        {
            if (t[i] >= hardcodedVertexCount || t[i] < 0)
            {
                Debug.LogError($"Invalid triangle index {t[i]} at position {i} in quad {x}, {z}.");
            }
        }

    }



    public void SetSideVerticesFromTyles()
    {

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
    void ValidateTriangleIndices(int[] t, int vertexCount)
    {
        for (int i = 0; i < t.Length; i++)
        {
            if (t[i] >= vertexCount || t[i] < 0)
            {
                Debug.LogError($"Invalid triangle index: {t[i]} at position {i}. Vertex count: {vertexCount}");
            }
        }
    }

    public void GenerateMesh()
    {
        // Debug.Log($"mesh stats: vertices length {vertices.Length},triangles length {triangles.Length}, uv length {uv.Length}");
        for (int i = 0; i < triangles.Length; i++)
        {
            if (triangles[i] >= vertices.Length || triangles[i] < 0)
            {
                // Debug.Log($"Inside chunk {chunkCoordX}, {chunkCoordZ}");
                Debug.LogError($"Triangle index out of bounds: {triangles[i]} (Vertex Count: {vertices.Length}) Inside chunk {chunkCoordX}, {chunkCoordZ}");
                triangles[i] = vertices.Length - 1;
            }
        }
        Mesh mesh = new Mesh();
        mesh.name = $"chunkmesh {chunkCoordX}/{chunkCoordZ}";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.normals = normals;
        mesh.RecalculateNormals();
        // RecalculateNormalsSeamless(mesh);

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    public void GenerateSidemesh()
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
