using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
// using UnityEngine;
using System.IO;
using System.Text;
// using UnityEditor;
using UnityEngine.VFX;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace biome
{
    public class BiomeGenerator : MonoBehaviour
    {
        public bool visualizeRaycasts = true;
        public bool visualizeHitPoints = true;

        public HitPoint[,] hitPoints;
        public Vector2Int heightmapResolution;
        public Texture2D inMap_01;

        public BiomeData[] biomes;

        // public List<HitPoint>[] hitPoints_per_biome;

        [HideInInspector]
        public Bounds combinedBounds;

        Texture2D exportMap;

        public Transform terrainHolder;

        public Material groundMat;

        private Texture originalTexture;

        public static float biggestColorDifference = 0f;

        System.Random r; //ONE random to rule them all. => deterministic random. gets passed down to any randomization
        // public int seed = 123;

        void Start()
        {

        }


        public void GenerateBiomes()
        {
            GenerateHitPoints();
            InstantiateBiomesFromHitpoints();
            GenerateTextureFromSize();
            SetBiomeColorToTrees();
        }

        //bG.inMap_01.width, bG.inMap_01.height, bG.groundMat, bG.hitPoints
        public void GenerateTextureFromSize()
        {
            // Create a new texture
            Texture2D texture = new Texture2D((int)inMap_01.width, (int)inMap_01.height);
            texture.filterMode = FilterMode.Point;

            // Generate texture content (here, a simple checkerboard pattern)
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    Color color = hitPoints[x, y].assignedBiome.colors[0];
                    texture.SetPixel(x, y, color);
                    Debug.Log(color);
                }
            }

            // Apply changes and save the texture
            texture.Apply();
            SaveTextureToAssets(texture, "GeneratedTexture");

            // Apply the texture to the material's albedo
            // material.mainTexture = texture;
            groundMat.SetTexture("_ColorMap", texture);
            // Debug.Log(texture);
#if UNITY_EDITOR
            EditorUtility.SetDirty(groundMat);
#endif
            // Set texture import settings
            SetTextureImportSettings(texture);

            SetShaderProperties(Mathf.Abs(combinedBounds.min.x) + combinedBounds.max.x, Mathf.Abs(combinedBounds.min.z) + combinedBounds.max.z, combinedBounds.min.x, combinedBounds.min.z, groundMat);
            // material.SetTexture("_MainTex", texture);
        }


        void SetShaderProperties(float x, float z, float offX, float offZ, Material myMat)
        {
            // Make sure to use the exact property names as in your Shader Graph
            string tilingXPropertyName = "_TilingX";
            string tilingZPropertyName = "_TilingZ";

            string offsetXPropertyName = "_OffsetX";
            string offsetZPropertyName = "_OffsetZ";

            // Set the tiling properties on the material
            myMat.SetFloat(tilingXPropertyName, 1f / x);
            myMat.SetFloat(tilingZPropertyName, 1f / z);

            myMat.SetFloat(offsetXPropertyName, offX);
            myMat.SetFloat(offsetZPropertyName, offZ);
        }

        void SaveTextureToAssets(Texture2D texture, string textureName)
        {
            // Convert the texture to a byte array
            byte[] bytes = texture.EncodeToPNG();

            // Specify the file path for saving in the Assets folder
            string filePath = Application.dataPath + "/biome_generator/ressources/" + textureName + ".png";

            // Write the bytes to the file
            System.IO.File.WriteAllBytes(filePath, bytes);

            Debug.Log("Texture saved to: " + filePath);
        }

        void SetTextureImportSettings(Texture2D texture)
        {
            AssetDatabase.Refresh();
            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(texture);
            assetPath = Application.dataPath + "/biome_generator/ressources/" + "GeneratedTexture" + ".png";
            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Debug.Log("assetPath: " + assetPath);
            if (textureImporter != null)
            {
                textureImporter.textureType = TextureImporterType.Default;
                textureImporter.npotScale = TextureImporterNPOTScale.None;
                textureImporter.mipmapEnabled = false;
                textureImporter.filterMode = FilterMode.Point;
                textureImporter.textureCompression = TextureImporterCompression.Uncompressed;

                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();
            }
        }

        public void SaveHeightMap()
        {
            if (hitPoints == null) GenerateHitPoints();
            exportMap = GenerateHeightmap(hitPoints, Color.white, Color.black, combinedBounds.max.y, combinedBounds.min.y);
            SaveTextureToPNG(exportMap, "biome_generator/export", "heightMap");
        }

        public void GenerateHitPoints()
        {
            if (terrainHolder == null)
            {
                FindAndAssignChunkHolder();
            }


            combinedBounds = CalculateCombinedBounds(terrainHolder);
            Debug.Log($"combinedBounds {combinedBounds}");

            if (inMap_01)
            {

                hitPoints = GenerateHitPointsArray(inMap_01.width, inMap_01.height, combinedBounds, inMap_01);
            }
            else
            {
                // fallback if no map is set
                hitPoints = GenerateHitPointsArray(heightmapResolution.x, heightmapResolution.y, combinedBounds, null);
            }
        }

        public void FindAndAssignChunkHolder()
        {
            // Start the search from the current object (or any root you choose)
            Transform found = FindChildByName(transform, "chunkHolder");
            if (found != null)
            {
                terrainHolder = found;
                Debug.Log("chunkHolder found and assigned to terrainHolder.");
            }
            else
            {
                Debug.LogWarning("chunkHolder not found in the hierarchy.");
            }
        }
        // Recursive helper function to search for a child by name
        private Transform FindChildByName(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }
                // Recursively search the child's children
                Transform result = FindChildByName(child, childName);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        public string LogHitpoints()
        {
            if (hitPoints == null)
            {
                Debug.LogWarning("HitPoints array is null.");
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();

            int numRows = hitPoints.GetLength(0);
            int numCols = hitPoints.GetLength(1);

            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    string hitPointString = hitPoints[i, j].ToString();
                    Debug.Log(hitPointString);
                    sb.AppendLine(hitPointString);
                }
            }

            return sb.ToString();
        }


        public void InstantiateMoss()
        {
            foreach (BiomeData d in biomes)
            {
                d.SetMossMaterial(d.colors[2]);
            }

            int rows = hitPoints.GetLength(0);
            int columns = hitPoints.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    HitPoint h = hitPoints[i, j];

                    if (h.assignedBiome.generateMoss)
                    {
                        Debug.Log(h.assignedBiome);
                        if ((i % h.assignedBiome.mossAmount == 0) && (j % h.assignedBiome.mossAmount == 0))
                        {
                            Debug.Log("mossing");
                            GenerateMoss(h);
                        }

                    }
                }
            }
        }




        public void InstantiateBiomesFromHitpoints()
        {
            DeleteBiomes();
            r = new System.Random(123);

            if (hitPoints == null)
            {
                GenerateHitPoints();
            }

            if (biomes == null || hitPoints == null)
            {
                Debug.LogWarning("Biome data or hit points not assigned.");
                return;
            }

            int numRows = hitPoints.GetLength(0);
            int numCols = hitPoints.GetLength(1);

            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    if (hitPoints[i, j].assignedBiome != null)
                    {
                        GameObject instantiatedTree = hitPoints[i, j].assignedBiome.InstantiateTreeFromBiome(r, hitPoints[i, j]);
                        if (instantiatedTree != null && hitPoints[i, j].parentTerrain != null)
                        {
                            instantiatedTree.transform.parent = hitPoints[i, j].parentTerrain.transform;
                        }

                    }

                }
            }


            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    if (hitPoints[i, j].assignedBiome != null)
                    {
                        GameObject[] b = hitPoints[i, j].assignedBiome.InstantiateBushesFromBiome(r, hitPoints[i, j], combinedBounds.max.y);
                        if (b != null && hitPoints[i, j].parentTerrain != null)
                        {
                            foreach (GameObject o in b)
                            {
                                o.transform.parent = hitPoints[i, j].parentTerrain.transform;
                            }
                        }

                    }

                }
            }
        }

        public void SetBiomeColorToTrees()
        {
            foreach (BiomeData b in biomes)
            {
                b.SetColorOfTreeMaterial();
                // b.SetBushMaterial(b.colors[2]);
            }
        }

        public void DeleteBiomes()
        {
            if (terrainHolder == null)
            {
                Debug.LogWarning("terrainHolder is not assigned.");
                return;
            }

            // Get all transforms under terrainHolder (including nested children).
            Transform[] allTransforms = terrainHolder.GetComponentsInChildren<Transform>(true);

            int countDeleted = 0;
            foreach (Transform t in allTransforms)
            {
                // If the transform has already been destroyed, skip it.
                if (t == null)
                    continue;

                // Skip the root object (terrainHolder) itself.
                if (t == terrainHolder)
                    continue;

                int layer = t.gameObject.layer;
                // Check if the object's layer is NOT "cliff", "ground", or "chunk"
                if (layer != LayerMask.NameToLayer("cliff") &&
                    layer != LayerMask.NameToLayer("ground") &&
                    layer != LayerMask.NameToLayer("chunk"))
                {
                    DestroyImmediate(t.gameObject);
                    countDeleted++;
                }
            }

            Debug.Log($"Deleted {countDeleted} biome GameObjects.");
        }




        void CollectGrandchildren(Transform parent, List<Transform> grandchildren) // Recursive method to collect all grandchildren of a transform
        {
            foreach (Transform child in parent)
            {
                foreach (Transform grandchild in child)
                {
                    // Add the grandchild to the list
                    grandchildren.Add(grandchild);

                    // Recursively collect grandchildren of this grandchild
                    CollectGrandchildren(grandchild, grandchildren);
                }
            }
        }


        Texture2D GenerateHeightmap(HitPoint[,] hPs, Color positiveColor, Color negativeColor, float positiveY, float negativeY)
        {
            int width = hPs.GetLength(0);
            int depth = hPs.GetLength(1);

            Texture2D texture = new Texture2D(width, depth, TextureFormat.RGBA32, false, true);


            Debug.Log($"pos y: {positiveY}, neg y: {negativeY}");

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    float worldPosY = hPs[x, z].worldPosition.y;


                    // Mapping y values to grayscale colors
                    float normalizedValue = Mathf.InverseLerp(negativeY, positiveY, worldPosY);

                    Color pixelColor = Color.Lerp(negativeColor, positiveColor, normalizedValue);

                    Debug.Log($"pixel {x}, {z}: worldPosY {worldPosY} gets set to {normalizedValue}, resulting in {pixelColor}");
                    texture.SetPixel(x, z, pixelColor);
                }
            }

            texture.Apply(); // Apply changes

            return texture;
        }


        void SaveTextureToPNG(Texture2D texture, string subPath, string fileName)
        {
            byte[] bytes = texture.EncodeToPNG();
            string filePath = Application.dataPath + "/" + subPath + "/" + fileName + ".png";
            System.IO.File.WriteAllBytes(filePath, bytes);
            Debug.Log("Texture saved to: " + filePath);
        }

        HitPoint[,] GenerateHitPointsArray(int numPointsX, int numPointsZ, Bounds b, Texture2D t)
        {
            HitPoint[,] hitPoints = new HitPoint[numPointsX, numPointsZ];   //the original array of hitPoints

            // Calculate the step size for each axis
            float stepX = b.size.x / numPointsX;
            float stepZ = b.size.z / numPointsZ;

            LayerMask groundMask = LayerMask.GetMask("ground");
            LayerMask rockMask = LayerMask.GetMask("rock");

            Debug.Log("groundMask: " + groundMask.value);
            Debug.Log("rockmask; " + rockMask.value);

            LayerMask combinedMask = groundMask | rockMask;

            // Cast raycasts and generate hit points
            for (int i = 0; i < numPointsX; i++)
            {
                for (int j = 0; j < numPointsZ; j++)
                {
                    float x = b.min.x + i * stepX + stepX / 2;
                    float z = b.min.z + j * stepZ + stepZ / 2;

                    Vector3 rayOrigin = new Vector3(x, b.max.y + 1, z);
                    // Debug.Log($"Cast at pos {rayOrigin}");
                    RaycastHit hit;

                    if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 1000, combinedMask))
                    {
                        // Debug.Log("Hit");
                        bool isRock = false;

                        if (hit.transform.gameObject.layer == LayerMask.NameToLayer("rock"))
                        {
                            isRock = true;
                        }

                        if (t == null)  //NO biomeMap assigned.
                        {
                            hitPoints[i, j] = new HitPoint(new Vector2Int(i, j), hit.point, hit.normal, Color.black, ColorHSL.FromRGB(Color.black), isRock, hit.transform.gameObject);
                        }
                        else            //biomeMap assigned => Generate 
                        {
                            Color c = GetPixelFromTexture(t, i, j);
                            ColorHSL cHSL = ColorHSL.FromRGB(c);

                            hitPoints[i, j] = new HitPoint(new Vector2Int(i, j), hit.point, hit.normal, c, cHSL, isRock, hit.transform.gameObject);
                            hitPoints[i, j].SetBiomeFromColor(biomes, cHSL);

                            // hitPoints_per_biome[hitPoints[i, j].assignedBiomeIndex].Add(hitPoints[i, j]);

                            if (hitPoints[i, j].assignedBiome.generateMoss)
                            {
                                hitPoints[i, j].terrainPoints = DetectCollisionPoints(4, hit.point, GetTileOffset(), LayerMask.GetMask("ground"));
                            }

                        }
                    }
                }
            }

            return hitPoints;
        }







        Color GetPixelFromTexture(Texture2D texture, int x, int z)
        {
            // Ensure the coordinates are within the texture bounds
            x = Mathf.Clamp(x, 0, texture.width - 1);
            z = Mathf.Clamp(z, 0, texture.height - 1);

            // Read the color from the specified pixel
            Color pixelColor = texture.GetPixel(x, z);

            return pixelColor;
        }
        Vector3[] DetectCollisionPoints(int numOfRays, Vector3 center, Vector2 tileOffset, LayerMask mask)
        {
            Vector3[] collisionPoints = new Vector3[numOfRays];
            // Debug.Log("circle::::::::::");
            Vector3[] p = CreateQuadPoints(center, tileOffset.x / 2);

            Vector3 offset = new Vector3(0, combinedBounds.max.y + 1f, 0);
            for (int i = 0; i < p.Length; i++)
            {
                Ray ray = new Ray(p[i] + offset, Vector3.down);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, combinedBounds.max.y - combinedBounds.min.y + 2f, mask))
                {
                    collisionPoints[i] = hit.point;
                }
                else
                {
                    collisionPoints[i] = Vector3.zero; // Set to zero if no collision
                }
            }

            return collisionPoints;
        }

        Vector3[] CreateQuadPoints(Vector3 center, float offset)
        {
            float setOffset = offset * .8f;
            Vector3[] quadPoints = new Vector3[4];

            quadPoints[0] = center + new Vector3(-setOffset, 0f, -setOffset);
            quadPoints[1] = center + new Vector3(-setOffset, 0f, setOffset);
            quadPoints[2] = center + new Vector3(setOffset, 0f, setOffset);
            quadPoints[3] = center + new Vector3(setOffset, 0f, -setOffset);

            return quadPoints;
        }
        Vector2 GetTileOffset()
        {
            if (inMap_01 != null)
            {
                float offsX = (combinedBounds.max.x - combinedBounds.min.x) / inMap_01.width;
                float offsZ = (combinedBounds.max.z - combinedBounds.min.z) / inMap_01.width;
                return new Vector2(offsX, offsZ);
            }
            else
            {
                float offsX = (combinedBounds.max.x - combinedBounds.min.x) / heightmapResolution.x;
                float offsZ = (combinedBounds.max.z - combinedBounds.min.z) / heightmapResolution.y;
                return new Vector2(offsX, offsZ);
            }


        }

        public void SetTerrainColorFromHitpoints()
        {
            Texture2D t = GenerateColorTextureFromHitPoints(hitPoints, biomes[0]);
            SaveTextureToDisk(t, "colorMap");
        }

        /// <summary>
        /// Generates a Texture2D from the hitPoints array.
        /// Each pixel is filled with hitPoints[x, y].assignedBiome.colors[1].
        /// </summary>
        /// <param name="hitPoints">2D array of HitPoint</param>
        /// <returns>The generated Texture2D</returns>
        public Texture2D GenerateColorTextureFromHitPoints(HitPoint[,] hitPoints, BiomeData defaultBiome)
        {
            if (hitPoints == null)
            {
                GenerateHitPoints();
            }

            int width = hitPoints.GetLength(0);
            int height = hitPoints.GetLength(1);

            // Create a texture with dimensions based on the hitPoints array.
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    HitPoint hp = hitPoints[x, y];
                    Color pixelColor = Color.magenta; // default/fallback color

                    if (hp.assignedBiome != null && hp.assignedBiome.colors != null && hp.assignedBiome.colors.Length > 1)
                    {
                        // Use hitPoint.biomeIntensity (0 to 1) to blend from black to the biome's color at index 1.
                        pixelColor = Color.Lerp(defaultBiome.colors[1], hp.assignedBiome.colors[1], hp.biomeIntensity);
                    }
                    else
                    {
                        Debug.LogWarning($"HitPoint at [{x}, {y}] does not have a valid assigned biome or color entry.");
                    }

                    // Ensure the pixel is fully opaque.
                    pixelColor.a = 1f;

                    texture.SetPixel(x, y, pixelColor);
                }
            }

            texture.Apply(); // Apply all pixel changes.
            return texture;
        }





        /// <summary>
        /// Saves a Texture2D as a PNG file to the Assets/Resources/Textures folder.
        /// Overwrites any existing file with the same name.
        /// </summary>
        /// <param name="colorMap">The texture to save.</param>
        /// <param name="textureName">The name of the texture file (without extension).</param>
        public void SaveTextureToDisk(Texture2D colorMap, string textureName)
        {
            // Build the folder path.
            string folderPath = Path.Combine(Application.dataPath, "Resources", "Textures");

            // Ensure the directory exists.
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Create the full file path.
            string filePath = Path.Combine(folderPath, textureName + ".png");

            // Encode the texture into PNG format.
            byte[] bytes = colorMap.EncodeToPNG();

            // Write the PNG file to disk, overwriting any existing file.
            File.WriteAllBytes(filePath, bytes);
            Debug.Log("Texture saved to: " + filePath);
        }
        public Bounds CalculateCombinedBounds(Transform terrains) // used to get the dimensions of terrain (WITHOUT trees)
        {
            Bounds combinedBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool boundsInitialized = false;

            // Get all Renderer components on the terrain and its children
            Renderer[] renderers = terrains.GetComponentsInChildren<Renderer>();

            // Get the layer number corresponding to "ground"
            int groundLayer = LayerMask.NameToLayer("ground");

            foreach (Renderer r in renderers)
            {
                // Only include renderers on the ground layer
                if (r != null && r.gameObject.layer == groundLayer)
                {
                    Bounds meshBounds = r.bounds;
                    if (!boundsInitialized)
                    {
                        combinedBounds = meshBounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(meshBounds);
                    }
                }
            }

            return combinedBounds;
        }



        void GenerateMoss(HitPoint h)
        {
            if (h.assignedBiome == biomes[0])
            {
                return;
            }
            if (HitPointIsSurroundedBySameBiome(h))
            {
                GameObject m = h.assignedBiome.InstantiateMossFromBiome(r, h, 2);
                m.transform.parent = h.parentTerrain.transform;
            }
            else
            {
                GameObject m = h.assignedBiome.InstantiateMossFromBiome(r, h, 1);
                m.transform.parent = h.parentTerrain.transform;
            }
            // GameObject m = CreateQuadAtPosition(h.worldPosition, h.terrainPoints, h.assignedBiome.mossMat);

            // return m;
        }
        public bool HitPointIsSurroundedBySameBiome(HitPoint h)
        {
            int rows = hitPoints.GetLength(0);
            int cols = hitPoints.GetLength(1);


            // Step 1: Check if the HitPoint lies on the border
            if (h.id.x == 0 || h.id.x == rows - 1 || h.id.y == 0 || h.id.y == cols - 1)
            {
                return false;
            }

            // Step 2: Collect the 8 surrounding hitPoints
            BiomeData biome = hitPoints[h.id.x, h.id.y].assignedBiome;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // Skip the current hitPoint

                    int nx = h.id.x + dx;
                    int ny = h.id.y + dy;

                    // Check if the neighboring hitPoint has the same assigned biome
                    if (hitPoints[nx, ny].assignedBiome != biome)
                    {
                        return false;
                    }
                }
            }

            // Step 3: All 8 surrounding hitPoints have the same Biome assigned
            return true;
        }


        GameObject CreateQuadAtPosition(Vector3 center, Vector3[] corners, Material m) //unused. initial idea for moss generation.
        {
            Debug.Log("mesh moss");
            if (corners == null) { return null; }

            // Create a new GameObject
            GameObject quadObject = new GameObject("Quad");

            // Add MeshFilter and MeshRenderer components
            MeshFilter meshFilter = quadObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = quadObject.AddComponent<MeshRenderer>();

            // Create the quad mesh
            Mesh quadMesh = new Mesh();

            Vector3 offsetFromGround = new Vector3(0f, 1f, 0f);
            // Define vertices
            Vector3[] vertices = new Vector3[5];
            vertices[0] = corners[0] + offsetFromGround;
            vertices[1] = corners[1] + offsetFromGround;
            vertices[2] = corners[2] + offsetFromGround;
            vertices[3] = corners[3] + offsetFromGround;
            vertices[4] = center + offsetFromGround;

            // Define UVs
            Vector2[] uvs = new Vector2[]
            {
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(.5f, .5f)
            };

            // Define normals
            Vector3[] normals = new Vector3[]
            {
            Vector3.up,
            Vector3.up,
            Vector3.up,
            Vector3.up,
            Vector3.up
            };

            // Debug.Log("v0: " + vertices[0] + "v1: " + vertices[1] + "v2: " + vertices[2] + "v3: " + vertices[3]);
            // Define triangles
            int[] triangles = new int[] { 0, 1, 4, 1, 2, 4, 2, 3, 4, 3, 0, 4 };

            // Set mesh data
            quadMesh.vertices = vertices;
            quadMesh.uv = uvs;
            quadMesh.normals = normals;
            quadMesh.triangles = triangles;

            // Assign the mesh to the MeshFilter
            meshFilter.mesh = quadMesh;

            // Assign the material to the MeshRenderer
            meshRenderer.material = m;

            return quadObject;
        }
    }



    public struct HitPoint
    {
        public HitPoint(Vector2Int _id, Vector3 _p, Vector3 _n, Color _pC, ColorHSL _cHSL, bool _isR, GameObject _parentT)
        {
            id = _id;
            worldPosition = _p;
            terrainPoints = null;
            worldNormal = _n;
            pointColor = _pC;
            pointColorHSL = _cHSL;
            assignedBiome = null;
            assignedBiomeIndex = -1;
            biomeIntensity = 1f;
            isRock = _isR;
            isSet = false;
            parentTerrain = _parentT;

        }
        public Vector2Int id;
        public Vector3 worldPosition;
        public Vector3[] terrainPoints; //filled but unused. was initial idea for moss generation. now replaced by meshes which get raycasted down per vertex
        public Vector3 worldNormal;
        public Color pointColor;
        public ColorHSL pointColorHSL; //custom struct to make colors compareable
        public BiomeData assignedBiome;
        public int assignedBiomeIndex;
        public float biomeIntensity;
        public bool isRock;
        public bool isSet;
        public GameObject parentTerrain;

        public readonly bool HasSameBiomeAssigned(HitPoint p)   //if true, use BIG moss prefab
        {
            if (p.assignedBiome == assignedBiome)
            {
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            // string terrainPointsString = (terrainPoints != null) ? $"Terrain Points: {string.Join(", ", terrainPoints)}" : "Terrain Points: null"; //removed from string because unused, see above
            string assignedBiomeString = (assignedBiome != null) ? $"Biome: {assignedBiome}, Intensity: {biomeIntensity} " : "Biome: null";

            return $"ID: {id}, pos: {worldPosition}, Col: {pointColor}, HSL: {pointColorHSL}, {assignedBiomeString}, Is Set: {isSet}, parentGO: {parentTerrain}";
        }
        public void SetBiomeFromColor(BiomeData[] biomes, ColorHSL cHSL)
        {
            float comparebase = .7f;
            int returnIndex = -1;
            for (int i = 1; i < biomes.Length; i++)
            {
                (float hueD, float satD, float lumD) = cHSL.CompareColorsHueSaturationLuminosity(ColorHSL.FromRGB(biomes[i].idColor));
                if ((hueD + satD + lumD) < comparebase)
                {
                    comparebase = hueD + satD + lumD;
                    returnIndex = i;
                }
            }

            if (returnIndex == -1)
            {
                if (biomes.Length == 0) { Debug.LogError("No Biome Data assigned. Set Scriptable Objexts"); }
                assignedBiome = biomes[0];
                assignedBiomeIndex = 0;
            }
            else
            {
                assignedBiome = biomes[returnIndex];
                assignedBiomeIndex = returnIndex;
                biomeIntensity = Mathf.Abs(comparebase * 1f - 1);
            }

        }
    }



    public struct ColorHSL
    {
        public float H; // from 0 to 360 degrees
        public float S; // from 0 to 1
        public float L; // from 0 to 1 

        public static ColorHSL FromRGB(Color color)
        {
            float r = color.r;
            float g = color.g;
            float b = color.b;

            float max = Mathf.Max(r, Mathf.Max(g, b));
            float min = Mathf.Min(r, Mathf.Min(g, b));

            float h = 0;
            if (max == min)
            {
                h = 0; // grayscale
            }
            else if (max == r)
            {
                h = (60 * (g - b) / (max - min) + 360) % 360;
            }
            else if (max == g)
            {
                h = (60 * (b - r) / (max - min) + 120);
            }
            else if (max == b)
            {
                h = (60 * (r - g) / (max - min) + 240);
            }

            float l = (max + min) / 2;
            float s = (l == 0 || max == min) ? 0 : (l <= 0.5f) ? (max - min) / (max + min) : (max - min) / (2 - max - min);

            return new ColorHSL { H = h, S = s, L = l };
        }
        public override string ToString()
        {
            return $"(H: {H}, S: {S}, L: {L})";
        }
    }

    public static class ColorExtensions
    {
        public static (float hueD, float satD, float lumD) CompareColorsHueSaturationLuminosity(this ColorHSL a, ColorHSL b)
        {
            float hueD = CompareHues(a.H, b.H); // from 0 to 1
            float satD = Mathf.Abs(a.S - b.S); // from 0 to 1
            float lumD = Mathf.Abs(a.L - b.L); // from 0 to 1
            // Debug.Log($"Colors: a: {a}, b: {b}, hue difference: {hueD}");
            return (hueD, satD, lumD);
        }
        public static float CompareHues(float a, float b)
        {
            // Calculate the angular difference between a and b
            float angularDifference = Mathf.Abs((a - b + 180) % 360 - 180);

            // Normalize the angular difference to a range between 0 and 1
            float normalizedDifference = angularDifference / 180f;


            float difference = normalizedDifference;
            return difference;
        }

    }
}