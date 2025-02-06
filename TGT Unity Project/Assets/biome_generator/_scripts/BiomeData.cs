using System.Collections;
using System.Collections.Generic;
using tree_gen;
using UnityEngine;

namespace biome
{
    [CreateAssetMenu(fileName = "BiomeData", menuName = "Custom/BiomeData", order = 1)]
    public class BiomeData : ScriptableObject
    {
        public Color idColor;
        public Color[] colors;

        public TreeData[] td;
        public float cutoff_small = .1f;
        public float cutoff_medium = .5f;

        public float bush_offset_from_tree = 2f;

        public Material treeMat;


        public bool generateMoss;
        public float mossSize = 1f;
        public int mossAmount = 1;  //maximum 4
        public GameObject[] mossPrefabs;
        public Material mossMat;
        Material mossMatToAssign;

        public Material bushMat;
        // Material bushMatToAssign;



        public GameObject InstantiateTreeFromBiome(System.Random r, HitPoint h)
        {
            if (td == null || td.Length == 0)
            {
                // Debug.Log("no treeData set in biome");
                return null;
            }
            else
            {

                if (td[0] != null)
                {
                    return td[0].InstantiateTree(r, h, cutoff_small, cutoff_medium);
                }
                return null;

            }

        }

        public GameObject[] InstantiateBushesFromBiome(System.Random r, HitPoint h, float upperBound)
        {
            if (td == null || td.Length == 0)
            {
                Debug.Log("no treeData set in biome");
                return null;
            }
            else
            {
                if (td[0] != null)
                {
                    Vector3[] bushPoints = GetCircularPositionsFromCenter(6, h.worldPosition, 3f, upperBound + 1);
                    GameObject[] bushes = new GameObject[6];

                    for (int i = 0; i < bushPoints.Length; i++)
                    {
                        bushes[i] = td[0].InstantiateBush(r, bushPoints[i]);
                        MeshRenderer renderer = bushes[i].GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            // Assign the material to the MeshRenderer component
                            Debug.Log($"assigning material");
                            renderer.material = bushMat;
                        }
                    }
                    return bushes;
                }
                return null;
            }
        }

        Vector3 GetGroundPointFromRay(Vector3 rayStartPoint)
        {
            if (Physics.Raycast(rayStartPoint, Vector3.down, out RaycastHit hit, 200, LayerMask.GetMask("ground")))
            {
                return hit.point;
            }
            else
            {
                return Vector3.zero;
            }
        }

        Vector3[] GetCircularPositionsFromCenter(int amount, Vector3 center, float radius, float yPosition)
        {
            Vector3[] positions = new Vector3[amount];

            // Calculate the angle between each position
            float angleStep = 360f / amount;

            // Generate positions around the circle
            for (int i = 0; i < amount; i++)
            {
                // Calculate the angle for this position
                float angle = i * angleStep * Mathf.Deg2Rad;

                // Calculate the position on the circle using trigonometry
                float x = center.x + radius * Mathf.Cos(angle);
                float z = center.z + radius * Mathf.Sin(angle);

                // Set the position in the array
                positions[i] = GetGroundPointFromRay(new Vector3(x, yPosition, z));
            }

            return positions;
        }

        public void SetMossMaterial(Color c)
        {
            if (mossMat == null) return;

            Material m = new Material(mossMat);
            if (m != null)
            {
                // Set the color value of the MainColor property
                m.SetColor("_Color", c);
            }
            else
            {
                Debug.LogWarning("Material not assigned.");
            }
            mossMatToAssign = m; //set back to material to assign to moss
        }



        public GameObject InstantiateMossFromBiome(System.Random r, HitPoint h, int mossPrefab)
        { // 0 = small, 1 = medium, 2 = big
            if (mossPrefabs == null || mossPrefabs.Length == 0)
            {
                Debug.Log("no mosses set in biome");
                return null;
            }
            else
            {
                if (mossPrefabs[mossPrefab] != null)
                {
                    GameObject m = Instantiate(mossPrefabs[mossPrefab], h.worldPosition, Quaternion.identity);
                    // m.transform.localScale = new Vector3(mossSize, mossSize, mossSize);
                    MeshRenderer renderer = m.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        // Assign the material to the MeshRenderer component
                        renderer.material = mossMatToAssign;
                    }
                    CopyAndScaleMeshInstance(m, h.assignedBiome.mossSize);
                    SnapMeshToGround(m, 100, .1f);
                    return m;
                }
            }

            return null;
        }
        void CopyAndScaleMeshInstance(GameObject instance, float factor)
        {
            // Get the MeshFilter component of the instance
            MeshFilter meshFilter = instance.GetComponent<MeshFilter>();

            // Get the original shared mesh from the MeshFilter component
            Mesh originalMesh = meshFilter.sharedMesh;

            // Create a new mesh for this instance
            Mesh newMesh = new Mesh
            {
                // Clone the original mesh to the new mesh
                vertices = originalMesh.vertices,
                normals = originalMesh.normals,
                triangles = originalMesh.triangles,
                uv = originalMesh.uv,
                colors = originalMesh.colors,
                tangents = originalMesh.tangents
            };

            // Modify the vertices of the new mesh (example modification)
            // You can modify vertices here according to your requirements
            Vector3[] vertices = newMesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] *= factor;
                // Modify vertex position based on individual instance requirements
                // vertices[i] += new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
            }
            newMesh.vertices = vertices;

            // Assign the new mesh to the MeshFilter component
            meshFilter.mesh = newMesh;
        }


        Mesh SnapMeshToGround(GameObject o, float rayStartPositionY, float offsetFromGround)
        {
            Mesh m = GetMeshOfGameObject(o);

            // Clone the original mesh to modify it without affecting the original
            Mesh snappedMesh = Instantiate(m);

            // Get the vertices of the mesh
            Vector3[] vertices = snappedMesh.vertices;

            // Iterate over each vertex of the mesh
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 localVertexPosition = m.vertices[i];

                // Get the world position of the vertex by transforming it from local space to world space
                Vector3 worldVertexPosition = o.transform.TransformPoint(localVertexPosition);

                // Define the ray start point
                Vector3 rayStartPoint = new Vector3(worldVertexPosition.x, rayStartPositionY, worldVertexPosition.z);

                // Cast a ray downwards from the ray start point
                if (Physics.Raycast(rayStartPoint, Vector3.down, out RaycastHit hit, 200, LayerMask.GetMask("ground")))
                {
                    // Adjust the vertex y position to the collision position plus the offsetFromGround
                    vertices[i].y = hit.point.y + offsetFromGround;
                    // Debug.Log(hit.point);
                }
            }

            // Update the vertices of the mesh
            m.vertices = vertices;
            o.transform.position = new Vector3(o.transform.position.x, 0f, o.transform.position.z);
            // Recalculate the normals and bounds of the mesh
            m.RecalculateNormals();
            m.RecalculateBounds();

            return m;
        }

        Mesh GetMeshOfGameObject(GameObject obj)
        {
            // Get the MeshFilter component attached to the GameObject
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();

            // Check if the MeshFilter component exists
            if (meshFilter != null)
            {
                // Return the shared mesh of the MeshFilter component
                return meshFilter.sharedMesh;
            }
            else
            {
                // Log a warning if the MeshFilter component is not found
                Debug.LogWarning("MeshFilter component not found on the GameObject.");
                return null;
            }
        }

        public void SetColorOfTreeMaterial()
        {
            if (treeMat != null)
            {
                treeMat.SetColor("_ColorBase", colors[0]);
                treeMat.SetColor("_ColorMasked", colors[1]);
                Debug.Log($"treeMat: {treeMat}, color0: {colors[0]}, color1: {colors[1]}");
            }
        }
        public void SetBushMaterial(Color c)
        {
            if (bushMat != null) {
                bushMat.SetColor("_BaseColor", c);
            }
        }
    }
}