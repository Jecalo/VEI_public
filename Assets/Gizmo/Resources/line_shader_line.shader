Shader "Unlit/line_shader_line"
{
    Properties
    {
        
    }
    SubShader
    {
        Pass
        {
            //ZTest Always
            //ZWrite Off
            Tags { "Queue" = "Overlay" "RenderType" = "Overlay" }
            LOD 100
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            //#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma target 4.5
            #include "UnityCG.cginc"


            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };


            struct Props
            {
                float3 pos[2];
                float4 color;
            };

            StructuredBuffer<Props> _Props;

            v2f vert(uint instanceID : SV_InstanceID, uint vertexID : SV_VertexID)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(_Props[instanceID].pos[vertexID]);
                o.color = _Props[instanceID].color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}