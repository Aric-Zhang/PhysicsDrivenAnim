Shader "ZXT/ConeUnlitFront"
{
    Properties
    {
        _Color("Color",color) = (1,0,0,0.5)
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue" = "Transparent" 
            "LightMode"="ForwardBase"
        }

        LOD 100

        Pass
        {
            Cull Front
            //Blend SrcAlpha OneMinusSrcAlpha
            //BlendOp Max
            Blend SrcAlpha One
            //Blend One One
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            half4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = _Color;
                return col;
            }
            ENDCG
        }
    }
}
