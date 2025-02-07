using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace biome
{
    [CustomEditor(typeof(BiomeGenerator))]
    public class BiomeGeneratorEditor : Editor
    {
        static BiomeGenerator bG;

        public override void OnInspectorGUI()
        {
            bG = (BiomeGenerator)target;
            base.OnInspectorGUI();

            if (GUILayout.Button("Save Heightmap"))
            {
                bG.SaveHeightMap();
            }

            if (GUILayout.Button("Generate HitPoints"))
            {
                bG.GenerateHitPoints();

            }
            if (GUILayout.Button("Log HitPoints"))
            {
                string message = bG.LogHitpoints();
                File.AppendAllText("BiomeGenerationLog.txt", message + "\n");
            }
            if (GUILayout.Button("log"))
            {
                Debug.Log("biggest hueD: " + BiomeGenerator.biggestColorDifference);
                Debug.Log("bounds: " + bG.combinedBounds);
                Debug.Log("min x: " + bG.combinedBounds.min.x + "max x: " + bG.combinedBounds.max.x);
                Debug.Log(bG.combinedBounds.min.x + bG.combinedBounds.max.x);
                Debug.Log(bG.combinedBounds.min.y + bG.combinedBounds.max.y);
                Debug.Log(bG.combinedBounds.min.z + bG.combinedBounds.max.z);

                // bG.LogData();

            }


            if (GUILayout.Button("Instantiate Biomes From HitPoints"))
            {
                bG.InstantiateBiomesFromHitpoints();
            }

            // if (GUILayout.Button("Set Terrain Color From HitPoints"))
            // {
            //     bG.SetTerrainColorFromHitpoints();
            // }

            if (GUILayout.Button("Instantiate Moss"))
            {
                bG.InstantiateMoss();
            }

            if (GUILayout.Button("Set Ground Colormap"))
            {
                bG.GenerateGroundColorMap();
            }

            if (GUILayout.Button("Set Tree Colors"))
            {
                bG.SetBiomeColorToTrees();
            }
            if (GUILayout.Button("Delete Biomes"))
            {
                bG.DeleteBiomes();
            }

            if(GUILayout.Button("GENERATE ALL")){
                bG.GenerateBiomes();
            }


        }

        void OnDrawGizmos()
        {
            // DrawBoundsGizmo(bG.combinedBounds);

        }


        // Extra function to visualize the raycasts.
        static void VisualizeRaycasts(Bounds b, int numPointsX, int numPointsZ)
        {
            float stepX = b.size.x / numPointsX;
            float stepZ = b.size.z / numPointsZ;

            // Define your layer mask just as in your raycasting function.
            LayerMask groundMask = LayerMask.GetMask("ground");
            LayerMask rockMask = LayerMask.GetMask("rock");
            LayerMask combinedMask = groundMask | rockMask;

            for (int i = 0; i < numPointsX; i++)
            {
                for (int j = 0; j < numPointsZ; j++)
                {
                    float x = b.min.x + i * stepX + stepX / 2;
                    float z = b.min.z + j * stepZ + stepZ / 2;
                    Vector3 rayOrigin = new Vector3(x, b.max.y + 1, z);
                    RaycastHit hit;
                    Vector3 rayEnd;

                    // Perform the raycast. Use the same max distance as before.
                    if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 1000f, combinedMask))
                    {
                        rayEnd = hit.point;
                    }
                    else
                    {
                        // Fallback: draw ray to the bottom of the bounds.
                        rayEnd = new Vector3(x, b.min.y, z);
                    }

                    // Draw the ray.
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(rayOrigin, rayEnd);

                    // Optionally, draw a sphere at the ray origin and hit.
                    // Gizmos.DrawSphere(rayOrigin, 0.1f);
                    // if (hit.collider != null)
                    // {
                    //     Gizmos.color = Color.green;
                    //     Gizmos.DrawSphere(hit.point, 0.1f);
                    // }
                }
            }
        }





        [DrawGizmo(GizmoType.Selected)]
        static void DrawGizmos(BiomeGenerator bG, GizmoType gizmoType)
        {
            Handles.color = Color.green;
            Handles.DrawWireCube(bG.combinedBounds.center, bG.combinedBounds.size);

            if (bG.visualizeHitPoints)
            {
                if (bG.hitPoints != null)
                {
                    // Debug.Log("drawing Gizmos");
                    for (int i = 0; i < bG.hitPoints.GetLength(0); i++)
                    {
                        for (int j = 0; j < bG.hitPoints.GetLength(1); j++)
                        {
                            if (bG.hitPoints[i, j].assignedBiome != null)
                            {
                                Handles.color = bG.hitPoints[i, j].assignedBiome.idColor;
                                if (bG.hitPoints[i, j].isRock)
                                {
                                    Handles.color = Color.black;
                                }
                            }
                            Handles.DrawSolidDisc(bG.hitPoints[i, j].worldPosition + new Vector3(0f, bG.hitPoints[i, j].biomeIntensity, 0f), Vector3.up, bG.hitPoints[i, j].biomeIntensity);

                            if (bG.hitPoints[i, j].terrainPoints != null)
                            {
                                for (int tP = 0; tP < bG.hitPoints[i, j].terrainPoints.Length; tP++)
                                {
                                    Vector3 tPP = bG.hitPoints[i, j].terrainPoints[tP];
                                    Handles.DrawWireDisc(tPP + new Vector3(0f, bG.hitPoints[i, j].biomeIntensity, 0f), Vector3.up, .1f);
                                }
                            }
                        }
                    }
                }
            }


            if (!bG.visualizeRaycasts || bG.combinedBounds == null)
                return;

            // Ensure that bounds are valid (you might compute or assign them elsewhere).
            // Also, numPointsX and numPointsZ should be set accordingly.
            if (bG.combinedBounds.size == Vector3.zero)
                return;

            VisualizeRaycasts(bG.combinedBounds, bG.inMap_01.width, bG.inMap_01.height);

        }


        void DrawBoundsGizmo(Bounds bounds)
        {
            Handles.color = Color.yellow;
            Handles.DrawWireCube(bounds.center, bounds.size);
        }


        static void DrawGridGizmos(Vector2 v, Bounds b)
        {
            // Calculate the step size for each axis
            float stepX = b.size.x / v.x;
            float stepZ = b.size.z / v.y;
            float r = (stepX + stepZ) / 10;

            // Draw spheres on the top plane of the bounds
            for (float x = b.min.x + stepX / 2; x <= b.max.x; x += stepX)
            {
                for (float z = b.min.z + stepZ / 2; z <= b.max.z; z += stepZ)
                {
                    Vector3 position = new Vector3(x, b.max.y, z);
                    Handles.DrawWireDisc(position, Vector2.up, r);
                }
            }
        }

        static void DrawGizmoPerPoint(HitPoint[,] a, Color c)
        {
            Handles.color = c;

            int numRows = a.GetLength(0);
            int numCols = a.GetLength(1);

            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    // Handles.color = a[i, j].pointColor;
                    Handles.DrawWireDisc(a[i, j].worldPosition, Vector3.up, .5f);
                }
            }
        }
    }

}