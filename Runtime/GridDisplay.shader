Shader "Unlit/GridDisplay"
{
    Properties
    {
    }

    SubShader
    {
        Pass
        {
            CGPROGRAM

            #pragma vertex vertexFunc
            #pragma fragment fragmentFunc
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION; 
                fixed4 color : COLOR0;
            };


            v2f vertexFunc(appdata_full v)
            {
                v2f o;
                o.color = v.color;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 fragmentFunc(v2f i) : COLOR
            {
                return fixed4(1, 1, 1, 1);
                return i.color;

            }

        ENDCG
        }
    }
}