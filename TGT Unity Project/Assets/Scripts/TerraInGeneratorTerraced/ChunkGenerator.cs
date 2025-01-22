using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TGeneratorT))]
[ExecuteInEditMode]
public class ChunkGenerator : MonoBehaviour
{
    TGeneratorT tgt;

    private Transform chunkHolder;

    public TChunk chunkPrefab;
    TChunk[] chunks;

    public int tylesPerChunkX = 8;
    public int tylesPerChunkZ = 8;

    [HideInInspector]
    public int chunkCountX;
    [HideInInspector]
    public int chunkCountZ;

    private void Awake()
    {
        tgt = GetComponent<TGeneratorT>();

        if (tgt.heightMap.width % 2 != 0)
        {
            Debug.LogWarning($"heightMap width ({tgt.heightMap.width}) is not even. ChunkCreation will fail");
        }

        float cCX = tgt.heightMap.width / (float)tylesPerChunkX;
        float cCZ = tgt.heightMap.height / (float)tylesPerChunkZ;

        chunkCountX = (int)cCX;
        chunkCountZ = (int)cCZ;

        if (cCX != chunkCountX || cCZ != chunkCountZ)
        {
            Debug.LogWarning($"heightMap not evenly divisible by Tyles Per Chunk");
        }

    }

    public void GenerateChunkMeshes()
    {
        GameObject holderObject = new GameObject("chunkHolder");

        // Get the Transform component
        chunkHolder = holderObject.transform;
        if (tgt != null)
        {
            chunkHolder.SetParent(tgt.gameObject.transform);
        }
        CreateChunks();
    }

    void CreateChunks()
    {
        chunks = new TChunk[chunkCountX * chunkCountZ];

        float chunkOffsetX = 0f;
        float chunkOffsetZ = 0f;

        if (tgt.setMapToSceneCenter)
        {
            chunkOffsetX = -tylesPerChunkX * chunkCountX * tgt.widthPerPixel / 2f;
            chunkOffsetZ = -tylesPerChunkZ * chunkCountZ * tgt.widthPerPixel / 2f;
        }

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++, i++)
            {
                TChunk c = chunks[i] = Instantiate(chunkPrefab);
                c.name = tgt.mapName + "_chunk_" + x + ", " + z;
                c.gameObject.SetActive(true);
                c.transform.position = new Vector3(x * tylesPerChunkX * tgt.widthPerPixel + chunkOffsetX, 0, z * tylesPerChunkZ * tgt.widthPerPixel + chunkOffsetZ);
                if (chunkHolder != null)
                {
                    c.transform.SetParent(chunkHolder);
                }
                c.InitializeChunk(x, z, i, tgt, this);
                c.CreateMeshFromTyles();
                c.ReGenerateMesh();

                c.CreateSidemeshFromTyles();
                c.ReGenerateSidemesh();
            }
        }
    }




}
