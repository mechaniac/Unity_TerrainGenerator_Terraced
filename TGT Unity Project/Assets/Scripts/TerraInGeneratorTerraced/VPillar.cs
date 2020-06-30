using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VPillar : MonoBehaviour
{

    public Tyle[] tyles;

    public float[] vertexHeights;

    public void InstantiatePillar()
    {
        tyles = new Tyle[4];

    }
    public void SetVertexHeightsFromTyles()
    {
        vertexHeights = new float[4];

        for (int i = 0; i < tyles.Length; i++)
        {
            if(tyles[i] != null)
            {
                vertexHeights[i] = tyles[i].height;
            }
            
        }
    }

    public void ContractVerticeHeights(float maxHeigtDifference)
    {
        List<int> v0List = new List<int>();

        for (int i = 1; i < vertexHeights.Length; i++)
        {
            if( Mathf.Abs(vertexHeights[0] - vertexHeights[i]) < maxHeigtDifference)
            {
                v0List.Add(i);
            }
        }

        if (v0List.Count > 0)
        {
            float mergedHeight = vertexHeights[0];
            for (int i = 0; i < v0List.Count; i++)
            {
                mergedHeight += vertexHeights[v0List[i]];
            }

            vertexHeights[0] = mergedHeight / (float) (1 + v0List.Count);

            for (int i = 0; i < v0List.Count; i++)
            {
                vertexHeights[v0List[i]] = vertexHeights[0];
            }
        }
        
        //-----------------------------------------
        //-----------------------------------------
        //-----------------------------------------

        List<int> v1List = new List<int>();

        for (int i = 2; i < vertexHeights.Length; i++)
        {
            if(Math.Abs(vertexHeights[1] - vertexHeights[i]) < maxHeigtDifference)
            {
                v1List.Add(i);
            }
        }
        if (v1List.Count > 0)
        {
            float mergedHeight = vertexHeights[1];
            for (int i = 0; i < v1List.Count; i++)
            {
                mergedHeight += vertexHeights[v1List[i]];
            }

            vertexHeights[1] = mergedHeight / (float) (1 + v1List.Count);

            for (int i = 0; i < v1List.Count; i++)
            {
                vertexHeights[v1List[i]] = vertexHeights[1];
            }
        }
        
        //-----------------------------------------
        //-----------------------------------------
        //-----------------------------------------

        if (Mathf.Abs( vertexHeights[2] - vertexHeights[3]) < maxHeigtDifference)
        {
            float mergedHeight = vertexHeights[2] + vertexHeights[3];
            vertexHeights[2] = mergedHeight / 2f;
            vertexHeights[3] = mergedHeight / 2f;
        }
    }


    public void ContractVerticeHeightsBackup()
    {
        float contractedHeight = 0f;
        for (int i = 0; i < vertexHeights.Length; i++)
        {
            contractedHeight += vertexHeights[i];
        }

        if(contractedHeight <= 1f)
        {
            for (int i = 0; i < vertexHeights.Length; i++)
            {
                vertexHeights[i] = contractedHeight / 4f;
            }
        }
    }

    
}
