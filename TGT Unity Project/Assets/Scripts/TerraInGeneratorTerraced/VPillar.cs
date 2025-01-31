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

    public int[] vertexIndices;
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
    public void PushVerticesFromPillar(List<Vector3> vertices, List<Vector2> uv, List<Vector3> normals)
    {
        Debug.Log($"pushing VertexHeights {vertexHeights[0]}, {vertexHeights[1]}, {vertexHeights[2]}, {vertexHeights[3]}");
        Dictionary<float, int> uniqueHeights = new Dictionary<float, int>();
        vertexIndices = new int[vertexHeights.Length];

        float[] roundedVertexHeights = RoundArray(vertexHeights, 3);

        for (int i = 0; i < roundedVertexHeights.Length; i++)
        {
            if (uniqueHeights.TryGetValue(roundedVertexHeights[i], out int existingIndex))
            {
                vertexIndices[i] = existingIndex;
            }
            else
            {
                Vector3 v = new Vector3(transform.position.x, roundedVertexHeights[i], transform.position.z);
                vertices.Add(v);
                int newIndex = vertices.Count - 1;
                uniqueHeights[roundedVertexHeights[i]] = newIndex;
                vertexIndices[i] = newIndex;

                uv.Add(new Vector2(transform.position.x, transform.position.z));
                normals.Add(new Vector3(0, 0, 0));
            }
        }
        Debug.Log($"setting VertexIndices {vertexIndices[0]}, {vertexIndices[1]}, {vertexIndices[2]}, {vertexIndices[3]}");
    }

    public static float[] RoundArray(float[] values, int roundDecimals)
    {
        float[] roundedValues = new float[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            roundedValues[i] = (float)Math.Round(values[i], roundDecimals);
        }
        return roundedValues;
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
