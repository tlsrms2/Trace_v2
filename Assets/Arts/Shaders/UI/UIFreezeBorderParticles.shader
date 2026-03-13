Shader "UI/FreezeBorderParticles"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _EdgeColor ("Edge Color", Color) = (0,0.827451,0.9490196,1)
        _EdgeDirection ("Edge Direction", Vector) = (0,1,0,0)
        _GradientSoftness ("Gradient Softness", Range(0.05, 1)) = 0.45
        _GlowStrength ("Glow Strength", Range(0, 4)) = 1.35
        _ParticleDensity ("Particle Density", Range(0, 4)) = 1.4
        _ParticleSpeed ("Particle Speed", Range(0, 5)) = 1.25
        _ParticleIntensity ("Particle Intensity", Range(0, 2)) = 0.8
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile _ UNITY_UI_CLIP_RECT
            #pragma multi_compile _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

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
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 localPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            fixed4 _Color;
            fixed4 _EdgeColor;
            float4 _MainTex_ST;
            float4 _ClipRect;
            float4 _EdgeDirection;
            float _GradientSoftness;
            float _GlowStrength;
            float _ParticleDensity;
            float _ParticleSpeed;
            float _ParticleIntensity;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float ParticleLayer(float2 uv, float edgeFactor, float timeOffset)
            {
                float2 flowUv = uv * (10.0 * max(_ParticleDensity, 0.001));
                flowUv += float2(_Time.y * _ParticleSpeed * 0.2 + timeOffset, _Time.y * _ParticleSpeed * 0.33 - timeOffset);

                float2 cell = floor(flowUv);
                float2 local = frac(flowUv) - 0.5;
                float rnd = Hash21(cell + timeOffset);
                float2 particlePos = float2(Hash21(cell + 1.37 + timeOffset), Hash21(cell + 2.41 + timeOffset)) - 0.5;
                particlePos *= lerp(0.1, 0.42, rnd);

                float dist = length(local - particlePos);
                float pulse = 0.35 + 0.65 * sin(_Time.y * (_ParticleSpeed * 3.0 + rnd * 5.0) + rnd * 6.28318);
                float sparkle = smoothstep(0.24, 0.0, dist) * pulse;
                return saturate(sparkle * edgeFactor * _ParticleIntensity);
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.localPos = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 baseSample = (tex2D(_MainTex, IN.uv) + _TextureSampleAdd) * IN.color;

                float edgeProximity = 0.0;
                if (_EdgeDirection.y > 0.5)
                {
                    edgeProximity = IN.localPos.y;
                }
                else if (_EdgeDirection.y < -0.5)
                {
                    edgeProximity = 1.0 - IN.localPos.y;
                }
                else if (_EdgeDirection.x > 0.5)
                {
                    edgeProximity = IN.localPos.x;
                }
                else
                {
                    edgeProximity = 1.0 - IN.localPos.x;
                }

                float softness = max(_GradientSoftness, 0.001);
                float edgeFade = pow(saturate(edgeProximity), lerp(0.5, 3.0, softness));
                float body = saturate(edgeFade * _GlowStrength);

                float shimmerA = ParticleLayer(IN.localPos, edgeFade, 0.17);
                float shimmerB = ParticleLayer(IN.localPos.yx + 2.0, edgeFade, 1.91);
                float particles = saturate(shimmerA + shimmerB);

                fixed3 finalRgb = _EdgeColor.rgb * saturate(body + particles);
                float finalAlpha = saturate((body + particles) * _EdgeColor.a) * baseSample.a;
                fixed4 color = fixed4(finalRgb, finalAlpha);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
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
