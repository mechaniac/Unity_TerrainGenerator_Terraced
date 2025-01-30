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

    public Vector3 GetVertexFromCornerPillar(int i)
    {
        Vector3 v = new Vector3(vPillars[i].transform.position.x, vPillars[i].vertexHeights[i], vPillars[i].transform.position.z);

        return v;
    }

    public int GetVertexIndexFromCornerPillar(int i){
        int vertexIndex = vPillars[i].vertexIndices[i];

        return vertexIndex;
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

    public int[] GetTopQuadVertices(){
        int[] t = new int[6];

        Debug.Log($" pillar indices lenght: {vPillars[0].vertexIndices.Length}");
        t[0] = vPillars[0].vertexIndices[0];
        t[1]= vPillars[3].vertexIndices[3];
        t[2] = vPillars[2].vertexIndices[2];

        t[3] = t[2];
        t[4] = vPillars[1].vertexIndices[1];
        t[5] = t[0];

        return t;
    }





}
