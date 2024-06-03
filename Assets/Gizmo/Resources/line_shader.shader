Shader "Unlit/line_shader"
{
    Properties
    {
        //_Color ("Line color", color) = (0.0, 0.0, 0.0, 1.0)
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

            struct appdata
            {
                float4 vertex : POSITION;
                //UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                //UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            //UNITY_INSTANCING_BUFFER_START(Props)
            //UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            //UNITY_INSTANCING_BUFFER_END(Props)

            struct Props
            {
                float4x4 trs;
                float4 color;
            };

            StructuredBuffer<Props> _Props;

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                //UNITY_SETUP_INSTANCE_ID(v);
                //UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(mul(_Props[instanceID].trs, v.vertex));
                o.color = _Props[instanceID].color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                //UNITY_SETUP_INSTANCE_ID(i);
                return i.color;
            }
            ENDCG
        }
    }
}
