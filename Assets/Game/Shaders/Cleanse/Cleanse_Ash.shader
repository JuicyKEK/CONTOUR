// Очищение в пепел: объект от точек сереет, трескается и осыпается кусками.
// Управляется _CleanseProgress (0 - объект целый, 1 - его нет). Источники и габариты задаёт скрипт по мешу объекта.
// Общий код - CleanseCommon.hlsl (эффекты) и CleansePasses.hlsl (проходы).
Shader "Custom/Cleanse/Ash"
{
    Properties
    {
        [Header(Surface)]
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.3
        _Metallic ("Metallic", Range(0, 1)) = 0
        [Toggle(_CLEANSE_TRIPLANAR)] _Triplanar ("Triplanar (mesh without UV)", Float) = 0
        _TileMeters ("Triplanar Tile, m", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull (Off shows the inside through holes)", Float) = 0

        [Header(Cleanse)]
        _CleanseProgress ("Progress (0 whole, 1 gone)", Range(0, 1)) = 0
        _BoundsMin ("Bounds Min (object space, set from the mesh)", Vector) = (-0.5, -0.5, -0.5, 0)
        _BoundsMax ("Bounds Max (object space, set from the mesh)", Vector) = (0.5, 0.5, 0.5, 0)
        _NoiseScale ("Noise Scale", Float) = 4
        _Source0 ("Source 1 (xyz 0..1 inside bounds, w delay 0..0.5)", Vector) = (0.75, 0.8, 0.5, 0)
        _Source1 ("Source 2", Vector) = (0.2, 0.45, 0.85, 0.12)
        _Source2 ("Source 3", Vector) = (0.6, 0.2, 0.15, 0.25)
        _Source3 ("Source 4", Vector) = (0.15, 0.85, 0.2, 0.4)
        _FrontNoise ("Edge Irregularity", Range(0, 1)) = 0.35
        _ArrivalMax ("Arrival Max (set by script from the mesh and sources, 0 = worst case)", Float) = 0

        [Header(Ash)]
        _AshColor ("Ash Color", Color) = (0.42, 0.4, 0.38, 1)
        _AshWidth ("Ash Band", Range(0.01, 0.5)) = 0.16
        _CrackScale ("Crack Pieces", Range(2, 40)) = 10
        _CrumbleWidth ("Crumble Band", Range(0.01, 0.5)) = 0.14
        _FallDistance ("Fall", Range(0, 0.3)) = 0.06
        [HDR] _GlowColor ("Crack Glow Color", Color) = (4, 1.2, 0.25, 1)
        _GlowIntensity ("Crack Glow Intensity", Range(0, 10)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" "Queue" = "AlphaTest" }

        HLSLINCLUDE
        #define CLEANSE_ASH 1
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Assets/Game/Shaders/Cleanse/CleanseInput.hlsl"
        #include "Assets/Game/Shaders/Cleanse/CleanseCommon.hlsl"
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _CLEANSE_TRIPLANAR
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #define CLEANSE_PASS_FORWARD
            #include "Assets/Game/Shaders/Cleanse/CleansePasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #define CLEANSE_PASS_SHADOW
            #include "Assets/Game/Shaders/Cleanse/CleansePasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #define CLEANSE_PASS_DEPTH
            #include "Assets/Game/Shaders/Cleanse/CleansePasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #define CLEANSE_PASS_DEPTHNORMALS
            #include "Assets/Game/Shaders/Cleanse/CleansePasses.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
