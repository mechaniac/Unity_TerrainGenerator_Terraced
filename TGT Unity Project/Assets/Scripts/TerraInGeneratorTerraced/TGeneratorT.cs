using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class TGeneratorT : MonoBehaviour
{
    ChunkGenerator cg;

    public System.Random random;

    public Texture2D heightMap;

    public Material topMeshMat;
    public Material sideMeshMat;
    public string mapName;
    public int widthPerPixel;
    

    public bool setMapToSceneCenter;

    public Tyle tylePrefab;
    public VPillar vPillarPrefab;

    [HideInInspector]
    public Tyle[] tyles;
    VPillar[] vPillars;

    [HideInInspector]
    public int tylesX;
    [HideInInspector]
    public int tylesZ;

    public float maxSlopeHeight = .35f;
    public float heightMultiplier;
    public float maxRandomValue = 0.0f;

    private void Awake()
    {
        InitializeMap();
    }
    private void Start()
    {
        GenerateTerrain();
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
        DeleteTerrain();
        // Debug.Log("Terrain generated with maxSlopeHeight: " + maxSlopeHeight);
        // Add your terrain generation logic here
        InitializeMap();

        InstantiateTyles();
        SetTileNeighbours();
        InstantiatePillars();
        SetTylesHeightFromHeightmap();
        AssignTylesToPillars();
        SetPillarVerticesFromTyles(maxRandomValue);
        ContractAllVerticeHeights(maxSlopeHeight);
        cg.GenerateChunkMeshes();
    }

    void InitializeMap()
    {
        if (heightMap == null) return;

        tylesX = heightMap.width;
        tylesZ = heightMap.height;

        string assetPath = AssetDatabase.GetAssetPath(heightMap);
        mapName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        Debug.Log("File Name: " + mapName);

        cg = GetComponent<ChunkGenerator>();
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
                t.InstantiateTyle();
                t.name = $"Tile_{x}_{z}";
            }
        }
    }

    void SetTileNeighbours()
    {
        for (int z = 0, i = 0; z < tylesZ; z++)
        {
            for (int x = 0; x < tylesX; x++, i++)
            {
                if ((i + 1) % tylesX != 0)
                {
                    //Debug.Log($"i: {i}");
                    tyles[i].neighbours[0] = tyles[i + 1];
                }

                if (i < tylesX * tylesZ - tylesX)
                {
                    tyles[i].neighbours[1] = tyles[i + tylesX];
                }

                if (i % tylesX != 0)
                {
                    tyles[i].neighbours[2] = tyles[i - 1];
                }

                if (i > tylesX - 1)
                {
                    tyles[i].neighbours[3] = tyles[i - tylesX];
                }



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
                p.name = $"Pillar_{x}_{z}";
            }
        }
    }

    void AssignTylesToPillars()
    {
        for (int z = 0, i = 0; z < tylesZ + 1; z++)
        {
            for (int x = 0; x < tylesX + 1; x++, i++)
            {
                if (x < tylesX && z < tylesZ)
                {
                    vPillars[i].tyles[0] = tyles[i - z];
                    tyles[i - z].vPillars[0] = vPillars[i];
                }

                if (x > 0 && z < tylesZ)
                {
                    vPillars[i].tyles[1] = tyles[i - 1 - z];
                    tyles[i - 1 - z].vPillars[1] = vPillars[i];
                }

                if (x > 0 && z > 0)
                {
                    vPillars[i].tyles[2] = tyles[i - tylesX - z - 1];
                    tyles[i - tylesX - z - 1].vPillars[2] = vPillars[i];
                }

                if (x < tylesX && z > 0)
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
