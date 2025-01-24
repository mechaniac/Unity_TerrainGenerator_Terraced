//
// ShaderGraphEssentials for Unity
// (c) 2019 PH Graphics
// Source code may be used and modified for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 
// *** A NOTE ABOUT PIRACY ***
// 
// If you got this asset from a pirate site, please consider buying it from the Unity asset store. This asset is only legally available from the Unity Asset Store.
// 
// I'm a single indie dev supporting my family by spending hundreds and thousands of hours on this and other assets. It's very offensive, rude and just plain evil to steal when I (and many others) put so much hard work into the software.
// 
// Thank you.
//
// *** END NOTE ABOUT PIRACY ***
//
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderGraphEssentials
{
    class BakeShaderData
    {
        public BakeTextureNode Node { get; set; }
        internal GraphData Graph { get; set; }
        public Shader Shader { get; set; }
        public string ShaderString { get; set; }
        public bool HasError { get; set; }
        public string OutputIdName { get; set; }
    }
    // Most of this class use copy pasted / inspired / modified versions of PreviewManager.cs in Unity's SG.
     class BakeTextureManager
    {
        private const string DefaultPath = "SGE_DefaultBakedTexture.png";

        BakeTextureManager()
        {
        }

        internal static void BakeShaderIntoTexture(BakeShaderData shaderData)
        {
            var node = shaderData.Node;
            var renderTexture = new RenderTexture(node.Width, node.Height, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave };
            renderTexture.Create();

            // setup mesh
            Mesh quadMesh = Resources.GetBuiltinResource(typeof(Mesh), "Quad.fbx") as Mesh;
            Material bakeMaterial = new Material(shaderData.Shader);
            MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();

            // setup camera
            GameObject cameraGo = new GameObject();
            var camera = cameraGo.AddComponent<Camera>();
            camera.cameraType = CameraType.Preview;
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.Depth;
            camera.fieldOfView = 15;
            camera.farClipPlane = 10.0f;
            camera.nearClipPlane = 2.0f;
            camera.backgroundColor = new Color(49.0f / 255.0f, 49.0f / 255.0f, 49.0f / 255.0f, 1.0f);
            camera.renderingPath = RenderingPath.Forward;
            camera.useOcclusionCulling = false;
            camera.clearFlags = CameraClearFlags.Depth;
            camera.transform.position = -Vector3.forward * 2;
            camera.transform.rotation = Quaternion.identity;
            camera.orthographicSize = 0.5f;
            camera.orthographic = true;
            camera.targetTexture = renderTexture;

            Stack<AbstractMaterialNode> tempNodeWave = new Stack<AbstractMaterialNode>(); 
            HashSet<AbstractMaterialNode > tempAddedToNodeWave = new HashSet<AbstractMaterialNode>();
            HashSet<AbstractMaterialNode> nodesToDraw = new HashSet<AbstractMaterialNode>();
            
            // setup material and fill properties from all the previous nodes
            var sources = new HashSet<AbstractMaterialNode>
            {
                node
            };
            
            PropagateNodes(sources, PropagationDirection.Upstream, tempNodeWave, tempAddedToNodeWave, nodesToDraw);
            
            PooledList<PreviewProperty> perMaterialPreviewProperties = PooledList<PreviewProperty>.Get();
            CollectPreviewProperties(shaderData.Graph, nodesToDraw, perMaterialPreviewProperties, materialPropertyBlock);

            AssignPerMaterialPreviewProperties(bakeMaterial, perMaterialPreviewProperties);

            // draw the quad into the camera's render texture
            Graphics.DrawMesh(quadMesh, Matrix4x4.identity, bakeMaterial, 1, camera, 0, materialPropertyBlock, ShadowCastingMode.Off, false, null, false);

            camera.Render();

            // get the render texture from GPU to CPU and save it as an asset
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            var textureBytes = texture.EncodeToPNG();

            string path = "";
            bool textureFound = false;
            if (node.OutputTexture != null && AssetDatabase.Contains(node.OutputTexture))
            {
                try
                {
                    path = AssetDatabase.GetAssetPath(node.OutputTexture);
                    System.IO.File.WriteAllBytes(path, textureBytes);
                    AssetDatabase.ImportAsset(path);
                    textureFound = true;
                } catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            if (!textureFound)
            {
                path = Application.dataPath + "/" + DefaultPath;
                System.IO.File.WriteAllBytes(path, textureBytes);
                path = "Assets/" + DefaultPath;
                AssetDatabase.ImportAsset(path);
                Debug.LogWarning("No previous baked texture was found so a new one was created at Assets/" + DefaultPath + ". Please rename or move the texture as this specific texture path might be overriden.");
            }

            node.OutputTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));

            // cleanup
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(cameraGo);
            UnityEngine.Object.DestroyImmediate(bakeMaterial);
        }
        
        static void AssignPerMaterialPreviewProperties(Material mat, List<PreviewProperty> perMaterialPreviewProperties)
        {
            foreach (var prop in perMaterialPreviewProperties)
            {
                switch (prop.propType)
                {
                    case PropertyType.VirtualTexture:

                        // setup the VT textures on the material
                        bool setAnyTextures = false;
                        var vt = prop.vtProperty.value;
                        for (int layer = 0; layer < vt.layers.Count; layer++)
                        {
                            var texture = vt.layers[layer].layerTexture?.texture;
                            int propIndex = mat.shader.FindPropertyIndex(vt.layers[layer].layerRefName);
                            if (propIndex != -1)
                            {
                                mat.SetTexture(vt.layers[layer].layerRefName, texture);
                                setAnyTextures = true;
                            }
                        }
                        // also put in a request for the VT tiles, since preview rendering does not have feedback enabled
                        if (setAnyTextures)
                        {
#if ENABLE_VIRTUALTEXTURES
                            int stackPropertyId = Shader.PropertyToID(prop.vtProperty.referenceName);
                            try
                            {
                                // Ensure we always request the mip sized 256x256
                                int width, height;
                                UnityEngine.Rendering.VirtualTexturing.Streaming.GetTextureStackSize(mat, stackPropertyId, out width, out height);
                                int textureMip = (int)Math.Max(Mathf.Log(width, 2f), Mathf.Log(height, 2f));
                                const int baseMip = 8;
                                int mip = Math.Max(textureMip - baseMip, 0);
                                UnityEngine.Rendering.VirtualTexturing.Streaming.RequestRegion(mat, stackPropertyId, new Rect(0.0f, 0.0f, 1.0f, 1.0f), mip, UnityEngine.Rendering.VirtualTexturing.System.AllMips);
                            }
                            catch (InvalidOperationException)
                            {
                                // This gets thrown when the system is in an indeterminate state (like a material with no textures assigned which can obviously never have a texture stack streamed).
                                // This is valid in this case as we're still authoring the material.
                            }
#endif // ENABLE_VIRTUALTEXTURES
                        }
                        break;
                }
            }   
        }

        static void CollectPreviewProperties(GraphData m_Graph, IEnumerable<AbstractMaterialNode> nodesToCollect, PooledList<PreviewProperty> perMaterialPreviewProperties,
            MaterialPropertyBlock mSharedPreviewPropertyBlock)
        {
            using (var tempPreviewProps = PooledList<PreviewProperty>.Get())
            {
                // collect from all of the changed nodes
                foreach (var propNode in nodesToCollect)
                    propNode.CollectPreviewMaterialProperties(tempPreviewProps);

                // also grab all graph properties (they are updated every frame)
                foreach (var prop in m_Graph.properties)
                    tempPreviewProps.Add(prop.GetPreviewMaterialProperty());

                foreach (var previewProperty in tempPreviewProps)
                {
                    previewProperty.SetValueOnMaterialPropertyBlock(mSharedPreviewPropertyBlock);

                    // virtual texture assignments must be pushed to the materials themselves (MaterialPropertyBlocks not supported)
                    if ((previewProperty.propType == PropertyType.VirtualTexture) &&
                        (previewProperty.vtProperty?.value?.layers != null))
                    {
                        perMaterialPreviewProperties.Add(previewProperty);
                    }
                }
            }
        }
        
        enum PropagationDirection
        {
            Upstream,
            Downstream
        }

        static void PropagateNodes(HashSet<AbstractMaterialNode> sources, PropagationDirection dir, Stack<AbstractMaterialNode> m_TempNodeWave, HashSet<AbstractMaterialNode> m_TempAddedToNodeWave, HashSet<AbstractMaterialNode> result)
        {
            Action<AbstractMaterialNode> AddNextLevelNodesToWave =
                nextLevelNode =>
                {
                    if (!m_TempAddedToNodeWave.Contains(nextLevelNode))
                    {
                        m_TempNodeWave.Push(nextLevelNode);
                        m_TempAddedToNodeWave.Add(nextLevelNode);
                    }
                };
            
            if (sources.Count > 0)
            {
                // NodeWave represents the list of nodes we still have to process and add to result
                m_TempNodeWave.Clear();
                m_TempAddedToNodeWave.Clear();
                foreach (var node in sources)
                {
                    m_TempNodeWave.Push(node);
                    m_TempAddedToNodeWave.Add(node);
                }

                while (m_TempNodeWave.Count > 0)
                {
                    var node = m_TempNodeWave.Pop();
                    if (node == null)
                        continue;

                    result.Add(node);

                    // grab connected nodes in propagation direction, add them to the node wave
                    ForeachConnectedNode(node, dir, AddNextLevelNodesToWave);
                }

                // clean up any temp data
                m_TempNodeWave.Clear();
                m_TempAddedToNodeWave.Clear();
            }
        }

        static void ForeachConnectedNode(AbstractMaterialNode node, PropagationDirection dir, Action<AbstractMaterialNode> action)
        {
            using (var tempEdges = PooledList<IEdge>.Get())
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                // Loop through all nodes that the node feeds into.
                if (dir == PropagationDirection.Downstream)
                    node.GetOutputSlots(tempSlots);
                else
                    node.GetInputSlots(tempSlots);

                foreach (var slot in tempSlots)
                {
                    // get the edges out of each slot
                    tempEdges.Clear();                            // and here we serialize another list, ouch!
                    node.owner.GetEdges(slot.slotReference, tempEdges);
                    foreach (var edge in tempEdges)
                    {
                        // We look at each node we feed into.
                        var connectedSlot = (dir == PropagationDirection.Downstream) ? edge.inputSlot : edge.outputSlot;
                        var connectedNode = connectedSlot.node;

                        action(connectedNode);
                    }
                }
            }
        }
    }
}