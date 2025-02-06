using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System;

namespace terrain
{

    public class TGeneratorT : MonoBehaviour
    {

        private string logFilePath = "TerrainGenerationLog.txt";

        private void WriteLog(string message)
        {
            if (enableLogging)
            {
                File.AppendAllText(logFilePath, message + "\n");
            }
        }

        ChunkGenerator cg;

        public System.Random random;

        public Texture2D heightMap;

        public Material topMeshMat;
        public Material sideMeshMat;
        public string mapName;
        public int widthPerPixel;

        public bool enableLogging = true; // Toggle logging on/off


        public bool setMapToSceneCenter;

        public bool generateNormals;

        public Tyle tylePrefab;
        public VPillar vPillarPrefab;

        [HideInInspector]
        public Tyle[] tyles;
        public VPillar[] vPillars;

        [HideInInspector]
        public int tylesX;
        [HideInInspector]
        public int tylesZ;

        public float maxSlopeHeight = .35f;
        public float heightMultiplier;
        public float maxRandomValue = 0.0f;

        public float sineAmplitude;
        public float sineFrequency;
        public float sinePhase;

        private void Awake()
        {
            InitializeMap();
        }
        private void Start()
        {
            GenerateTerrain();

            biome.BiomeGenerator biomeGen = GetComponent<biome.BiomeGenerator>();
            if (biomeGen != null)
            {
                biomeGen.GenerateBiomes();
            }
        }

        public void DeleteTerrain()
        {
            // Names of objects to delete
            string[] targetNames = { "pillarholder", "chunkHolder", "tylesHolder" };

            foreach (string targetName in targetNames)
            {
                // Find objects in the hierarchy with the given name
                Transform target = transform.Find(targetName);

                if (target != null)
                {
                    // Destroy the object and all its children immediately
                    DestroyImmediate(target.gameObject);
                    // Debug.Log($"{targetName} and its children have been deleted.");
                }
            }
        }


        public void GenerateTerrain() //MAIN Stack
        {
            random = new System.Random(123);
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            long totalTime = 0;
            string longestTask = "";
            long longestTime = 0;

            string header = "\n========== Terrain Generation Log ==========" +
                            "\nTimestamp: " + DateTime.Now +
                            "\nMap Name: " + mapName +
                            "\n---------------------------------------------\n";
            WriteLog(header);
            if (enableLogging) Debug.Log(header);

            void LogTask(string taskName)
            {
                long elapsed = stopwatch.ElapsedMilliseconds;
                totalTime += elapsed;
                if (elapsed > longestTime)
                {
                    longestTime = elapsed;
                    longestTask = taskName;
                }
                if (enableLogging) Debug.Log(taskName + " took: " + elapsed + " ms");
                WriteLog(taskName + " took: " + elapsed + " ms\n");
            }

            stopwatch.Start(); DeleteTerrain(); stopwatch.Stop(); LogTask("DeleteTerrain");
            stopwatch.Restart(); InitializeMap(); stopwatch.Stop(); LogTask("InitializeMap");
            stopwatch.Restart(); InstantiateTyles(); stopwatch.Stop(); LogTask("InstantiateTyles");
            stopwatch.Restart(); SetTileNeighbours(); stopwatch.Stop(); LogTask("SetTileNeighbours");
            stopwatch.Restart(); InstantiatePillars(); stopwatch.Stop(); LogTask("InstantiatePillars");
            stopwatch.Restart(); AssignTylesToPillars(); stopwatch.Stop(); LogTask("AssignTylesToPillars");
            stopwatch.Restart(); SetPillarVerticesFromTyles(maxRandomValue); stopwatch.Stop(); LogTask("SetPillarVerticesFromTyles");
            stopwatch.Restart(); ContractAllVerticeHeights(maxSlopeHeight); stopwatch.Stop(); LogTask("ContractAllVerticeHeights");
            stopwatch.Restart(); cg.GenerateChunkMeshes(); stopwatch.Stop(); LogTask("GenerateChunkMeshes");

            string summary = "\nTotal Time: " + totalTime + " ms" +
                             "\nLongest Task: " + longestTask + " (" + longestTime + " ms)\n";
            if (enableLogging) Debug.Log(summary);
            WriteLog(summary);

            LogTerrainGenerationDetails();
        }

        public void LogTerrainGenerationDetails()
        {
            string logMessage = "Terrain Generation Details:\n" +
                                "- Map Name: " + mapName + "\n" +
                                "- HeightMap Size: " + (heightMap != null ? heightMap.width + "x" + heightMap.height : "No HeightMap Provided") + "\n" +
                                "- Width Per Pixel: " + widthPerPixel + "\n" +
                                "- Number of Tyles: " + tylesX + " x " + tylesZ + " (Total: " + (tylesX * tylesZ) + ")\n" +
                                "- Number of Pillars: " + (vPillars != null ? vPillars.Length : 0) + "\n" +
                                "- Set Map to Scene Center: " + setMapToSceneCenter + "\n" +
                                "- Top Mesh Material: " + (topMeshMat != null ? topMeshMat.name : "None") + "\n" +
                                "- Side Mesh Material: " + (sideMeshMat != null ? sideMeshMat.name : "None");

            if (enableLogging) Debug.Log(logMessage);
            WriteLog(logMessage + "\n");
        }


        void InitializeMap()
        {
            if (heightMap == null) return;

            tylesX = heightMap.width;
            tylesZ = heightMap.height;

            string assetPath = AssetDatabase.GetAssetPath(heightMap);
            mapName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            // Debug.Log("File Name: " + mapName);

            cg = GetComponent<ChunkGenerator>();

            if (topMeshMat != null && heightMap != null)
            {
                topMeshMat.SetTexture("_ColorMap", heightMap);
            }
        }

        void InstantiateTyles()
        {
            tyles = new Tyle[tylesX * tylesZ];
            GameObject tylesHolder = new GameObject("tylesHolder");
            tylesHolder.transform.parent = transform;

            for (int z = 0, i = 0; z < tylesZ; z++)
            {
                for (int x = 0; x < tylesX; x++, i++)
                {
                    Tyle t = tyles[i] = Instantiate(tylePrefab);

                    t.transform.position = new Vector3(x * widthPerPixel + widthPerPixel / 2f, 0, z * widthPerPixel + widthPerPixel / 2f);
                    t.gameObject.SetActive(true);
                    t.transform.SetParent(tylesHolder.transform, true);
                    t.InstantiateTyle(x, z, i);
                    t.height = heightMap.GetPixel(x, z).grayscale * heightMultiplier;
                    t.name = $"Tile_{i}_{x}_{z}";
                }
            }
        }

        void SetTileNeighbours() //in a separate loop, so all neighbours exist.
        {
            for (int z = 0, i = 0; z < tylesZ; z++)
            {
                for (int x = 0; x < tylesX; x++, i++)
                {
                    tyles[i].SetNeighbours(i, tyles, tylesX, tylesZ);
                }
            }

        }


        void SetTylesHeightFromHeightmap()
        {
            for (int z = 0, i = 0; z < tylesZ; z++)
            {
                for (int x = 0; x < tylesX; x++, i++)
                {
                    tyles[i].height = heightMap.GetPixel(x, z).grayscale * heightMultiplier;
                }
            }
        }

        void InstantiatePillars()
        {
            vPillars = new VPillar[(tylesX + 1) * (tylesZ + 1)];
            GameObject pillarHolder = new GameObject("pillarholder");
            pillarHolder.transform.parent = transform;


            for (int z = 0, i = 0; z < tylesZ + 1; z++)
            {
                for (int x = 0; x < tylesX + 1; x++, i++)
                {
                    VPillar p = vPillars[i] = Instantiate(vPillarPrefab);
                    p.transform.position = new Vector3(x * widthPerPixel, 0, z * widthPerPixel);
                    p.gameObject.SetActive(true);
                    p.InstantiatePillar(random);
                    p.transform.parent = pillarHolder.transform;
                    p.name = $"Pillar_{i}_{x}/{z}";
                    p.tgt = this;
                }
            }
        }

        void AssignTylesToPillars()
        {
            for (int z = 0, i = 0; z < tylesZ + 1; z++) // Loop through pillars by row and column
            {
                for (int x = 0; x < tylesX + 1; x++, i++)
                {
                    // Bottom-right tile relative to the pillar
                    if (x < tylesX && z < tylesZ) // Exclude the rightmost column and topmost row
                    {
                        vPillars[i].tyles[0] = tyles[i - z];
                        tyles[i - z].vPillars[0] = vPillars[i];
                    }

                    // Bottom-left tile relative to the pillar
                    if (x > 0 && z < tylesZ) // Exclude the leftmost column and topmost row
                    {
                        vPillars[i].tyles[1] = tyles[i - 1 - z];
                        tyles[i - 1 - z].vPillars[1] = vPillars[i];
                    }

                    // Top-left tile relative to the pillar
                    if (x > 0 && z > 0) // Exclude the leftmost column and bottommost row
                    {
                        vPillars[i].tyles[2] = tyles[i - tylesX - z - 1];
                        tyles[i - tylesX - z - 1].vPillars[2] = vPillars[i];
                    }

                    // Top-right tile relative to the pillar
                    if (x < tylesX && z > 0) // Exclude the rightmost column and bottommost row
                    {
                        vPillars[i].tyles[3] = tyles[i - tylesX - z];
                        tyles[i - tylesX - z].vPillars[3] = vPillars[i];
                    }
                }
            }
        }

        void SetPillarVerticesFromTyles(float randomValue)
        {
            for (int i = 0; i < vPillars.Length; i++)
            {
                vPillars[i].SetVertexHeightsFromTyles(randomValue);
            }
        }

        void ContractAllVerticeHeights(float height)
        {
            for (int i = 0; i < vPillars.Length; i++)
            {
                vPillars[i].ContractVerticeHeights(height);
            }
        }
    }
}