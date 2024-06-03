Shader "Unlit/id_shader"
{
    Properties
    {
        _ObjId("Object id", integer) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                uint vid : SV_VertexID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                uint vid : ID_Int;
            };


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.vid = v.vid;
                return o;
            }

            //CBUFFER_START(UnityPerMaterial)
            int _ObjId;
            //CBUFFER_END

            fixed4 frag(v2f i) : SV_Target
            {
                //return fixed4(i.vid / 100.0, 0.0, 0.0, 1.0);
                return fixed4(1.0, 1.0, 1.0, 1.0);
                if (i.vid == 0) { return fixed4(1.0, 1.0, 1.0, 1.0); }
                else { return fixed4(0.0, 0.0, 0.0, 1.0); }
            }
            ENDCG
        }
    }
}
