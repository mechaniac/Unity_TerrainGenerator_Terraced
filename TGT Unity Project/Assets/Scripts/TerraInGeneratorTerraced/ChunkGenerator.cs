using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TGeneratorT))]
public class ChunkGenerator : MonoBehaviour
{
    TGeneratorT tgt;

    public TChunk chunkPrefab;
    TChunk[] chunks;

    public int tylesPerChunkX = 8;
    public int tylesPerChunkZ = 8;

    public int chunkCountX;
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
        CreateChunks();
    }

    void CreateChunks()
    {
        chunks = new TChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++, i++)
            {
                TChunk c = chunks[i] = Instantiate(chunkPrefab);
                c.gameObject.SetActive(true);
                //c.transform.position = new Vector3(x * tylesPerChunkX * tgt.widthPerPixel, 0, z * tylesPerChunkZ * tgt.widthPerPixel);
                c.InitializeChunk(x, z, i, tgt, this);
                c.CreateMeshFromTyles();
                c.ReGenerateMesh();

                c.CreateSidemeshFromTyles();
                c.ReGenerateSidemesh();
            }
        }
    }




}
