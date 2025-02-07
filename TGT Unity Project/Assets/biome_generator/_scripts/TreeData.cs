using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using tree_gen;
using Unity.Mathematics;
using System;

namespace biome
{
    [CreateAssetMenu(fileName = "TreeData", menuName = "Custom/TreeData", order = 1)]
    public class TreeData : ScriptableObject
    {
        public procedural_stem_01[] trees_big;
        public procedural_stem_01[] trees_medium;
        public GameObject[] trees_small;

        public GameObject[] bushes_01;
        public float treeSize = 1f;
        public float bushSize = 1f;

        public GameObject InstantiateTree(System.Random r, HitPoint h, float cutoff_small, float cutoff_medium)
        {
            if (trees_big == null)
            {
                // Debug.LogWarning("no tree set in TreeData");
                return null;
            }

            if (h.biomeIntensity <= cutoff_small)
            {
                GameObject ts = Instantiate(trees_small[GetRandomArrayEntry(r, trees_small.Length)], h.worldPosition, quaternion.identity);
                ts.transform.localScale = new Vector3(treeSize, treeSize, treeSize);
                return ts;
            }

            if (h.biomeIntensity <= cutoff_medium)
            {
                procedural_stem_01 tm = Instantiate(trees_medium[GetRandomArrayEntry(r, trees_medium.Length)], h.worldPosition, quaternion.identity);
                tm.InstantiateBranchesAndLeaves(r.Next(100, 100000));
                tm.transform.localScale = new Vector3(treeSize, treeSize, treeSize);
                return tm.gameObject;
            }
            else
            {
                procedural_stem_01 t = Instantiate(trees_big[0], h.worldPosition, quaternion.identity);
                t.InstantiateBranchesAndLeaves(r.Next(100, 100000));
                t.transform.localScale = new Vector3(treeSize, treeSize, treeSize);
                return t.gameObject;
            }


        }

        public GameObject InstantiateBush(System.Random r, Vector3 worldPos)
        {
            if (bushes_01 == null || bushes_01.Length == 0) return null;


            GameObject o = Instantiate(bushes_01[0], worldPos, quaternion.identity);
            o.transform.localScale = new Vector3(bushSize, bushSize, bushSize);
            o.transform.localRotation = GetRandomYRotation(r);
            return o.gameObject;
        }

        Quaternion GetRandomYRotation(System.Random r)
        {
            // Generate a random value between 0 and 1
            double randomValue = r.NextDouble();

            // Convert the random value to a random angle between -180 and 180 degrees
            float randomAngle = (float)(randomValue * 360f) - 180f;

            // Create a quaternion rotation around the y-axis with the random angle
            Quaternion randomRotation = Quaternion.Euler(0f, randomAngle, 0f);

            return randomRotation;
        }

        int GetRandomArrayEntry(System.Random r, int length)
        {
            // Ensure length is non-negative
            if (length <= 0)
            {
                throw new ArgumentException("Array length must be greater than zero.");
            }

            // Create a new instance of System.Random


            // Generate a random index within the range of the array length
            int randomIndex = r.Next(length);

            return randomIndex;
        }
    }
}
