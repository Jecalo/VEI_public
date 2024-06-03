Shader "Unlit/tri_shader"
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
            Cull off
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
                float3 pos : POS;
                fixed4 color : COLOR;
            };
            

            struct Props
            {
                float3 pos[3];
                float4 color;
            };

            StructuredBuffer<Props> _Props;

            v2f vert(uint instanceID : SV_InstanceID, uint vertexID : SV_VertexID)
            {
                v2f o;
                o.pos = _Props[instanceID].pos[vertexID];
                o.vertex = UnityObjectToClipPos(o.pos);
                o.color = _Props[instanceID].color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(cross(ddy(i.pos.xyz), ddx(i.pos.xyz)));
                float3 forward = mul((float3x3)unity_CameraToWorld, float3(0.0, 0.0, 1.0));
                return fixed4(i.color.xyz * (acos(dot(n, forward)) / 3.14), i.color.w);
            }
            ENDCG
        }
    }
}