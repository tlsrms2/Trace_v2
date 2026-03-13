Shader "UI/FreezeBorderParticles"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _EdgeColor ("Edge Color", Color) = (0, 0.827451, 0.9490196, 1)
        _EdgeDirection ("Edge Direction", Vector) = (0, 1, 0, 0)
        _GradientSoftness ("Gradient Softness", Range(0.05, 1)) = 0.45
        _GlowStrength ("Glow Strength", Range(0, 4)) = 1.35
        _ParticleDensity ("Particle Density", Range(0, 4)) = 1.4
        _ParticleSpeed ("Particle Speed", Range(0, 5)) = 1.25
        _ParticleIntensity ("Particle Intensity", Range(0, 2)) = 0.8
        _FlowBlend ("Flow Blend", Range(0, 1)) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "FreezeBorderParticles"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            fixed4 _Color;
            fixed4 _EdgeColor;
            float4 _EdgeDirection;
            float _GradientSoftness;
            float _GlowStrength;
            float _ParticleDensity;
            float _ParticleSpeed;
            float _ParticleIntensity;
            float _FlowBlend;
            float4 _ClipRect;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPosition = v.vertex;
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise21(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                local = local * local * (3.0 - 2.0 * local);

                float a = Hash21(cell);
                float b = Hash21(cell + float2(1.0, 0.0));
                float c = Hash21(cell + float2(0.0, 1.0));
                float d = Hash21(cell + float2(1.0, 1.0));

                return lerp(lerp(a, b, local.x), lerp(c, d, local.x), local.y);
            }

            float FlowNoise(float2 uv)
            {
                float value = 0.0;
                float amplitude = 0.55;
                float frequency = 1.0;

                value += Noise21(uv * frequency) * amplitude;
                frequency *= 2.03;
                amplitude *= 0.5;
                value += Noise21(uv * frequency) * amplitude;
                frequency *= 2.07;
                amplitude *= 0.5;
                value += Noise21(uv * frequency) * amplitude;

                return value;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseColor = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                float2 dir = normalize(max(abs(_EdgeDirection.xy), 1e-5) * sign(_EdgeDirection.xy));
                float horizontalWeight = abs(dir.x);
                float verticalWeight = abs(dir.y);

                float edgeCoord =
                    horizontalWeight > 0.5
                    ? (dir.x > 0.0 ? 1.0 - i.texcoord.x : i.texcoord.x)
                    : (dir.y > 0.0 ? 1.0 - i.texcoord.y : i.texcoord.y);

                float flowCoord =
                    horizontalWeight > 0.5
                    ? i.texcoord.y
                    : i.texcoord.x;

                float softness = max(_GradientSoftness, 0.001);
                float time = _Time.y * _ParticleSpeed;
                float density = max(_ParticleDensity, 0.01);
                float contourWarp =
                    (FlowNoise(float2(flowCoord * (4.0 + density), time * 0.22 + edgeCoord * 2.1)) - 0.5) * 0.22;
                float edgeDepth = saturate(edgeCoord + contourWarp);
                float coreWidth = softness * 0.55;
                float fadeWidth = softness * 1.05;
                float edgeMask = 1.0 - smoothstep(0.0, coreWidth, edgeDepth);
                float innerMist = 1.0 - smoothstep(coreWidth * 0.45, fadeWidth, edgeDepth);
                float outerCore = pow(edgeMask, 0.55);

                float2 flowUv = float2(
                    flowCoord * (6.0 + density * 3.0) + time * 0.55,
                    edgeCoord * (8.0 + density * 2.0) - time * 0.9
                );
                float2 swirlUv = float2(
                    flowCoord * (11.0 + density * 2.5) - time * 0.35,
                    edgeCoord * 12.0 + time * 0.65
                );

                float currentNoise = FlowNoise(flowUv);
                float swirlNoise = FlowNoise(swirlUv);
                float ridge = saturate(1.0 - abs(currentNoise - 0.56) * 3.8);
                float foam = saturate(swirlNoise * 1.3 - 0.45);
                float wave = sin((flowCoord * 12.0) + time * 2.4 + swirlNoise * 3.14159) * 0.5 + 0.5;

                float liquidFlow = (ridge * 0.7 + foam * 0.55 + wave * 0.25);
                liquidFlow *= lerp(innerMist, outerCore, 0.7);
                liquidFlow *= lerp(0.5, 1.0, _FlowBlend);

                float glow = outerCore * (0.65 + _GlowStrength * 0.85);
                float mist = innerMist * (0.14 + foam * 0.16 + _GlowStrength * 0.05);
                float particle = liquidFlow * _ParticleIntensity;

                fixed3 tint = _EdgeColor.rgb * (glow + mist + particle);
                float alpha = saturate(baseColor.a * (outerCore * 0.8 + mist + particle * 0.9));
                fixed4 color = fixed4(tint, alpha);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
