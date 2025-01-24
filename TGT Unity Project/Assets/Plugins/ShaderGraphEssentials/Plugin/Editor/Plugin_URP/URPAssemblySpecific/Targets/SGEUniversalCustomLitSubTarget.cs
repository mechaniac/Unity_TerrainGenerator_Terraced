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
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.ShaderGraph.Serialization;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using static ShaderGraphEssentials.SGESubShaderUtils;
using static ShaderGraphEssentials.SGEShaderUtils;

namespace ShaderGraphEssentials
{
   sealed class SGEUniversalCustomLitSubTarget : SGEUniversalSubTarget, ILegacyTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("1586b3525f0fc80429dc737b7f7c754c"); // SGEUniversalCustomLitSubTarget.cs
        static readonly GUID kInternalSourceCodeGuid = new GUID("88bc0d7c5620cfd42b495c5b08e2a4c3"); // SGE_CustomLightingInternal.cs
        static readonly GUID kInternalFullSourceCodeGuid = new GUID("63cbeb35240376b4b8f71e4ccfc027a4"); // SGE_CustomLightingInternalFull.cs
        
        public override int latestVersion => 2;
        
        [SerializeField]
        private NormalDropOffSpace m_NormalDropOffSpace = NormalDropOffSpace.Tangent;
        
        [SerializeField]
        bool m_BlendModePreserveSpecular = true;

        public SGEUniversalCustomLitSubTarget()
        {
            displayName = "SGE CustomLit";
        }
        
        protected override SGEShaderID shaderID => SGEShaderID.SGE_CustomLit;
        
        public NormalDropOffSpace normalDropOffSpace
        {
            get => m_NormalDropOffSpace;
            set => m_NormalDropOffSpace = value;
        }
        
        public bool blendModePreserveSpecular
        {
            get => m_BlendModePreserveSpecular;
            set => m_BlendModePreserveSpecular = value;
        }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            context.AddAssetDependency(kInternalSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            context.AddAssetDependency(kInternalFullSourceCodeGuid, AssetCollection.Flags.SourceDependency);

            base.Setup(ref context);

            // Process SubShaders
            context.AddSubShader(PostProcessSubShader(SubShaders.LitSubShader(target, target.renderType.ToString(), target.renderQueue.ToString(), target.disableBatching.ToString(), blendModePreserveSpecular)));
        }
        
        public override void ProcessPreviewMaterial(Material material)
        {
            /*if (target.allowMaterialOverride)
            {
                // copy our target's default settings into the material
                // (technically not necessary since we are always recreating the material from the shader each time,
                // which will pull over the defaults from the shader definition)
                // but if that ever changes, this will ensure the defaults are set
                material.SetFloat(Property.SpecularWorkflowMode, (float)workflowMode);
                material.SetFloat(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                material.SetFloat(Property.ReceiveShadows, target.receiveShadows ? 1.0f : 0.0f);
                material.SetFloat(Property.SurfaceType, (float)target.surfaceType);
                material.SetFloat(Property.BlendMode, (float)target.alphaMode);
                material.SetFloat(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                material.SetFloat(Property.CullMode, (int)target.renderFace);
                material.SetFloat(Property.ZWriteControl, (float)target.zWriteControl);
                material.SetFloat(Property.ZTest, (float)target.zTestMode);
            }*/

            // call the full unlit material setup function
            //ShaderGraphLitGUI.UpdateMaterial(material, Unity.Rendering.Universal.ShaderUtils.MaterialUpdateType.CreatedNewMaterial);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);
            
            var descs = context.blocks.Select(x => x.descriptor);

            // Lit
            context.AddField(UniversalFields.NormalDropOffOS,     normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddField(UniversalFields.NormalDropOffTS,     normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddField(UniversalFields.NormalDropOffWS,     normalDropOffSpace == NormalDropOffSpace.World);
            context.AddField(UniversalFields.Normal,              descs.Contains(BlockFields.SurfaceDescription.NormalOS) ||
                                                                             descs.Contains(BlockFields.SurfaceDescription.NormalTS) ||
                                                                             descs.Contains(BlockFields.SurfaceDescription.NormalWS));

        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.NormalOS,           normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS,           normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS,           normalDropOffSpace == NormalDropOffSpace.World);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Specular);
            context.AddBlock(SGEFields.SurfaceDescription.Glossiness);
            context.AddBlock(SGEFields.SurfaceDescription.Shininess);
            context.AddBlock(SGEFields.SurfaceDescription.CustomLightingData1);
            context.AddBlock(SGEFields.SurfaceDescription.CustomLightingData2);
        }
        
        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // if using material control, add the material property to control workflow mode
            /*if (target.allowMaterialOverride)
            {
                collector.AddFloatProperty(Property.SpecularWorkflowMode, (float)workflowMode);
                collector.AddFloatProperty(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.ReceiveShadows, target.receiveShadows ? 1.0f : 0.0f);

                // setup properties using the defaults
                collector.AddFloatProperty(Property.SurfaceType, (float)target.surfaceType);
                collector.AddFloatProperty(Property.BlendMode, (float)target.alphaMode);
                collector.AddFloatProperty(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.SrcBlend, 1.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddFloatProperty(Property.DstBlend, 0.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddToggleProperty(Property.ZWrite, (target.surfaceType == SurfaceType.Opaque));
                collector.AddFloatProperty(Property.ZWriteControl, (float)target.zWriteControl);
                collector.AddFloatProperty(Property.ZTest, (float)target.zTestMode);    // ztest mode is designed to directly pass as ztest
                collector.AddFloatProperty(Property.CullMode, (float)target.renderFace);    // render face enum is designed to directly pass as a cull mode
                
                bool enableAlphaToMask = (target.alphaClip && (target.surfaceType == SurfaceType.Opaque));
                collector.AddFloatProperty(Property.AlphaToMask, enableAlphaToMask ? 1.0f : 0.0f);
            }*/
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            context.AddProperty("Fragment Normal Space", new EnumField(NormalDropOffSpace.Tangent) { value = normalDropOffSpace }, (evt) =>
            {
                if (Equals(normalDropOffSpace, evt.newValue))
                    return;

                registerUndo("Change Fragment Normal Space");
                normalDropOffSpace = (NormalDropOffSpace)evt.newValue;
                onChange();
            });
            
            if (target.blendMode != BlendMode.Off)
            {
                if (target.blendMode == BlendMode.Alpha || target.blendMode == BlendMode.Additive)
                    context.AddProperty("Preserve Specular Lighting", new Toggle() { value = blendModePreserveSpecular }, (evt) =>
                    {
                        if (Equals(blendModePreserveSpecular, evt.newValue))
                            return;

                        registerUndo("Change Preserve Specular");
                        blendModePreserveSpecular = evt.newValue;
                        onChange();
                    });
            }
        }
        
        protected override int ComputeMaterialNeedsUpdateHash()
        {
            int hash = base.ComputeMaterialNeedsUpdateHash();
            hash = hash * 23 /*+ target.allowMaterialOverride.GetHashCode()*/;
            return hash;
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if(!(masterNode is SGECustomLitMasterNode1 sgeCustomLitMasterNode))
                return false;
            
            m_NormalDropOffSpace = sgeCustomLitMasterNode.m_NormalDropOffSpace;

            // Handle mapping of Normal block specifically
            BlockFieldDescriptor normalBlock;
            switch(m_NormalDropOffSpace)
            {
                case NormalDropOffSpace.Object:
                    normalBlock = BlockFields.SurfaceDescription.NormalOS;
                    break;
                case NormalDropOffSpace.World:
                    normalBlock = BlockFields.SurfaceDescription.NormalWS;
                    break;
                default:
                    normalBlock = BlockFields.SurfaceDescription.NormalTS;
                    break;
            }

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>()
            {
                { BlockFields.VertexDescription.Position, 8 },
                { BlockFields.VertexDescription.Normal, 11 },
                { BlockFields.VertexDescription.Tangent, 12 },
                { BlockFields.SurfaceDescription.BaseColor, 0 },
                { BlockFields.SurfaceDescription.Specular, 1 },
                { SGEFields.SurfaceDescription.Shininess, 2 },
                { SGEFields.SurfaceDescription.Glossiness, 3 },
                { normalBlock, 4 },
                { BlockFields.SurfaceDescription.Emission, 5 },
                { BlockFields.SurfaceDescription.Alpha, 6 },
                { BlockFields.SurfaceDescription.AlphaClipThreshold, 7 },
                { SGEFields.SurfaceDescription.CustomLightingData1, 9 },
                { SGEFields.SurfaceDescription.CustomLightingData2, 10 },
            };

            return true;
        }
        
        internal override void OnAfterParentTargetDeserialized()
        {
            Assert.IsNotNull(target);

            if (this.sgVersion < latestVersion)
            {
                // Upgrade old incorrect Premultiplied blend into
                // equivalent Alpha + Preserve Specular blend mode.
                if (this.sgVersion < 1)
                {
                    if (target.blendMode == BlendMode.Premultiply)
                    {
                        target.blendMode= BlendMode.Alpha;
                        blendModePreserveSpecular = true;
                    }
                    else
                        blendModePreserveSpecular = false;
                }
                ChangeVersion(latestVersion);
            }
        }


#region SubShader
        static class SubShaders
        {
            // SM 4.5, compute with dots instancing
            public static SubShaderDescriptor LitSubShader(SGEUniversalTarget target, string renderType, string renderQueue, string disableBatchingTag, bool blendModePreserveSpecular)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = UniversalTarget.kLitMaterialTypeTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    disableBatchingTag = disableBatchingTag,
                    generatesPreview = true,
                    passes = new PassCollection()
                };
                
                result.passes.Add(LitPasses.Forward(target, blendModePreserveSpecular, CorePragmas.Forward, LitKeywords.Forward));
                
                result.passes.Add(LitPasses.GBuffer(target, blendModePreserveSpecular));

                // cull the shadowcaster pass if we know it will never be used
                if (target.castShadows)
                    result.passes.Add(PassVariant(CorePasses.ShadowCaster(target), CorePragmas.Instanced));

                if (target.zWrite == ZWrite.On)
                    result.passes.Add(PassVariant(CorePasses.DepthOnly(target), CorePragmas.Instanced));
                
                result.passes.Add(PassVariant(LitPasses.DepthNormal(target), CorePragmas.Instanced));
                
                result.passes.Add(PassVariant(LitPasses.Meta(target), CorePragmas.Default));

                // Currently neither of these passes (selection/picking) can be last for the game view for
                // UI shaders to render correctly. Verify [1352225] before changing this order.
                result.passes.Add(PassVariant(CorePasses.SceneSelection(target), CorePragmas.Default));
                result.passes.Add(PassVariant(CorePasses.ScenePicking(target), CorePragmas.Default));
                result.passes.Add(PassVariant(LitPasses._2D(target), CorePragmas.Default));

                return result;
            }
        }
#endregion

#region Passes
        static class LitPasses
        {
            static void AddReceiveShadowsControlToPass(ref PassDescriptor pass, bool receiveShadows)
            {
                if (!receiveShadows)
                    pass.defines.Add(LitKeywords.ReceiveShadowsOff, 1);
            }

            static void AddSimpleLitDefinesToPass(ref PassDescriptor pass)
            {
                pass.defines.Add(CustomLitDefines.CustomLit);
            }
            
            public static PassDescriptor Forward(
                SGEUniversalTarget target,
                bool blendModePreserveSpecular,
                PragmaCollection pragmas,
                KeywordCollection keywords)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "SGE SimpleLit Forward",
                    referenceName = "SHADERPASS_FORWARD",
                    lightMode = "UniversalForward",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target, blendModePreserveSpecular),
                    pragmas = pragmas ?? CorePragmas.Forward,     // NOTE: SM 2.0 only GL
                    defines = new DefineCollection { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection { keywords },
                    includes = new IncludeCollection { LitIncludes.Forward },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                AddSimpleLitDefinesToPass(ref result);
                CorePasses.AddTargetSurfaceControlsToPass(ref result, target, blendModePreserveSpecular);
                CorePasses.AddAlphaToMaskControlToPass(ref result, target);
                AddReceiveShadowsControlToPass(ref result, target.receiveShadows);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }
            
            public static PassDescriptor ForwardOnly(
                SGEUniversalTarget target,
                BlockFieldDescriptor[] vertexBlocks,
                BlockFieldDescriptor[] pixelBlocks,
                bool blendModePreserveSpecular,
                PragmaCollection pragmas,
                KeywordCollection keywords)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "SGE SimpleLit Forward Only",
                    referenceName = "SHADERPASS_FORWARDONLY",
                    lightMode = "UniversalForwardOnly",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = vertexBlocks,
                    validPixelBlocks = pixelBlocks,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target, blendModePreserveSpecular),
                    pragmas = pragmas,
                    defines = new DefineCollection { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection { keywords },
                    includes = new IncludeCollection { LitIncludes.Forward },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };
                
                AddSimpleLitDefinesToPass(ref result);
                CorePasses.AddTargetSurfaceControlsToPass(ref result, target, blendModePreserveSpecular);
                CorePasses.AddAlphaToMaskControlToPass(ref result, target);
                AddReceiveShadowsControlToPass(ref result, target.receiveShadows);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }
            
            // Deferred only in SM4.5, MRT not supported in GLES2
            public static PassDescriptor GBuffer(SGEUniversalTarget target, bool blendModePreserveSpecular)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "SGE SimpleLit GBuffer",
                    referenceName = "SHADERPASS_GBUFFER",
                    lightMode = "UniversalGBuffer",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.GBuffer,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target, blendModePreserveSpecular),
                    pragmas = CorePragmas.GBuffer,
                    defines = new DefineCollection { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection { LitKeywords.GBuffer },
                    includes = new IncludeCollection { LitIncludes.GBuffer },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };
                
                AddSimpleLitDefinesToPass(ref result);
                CorePasses.AddTargetSurfaceControlsToPass(ref result, target, blendModePreserveSpecular);
                AddReceiveShadowsControlToPass(ref result, target.receiveShadows);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor Meta(SGEUniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "SGE SimpleLit Meta",
                    referenceName = "SHADERPASS_META",
                    lightMode = "Meta",

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentMeta,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Meta,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.Meta,
                    pragmas = CorePragmas.Default,
                    defines = new DefineCollection() { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection() { CoreKeywordDescriptors.EditorVisualization },
                    includes = LitIncludes.Meta,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                AddSimpleLitDefinesToPass(ref result);
                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }
            
            public static PassDescriptor _2D(SGEUniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    referenceName = "SHADERPASS_2D",
                    lightMode = "Universal2D",

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentColorAlpha,

                    // Fields
                    structs = CoreStructCollections.Default,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection(),
                    includes = LitIncludes._2D,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                AddSimpleLitDefinesToPass(ref result);
                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor DepthNormal(SGEUniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "DepthNormals",
                    referenceName = "SHADERPASS_DEPTHNORMALS",
                    lightMode = "DepthNormals",
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

                CorePasses.AddAlphaClipControlToPass(ref result, target);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }
            
            
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
                    keywords = new KeywordCollection(),
                    includes = new IncludeCollection { CoreIncludes.DepthNormalsOnly },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }
        }
#endregion


#region PortMasks
        static class LitBlockMasks
        {
            public static readonly BlockFieldDescriptor[] FragmentLit = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalOS,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Specular,
                SGEFields.SurfaceDescription.Shininess,
                SGEFields.SurfaceDescription.Glossiness,
                SGEFields.SurfaceDescription.CustomLightingData1,
                SGEFields.SurfaceDescription.CustomLightingData2,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold
            };

            public static readonly BlockFieldDescriptor[] FragmentMeta = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };
        }
#endregion

#region RequiredFields
        static class LitRequiredFields
        {
            public static readonly FieldCollection Forward = new FieldCollection()
            {
                StructFields.Attributes.uv1,                            // needed for meta vertex position
                StructFields.Attributes.uv2,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,                        // needed for vertex lighting
                UniversalStructFields.Varyings.staticLightmapUV,
                UniversalStructFields.Varyings.dynamicLightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };

            public static readonly FieldCollection GBuffer = new FieldCollection()
            {
                StructFields.Attributes.uv1,                            // needed for meta vertex position
                StructFields.Attributes.uv2,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,                        // needed for vertex lighting
                UniversalStructFields.Varyings.staticLightmapUV,
                UniversalStructFields.Varyings.dynamicLightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };

            public static readonly FieldCollection Meta = new FieldCollection()
            {
                StructFields.Attributes.positionOS,
                StructFields.Attributes.normalOS,
                StructFields.Attributes.uv0,                            //
                StructFields.Attributes.uv1,                            // needed for meta vertex position
                StructFields.Attributes.uv2,                            // needed for meta UVs
                StructFields.Attributes.instanceID,                     // needed for rendering instanced terrain
                StructFields.Varyings.positionCS,
                StructFields.Varyings.texCoord0,                        // needed for meta UVs
                StructFields.Varyings.texCoord1,                        // VizUV
                StructFields.Varyings.texCoord2,                        // LightCoord
            };
        }
#endregion

#region Defines
static class CustomLitDefines
{
    public static readonly KeywordDescriptor SpecularColor = new KeywordDescriptor()
    {
        displayName = "Specular Color",
        referenceName = "_SPECULAR_COLOR 1",
        type = KeywordType.Boolean,
        definition = KeywordDefinition.ShaderFeature,
        scope = KeywordScope.Local,
        stages = KeywordShaderStage.Fragment,
    };

    public static readonly DefineCollection CustomLit = new DefineCollection()
    {
        {SpecularColor, 1},
    };
}
#endregion

#region Keywords
        static class LitKeywords
        {
            public static readonly KeywordDescriptor ReceiveShadowsOff = new KeywordDescriptor()
            {
                displayName = "Receive Shadows Off",
                referenceName = ShaderKeywordStrings._RECEIVE_SHADOWS_OFF,
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordCollection Forward = new KeywordCollection
            {
                { CoreKeywordDescriptors.ScreenSpaceAmbientOcclusion },
                { CoreKeywordDescriptors.StaticLightmap },
                { CoreKeywordDescriptors.DynamicLightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.AdditionalLights },
                { CoreKeywordDescriptors.AdditionalLightShadows },
                { CoreKeywordDescriptors.ReflectionProbeBlending },
                { CoreKeywordDescriptors.ReflectionProbeBoxProjection },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.ShadowsShadowmask },
                { CoreKeywordDescriptors.DBuffer },
                { CoreKeywordDescriptors.LightLayers },
                { CoreKeywordDescriptors.DebugDisplay },
                { CoreKeywordDescriptors.LightCookies },
                { CoreKeywordDescriptors.ForwardPlus },
            };

            public static readonly KeywordCollection GBuffer = new KeywordCollection
            {
                { CoreKeywordDescriptors.StaticLightmap },
                { CoreKeywordDescriptors.DynamicLightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.ReflectionProbeBlending },
                { CoreKeywordDescriptors.ReflectionProbeBoxProjection },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.ShadowsShadowmask },
                { CoreKeywordDescriptors.MixedLightingSubtractive },
                { CoreKeywordDescriptors.DBuffer },
                { CoreKeywordDescriptors.GBufferNormalsOct },
                { CoreKeywordDescriptors.RenderPassEnabled },
                { CoreKeywordDescriptors.DebugDisplay },
            };
        }
#endregion

#region Includes
        static class LitIncludes
        {
            const string kShadows = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl";
            const string kMetaInput = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl";
            const string kForwardPass = "Assets/Plugins/ShaderGraphEssentials/Plugin/Editor/Plugin_URP/Shaders/CustomLitForwardPass.hlsl";
            const string kCustomLitInternal = "Assets/Plugins/ShaderGraphEssentials/Plugin/Editor/Plugin_URP/Shaders/SGE_CustomLightingInternalFull.hlsl";
            const string kGBuffer = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl";
            const string kPBRGBufferPass = "Assets/Plugins/ShaderGraphEssentials/Plugin/Editor/Plugin_URP/Shaders/CustomLitGBufferPass.hlsl";
            const string kLightingMetaPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl";
            const string k2DPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl";

            public static readonly IncludeCollection Forward = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.DOTSPregraph },
                { CoreIncludes.WriteRenderLayersPregraph },
                { CoreIncludes.CorePregraph },
                { kCustomLitInternal, IncludeLocation.Pregraph }, 
                { kShadows, IncludeLocation.Pregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kForwardPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection GBuffer = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.DOTSPregraph },
                { CoreIncludes.WriteRenderLayersPregraph },
                { CoreIncludes.CorePregraph },
                { kShadows, IncludeLocation.Pregraph },
                { kCustomLitInternal, IncludeLocation.Pregraph }, 
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kGBuffer, IncludeLocation.Postgraph },
                { kPBRGBufferPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection Meta = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kCustomLitInternal, IncludeLocation.Pregraph }, 
                { kMetaInput, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kLightingMetaPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection _2D = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { kCustomLitInternal, IncludeLocation.Pregraph }, 
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { k2DPass, IncludeLocation.Postgraph },
            };
            
            public static readonly IncludeCollection DepthNormalsOnly = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { kCustomLitInternal, IncludeLocation.Pregraph }, 
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { CoreIncludes.kDepthNormalsOnlyPass, IncludeLocation.Postgraph },
            };
        }
#endregion
    }
}
