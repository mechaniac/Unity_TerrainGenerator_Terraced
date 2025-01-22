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
            
            if(vPillars[i] != null)
            {
                v[i] = new Vector3(vPillars[i].transform.position.x, vPillars[i].vertexHeights[i], vPillars[i].transform.position.z);
            }
            else
            {
                v[i] = new Vector3(transform.position.x, 0, transform.position.z);
            }

            
        }
        return v;
    }

    public Vector3 GetVertice(int i)
    {
        Vector3 v = new Vector3(vPillars[i].transform.position.x, vPillars[i].vertexHeights[i], vPillars[i].transform.position.z);

        return v;
    }

    public void InstantiateTyle()
    {
        vPillars = new VPillar[4];
        neighbours = new Tyle[4];
    }

}
