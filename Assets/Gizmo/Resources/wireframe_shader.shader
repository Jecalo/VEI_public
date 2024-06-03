Shader "Unlit/Wireframe"
{
	Properties
	{
		_WireframeColor("Wireframe front colour", color) = (0.0, 0.0, 0.0, 1.0)
		_WireframeWidth("Wireframe width threshold", float) = 0.05
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Transparent"}
		LOD 100
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			Cull Back
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			struct g2f
			{
				float4 pos : SV_POSITION;
				float3 barycentric : TEXCOORD0;
			};


			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			[maxvertexcount(3)]
			void geom(triangle v2f IN[3], inout TriangleStream<g2f> triStream) {
				g2f o;
				o.pos = IN[0].vertex;
				o.barycentric = float3(1.0, 0.0, 0.0);
				triStream.Append(o);
				o.pos = IN[1].vertex;
				o.barycentric = float3(0.0, 1.0, 0.0);
				triStream.Append(o);
				o.pos = IN[2].vertex;
				o.barycentric = float3(0.0, 0.0, 1.0);
				triStream.Append(o);
			}

			CBUFFER_START(UnityPerMaterial)
			fixed4 _WireframeColor;
			float _WireframeWidth;
			CBUFFER_END

			fixed4 frag(g2f i) : SV_Target
			{
				//float closest = min(i.barycentric.x, min(i.barycentric.y, i.barycentric.z));
				//float alpha = step(closest, _WireframeWidth);
				//return fixed4(_WireframeColor.r, _WireframeColor.g, _WireframeColor.b, alpha);

				float3 br = smoothstep(float3(0.0, 0.0, 0.0), fwidth(i.barycentric) * 1.5, i.barycentric);
				float closest = min(br.x, min(br.y, br.z));
				float alpha = step(closest, _WireframeWidth);
				return fixed4(_WireframeColor.r, _WireframeColor.g, _WireframeColor.b, alpha);
			}
			ENDCG
		}
	}
}
