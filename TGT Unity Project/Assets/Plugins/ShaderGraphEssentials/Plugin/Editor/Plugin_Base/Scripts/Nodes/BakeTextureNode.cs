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
using System.Reflection;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;


namespace ShaderGraphEssentials
{
    class BakeTextureControlAttribute : Attribute, IControlAttribute
    {
        VisualElement IControlAttribute.InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (!(node is BakeTextureNode))
                throw new ArgumentException("Node must inherit from BakeTextureNode.", "node");
            return new BakeTextureControlView((BakeTextureNode)node);
        }
    }

    class BakeTextureControlView : VisualElement
    {
        BakeTextureNode m_Node;

        public BakeTextureControlView(BakeTextureNode node)
        {
            m_Node = node;
            Add(new Button(OnBakeTexture) { text = "Bake Texture" });
        }

        void OnBakeTexture()
        {
            m_Node.OnBakeTexture();
        }
    }

    [Title("Utility", "Bake Texture")]
    class BakeTextureNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return true; } }

        [SerializeField]
        private int m_width = 256;
        [IntegerControl("Width")]
        public int Width
        {
            get { return m_width; }
            set
            {
                if (m_width == value)
                    return;

                m_width = value;

                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        private int m_height = 256;
        [IntegerControl("Height")]
        public int Height
        {
            get { return m_height; }
            set
            {
                if (m_height == value)
                    return;

                m_height = value;

                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        private SerializableTexture m_outputTexture = new SerializableTexture();

        [TextureControl("Output texture")]
        public Texture OutputTexture
        {
            get { return m_outputTexture.texture; }
            set
            {
                if (m_outputTexture.texture == value)
                    return;
                m_outputTexture.texture = value;
                Dirty(ModificationScope.Node);
            }
        }

        [BakeTextureControl]
        int controlDummy { get; set; }

        public BakeTextureNode()
        {
            name = "Bake Texture";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("PreviewFunction", BindingFlags.Static | BindingFlags.NonPublic);
        }



        static string PreviewFunction(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = In;
}
";
        }

        internal void OnBakeTexture()
        {
            var graph = owner;
            var wasAsyncAllowed = ShaderUtil.allowAsyncCompilation;
            // we don't handle async compilation here, the user is waiting for it anyway
            ShaderUtil.allowAsyncCompilation = false;
            
            var generator = new Generator(owner, this, GenerationMode.Preview, $"hidden/preview/{this.GetVariableNameForNode()}", null);

            BakeShaderData shaderData = new BakeShaderData();
            shaderData.ShaderString = generator.generatedShader; // TODO maybe shader isn't generated yet?
            shaderData.Shader = ShaderUtil.CreateShaderAsset(shaderData.ShaderString);
            shaderData.Node = this;
            shaderData.Graph = graph;
            shaderData.HasError = false; // TODO handle shader errors
            shaderData.OutputIdName = "Out";

            BakeTextureManager.BakeShaderIntoTexture(shaderData);
            
            ShaderUtil.allowAsyncCompilation = wasAsyncAllowed;
        }
    }
}