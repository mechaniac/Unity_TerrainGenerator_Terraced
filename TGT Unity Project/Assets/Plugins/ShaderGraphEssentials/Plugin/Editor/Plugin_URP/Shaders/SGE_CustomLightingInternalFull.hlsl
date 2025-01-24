#ifndef UNIVERSAL_LIGHTING_INCLUDED
#define UNIVERSAL_LIGHTING_INCLUDED
// necessary otherwise it'll throw en error on the node itself in the graph
// because the node HAS to compile by itself
// so without this, the master node is fine but this node itself won't compile because it doesn't know what "Light" is
struct Light
{
    half3   direction;
    half3   color;
    half    distanceAttenuation;
    half    shadowAttenuation;
    uint    layerMask;
};
// struct copied from "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"
#endif

// the only difference with this file and SGE_CustomLightingDataInternal.hlsl
// is that this one doens't #ifdef out the struct below.
// This file is included automatically in the graph using CustomLit
// whereas the SGE_CustomLightingDataInternal.hlsl is only there to get the user-defined custom function to compile.
#define SGE_CUSTOM_LIGHTING_INTERNAL_INCLUDED
struct SGECustomLightingData
{
	float3 albedo;
	float3 normal;
	float3 specular;
	float glossiness;
	float smoothness; // converted from shininess
	float3 emission;
	float alpha;
	float4 customLightingData1; // custom data you can use how you want (pass texture UV, vertex color ...etc)
	float4 customLightingData2;
};
