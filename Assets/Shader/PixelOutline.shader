Shader "Custom/PixelSpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0,4)) = 1
        _OutlineEnabled ("Outline Enabled", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineEnabled;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 original = tex2D(_MainTex, i.uv) * i.color;

                if (_OutlineEnabled < 0.5 || _OutlineWidth <= 0)
                    return original;

                float2 d = _MainTex_TexelSize.xy * _OutlineWidth;

                float a = 0;
                a = max(a, tex2D(_MainTex, i.uv + float2( d.x, 0)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2(-d.x, 0)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2(0,  d.y)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2(0, -d.y)).a);

                a = max(a, tex2D(_MainTex, i.uv + float2( d.x,  d.y)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2(-d.x,  d.y)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2( d.x, -d.y)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2(-d.x, -d.y)).a);

                // 只在原图透明、相邻像素不透明的位置绘制白边
                if (original.a < 0.01 && a > 0.01)
                {
                    return fixed4(
                        _OutlineColor.rgb,
                        _OutlineColor.a * a * i.color.a
                    );
                }

                return original;
            }
            ENDCG
        }
    }
}