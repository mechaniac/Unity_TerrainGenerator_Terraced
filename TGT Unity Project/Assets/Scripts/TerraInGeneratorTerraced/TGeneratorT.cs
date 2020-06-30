using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TGeneratorT : MonoBehaviour
{
    ChunkGenerator cg;

    public Texture2D heightMap;
    public int widthPerPixel;

    public Tyle tylePrefab;
    public VPillar vPillarPrefab;

    public Tyle[] tyles;
    VPillar[] vPillars;

    public int tylesX;
    public int tylesZ;

    public float maxSlopeHeight = .35f;

    private void Awake()
    {
        InitializeMap();
    }
    private void Start()
    {
        InstantiateTyles();
        SetTileNeighbours();
        InstantiatePillars();
        SetTylesHeightFromHeightmap();
        AssignTylesToPillars();
        SetPillarVerticesFromTyles();
        ContractAllVerticeHeights(maxSlopeHeight);
        cg.GenerateChunkMeshes();
    }

    void InitializeMap()
    {
        if (heightMap == null) return;

        tylesX = heightMap.width;
        tylesZ = heightMap.height;

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
                tyles[i].height = heightMap.GetPixel(x, z).grayscale;
            }
        }
    }

    void InstantiatePillars()
    {
        vPillars = new VPillar[(tylesX + 1) * (tylesZ + 1)];
        GameObject pillarHolder = new GameObject("pillarhOlder");
        pillarHolder.transform.parent = transform;


        for (int z = 0, i = 0; z < tylesZ + 1; z++)
        {
            for (int x = 0; x < tylesX + 1; x++, i++)
            {
                VPillar p = vPillars[i] = Instantiate(vPillarPrefab);
                p.transform.position = new Vector3(x * widthPerPixel, 0, z * widthPerPixel);
                p.gameObject.SetActive(true);
                p.InstantiatePillar();
                p.transform.parent = pillarHolder.transform;
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

    void SetPillarVerticesFromTyles()
    {
        for (int i = 0; i < vPillars.Length; i++)
        {
            vPillars[i].SetVertexHeightsFromTyles();
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
