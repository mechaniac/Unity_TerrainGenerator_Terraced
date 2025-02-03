using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;


namespace terrain
{


    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class TChunk : MonoBehaviour
    {
        int id;
        int chunkCoordX;
        int chunkCoordZ;


        Vector3 chunkOffset;

        //MESH fields

        List<Vector3> vertices_temp;
        List<int> triangles_temp;
        List<Vector2> uv_temp;
        List<Vector3> normals_temp;

        //SIDE MESH fields
        List<Vector3> sideVertices;
        List<int> sideTriangles;
        List<Vector2> sideUv;
        List<Vector3> sideNormals;



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

            // LogChunk();
            // LogAllTriangles();

            GenerateMesh();
            // ChunkLog();
            if (true) //chunkCoordX == 0 && chunkCoordZ == 0
            {
                SetSideMeshesFromTyles();
                GenerateSidemesh();
            }


            // LogMeshGen_02();
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

        public void SetVerticesFromPillars()
        {
            // Debug.Log("Setting verticesFrom Pillars");
            int i = 0;
            for (int z = 0; z < cg.tylesPerChunkZ + 1; z++)
            {
                for (int x = 0; x < cg.tylesPerChunkX + 1; x++, i++)
                {
                    int pillarIndex = x
                    + z * (tgt.tylesX + 1)  // Local offset within the global grid
                    + chunkCoordX * cg.tylesPerChunkX  // Global horizontal chunk offset
                    + chunkCoordZ * (tgt.tylesX + 1) * cg.tylesPerChunkZ;  // Global vertical chunk offset
                                                                           // Debug.Log($"Pillar {i}, {pillarIndex} : x: {x}, z: {z}, tyles per chunkx {cg.tylesPerChunkX}, position: {tgt.vPillars[pillarIndex].transform.position}, pillarOriginal ID: {tgt.vPillars[pillarIndex].name}");

                    // Debug.Log("before added");

                    if (pillarIndex < tgt.vPillars.Length && tgt.vPillars[pillarIndex] != null)
                    {
                        // Debug.Log("added!");
                        VPillar p = tgt.vPillars[pillarIndex];
                        // Debug.Log($" Push Vertices from Pillar {i}: {x}, {z} ---------------------------------------------------------------------- {p.name}");
                        p.PushVerticesFromPillar(vertices_temp, uv_temp, normals_temp);
                        vertices_temp.Add(p.transform.position);

                        // Calculate normal (assuming quad is planar)
                        normals_temp.Add(Vector3.one);

                        uv_temp.Add(new Vector2(x, z));
                    }
                }
            }
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
                        triangles_temp.AddRange(t.GetTopQuadVertices());

                    }

                }
            }

        }




        public void SetSideMeshesFromTyles()
        {
            if (sideMesh == null) CreateSideMeshObject();

            sideVertices = new List<Vector3>();
            sideTriangles = new List<int>();
            sideUv = new List<Vector2>();
            sideNormals = new List<Vector3>();

            float y_adjustment = tgt.heightMultiplier;

            for (int z = 0, i = 0; z < cg.tylesPerChunkZ; z++)
            {
                for (int x = 0; x < cg.tylesPerChunkX; x++, i += 4)
                {
                    Tyle t = tgt.tyles[x + z * tgt.tylesX + chunkCoordX * cg.tylesPerChunkX + chunkCoordZ * tgt.tylesX * cg.tylesPerChunkZ];
                    if (t.neighbours[2] != null)
                    {
                        //------------First Quad First Triangle-------- direction -x
                        // Pillar 3 - left up
                        if (t.neighbours[2].vPillars[2].vertexIndices[2] != t.vPillars[3].vertexIndices[3])
                        {
                            // Debug.Log($"Tyle {t.name}, {x}, {z}. sideTriangle 1");
                            Vector3[] vs = new Vector3[3];
                            vs[0] = vertices_temp[t.neighbours[2].vPillars[2].vertexIndices[2]];
                            vs[1] = vertices_temp[t.vPillars[3].vertexIndices[3]];
                            vs[2] = vertices_temp[t.vPillars[0].vertexIndices[0]];

                            sideVertices.Add(vs[0]);
                            sideVertices.Add(vs[1]);
                            sideVertices.Add(vs[2]);

                            sideTriangles.Add(sideVertices.Count - 3);
                            sideTriangles.Add(sideVertices.Count - 2);
                            sideTriangles.Add(sideVertices.Count - 1);


                            if (vs[0].y < vs[1].y) //if face is up or down ()
                            {
                                sideUv.Add(new Vector2(0, vertices_temp[t.neighbours[2].vPillars[2].vertexIndices[2]].y / y_adjustment));
                                sideUv.Add(new Vector2(0, vertices_temp[t.vPillars[3].vertexIndices[3]].y / y_adjustment));
                                sideUv.Add(new Vector2(1, vertices_temp[t.vPillars[0].vertexIndices[0]].y / y_adjustment));
                                if (tgt.generateNormals)
                                {
                                    sideNormals.Add(new Vector3(-1, 0, 0));
                                    sideNormals.Add(new Vector3(-1, 0, 0));
                                    sideNormals.Add(new Vector3(-1, 0, 0));
                                }

                            }
                            else
                            {
                                sideUv.Add(new Vector2(1, vertices_temp[t.neighbours[2].vPillars[2].vertexIndices[2]].y / y_adjustment));
                                sideUv.Add(new Vector2(1, vertices_temp[t.vPillars[3].vertexIndices[3]].y / y_adjustment));
                                sideUv.Add(new Vector2(0, vertices_temp[t.vPillars[0].vertexIndices[0]].y / y_adjustment));
                                if (tgt.generateNormals)
                                {
                                    sideNormals.Add(new Vector3(1, 0, 0));
                                    sideNormals.Add(new Vector3(1, 0, 0));
                                    sideNormals.Add(new Vector3(1, 0, 0));
                                }

                            }


                        }
                        //------------First Quad Second Triangle-------- direction -x
                        if (t.neighbours[2].vPillars[1].vertexIndices[1] != t.vPillars[0].vertexIndices[0])
                        {
                            // Debug.Log($"Tyle {i}, {x}, {z}. sideTriangle 2");
                            Vector3[] vs = new Vector3[3];
                            vs[0] = vertices_temp[t.vPillars[0].vertexIndices[0]];
                            vs[1] = vertices_temp[t.neighbours[2].vPillars[1].vertexIndices[1]];
                            vs[2] = vertices_temp[t.neighbours[2].vPillars[2].vertexIndices[2]];

                            sideVertices.Add(vs[0]);
                            sideVertices.Add(vs[1]);
                            sideVertices.Add(vs[2]);

                            sideTriangles.Add(sideVertices.Count - 3);
                            sideTriangles.Add(sideVertices.Count - 2);
                            sideTriangles.Add(sideVertices.Count - 1);

                            if (vs[0].y > vs[1].y) //if face is up or down ()
                            {
                                sideUv.Add(new Vector2(1, vertices_temp[t.vPillars[0].vertexIndices[0]].y / y_adjustment));
                                sideUv.Add(new Vector2(1, vertices_temp[t.neighbours[2].vPillars[1].vertexIndices[1]].y / y_adjustment));
                                sideUv.Add(new Vector2(0, vertices_temp[t.neighbours[2].vPillars[2].vertexIndices[2]].y / y_adjustment));
                                if (tgt.generateNormals)
                                {
                                    sideNormals.Add(new Vector3(-1, 0, 0));
                                    sideNormals.Add(new Vector3(-1, 0, 0));
                                    sideNormals.Add(new Vector3(-1, 0, 0));
                                }

                            }
                            else
                            {
                                sideUv.Add(new Vector2(0, vertices_temp[t.vPillars[0].vertexIndices[0]].y / y_adjustment));
                                sideUv.Add(new Vector2(0, vertices_temp[t.neighbours[2].vPillars[1].vertexIndices[1]].y / y_adjustment));
                                sideUv.Add(new Vector2(1, vertices_temp[t.neighbours[2].vPillars[2].vertexIndices[2]].y / y_adjustment));
                                if (tgt.generateNormals)
                                {
                                    sideNormals.Add(new Vector3(1, 0, 0));
                                    sideNormals.Add(new Vector3(1, 0, 0));
                                    sideNormals.Add(new Vector3(1, 0, 0));
                                }

                            }


                        }
                    }

                    if (t.neighbours[3] != null)
                    {
                        //------------Second Quad First Triangle-------- direction -z
                        if (t.neighbours[3].vPillars[3].vertexIndices[3] != t.vPillars[0].vertexIndices[0])
                        {

                            Vector3[] vs = new Vector3[3];
                            vs[0] = vertices_temp[t.neighbours[3].vPillars[3].vertexIndices[3]];
                            vs[1] = vertices_temp[t.vPillars[0].vertexIndices[0]];
                            vs[2] = vertices_temp[t.vPillars[1].vertexIndices[1]];

                            sideVertices.Add(vs[0]);
                            sideVertices.Add(vs[1]);
                            sideVertices.Add(vs[2]);


                            sideTriangles.Add(sideVertices.Count - 3);
                            sideTriangles.Add(sideVertices.Count - 2);
                            sideTriangles.Add(sideVertices.Count - 1);


                            if (vs[0].y < vs[1].y) //if face is up or down ()
                            {
                                sideUv.Add(new Vector2(0, vertices_temp[t.neighbours[3].vPillars[3].vertexIndices[3]].y / y_adjustment));
                                sideUv.Add(new Vector2(0, vertices_temp[t.vPillars[0].vertexIndices[0]].y / y_adjustment));
                                sideUv.Add(new Vector2(1, vertices_temp[t.vPillars[1].vertexIndices[1]].y / y_adjustment));
                                if (tgt.generateNormals)
                                {
                                    sideNormals.Add(new Vector3(0, 0, -1));
                                    sideNormals.Add(new Vector3(0, 0, -1));
                                    sideNormals.Add(new Vector3(0, 0, -1));
                                }

                            }
                            else
                            {
                                sideUv.Add(new Vector2(1, vertices_temp[t.neighbours[3].vPillars[3].vertexIndices[3]].y / y_adjustment));
                                sideUv.Add(new Vector2(1, vertices_temp[t.vPillars[0].vertexIndices[0]].y / y_adjustment));
                                sideUv.Add(new Vector2(0, vertices_temp[t.vPillars[1].vertexIndices[1]].y / y_adjustment));
                                if (tgt.generateNormals)
                                {
                                    sideNormals.Add(new Vector3(0, 0, 1));
                                    sideNormals.Add(new Vector3(0, 0, 1));
                                    sideNormals.Add(new Vector3(0, 0, 1));
                                }

                            }



                        }
                        //------------Second Quad Second Triangle-------- direction -z
                        if (t.neighbours[3].vPillars[2].vertexIndices[2] != t.vPillars[1].vertexIndices[1])
                        {
                            Vector3[] vs = new Vector3[3];
                            vs[0] = vertices_temp[t.vPillars[1].vertexIndices[1]];
                            vs[1] = vertices_temp[t.neighbours[3].vPillars[2].vertexIndices[2]];
                            vs[2] = vertices_temp[t.neighbours[3].vPillars[3].vertexIndices[3]];
                            // Debug.Log($"Tyle {i}, {x}, {z}. sideTriangle 4");

                            sideVertices.Add(vs[0]);
                            sideVertices.Add(vs[1]);
                            sideVertices.Add(vs[2]);


                            sideTriangles.Add(sideVertices.Count - 3);
                            sideTriangles.Add(sideVertices.Count - 2);
                            sideTriangles.Add(sideVertices.Count - 1);

                            if (vs[0].y > vs[1].y) //if face is up or down ()
                            {
                                sideUv.Add(new Vector2(1, vertices_temp[t.vPillars[1].vertexIndices[1]].y / y_adjustment));
                                sideUv.Add(new Vector2(1, vertices_temp[t.neighbours[3].vPillars[2].vertexIndices[2]].y / y_adjustment));
                                sideUv.Add(new Vector2(0, vertices_temp[t.neighbours[3].vPillars[3].vertexIndices[3]].y / y_adjustment));
                                if (tgt.generateNormals)
                                {
                                    sideNormals.Add(new Vector3(0, 0, -1));
                                    sideNormals.Add(new Vector3(0, 0, -1));
                                    sideNormals.Add(new Vector3(0, 0, -1));
                                }

                            }
                            else
                            {
                                sideUv.Add(new Vector2(0, vertices_temp[t.vPillars[1].vertexIndices[1]].y / y_adjustment));
                                sideUv.Add(new Vector2(0, vertices_temp[t.neighbours[3].vPillars[2].vertexIndices[2]].y / y_adjustment));
                                sideUv.Add(new Vector2(1, vertices_temp[t.neighbours[3].vPillars[3].vertexIndices[3]].y / y_adjustment));
                                if (tgt.generateNormals)
                                {
                                    sideNormals.Add(new Vector3(0, 0, 1));
                                    sideNormals.Add(new Vector3(0, 0, 1));
                                    sideNormals.Add(new Vector3(0, 0, 1));
                                }

                            }



                        }
                    }


                }
            }

            // Debug.Log($"sideTriangles added-------------------------------------------------------------------- : {sideTriangles.Count} ");
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



        public void GenerateMesh()
        {
            // Debug log for mesh statistics
            // Debug.Log($"Mesh Stats: Vertices: {vertices_temp.Count}, Triangles: {triangles_temp.Count}, UVs: {uv_temp.Count}");

            // Validate triangle indices to prevent out-of-bounds errors
            for (int i = 0; i < triangles_temp.Count; i++)
            {
                if (triangles_temp[i] >= vertices_temp.Count || triangles_temp[i] < 0)
                {
                    Debug.LogError($"Triangle index out of bounds: {triangles_temp[i]} (Vertex Count: {vertices_temp.Count}) Inside chunk {chunkCoordX}, {chunkCoordZ}");
                    triangles_temp[i] = vertices_temp.Count - 1; // Fix invalid index
                }
            }

            // Create and assign the mesh
            Mesh mesh = new Mesh
            {
                name = $"chunkmesh {chunkCoordX}/{chunkCoordZ}"
            };

            mesh.SetVertices(vertices_temp);
            mesh.SetTriangles(triangles_temp, 0);
            mesh.SetUVs(0, uv_temp);
            mesh.SetNormals(normals_temp);
            mesh.RecalculateNormals();

            GetComponent<MeshFilter>().mesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = mesh;

            // Clear temporary lists after mesh generation
            // ClearMeshData();
        }
        private void ClearMeshData()
        {
            vertices_temp.Clear();
            triangles_temp.Clear();
            uv_temp.Clear();
            normals_temp.Clear();
        }

        public void GenerateSidemesh()
        {
            Mesh mesh = new Mesh();

            mesh.SetVertices(sideVertices);
            mesh.SetTriangles(sideTriangles, 0);
            mesh.SetUVs(0, sideUv);
            if (tgt.generateNormals)
            {
                mesh.SetNormals(sideNormals);
            }


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
}