Shader "Custom/test_shader"
{
	Properties
	{
		_WireframeColor("Wireframe front colour", color) = (0.0, 0.0, 0.0, 1.0)
		_WireframeWidth("Wireframe width threshold", float) = 0.05
	}

	//	SubShader
	//{
	//	Tags { "RenderType" = "Opaque" "Queue" = "Transparent"}
	//	LOD 100
	//	Blend SrcAlpha OneMinusSrcAlpha

	//	Pass
	//	{
	//		Cull Back
	//		CGPROGRAM
	//		#pragma require geometry
	//		#pragma vertex vert
	//		#pragma fragment frag
	//		#pragma geometry geom
	//		#pragma target 4.5
	//		#include "UnityCG.cginc"

	//		struct appdata
	//		{
	//			float4 vertex : POSITION;
	//		};

	//		struct v2f
	//		{
	//			float4 vertex : SV_POSITION;
	//		};

	//		struct g2f
	//		{
	//			float4 pos : SV_POSITION;
	//			float3 barycentric : TEXCOORD0;
	//		};


	//		v2f vert(appdata v)
	//		{
	//			v2f o;
	//			o.vertex = UnityObjectToClipPos(v.vertex);
	//			return o;
	//		}

	//		[maxvertexcount(3)]
	//		void geom(triangle v2f IN[3], inout TriangleStream<g2f> triStream) {
	//			g2f o;
	//			o.pos = IN[0].vertex;
	//			o.barycentric = float3(1.0, 0.0, 0.0);
	//			triStream.Append(o);
	//			o.pos = IN[1].vertex;
	//			o.barycentric = float3(0.0, 1.0, 0.0);
	//			triStream.Append(o);
	//			o.pos = IN[2].vertex;
	//			o.barycentric = float3(0.0, 0.0, 1.0);
	//			triStream.Append(o);
	//		}

	//		CBUFFER_START(UnityPerMaterial)
	//		fixed4 _WireframeColor;
	//		float _WireframeWidth;
	//		CBUFFER_END

	//		fixed4 frag(g2f i) : SV_Target
	//		{
	//			float closest = min(i.barycentric.x, min(i.barycentric.y, i.barycentric.z));

	//			if ((ddx(i.barycentric.x) <= 0.0) != (ddx(i.barycentric.y) <= 0.0) == (ddx(i.barycentric.z) <= 0.0)) { return _WireframeColor; }
	//			else { return fixed4(0.0, 0.0, 0.0, 0.0); }
	//		}
	//		ENDCG
	//	}
	//}

		SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5

			struct v2f {
				fixed4 color : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			v2f vert(float4 vertex : POSITION, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(vertex);
				float f = (float)vid;
				o.color = half4(sin(f / 10),sin(f / 100),sin(f / 1000),0) * 0.5 + 0.5;
				return o;
			}

			fixed4 frag(v2f i, uint tid : SV_PrimitiveID) : SV_Target
			{
				if (ddx(tid) != 0.0) { return fixed4(0.0, 1.0, 0.0, 1.0); }
				else { return fixed4(0.0, 0.0, 0.0, 1.0); }
				return fixed4(tid / 100.0, 0.0, 0.0, 1.0);
			}
			ENDCG
		}
	}
}