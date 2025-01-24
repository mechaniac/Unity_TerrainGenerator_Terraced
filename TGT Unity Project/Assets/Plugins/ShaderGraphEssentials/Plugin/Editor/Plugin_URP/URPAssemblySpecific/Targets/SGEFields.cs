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

using UnityEditor.ShaderGraph;
using UnityEngine;

namespace ShaderGraphEssentials
{
    static class SGEFields
    {
        public static FieldDescriptor BlendModeOff = new FieldDescriptor(string.Empty, "BlendModeOff", "_BLEND_MODE_OFF 1");
        public static FieldDescriptor BlendModeAlpha = new FieldDescriptor(string.Empty, "BlendModeAlpha", "_BLEND_MODE_ALPHA 1");
        public static FieldDescriptor BlendModePremultiply = new FieldDescriptor(string.Empty, "BlendModePremultiply", "_BLEND_MODE_PREMULTIPLY 1");
        public static FieldDescriptor BlendModeAdditive = new FieldDescriptor(string.Empty, "BlendModeAdditive", "_BLEND_MODE_ADDITIVE 1");
        public static FieldDescriptor BlendModeMultiply = new FieldDescriptor(string.Empty, "BlendModeMultiply", "_BLEND_MODE_MULTIPLY 1");

        public static FieldDescriptor CullModeOff = new FieldDescriptor(string.Empty, "CullModeOff", "_CULL_MODE_OFF 1");
        public static FieldDescriptor CullModeFront = new FieldDescriptor(string.Empty, "CullModeFront", "_CULL_MODE_FRONT 1");
        public static FieldDescriptor CullModeBack = new FieldDescriptor(string.Empty, "CullModeBack", "_CULL_MODE_BACK 1");

        public static FieldDescriptor ZWrite = new FieldDescriptor(string.Empty, "ZWrite", "_ZWRITE 1");

        public static FieldDescriptor ZTestLess = new FieldDescriptor(string.Empty, "ZTestLess", "_ZTEST_LESS 1");
        public static FieldDescriptor ZTestGreater = new FieldDescriptor(string.Empty, "ZTestGreater", "_ZTEST_GREATER 1");
        public static FieldDescriptor ZTestLEqual = new FieldDescriptor(string.Empty, "ZTestLEqual", "_ZTEST_LEQUAL 1");
        public static FieldDescriptor ZTestGEqual = new FieldDescriptor(string.Empty, "ZTestGEqual", "_ZTEST_GEQUAL 1");
        public static FieldDescriptor ZTestEqual = new FieldDescriptor(string.Empty, "ZTestEqual", "_ZTEST_EQUAL 1");
        public static FieldDescriptor ZTestNotEqual = new FieldDescriptor(string.Empty, "ZTestNotEqual", "_ZTEST_NOTEQUAL 1");
        public static FieldDescriptor ZTestAlways = new FieldDescriptor(string.Empty, "ZTestAlways", "_ZTEST_ALWAYS 1");
        
        public static FieldDescriptor AlphaClip = new FieldDescriptor(string.Empty, "AlphaClip", "_AlphaClip 1");
        
        [GenerateBlocks("SGE Universal")]
        public struct SurfaceDescription
        {
            private static string name = "SurfaceDescription";

            public static BlockFieldDescriptor Shininess = new BlockFieldDescriptor(SGEFields.SurfaceDescription.name, "Shininess", "Shininess", "SURFACEDESCRIPTION_SHININESS",
                new FloatControl(0.5f), ShaderStage.Fragment);

            public static BlockFieldDescriptor Glossiness = new BlockFieldDescriptor(SGEFields.SurfaceDescription.name, "Glossiness", "Glossiness", "SURFACEDESCRIPTION_GLOSSINESS",
                new FloatControl(0.5f), ShaderStage.Fragment);

            public static BlockFieldDescriptor CustomLightingData1 = new BlockFieldDescriptor(SGEFields.SurfaceDescription.name, "CustomLightingData1", "Custom Lighting Data 1",
                "SURFACEDESCRIPTION_CUSTOMLIGHTINGDATA1",
                new ColorRGBAControl(Vector4.zero), ShaderStage.Fragment);

            public static BlockFieldDescriptor CustomLightingData2 = new BlockFieldDescriptor(SGEFields.SurfaceDescription.name, "CustomLightingData2", "Custom Lighting Data 2",
                "SURFACEDESCRIPTION_CUSTOMLIGHTINGDATA2",
                new ColorRGBAControl(Vector4.zero), ShaderStage.Fragment);
        }
    }
}