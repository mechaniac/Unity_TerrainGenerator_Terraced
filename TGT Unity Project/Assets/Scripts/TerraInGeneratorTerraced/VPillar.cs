using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VPillar : MonoBehaviour
{
    //STORES HEIGHTS from TYLES. 
    // CONTRACTs    
    // randomizes
    // averages
    // sets bleeding borderValues 

    public Tyle[] tyles;
    System.Random r;

    public float[] vertexHeights;
    public float average;

    

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

        average = AverageWithoutMax(vertexHeights);
        transform.position = new Vector3(transform.position.x, average, transform.position.z);

        for (int i = 0; i < vertexHeights.Length; i++)
        {
            if (vertexHeights[i] == float.MaxValue)
            {
                vertexHeights[i] = average;
            }
        }

        if (maxRandomValue > 0)
        {
            RandomizeFloatArray(vertexHeights, maxRandomValue);
        }
        

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
    public void ContractVerticeHeights(float maxHeightDifference)
{
    for (int i = 0; i < vertexHeights.Length; i++)
    {
        List<int> closeVertices = new List<int>();

        for (int j = i + 1; j < vertexHeights.Length; j++)
        {
            if (Mathf.Abs(vertexHeights[i] - vertexHeights[j]) < maxHeightDifference)
            {
                closeVertices.Add(j);
            }
        }

        if (closeVertices.Count > 0)
        {
            float mergedHeight = vertexHeights[i];

            foreach (int index in closeVertices)
            {
                mergedHeight += vertexHeights[index];
            }

            mergedHeight /= (1 + closeVertices.Count);

            vertexHeights[i] = mergedHeight;

            foreach (int index in closeVertices)
            {
                vertexHeights[index] = mergedHeight;
            }
        }
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
