using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VPillar : MonoBehaviour
{

    public Tyle[] tyles;
    System.Random r;

    public float[] vertexHeights;

    public void InstantiatePillar(System.Random _r)
    {
        r = _r;
        tyles = new Tyle[4];

    }
    public void SetVertexHeightsFromTyles(float maxRandomValue)
    {
        vertexHeights = new float[4];

        for (int i = 0; i < tyles.Length; i++)
        {
            if (tyles[i] != null)
            {
                vertexHeights[i] = tyles[i].height;
                //  Debug.Log($"setting vertexheight on {gameObject.name}  to {vertexHeights[i]} from tyle {i}");
            }
            else
            {
                vertexHeights[i] = float.MaxValue;
            }
        }

        float average = AverageWithoutMax(vertexHeights);

        for (int i = 0; i < vertexHeights.Length; i++)
        {
            if (vertexHeights[i] == float.MaxValue)
            {
                vertexHeights[i] = average;
            }
        }

        RandomizeFloatArray(vertexHeights,maxRandomValue);
    }

    public float AverageWithoutMax(float[] a)
    {
        if (a == null || a.Length == 0)
        {
            throw new ArgumentException("Array cannot be null or empty");
        }

        float sum = 0;
        int count = 0;

        foreach (float value in a)
        {
            if (value != float.MaxValue)
            {
                sum += value;
                count++;
            }
        }

        if (count == 0)
        {
            throw new InvalidOperationException("No valid values to calculate average");
        }

        return sum / count;
    }
    public void RandomizeFloatArray(float[] array, float randomness)
    {
        if (array == null || array.Length == 0)
        {
            throw new ArgumentException("Array cannot be null or empty");
        }

        

        for (int i = 0; i < array.Length; i++)
        {
            float randomValue = (float)(r.NextDouble() * 2 - 1) * randomness; // Generates a value between -randomness and +randomness
            // Debug.Log($"setting vertexheight on {gameObject.name} from {array[i]} to plus {randomValue}, equals: {array[i] += randomValue}");
            array[i] += randomValue;
        }
    }
    public void ContractVerticeHeights(float maxHeigtDifference)
    {
        List<int> v0List = new List<int>();

        for (int i = 1; i < vertexHeights.Length; i++)
        {
            if (Mathf.Abs(vertexHeights[0] - vertexHeights[i]) < maxHeigtDifference)
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

            vertexHeights[0] = mergedHeight / (float)(1 + v0List.Count);

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
            if (Math.Abs(vertexHeights[1] - vertexHeights[i]) < maxHeigtDifference)
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

            vertexHeights[1] = mergedHeight / (float)(1 + v1List.Count);

            for (int i = 0; i < v1List.Count; i++)
            {
                vertexHeights[v1List[i]] = vertexHeights[1];
            }
        }

        //-----------------------------------------
        //-----------------------------------------
        //-----------------------------------------

        if (Mathf.Abs(vertexHeights[2] - vertexHeights[3]) < maxHeigtDifference)
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

        if (contractedHeight <= 1f)
        {
            for (int i = 0; i < vertexHeights.Length; i++)
            {
                vertexHeights[i] = contractedHeight / 4f;
            }
        }
    }


}
