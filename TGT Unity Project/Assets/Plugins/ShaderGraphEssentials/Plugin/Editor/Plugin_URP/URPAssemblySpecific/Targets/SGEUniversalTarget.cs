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
using System.Linq;
using ShaderGraphEssentials.Legacy;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.ShaderGraph.Serialization;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using CullMode = UnityEngine.Rendering.CullMode;
using RenderQueue = UnityEditor.ShaderGraph.RenderQueue;
#if HAS_VFX_GRAPH
using UnityEditor.VFX;
#endif

namespace ShaderGraphEssentials
{
    enum BlendMode
    {
        Off = 0,
        Alpha = 1,
        Premultiply = 2,
        Additive = 3,
        Multiply = 4
    }

    sealed class SGEUniversalTarget : Target, IHasMetadata, ILegacyTarget
#if HAS_VFX_GRAPH
        , IMaySupportVFX, IRequireVFXContext
#endif
    {
        public override int latestVersion => 1;
        
        // Constants
        static readonly GUID kSourceCodeGuid = new GUID("f9a02d7a49622a44bb0e1e7f11ade6bd"); // SGEUniversalTarget.cs
        public const string kPipelineTag = "UniversalPipeline";

        // SubTarget
        List<SubTarget> m_SubTargets;
        List<string> m_SubTargetNames;
        int activeSubTargetIndex => m_SubTargets.IndexOf(m_ActiveSubTarget);

        // View
        PopupField<string> m_SubTargetField;
        TextField m_CustomGUIField;
#if HAS_VFX_GRAPH
        Toggle m_SupportVFXToggle;
#endif

        [SerializeField]
        JsonData<SubTarget> m_ActiveSubTarget;

        [SerializeField] 
        private RenderType m_RenderType = RenderType.Opaque;

        [SerializeField] 
        private RenderQueue m_RenderQueue = RenderQueue.Geometry;

        [SerializeField]
        private BlendMode m_BlendMode = BlendMode.Off;
        
        [SerializeField]
        private bool m_AlphaClip = false;
        
        [SerializeField]
        bool m_CastShadows = true;

        [SerializeField]
        bool m_ReceiveShadows = true;

        [SerializeField]
        bool m_SupportsLODCrossFade = false;

        [SerializeField]
        private CullMode m_CullMode = CullMode.Back;

        [SerializeField] 
        private ZWrite m_ZWrite = ZWrite.On;

        [SerializeField] 
        private ZTest m_ZTest = ZTest.Less;
        
        [SerializeField]
        string m_CustomEditorGUI;
        
        [SerializeField]
        bool m_SupportVFX;

        internal override bool ignoreCustomInterpolators => false;
        internal override int padCustomInterpolatorLimit => 4;

        public SGEUniversalTarget()
        {
            displayName = "SGE Universal";
            m_SubTargets = TargetUtils.GetSubTargets(this);
            m_SubTargetNames = m_SubTargets.Select(x => x.displayName).ToList();
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
        }
        
        public string disableBatching
        {
            get
            {
                if (supportsLodCrossFade)
                    return $"{UnityEditor.ShaderGraph.DisableBatching.LODFading}";
                else
                    return $"{UnityEditor.ShaderGraph.DisableBatching.False}";
            }
        }


        public SubTarget activeSubTarget
        {
            get => m_ActiveSubTarget;
            set => m_ActiveSubTarget = value;
        }

        public RenderType renderType
        {
            get => m_RenderType;
            set => m_RenderType = value;
        }
        
        public RenderQueue renderQueue
        {
            get => m_RenderQueue;
            set => m_RenderQueue = value;
        }

        public BlendMode blendMode
        {
            get => m_BlendMode;
            set => m_BlendMode = value;
        }
        
        public bool alphaClip
        {
            get => m_AlphaClip;
            set => m_AlphaClip = value;
        }
        
        public bool castShadows
        {
            get => m_CastShadows;
            set => m_CastShadows = value;
        }

        public bool receiveShadows
        {
            get => m_ReceiveShadows;
            set => m_ReceiveShadows = value;
        }

        public bool supportsLodCrossFade
        {
            get => m_SupportsLODCrossFade;
            set => m_SupportsLODCrossFade = value;
        }

        public CullMode cullMode
        {
            get => m_CullMode;
            set => m_CullMode = value;
        }
        
        public ZWrite zWrite
        {
            get => m_ZWrite;
            set => m_ZWrite = value;
        }
        
        public ZTest zTest
        {
            get => m_ZTest;
            set => m_ZTest = value;
        }

        public string customEditorGUI
        {
            get => m_CustomEditorGUI;
            set => m_CustomEditorGUI = value;
        }

        public override bool IsActive()
        {
            bool isUniversalRenderPipeline = GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;
            return isUniversalRenderPipeline && activeSubTarget.IsActive();
        }

        public override bool IsNodeAllowedByTarget(Type nodeType)
        {
            SRPFilterAttribute srpFilter = NodeClassCache.GetAttributeOnNodeType<SRPFilterAttribute>(nodeType);
            bool worksWithThisSrp = srpFilter == null || srpFilter.srpTypes.Contains(typeof(UniversalRenderPipeline));

            SubTargetFilterAttribute subTargetFilter = NodeClassCache.GetAttributeOnNodeType<SubTargetFilterAttribute>(nodeType);
            bool worksWithThisSubTarget = subTargetFilter == null || subTargetFilter.subTargetTypes.Contains(activeSubTarget.GetType());

            return worksWithThisSrp && worksWithThisSubTarget && base.IsNodeAllowedByTarget(nodeType);
        }

        public override void Setup(ref TargetSetupContext context)
        {
            // Setup the Target
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            
            // Override EditorGUI
            if(!string.IsNullOrEmpty(m_CustomEditorGUI))
            {
                context.SetDefaultShaderGUI(m_CustomEditorGUI);
            }

            // Setup the active SubTarget
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
            m_ActiveSubTarget.value.target = this;
            m_ActiveSubTarget.value.Setup(ref context);
        }

        public override void OnAfterMultiDeserialize(string json)
        {
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
            m_ActiveSubTarget.value.target = this;

            // OnAfterMultiDeserialize order is not guaranteed to be hierarchical (target->subtarget).
            // Update active subTarget (only, since the target is shared and non-active subTargets could override active settings)
            // after Target has been deserialized and target <-> subtarget references are intact.
            m_ActiveSubTarget.value.OnAfterParentTargetDeserialized();
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            var descs = context.blocks.Select(x => x.descriptor);
            // Core fields
            context.AddField(Fields.GraphVertex,            descs.Contains(BlockFields.VertexDescription.Position) ||
                                                            descs.Contains(BlockFields.VertexDescription.Normal) ||
                                                            descs.Contains(BlockFields.VertexDescription.Tangent));
            context.AddField(Fields.GraphPixel);
            
            context.AddField(SGEFields.BlendModeOff, blendMode == BlendMode.Off);
            context.AddField(SGEFields.BlendModeAdditive, blendMode == BlendMode.Additive);
            context.AddField(SGEFields.BlendModeAlpha, blendMode == BlendMode.Alpha);
            context.AddField(SGEFields.BlendModeMultiply, blendMode == BlendMode.Multiply);
            context.AddField(SGEFields.BlendModePremultiply, blendMode == BlendMode.Premultiply);
            
            context.AddField(SGEFields.AlphaClip, alphaClip);

            context.AddField(SGEFields.CullModeOff, cullMode == CullMode.Off);
            context.AddField(SGEFields.CullModeFront, cullMode == CullMode.Front);
            context.AddField(SGEFields.CullModeBack, cullMode == CullMode.Back);
            
            context.AddField(SGEFields.ZWrite, zWrite == ZWrite.On);

            context.AddField(SGEFields.ZTestLess, zTest == ZTest.Less);
            context.AddField(SGEFields.ZTestGreater, zTest == ZTest.Greater);
            context.AddField(SGEFields.ZTestLEqual, zTest == ZTest.LEqual);
            context.AddField(SGEFields.ZTestGEqual, zTest == ZTest.GEqual);
            context.AddField(SGEFields.ZTestEqual, zTest == ZTest.Equal);
            context.AddField(SGEFields.ZTestNotEqual, zTest == ZTest.NotEqual);
            context.AddField(SGEFields.ZTestAlways, zTest == ZTest.Always);

            bool opaque = blendMode == BlendMode.Off;
            context.AddField(UniversalFields.SurfaceOpaque, opaque);
            context.AddField(UniversalFields.SurfaceTransparent,  !opaque);

            // SubTarget fields
            m_ActiveSubTarget.value.GetFields(ref context);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Core blocks
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha, alphaClip || blendMode != BlendMode.Off);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, alphaClip);

            // SubTarget blocks
            m_ActiveSubTarget.value.GetActiveBlocks(ref context);
        }
        
        public override void ProcessPreviewMaterial(Material material)
        {
            m_ActiveSubTarget.value.ProcessPreviewMaterial(material);
        }
        
        public override object saveContext => m_ActiveSubTarget.value?.saveContext;

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);
            activeSubTarget.CollectShaderProperties(collector, generationMode);

            collector.AddShaderProperty(LightmappingShaderProperties.kLightmapsArray);
            collector.AddShaderProperty(LightmappingShaderProperties.kLightmapsIndirectionArray);
            collector.AddShaderProperty(LightmappingShaderProperties.kShadowMasksArray);

            // SubTarget blocks
            m_ActiveSubTarget.value.CollectShaderProperties(collector, generationMode);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            // Core properties
            m_SubTargetField = new PopupField<string>(m_SubTargetNames, activeSubTargetIndex);
            context.AddProperty("Material", m_SubTargetField, (evt) =>
            {
                if (Equals(activeSubTargetIndex, m_SubTargetField.index))
                    return;

                registerUndo("Change Material");
                m_ActiveSubTarget = m_SubTargets[m_SubTargetField.index];
                onChange();
            });

            context.AddProperty("Render Type", new EnumField(RenderType.Opaque) { value = renderType }, (evt) =>
            {
                if (Equals(renderType, evt.newValue))
                    return;

                registerUndo("Change Render Type");
                renderType = (RenderType) evt.newValue;
                onChange();
            });
            
            context.AddProperty("Render Queue", new EnumField(RenderQueue.Background) { value = renderQueue }, (evt) =>
            {
                if (Equals(renderQueue, evt.newValue))
                    return;

                registerUndo("Change Render Queue");
                renderQueue = (RenderQueue) evt.newValue;
                onChange();
            });
            
            context.AddProperty("Blend", new EnumField(BlendMode.Off) { value = blendMode }, (evt) =>
            {
                if (Equals(blendMode, evt.newValue))
                    return;

                registerUndo("Change Blend");
                blendMode = (BlendMode) evt.newValue;
                onChange();
            });
            
            context.AddProperty("Alpha Clip", new Toggle() { value = alphaClip }, (evt) =>
            {
                if (Equals(alphaClip, evt.newValue))
                    return;

                registerUndo("Change Alpha Clip");
                alphaClip = evt.newValue;
                onChange();
            });
            
            context.AddProperty("Cast Shadows", new Toggle() { value = castShadows }, (evt) =>
            {
                if (Equals(castShadows, evt.newValue))
                    return;

                registerUndo("Change Cast Shadows");
                castShadows = evt.newValue;
                onChange();
            });
            
            context.AddProperty("Receive Shadows", new Toggle() { value = receiveShadows }, (evt) =>
            {
                if (Equals(receiveShadows, evt.newValue))
                    return;

                registerUndo("Change Receive Shadows");
                receiveShadows = evt.newValue;
                onChange();
            });
            
            context.AddProperty("Supports LOD Cross Fade", new Toggle() { value = supportsLodCrossFade }, (evt) =>
            {
                if (Equals(supportsLodCrossFade, evt.newValue))
                    return;
                registerUndo("Change Supports LOD Cross Fade");
                supportsLodCrossFade = evt.newValue;
                onChange();
            });
            
            context.AddProperty("CullMode", new EnumField(CullMode.Front) { value = cullMode}, (evt) =>
            {
                if (Equals(cullMode, evt.newValue))
                    return;

                registerUndo("Change ZWrite");
                cullMode = (CullMode) evt.newValue;
                onChange();
            });

            context.AddProperty("ZWrite", new EnumField(ZWrite.On) { value = zWrite}, (evt) =>
            {
                if (Equals(zWrite, evt.newValue))
                    return;

                registerUndo("Change ZWrite");
                zWrite = (ZWrite) evt.newValue;
                onChange();
            });
            
            context.AddProperty("ZTest", new EnumField(ZTest.Less) { value = zTest}, (evt) =>
            {
                if (Equals(zTest, evt.newValue))
                    return;

                registerUndo("Change ZTest");
                zTest = (ZTest) evt.newValue;
                onChange();
            });

            // Custom Editor GUI
            // Requires FocusOutEvent
            m_CustomGUIField = new TextField("") { value = customEditorGUI };
            m_CustomGUIField.RegisterCallback<FocusOutEvent>(s =>
            {
                if (Equals(customEditorGUI, m_CustomGUIField.value))
                    return;

                registerUndo("Change Custom Editor GUI");
                customEditorGUI = m_CustomGUIField.value;
                onChange();
            });
            context.AddProperty("Custom Editor GUI", m_CustomGUIField, (evt) => {});
            
            // SubTarget properties
            m_ActiveSubTarget.value.GetPropertiesGUI(ref context, onChange, registerUndo);
            
#if HAS_VFX_GRAPH
            if (VFXViewPreference.generateOutputContextWithShaderGraph)
            {
                // VFX Support
                if (!(m_ActiveSubTarget.value is UniversalSubTarget))
                    context.AddHelpBox(MessageType.Info, $"The {m_ActiveSubTarget.value.displayName} target does not support VFX Graph.");
                else
                {
                    m_SupportVFXToggle = new Toggle("") { value = m_SupportVFX };
                    context.AddProperty("Support VFX Graph", m_SupportVFXToggle, (evt) =>
                    {
                        m_SupportVFX = m_SupportVFXToggle.value;
                    });
                }
            }
#endif
        }

        public bool TrySetActiveSubTarget(Type subTargetType)
        {
            if(!subTargetType.IsSubclassOf(typeof(SubTarget)))
                return false;

            foreach(var subTarget in m_SubTargets)
            {
                if(subTarget.GetType().Equals(subTargetType))
                {
                    m_ActiveSubTarget = subTarget;
                    return true;
                }
            }

            return false;
        }

        private static CullMode UpgradeCullMode(ShaderGraphEssentials.Legacy.CullMode oldCullMode)
        {
            switch (oldCullMode)
            {
                case Legacy.CullMode.Back:
                    return CullMode.Back;
                case Legacy.CullMode.Front:
                    return CullMode.Front;
                case Legacy.CullMode.Off:
                    return CullMode.Off;
                default:
                    throw new ArgumentOutOfRangeException(nameof(oldCullMode), oldCullMode, null);
            }
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            void UpgradeAlphaClip(int clipId)
            {
                var clipThresholdId = clipId;
                var node = masterNode as AbstractMaterialNode;
                var clipThresholdSlot = node.FindSlot<Vector1MaterialSlot>(clipThresholdId);
                if(clipThresholdSlot == null)
                    return;

                clipThresholdSlot.owner = node;
                if(clipThresholdSlot.isConnected || clipThresholdSlot.value > 0.0f)
                {
                    m_AlphaClip = true;
                }
            }
            
            // Upgrade Target
            switch(masterNode)
            {
                case SGEUnlitMasterNode1 unlitMasterNode:
                    m_RenderType = unlitMasterNode.m_renderType;
                    m_RenderQueue = unlitMasterNode.m_renderQueue;
                    m_BlendMode = (BlendMode) unlitMasterNode.m_blendMode;
                    m_CullMode = UpgradeCullMode(unlitMasterNode.m_cullMode);
                    m_ZWrite = unlitMasterNode.m_zwrite;
                    m_ZTest = unlitMasterNode.m_ztest;
                    m_CustomEditorGUI = unlitMasterNode.m_customEditor;
                    UpgradeAlphaClip(8);
                    break;
                case SGESimpleLitMasterNode1 simpleLitMasterNode:
                    m_RenderType = simpleLitMasterNode.m_renderType;
                    m_RenderQueue = simpleLitMasterNode.m_renderQueue;
                    m_BlendMode = (BlendMode) simpleLitMasterNode.m_blendMode;
                    m_CullMode = UpgradeCullMode(simpleLitMasterNode.m_cullMode);
                    m_ZWrite = simpleLitMasterNode.m_zwrite;
                    m_ZTest = simpleLitMasterNode.m_ztest;
                    m_CustomEditorGUI = simpleLitMasterNode.m_customEditor;
                    UpgradeAlphaClip(7);
                    break;
                case SGECustomLitMasterNode1 customLitMasterNode:
                    m_RenderType = customLitMasterNode.m_renderType;
                    m_RenderQueue = customLitMasterNode.m_renderQueue;
                    m_BlendMode = (BlendMode) customLitMasterNode.m_blendMode;
                    m_CullMode = UpgradeCullMode(customLitMasterNode.m_cullMode);
                    m_ZWrite = customLitMasterNode.m_zwrite;
                    m_ZTest = customLitMasterNode.m_ztest;
                    m_CustomEditorGUI = customLitMasterNode.m_customEditor;
                    UpgradeAlphaClip(7);
                    break;
            }

            // Upgrade SubTarget
            foreach(var subTarget in m_SubTargets)
            {
                if(!(subTarget is ILegacyTarget legacySubTarget))
                    continue;

                if(legacySubTarget.TryUpgradeFromMasterNode(masterNode, out blockMap))
                {
                    m_ActiveSubTarget = subTarget;
                    return true;
                }
            }

            blockMap = null;
            return false;
        }

        public override bool WorksWithSRP(RenderPipelineAsset scriptableRenderPipeline)
        {
            return scriptableRenderPipeline?.GetType() == typeof(UniversalRenderPipelineAsset);
        }
        
#if HAS_VFX_GRAPH
        public void ConfigureContextData(VFXContext context, VFXContextCompiledData data)
        {
            if (!(m_ActiveSubTarget.value is IRequireVFXContext vfxSubtarget))
                return;

            vfxSubtarget.ConfigureContextData(context, data);
        }

#endif

        public bool CanSupportVFX()
        {
            if (m_ActiveSubTarget.value == null)
                return false;

            if (m_ActiveSubTarget.value is UniversalUnlitSubTarget)
                return true;

            if (m_ActiveSubTarget.value is UniversalLitSubTarget)
                return true;

            //It excludes:
            // - UniversalDecalSubTarget
            // - UniversalSpriteLitSubTarget
            // - UniversalSpriteUnlitSubTarget
            // - UniversalSpriteCustomLitSubTarget
            return false;
        }

        public bool SupportsVFX()
        {
#if HAS_VFX_GRAPH
            if (!CanSupportVFX())
                return false;

            return m_SupportVFX;
#else
            return false;
#endif
        }
        
        [Serializable]
        class SGEUniversalTargetLegacySerialization
        {
        }

        public override void OnAfterDeserialize(string json)
        {
            base.OnAfterDeserialize(json);

            if (this.sgVersion < latestVersion)
            {
                if (this.sgVersion == 0)
                {
                    // deserialize the old settings to upgrade
                    var oldSettings = JsonUtility.FromJson<SGEUniversalTargetLegacySerialization>(json);
                }
                ChangeVersion(latestVersion);
            }
        }

        #region Metadata
        string IHasMetadata.identifier
        {
            get
            {
                // defer to subtarget
                if (m_ActiveSubTarget.value is IHasMetadata subTargetHasMetaData)
                    return subTargetHasMetaData.identifier;
                return null;
            }
        }

        ScriptableObject IHasMetadata.GetMetadataObject(GraphDataReadOnly graph)
        {
            // defer to subtarget
            if (m_ActiveSubTarget.value is IHasMetadata subTargetHasMetaData)
                return subTargetHasMetaData.GetMetadataObject(graph);
            return null;
        }

        #endregion

    }

#region Passes
    static class CorePasses
    {
        /// <summary>
        ///  Automatically enables Alpha-To-Coverage in the provided opaque pass targets using alpha clipping
        /// </summary>
        /// <param name="pass">The pass to modify</param>
        /// <param name="target">The target to query</param>
        internal static void AddAlphaToMaskControlToPass(ref PassDescriptor pass, SGEUniversalTarget target)
        { 
            if (target.alphaClip && (target.blendMode == BlendMode.Off))
            {
                pass.renderStates.Add(RenderState.AlphaToMask("On"));
            }
        }

        internal static void AddAlphaClipControlToPass(ref PassDescriptor pass, SGEUniversalTarget target)
        {
            if (target.alphaClip)
                pass.defines.Add(CoreKeywordDescriptors.AlphaTestOn, 1);
        }
        
        internal static void AddLODCrossFadeControlToPass(ref PassDescriptor pass, SGEUniversalTarget target)
        {
            if (target.supportsLodCrossFade)
            {
                pass.includes.Add(CoreIncludes.LODCrossFade);
                pass.keywords.Add(CoreKeywordDescriptors.LODFadeCrossFade);
                pass.defines.Add(CoreKeywordDescriptors.UseUnityCrossFade, 1);
            }
        }

        internal static void AddTargetSurfaceControlsToPass(ref PassDescriptor pass, SGEUniversalTarget target, bool blendModePreserveSpecular = false)
        {
            // setup target control via define
            bool transparent = target.blendMode != BlendMode.Off;
            if (transparent)
            {
                pass.defines.Add(CoreKeywordDescriptors.SurfaceTypeTransparent, 1);

                // alpha premultiply in shader only needed when alpha is different for diffuse & specular
                if ((target.blendMode == BlendMode.Alpha || target.blendMode == BlendMode.Additive) && blendModePreserveSpecular)
                    pass.defines.Add(CoreKeywordDescriptors.AlphaPremultiplyOn, 1);
                else if (target.blendMode == BlendMode.Multiply)
                    pass.defines.Add(CoreKeywordDescriptors.AlphaModulateOn, 1);
            }

            AddAlphaClipControlToPass(ref pass, target);
        }

        // used by lit/unlit subtargets
        public static PassDescriptor DepthOnly(SGEUniversalTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "DepthOnly",
                referenceName = "SHADERPASS_DEPTHONLY",
                lightMode = "DepthOnly",
                useInPreview = true,

                // Template
                passTemplatePath = UniversalTarget.kUberTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.DepthOnly(target),
                pragmas = CorePragmas.Instanced,
                defines = new DefineCollection(),
                keywords = new KeywordCollection(),
                includes = new IncludeCollection { CoreIncludes.DepthOnly },

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);
            AddLODCrossFadeControlToPass(ref result, target);

            return result;
        }

        // used by lit/unlit subtargets
        public static PassDescriptor DepthNormal(SGEUniversalTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "DepthNormalsOnly",
                referenceName = "SHADERPASS_DEPTHNORMALS",
                lightMode = "DepthNormalsOnly",
                useInPreview = false,

                // Template
                passTemplatePath = UniversalTarget.kUberTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentDepthNormals,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.DepthNormals,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.DepthNormalsOnly(target),
                pragmas = CorePragmas.Instanced,
                defines = new DefineCollection(),
                keywords = new KeywordCollection(),
                includes = new IncludeCollection { CoreIncludes.DepthNormalsOnly },

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);
            AddLODCrossFadeControlToPass(ref result, target);

            return result;
        }
        
        // used by lit/unlit subtargets
        public static PassDescriptor DepthNormalOnly(SGEUniversalTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "DepthNormalsOnly",
                referenceName = "SHADERPASS_DEPTHNORMALSONLY",
                lightMode = "DepthNormalsOnly",
                useInPreview = false,

                // Template
                passTemplatePath = UniversalTarget.kUberTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentDepthNormals,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.DepthNormals,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.DepthNormalsOnly(target),
                pragmas = CorePragmas.Instanced,
                defines = new DefineCollection(),
                keywords = new KeywordCollection { CoreKeywordDescriptors.GBufferNormalsOct },
                includes = new IncludeCollection { CoreIncludes.DepthNormalsOnly },

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);
            AddLODCrossFadeControlToPass(ref result, target);

            return result;
        }

        
        // used by lit/unlit targets
        public static PassDescriptor ShadowCaster(SGEUniversalTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWCASTER",
                lightMode = "ShadowCaster",

                // Template
                passTemplatePath = UniversalTarget.kUberTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.ShadowCaster,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.ShadowCaster(target),
                pragmas = CorePragmas.Instanced,
                defines = new DefineCollection(),
                keywords = new KeywordCollection { CoreKeywords.ShadowCaster },
                includes = new IncludeCollection { CoreIncludes.ShadowCaster },

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);
            AddLODCrossFadeControlToPass(ref result, target);

            return result;
        }
        
        public static PassDescriptor SceneSelection(SGEUniversalTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTHONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Template
                passTemplatePath = UniversalTarget.kUberTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.SceneSelection(target),
                pragmas = CorePragmas.Instanced,
                defines = new DefineCollection { CoreDefines.SceneSelection, { CoreKeywordDescriptors.AlphaClipThreshold, 1 } },
                keywords = new KeywordCollection(),
                includes = CoreIncludes.SceneSelection,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);

            return result;
        }
        
        public static PassDescriptor ScenePicking(SGEUniversalTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "ScenePickingPass",
                referenceName = "SHADERPASS_DEPTHONLY",
                lightMode = "Picking",
                useInPreview = false,

                // Template
                passTemplatePath = UniversalTarget.kUberTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.ScenePicking(target),
                pragmas = CorePragmas.Instanced,
                defines = new DefineCollection { CoreDefines.ScenePicking, { CoreKeywordDescriptors.AlphaClipThreshold, 1 } },
                keywords = new KeywordCollection(),
                includes = CoreIncludes.ScenePicking,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);

            return result;
        }

        public static PassDescriptor _2DSceneSelection(SGEUniversalTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTHONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Template
                passTemplatePath = UniversalTarget.kUberTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.SceneSelection(target),
                pragmas = CorePragmas._2DDefault,
                defines = new DefineCollection { CoreDefines.SceneSelection, { CoreKeywordDescriptors.AlphaClipThreshold, 0 } },
                keywords = new KeywordCollection(),
                includes = CoreIncludes.ScenePicking,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);
            AddLODCrossFadeControlToPass(ref result, target);

            return result;
        }

        public static PassDescriptor _2DScenePicking(SGEUniversalTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "ScenePickingPass",
                referenceName = "SHADERPASS_DEPTHONLY",
                lightMode = "Picking",
                useInPreview = false,

                // Template
                passTemplatePath = UniversalTarget.kUberTemplatePath,
                sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.ScenePicking(target),
                pragmas = CorePragmas._2DDefault,
                defines = new DefineCollection { CoreDefines.ScenePicking, { CoreKeywordDescriptors.AlphaClipThreshold, 0 } },
                keywords = new KeywordCollection(),
                includes = CoreIncludes.SceneSelection,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddAlphaClipControlToPass(ref result, target);
            AddLODCrossFadeControlToPass(ref result, target);

            return result;
        }
    }
#endregion

#region PortMasks
    class CoreBlockMasks
    {
        public static readonly BlockFieldDescriptor[] Vertex = new BlockFieldDescriptor[]
        {
            BlockFields.VertexDescription.Position,
            BlockFields.VertexDescription.Normal,
            BlockFields.VertexDescription.Tangent,
        };

        public static readonly BlockFieldDescriptor[] FragmentAlphaOnly = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
        };

        public static readonly BlockFieldDescriptor[] FragmentColorAlpha = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.BaseColor,
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
        };
        
        public static readonly BlockFieldDescriptor[] FragmentDepthNormals = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.NormalOS,
            BlockFields.SurfaceDescription.NormalTS,
            BlockFields.SurfaceDescription.NormalWS,
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
        };
    }
#endregion

#region StructCollections
    static class CoreStructCollections
    {
        public static readonly StructCollection Default = new StructCollection
        {
            { Structs.Attributes },
            { UniversalStructs.Varyings },
            { Structs.SurfaceDescriptionInputs },
            { Structs.VertexDescriptionInputs },
        };
    }
#endregion

#region RequiredFields
    static class CoreRequiredFields
    {
        public static readonly FieldCollection ShadowCaster = new FieldCollection()
        {
            StructFields.Varyings.normalWS,
        };
        public static readonly FieldCollection DepthNormals = new FieldCollection()
        {
            StructFields.Attributes.uv1,                            // needed for meta vertex position
            StructFields.Varyings.normalWS,
            StructFields.Varyings.tangentWS,                        // needed for vertex lighting
        };
    }
#endregion

#region FieldDependencies
    static class CoreFieldDependencies
    {
        public static readonly DependencyCollection Default = new DependencyCollection()
        {
            { FieldDependencies.Default },
            new FieldDependency(UniversalStructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,    StructFields.Attributes.instanceID ),
            new FieldDependency(UniversalStructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,     StructFields.Attributes.instanceID ),
        };
    }
#endregion

#region RenderStates
    static class CoreRenderStates
    {
        public static readonly RenderStateCollection Default = new RenderStateCollection
        {
            // TODO render type
            // TODO render queue
            { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(SGEFields.BlendModeOff, true) },
            { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(SGEFields.BlendModeAlpha, true) },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(SGEFields.BlendModePremultiply, true) },
            { RenderState.Blend(Blend.SrcAlpha, Blend.One, Blend.One, Blend.One), new FieldCondition(SGEFields.BlendModeAdditive, true) },
            { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(SGEFields.BlendModeMultiply, true) },
            
            { RenderState.Cull(Cull.Off), new FieldCondition(SGEFields.CullModeOff, true) },
            { RenderState.Cull(Cull.Front), new FieldCondition(SGEFields.CullModeFront, true) },
            { RenderState.Cull(Cull.Back), new FieldCondition(SGEFields.CullModeBack, true) },
            
            { RenderState.ZWrite(ZWrite.On), new FieldCondition(SGEFields.ZWrite, true) },
            { RenderState.ZWrite(ZWrite.Off), new FieldCondition(SGEFields.ZWrite, false) },
            
            { RenderState.ZTest(ZTest.Less), new FieldCondition(SGEFields.ZTestLess, true) },
            { RenderState.ZTest(ZTest.Greater), new FieldCondition(SGEFields.ZTestGreater, true) },
            { RenderState.ZTest(ZTest.LEqual), new FieldCondition(SGEFields.ZTestLEqual, true) },
            { RenderState.ZTest(ZTest.GEqual), new FieldCondition(SGEFields.ZTestGEqual, true) },
            { RenderState.ZTest(ZTest.Equal), new FieldCondition(SGEFields.ZTestEqual, true) },
            { RenderState.ZTest(ZTest.NotEqual), new FieldCondition(SGEFields.ZTestNotEqual, true) },
            { RenderState.ZTest(ZTest.Always), new FieldCondition(SGEFields.ZTestAlways, true) },
        };
        
        // used by lit/unlit subtargets
        public static RenderStateCollection UberSwitchedRenderState(SGEUniversalTarget target, bool blendModePreserveSpecular = false)
        {
            var result = new RenderStateCollection();

            result.Add(RenderState.ZTest(target.zTest));
            result.Add(RenderState.ZWrite(target.zWrite));

            result.Add(RenderState.Cull(target.cullMode.ToString()));
            
            // Lift alpha multiply from ROP to shader in preserve spec for different diffuse and specular blends.
            Blend blendSrcRGB = blendModePreserveSpecular ? Blend.One : Blend.SrcAlpha;
            
            switch (target.blendMode)
            {
                case BlendMode.Alpha:
                    result.Add(RenderState.Blend(blendSrcRGB, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                    break;
                case BlendMode.Premultiply:
                    result.Add(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                    break;
                case BlendMode.Additive:
                    result.Add(RenderState.Blend(blendSrcRGB, Blend.One, Blend.One, Blend.One));
                    break;
                case BlendMode.Multiply:
                    result.Add(RenderState.Blend(Blend.DstColor, Blend.Zero, Blend.Zero, Blend.One)); // Multiply RGB only, keep A
                    break;
                }

            return result;
        }
        
        public static readonly RenderStateCollection Meta = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off) },
        };
        
        public static RenderStateDescriptor UberSwitchedCullRenderState(SGEUniversalTarget target)
        {
            return RenderState.Cull(target.cullMode.ToString());
        }

        // used by lit/unlit targets
        public static RenderStateCollection ShadowCaster(SGEUniversalTarget target)
        {
            var result = new RenderStateCollection
            {
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.On) },
                { UberSwitchedCullRenderState(target) },
                { RenderState.ColorMask("ColorMask 0") },
            };
            return result;
        }

        // used by lit/unlit targets
        public static RenderStateCollection DepthOnly(SGEUniversalTarget target)
        {
            var result = new RenderStateCollection
            {
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.On) },
                { UberSwitchedCullRenderState(target) },
                { RenderState.ColorMask("ColorMask 0") },
            };

            return result;
        }

        // used by lit target ONLY
        public static RenderStateCollection DepthNormalsOnly(SGEUniversalTarget target)
        {
            var result = new RenderStateCollection
            {
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.On) },
                { UberSwitchedCullRenderState(target) }
            };

            return result;
        }

        // Used by all targets
        public static RenderStateCollection SceneSelection(SGEUniversalTarget target)
        {
            var result = new RenderStateCollection
            {
                { RenderState.Cull(Cull.Off) },
            };

            return result;
        }

        public static RenderStateCollection ScenePicking(SGEUniversalTarget target)
        {
            var result = new RenderStateCollection
            {
                { UberSwitchedCullRenderState(target) }
            };

            return result;
        }
    }
#endregion

#region Pragmas
static class CorePragmas
    {
        public static readonly PragmaCollection Default = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection Instanced = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.MultiCompileInstancing },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection Forward = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection _2DDefault = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.ExcludeRenderers(new[]{ Platform.D3D9 }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection GBuffer = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.ExcludeRenderers(new[] { Platform.GLES, Platform.GLES3, Platform.GLCore }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };
    }
#endregion

#region Defines
static class CoreDefines
{
    public static readonly DefineCollection UseLegacySpriteBlocks = new DefineCollection
    {
        { CoreKeywordDescriptors.UseLegacySpriteBlocks, 1, new FieldCondition(CoreFields.UseLegacySpriteBlocks, true) },
    };

    public static readonly DefineCollection UseFragmentFog = new DefineCollection()
    {
        {CoreKeywordDescriptors.UseFragmentFog, 1},
    };
    
    public static readonly DefineCollection SceneSelection = new DefineCollection
    {
        { CoreKeywordDescriptors.SceneSelectionPass, 1 },
    };

    public static readonly DefineCollection ScenePicking = new DefineCollection
    {
        { CoreKeywordDescriptors.ScenePickingPass, 1 },
    };
}
#endregion

#region Includes
    static class CoreIncludes
    {
        const string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
        const string kTexture = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl";
        const string kCore = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl";
        const string kInput = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl";
        const string kLighting = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl";
        const string kGraphFunctions = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl";
        const string kVaryings = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl";
        const string kShaderPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl";
        const string kDepthOnlyPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl";
        internal const string kDepthNormalsOnlyPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl";
        const string kShadowCasterPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl";
        const string kTextureStack = "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl";
        const string kDBuffer = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl";
        const string kSelectionPickingPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl";
        const string kLODCrossFade = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl";
        
        // Files that are included with #include_with_pragmas
        const string kDOTS = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl";
        const string kRenderingLayers = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl";
        const string kProbeVolumes = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl";

        public static readonly IncludeCollection CorePregraph = new IncludeCollection
        {
            { kColor, IncludeLocation.Pregraph },
            { kTexture, IncludeLocation.Pregraph },
            { kCore, IncludeLocation.Pregraph },
            { kLighting, IncludeLocation.Pregraph },
            { kInput, IncludeLocation.Pregraph },
            { kTextureStack, IncludeLocation.Pregraph },        // TODO: put this on a conditional
        };
        
        public static readonly IncludeCollection DOTSPregraph = new IncludeCollection
        {
            { kDOTS, IncludeLocation.Pregraph, true },
        };

        public static readonly IncludeCollection WriteRenderLayersPregraph = new IncludeCollection
        {
            { kRenderingLayers, IncludeLocation.Pregraph, true },
        };


        public static readonly IncludeCollection ShaderGraphPregraph = new IncludeCollection
        {
            { kGraphFunctions, IncludeLocation.Pregraph },
        };

        public static readonly IncludeCollection CorePostgraph = new IncludeCollection
        {
            { kShaderPass, IncludeLocation.Pregraph  },
            { kVaryings, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection DepthOnly = new IncludeCollection
        {
            // Pre-graph
            { DOTSPregraph },
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kDepthOnlyPass, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection DepthNormalsOnly = new IncludeCollection
        {
            // Pre-graph
            { DOTSPregraph },
            { WriteRenderLayersPregraph },
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kDepthNormalsOnlyPass, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection ShadowCaster = new IncludeCollection
        {
            // Pre-graph
            { DOTSPregraph },
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kShadowCasterPass, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection DBufferPregraph = new IncludeCollection
        {
            { kDBuffer, IncludeLocation.Pregraph },
        };
        
        public static readonly IncludeCollection SceneSelection = new IncludeCollection
        {
            // Pre-graph
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kSelectionPickingPass, IncludeLocation.Postgraph },
        };

        public static readonly IncludeCollection ScenePicking = new IncludeCollection
        {
            // Pre-graph
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kSelectionPickingPass, IncludeLocation.Postgraph },
        };
        
        public static readonly IncludeCollection LODCrossFade = new IncludeCollection
        {
            { kLODCrossFade, IncludeLocation.Pregraph }
        };
    }
#endregion

#region KeywordDescriptors
static class CoreKeywordDescriptors
    {
        public static readonly KeywordDescriptor StaticLightmap = new KeywordDescriptor()
        {
            displayName = "Static Lightmap",
            referenceName = "LIGHTMAP_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };
        
        public static readonly KeywordDescriptor DynamicLightmap = new KeywordDescriptor()
        {
            displayName = "Dynamic Lightmap",
            referenceName = "DYNAMICLIGHTMAP_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor DirectionalLightmapCombined = new KeywordDescriptor()
        {
            displayName = "Directional Lightmap Combined",
            referenceName = "DIRLIGHTMAP_COMBINED",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor SampleGI = new KeywordDescriptor()
        {
            displayName = "Sample GI",
            referenceName = "_SAMPLE_GI",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };
        
        public static readonly KeywordDescriptor AlphaTestOn = new KeywordDescriptor()
        {
            displayName = ShaderKeywordStrings._ALPHATEST_ON,
            referenceName = ShaderKeywordStrings._ALPHATEST_ON,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };
        
        public static readonly KeywordDescriptor SurfaceTypeTransparent = new KeywordDescriptor()
        {
            displayName = ShaderKeywordStrings._SURFACE_TYPE_TRANSPARENT,
            referenceName = ShaderKeywordStrings._SURFACE_TYPE_TRANSPARENT,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global, // needs to match HDRP
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor AlphaPremultiplyOn = new KeywordDescriptor()
        {
            displayName = ShaderKeywordStrings._ALPHAPREMULTIPLY_ON,
            referenceName = ShaderKeywordStrings._ALPHAPREMULTIPLY_ON,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };
        
        public static readonly KeywordDescriptor AlphaModulateOn = new KeywordDescriptor()
        {
            displayName = ShaderKeywordStrings._ALPHAMODULATE_ON,
            referenceName = ShaderKeywordStrings._ALPHAMODULATE_ON,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor MainLightShadows = new KeywordDescriptor()
        {
            displayName = "Main Light Shadows",
            referenceName = "",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Off", referenceName = "" },
                new KeywordEntry() { displayName = "No Cascade", referenceName = "MAIN_LIGHT_SHADOWS" },
                new KeywordEntry() { displayName = "Cascade", referenceName = "MAIN_LIGHT_SHADOWS_CASCADE" },
                new KeywordEntry() { displayName = "Screen", referenceName = "MAIN_LIGHT_SHADOWS_SCREEN" },
            }
        };

        public static readonly KeywordDescriptor AdditionalLights = new KeywordDescriptor()
        {
            displayName = "Additional Lights",
            referenceName = "",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Off", referenceName = "" },
                new KeywordEntry() { displayName = "Vertex", referenceName = "ADDITIONAL_LIGHTS_VERTEX" },
                new KeywordEntry() { displayName = "Fragment", referenceName = "ADDITIONAL_LIGHTS" },
            },
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor AdditionalLightShadows = new KeywordDescriptor()
        {
            displayName = "Additional Light Shadows",
            referenceName = "_ADDITIONAL_LIGHT_SHADOWS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };
        
        public static readonly KeywordDescriptor ReflectionProbeBlending = new KeywordDescriptor()
        {
            displayName = "Reflection Probe Blending",
            referenceName = "_REFLECTION_PROBE_BLENDING",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor ReflectionProbeBoxProjection = new KeywordDescriptor()
        {
            displayName = "Reflection Probe Box Projection",
            referenceName = "_REFLECTION_PROBE_BOX_PROJECTION",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor ShadowsSoft = new KeywordDescriptor()
        {
            displayName = "Shadows Soft",
            referenceName = "_SHADOWS_SOFT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor MixedLightingSubtractive = new KeywordDescriptor()
        {
            displayName = "Mixed Lighting Subtractive",
            referenceName = "_MIXED_LIGHTING_SUBTRACTIVE",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor LightmapShadowMixing = new KeywordDescriptor()
        {
            displayName = "Lightmap Shadow Mixing",
            referenceName = "LIGHTMAP_SHADOW_MIXING",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor ShadowsShadowmask = new KeywordDescriptor()
        {
            displayName = "Shadows Shadowmask",
            referenceName = "SHADOWS_SHADOWMASK",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };
        
        public static readonly KeywordDescriptor LightLayers = new KeywordDescriptor()
        {
            displayName = "Light Layers",
            referenceName = "_LIGHT_LAYERS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };


        public static readonly KeywordDescriptor SmoothnessChannel = new KeywordDescriptor()
        {
            displayName = "Smoothness Channel",
            referenceName = "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor RenderPassEnabled = new KeywordDescriptor()
        {
            displayName = "Render Pass Enabled",
            referenceName = "_RENDER_PASS_ENABLED",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor ShapeLightType0 = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 0",
            referenceName = "USE_SHAPE_LIGHT_TYPE_0",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor ShapeLightType1 = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 1",
            referenceName = "USE_SHAPE_LIGHT_TYPE_1",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor ShapeLightType2 = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 2",
            referenceName = "USE_SHAPE_LIGHT_TYPE_2",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor ShapeLightType3 = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 3",
            referenceName = "USE_SHAPE_LIGHT_TYPE_3",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor UseLegacySpriteBlocks = new KeywordDescriptor()
        {
            displayName = "UseLegacySpriteBlocks",
            referenceName = "USELEGACYSPRITEBLOCKS",
            type = KeywordType.Boolean,
        };
        
        public static readonly KeywordDescriptor UseFragmentFog = new KeywordDescriptor()
        {
            displayName = "UseFragmentFog",
            referenceName = "_FOG_FRAGMENT",
            type = KeywordType.Boolean,
        };
        
        public static readonly KeywordDescriptor GBufferNormalsOct = new KeywordDescriptor()
        {
            displayName = "GBuffer normal octahedron encoding",
            referenceName = "_GBUFFER_NORMALS_OCT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor DBuffer = new KeywordDescriptor()
        {
            displayName = "Decals",
            referenceName = "",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Off", referenceName = "" },
                new KeywordEntry() { displayName = "DBuffer Mrt1", referenceName = "DBUFFER_MRT1" },
                new KeywordEntry() { displayName = "DBuffer Mrt2", referenceName = "DBUFFER_MRT2" },
                new KeywordEntry() { displayName = "DBuffer Mrt3", referenceName = "DBUFFER_MRT3" },
            },
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor DebugDisplay = new KeywordDescriptor()
        {
            displayName = "Debug Display",
            referenceName = "DEBUG_DISPLAY",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };
        
        public static readonly KeywordDescriptor FoveatedRendering = new KeywordDescriptor()
        {
            displayName = "Foveated Rendering Non Uniform Raster",
            referenceName = "_FOVEATED_RENDERING_NON_UNIFORM_RASTER",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        
        public static readonly KeywordDescriptor SceneSelectionPass = new KeywordDescriptor()
        {
            displayName = "Scene Selection Pass",
            referenceName = "SCENESELECTIONPASS",
            type = KeywordType.Boolean,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor ScenePickingPass = new KeywordDescriptor()
        {
            displayName = "Scene Picking Pass",
            referenceName = "SCENEPICKINGPASS",
            type = KeywordType.Boolean,
        };

        public static readonly KeywordDescriptor AlphaClipThreshold = new KeywordDescriptor()
        {
            displayName = "AlphaClipThreshold",
            referenceName = "ALPHA_CLIP_THRESHOLD",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.Predefined,
        };
        
        public static readonly KeywordDescriptor LightCookies = new KeywordDescriptor()
        {
            displayName = "Light Cookies",
            referenceName = "_LIGHT_COOKIES",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static readonly KeywordDescriptor ForwardPlus  = new KeywordDescriptor()
        {
            displayName = "Forward+",
            referenceName = "_FORWARD_PLUS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };
        
        public static readonly KeywordDescriptor EditorVisualization = new KeywordDescriptor()
        {
            displayName = "Editor Visualization",
            referenceName = "EDITOR_VISUALIZATION",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };
        
        public static readonly KeywordDescriptor UseUnityCrossFade = new KeywordDescriptor()
        {
            displayName = ShaderKeywordStrings.USE_UNITY_CROSSFADE,
            referenceName = ShaderKeywordStrings.USE_UNITY_CROSSFADE,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };
        
        public static readonly KeywordDescriptor LODFadeCrossFade = new KeywordDescriptor()
        {
            displayName = ShaderKeywordStrings.LOD_FADE_CROSSFADE,
            referenceName = ShaderKeywordStrings.LOD_FADE_CROSSFADE,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor ScreenSpaceAmbientOcclusion = new KeywordDescriptor()
        {
            displayName = "Screen Space Ambient Occlusion",
            referenceName = "_SCREEN_SPACE_OCCLUSION",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            stages = KeywordShaderStage.Fragment,
        };
    }
#endregion


#region CustomInterpolators
static class CoreCustomInterpDescriptors
{
    public static readonly CustomInterpSubGen.Collection Common = new CustomInterpSubGen.Collection
    {
        // Custom interpolators are not explicitly defined in the SurfaceDescriptionInputs template.
        // This entry point will let us generate a block of pass-through assignments for each field.
        CustomInterpSubGen.Descriptor.MakeBlock(CustomInterpSubGen.Splice.k_spliceCopyToSDI, "output", "input"),

        // sgci_PassThroughFunc is called from BuildVaryings in Varyings.hlsl to copy custom interpolators from vertex descriptions.
        // this entry point allows for the function to be defined before it is used.
        CustomInterpSubGen.Descriptor.MakeFunc(CustomInterpSubGen.Splice.k_splicePreSurface, "CustomInterpolatorPassThroughFunc", "Varyings", "VertexDescription", "CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC", "FEATURES_GRAPH_VERTEX")
    };
}
#endregion
}