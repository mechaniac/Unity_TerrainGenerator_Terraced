using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace terrain
{

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
        public int vertPerChunkX;


        [HideInInspector]
        public int vertPerChunkZ;

        [HideInInspector]
        public int chunkCountX;
        [HideInInspector]
        public int chunkCountZ;

        private void Awake()
        {
            // InitializeChunkHolder();
        }

        void InitializeChunkGenerator()
        {
            tgt = GetComponent<TGeneratorT>();

            // if (tgt.heightMap.width % 2 != 0)
            // {
            //     Debug.LogWarning($"heightMap width ({tgt.heightMap.width}) is not even. ChunkCreation might fail");
            // }

            float cCX = tgt.heightMap.width / (float)tylesPerChunkX;
            float cCZ = tgt.heightMap.height / (float)tylesPerChunkZ;

            vertPerChunkX = tylesPerChunkX + 1;
            vertPerChunkZ = tylesPerChunkZ + 1;


            chunkCountX = (int)cCX;
            chunkCountZ = (int)cCZ;
            // Debug.Log($"chunk Count X: {chunkCountX}, chunk Count Z: {chunkCountZ}");

            if (cCX != chunkCountX || cCZ != chunkCountZ)
            {
                Debug.Log($"cCX: {cCX}, cCZ: {cCZ}, chunkCountX: {chunkCountX}, chunkCounZ: {chunkCountZ}");
                Debug.LogWarning($"heightMap not evenly divisible by Tyles Per Chunk");
            }

        }

        public void GenerateChunkMeshes() //called from MAIN stack
        {
            InitializeChunkGenerator();


            GameObject holderObject = new GameObject("chunkHolder");

            // Get the Transform component
            chunkHolder = holderObject.transform;
            if (tgt != null)
            {
                chunkHolder.SetParent(tgt.gameObject.transform);
            }
            CreateChunks();
            Debug.Log($"chunkCreation DONE: chunks Length {chunks.Length}");
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
                    // Instantiate and configure the chunk
                    TChunk c = chunks[i] = Instantiate(chunkPrefab);
                    c.name = tgt.mapName + "_chunk_" + i + "_" + x + "_" + z;
                    c.gameObject.SetActive(true);
                    // Example: setting the chunk's position (uncomment or modify as needed)
                    // c.transform.position = new Vector3(x * tylesPerChunkX * tgt.widthPerPixel + chunkOffsetX, 0, z * tylesPerChunkZ * tgt.widthPerPixel + chunkOffsetZ);

                    if (chunkHolder != null)
                    {
                        // Create an intermediate parent GameObject for proper organization
                        GameObject chunkParentObj = new GameObject(c.name + "_parent");
                        Transform chunkParent = chunkParentObj.transform;
                        // Parent the new group to the overall chunkHolder
                        chunkParent.SetParent(chunkHolder);
                        // Set the chunk's parent to the intermediate GameObject
                        c.transform.SetParent(chunkParent);
                        
                        chunkParentObj.layer = LayerMask.NameToLayer("chunk");
                    }

                    c.InitializeChunk(x, z, i, tgt, this);
                    c.GenerateMeshes();
                }
            }
        }





    }
}