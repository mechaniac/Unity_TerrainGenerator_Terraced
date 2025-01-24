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

using UnityEngine;
using UnityEditor.ShaderGraph;
using static Unity.Rendering.Universal.ShaderUtils;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.Rendering.Universal;
#if HAS_VFX_GRAPH
using UnityEditor.VFX;
#endif

namespace ShaderGraphEssentials
{
    abstract class SGEUniversalSubTarget : SubTarget<SGEUniversalTarget>, IHasMetadata
#if HAS_VFX_GRAPH
        , IRequireVFXContext
#endif
    {
        static readonly GUID kSourceCodeGuid = new GUID("41634f6f5f60b204ab48334329540b95");  // SGEUniversalSubTarget.cs

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
        }

        protected abstract SGEShaderUtils.SGEShaderID shaderID { get; }
        
#if HAS_VFX_GRAPH
        // VFX Properties
        VFXContext m_ContextVFX = null;
        VFXContextCompiledData m_ContextDataVFX;
        protected bool TargetsVFX() => m_ContextVFX != null;

        public void ConfigureContextData(VFXContext context, VFXContextCompiledData data)
        {
            m_ContextVFX = context;
            m_ContextDataVFX = data;
        }

#endif

        protected SubShaderDescriptor PostProcessSubShader(SubShaderDescriptor subShaderDescriptor)
        {
#if HAS_VFX_GRAPH
            if (TargetsVFX())
                return VFXSubTarget.PostProcessSubShader(subShaderDescriptor, m_ContextVFX, m_ContextDataVFX);
#endif
            return subShaderDescriptor;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
#if HAS_VFX_GRAPH
            if (TargetsVFX())
                VFXSubTarget.GetFields(ref context, m_ContextVFX);
#endif
        }


        public virtual string identifier => GetType().Name;
        public virtual ScriptableObject GetMetadataObject(GraphDataReadOnly graphData)
        {
            var urpMetadata = ScriptableObject.CreateInstance<UniversalMetadata>();
            urpMetadata.shaderID = ShaderID.Unknown;//shaderID; // TODO: verify where this is used
            urpMetadata.castShadows = target.castShadows;
            return urpMetadata;
        }

        private int lastMaterialNeedsUpdateHash = 0;
        protected virtual int ComputeMaterialNeedsUpdateHash() => 0;

        public override object saveContext
        {
            get
            {
                int hash = ComputeMaterialNeedsUpdateHash();
                bool needsUpdate = hash != lastMaterialNeedsUpdateHash;
                if (needsUpdate)
                    lastMaterialNeedsUpdateHash = hash;

                return new UniversalShaderGraphSaveContext { updateMaterials = needsUpdate };
            }
        }
    }

    internal static class SGESubShaderUtils
    {
        internal static void AddFloatProperty(this PropertyCollector collector, string referenceName, float defaultValue, HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Default,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = declarationType,
                value = defaultValue,
                displayName = referenceName,
                overrideReferenceName = referenceName,
            });
        }

        internal static void AddToggleProperty(this PropertyCollector collector, string referenceName, bool defaultValue, HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = defaultValue,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = declarationType,
                displayName = referenceName,
                overrideReferenceName = referenceName,
            });
        }

        // Overloads to do inline PassDescriptor modifications
        // NOTE: param order should match PassDescriptor field order for consistency
        #region PassVariant
        internal static PassDescriptor PassVariant(in PassDescriptor source, PragmaCollection pragmas)
        {
            var result = source;
            result.pragmas = pragmas;
            return result;
        }

        #endregion
    }
}