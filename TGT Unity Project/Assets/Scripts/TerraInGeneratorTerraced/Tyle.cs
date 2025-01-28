using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tyle : MonoBehaviour
{
    public int id;

    public float height;

    public VPillar[] vPillars;

    public Vector3[] v;

    public Tyle[] neighbours;


    public Vector3[] GetVertices()
    {
        v = new Vector3[4];

        for (int i = 0; i < vPillars.Length; i++)
        {

            if (vPillars[i] != null)
            {
                v[i] = new Vector3(vPillars[i].transform.position.x, vPillars[i].transform.position.y, vPillars[i].transform.position.z);
            }
            else
            {
                v[i] = new Vector3(transform.position.x, 0, transform.position.z);
            }


        }
        return v;
    }

    public Vector3 GetVertex(int i)
    {
        Vector3 v = new Vector3(vPillars[i].transform.position.x, vPillars[i].vertexHeights[i], vPillars[i].transform.position.z);

        return v;
    }

    public void InstantiateTyle(int x, int z, int i)
    {
        id = i;
        vPillars = new VPillar[4];
        neighbours = new Tyle[4];
    }

    public void SetNeighbours(int index, Tyle[] tyles, int tylesX, int tylesZ)
    {
        if ((index + 1) % tylesX != 0) // if not furthest Right (plus X)
        {
            neighbours[0] = tyles[index + 1];
        }

        if (index < tylesX * tylesZ - tylesX) // if not furthest Up (plus Z)
        {
            neighbours[1] = tyles[index + tylesX];
        }

        if (index % tylesX != 0) // if not furthest Leftt (minus X)
        {
            neighbours[2] = tyles[index - 1];
        }

        if (index > tylesX - 1) // if not furthest Down (minus Z)
        {
            neighbours[3] = tyles[index - tylesX];
        }
    }

}
