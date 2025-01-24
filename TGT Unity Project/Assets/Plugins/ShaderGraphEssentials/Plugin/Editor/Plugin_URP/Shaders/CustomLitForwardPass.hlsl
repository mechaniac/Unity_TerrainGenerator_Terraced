void InitializeInputData(Varyings input, SurfaceDescription surfaceDescription, out InputData inputData)
{
    inputData = (InputData)0;
    
    inputData.positionWS = input.positionWS;

    #ifdef _NORMALMAP
        // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
        float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
        float3 bitangent = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

        inputData.tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
        
        #if _NORMAL_DROPOFF_TS
            inputData.normalWS = TransformTangentToWorld(surfaceDescription.NormalTS, inputData.tangentToWorld);
        #elif _NORMAL_DROPOFF_OS
            inputData.normalWS = TransformObjectToWorldNormal(surfaceDescription.NormalOS);
        #elif _NORMAL_DROPOFF_WS
            inputData.normalWS = surfaceDescription.NormalWS;
        #endif
    #else
        inputData.normalWS = input.normalWS;
    #endif
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        inputData.shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
        inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV.xy, input.sh, inputData.normalWS);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.sh, inputData.normalWS);
#endif
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
    
    #if defined(DEBUG_DISPLAY)
        #if defined(DYNAMICLIGHTMAP_ON)
            inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
        #endif
        #if defined(LIGHTMAP_ON)
            inputData.staticLightmapUV = input.staticLightmapUV;
        #else
            inputData.vertexSH = input.sh;
        #endif
    #endif
}

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 SGE_UniversalFragmentCustom(InputData inputData, SGECustomLightingData data)
{
    #if defined(DEBUG_DISPLAY)
        half4 debugColor;
    
        if (CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
        {
            return debugColor;
        }
    #endif

    uint meshRenderingLayers = GetMeshRenderingLayer();
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, aoFactor);
    
    half3 diffuseColor = inputData.bakedGI;
    half3 specularColor = half3(0, 0, 0);

#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        diffuseColor = inputData.bakedGI + SGE_DiffuseLightingCustom(mainLight, data);
        specularColor = SGE_SpecularLightingCustom(mainLight, inputData.viewDirectionWS, data);
    }

#ifdef _ADDITIONAL_LIGHTS
    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        {
            diffuseColor += SGE_DiffuseLightingCustom(light, data);
            specularColor += SGE_SpecularLightingCustom(light, inputData.viewDirectionWS, data);
        }
    LIGHT_LOOP_END
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    diffuseColor += inputData.vertexLighting;
#endif

    half3 finalColor = diffuseColor * data.albedo + data.emission;

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    finalColor += specularColor;
#endif

    return half4(finalColor, data.alpha);
}

void frag(
    PackedVaryings packedInput
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{    
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

#if defined(_SURFACE_TYPE_TRANSPARENT)
    bool isTransparent = true;
#else
    bool isTransparent = false;
#endif

#if defined(_ALPHATEST_ON)
    half alpha = AlphaDiscard(surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold);
#elif defined(_SURFACE_TYPE_TRANSPARENT)
    half alpha = surfaceDescription.Alpha;
#else
    half alpha = half(1.0);
#endif
    
    #if defined(LOD_FADE_CROSSFADE) && USE_UNITY_CROSSFADE
        LODFadeCrossFade(unpacked.positionCS);
    #endif

    InputData inputData;
    InitializeInputData(unpacked, surfaceDescription, inputData);
    // TODO: Mip debug modes would require this, open question how to do this on ShaderGraph.
    //SETUP_DEBUG_TEXTURE_DATA(inputData, unpacked.texCoord1.xy, _MainTex);
    
    half3 normalTS = half3(0, 0, 0);
    #if defined(_NORMALMAP) && defined(_NORMAL_DROPOFF_TS)
        normalTS = surfaceDescription.NormalTS;
    #endif

    
    // SGECustomLightingData remap
	SGECustomLightingData data;
	data.albedo = surfaceDescription.BaseColor;
	data.normal = inputData.normalWS;
	data.specular = surfaceDescription.Specular;
	data.glossiness = surfaceDescription.Glossiness;
	data.smoothness = surfaceDescription.Shininess * 128.0;
	data.emission = surfaceDescription.Emission;
	data.alpha = alpha;
	data.customLightingData1 = surfaceDescription.CustomLightingData1;
	data.customLightingData2 = surfaceDescription.CustomLightingData2;

	// custom lit style
	half4 color = SGE_UniversalFragmentCustom(inputData, data);

    color.rgb = MixFog(color.rgb, inputData.fogCoord); 
    
    color.a = OutputAlpha(color.a, isTransparent);

    outColor = color;

#ifdef _WRITE_RENDERING_LAYERS
    uint renderingLayers = GetMeshRenderingLayer();
    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
#endif
}
