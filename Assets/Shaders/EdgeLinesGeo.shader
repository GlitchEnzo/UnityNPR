Shader "NPR/EdgeLines"
{
	Properties
	{
		_Size("Size", Range(0, 3)) = 0.5
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM
            #pragma target 5.0
			#pragma vertex vert
			#pragma geometry GS_Main
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			float _Size;

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR0;
			};

			struct v2f
			{
				float4 color : COLOR0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				//o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.vertex = mul(_Object2World, v.vertex);
				o.color = v.color;
				return o;
			}

			[maxvertexcount(4)]
			void geo(line v2f p[2], inout TriangleStream<v2f> triStream)
			{
				float2 direction = p[1].vertex.xy - p[0].vertex.xy;
				float2 normal = float2(-direction.y, direction.x);

				float2 width = float2(1 / 1024, 1 / 768);
				float pixelWidth = 5;

				float2 upLeft = p[0].vertex.xy + normal * width * pixelWidth;
				float2 bottomLeft = p[0].vertex.xy - normal * width * pixelWidth;
				float2 upRight = p[1].vertex.xy + normal * width * pixelWidth;
				float2 bottomRight = p[1].vertex.xy - normal * width * pixelWidth;

				v2f pIn;

				pIn.vertex = float4(bottomRight, p[1].vertex.z, p[1].vertex.w);
				pIn.color = p[1].color;
				triStream.Append(pIn);

				pIn.vertex = float4(upRight, p[1].vertex.z, p[1].vertex.w);
				pIn.color = p[1].color;
				triStream.Append(pIn);

				pIn.vertex = float4(upLeft, p[0].vertex.z, p[0].vertex.w);
				pIn.color = p[0].color;
				triStream.Append(pIn);

				pIn.vertex = float4(bottomLeft, p[0].vertex.z, p[0].vertex.w);
				pIn.color = p[0].color;
				triStream.Append(pIn);
			}

			// Geometry Shader -----------------------------------------------------
			[maxvertexcount(4)]
			void GS_Main(point v2f p[1], inout TriangleStream<v2f> triStream)
			{
				float3 up = float3(0, 1, 0);
				float3 look = _WorldSpaceCameraPos - p[0].vertex;
				look.y = 0;
				look = normalize(look);
				float3 right = cross(up, look);

				float halfS = 0.5f * _Size;

				float4 v[4];
				v[0] = float4(p[0].vertex + halfS * right - halfS * up, 1.0f);
				v[1] = float4(p[0].vertex + halfS * right + halfS * up, 1.0f);
				v[2] = float4(p[0].vertex - halfS * right - halfS * up, 1.0f);
				v[3] = float4(p[0].vertex - halfS * right + halfS * up, 1.0f);

				float4x4 vp = mul(UNITY_MATRIX_MVP, _World2Object);
				v2f pIn;
				pIn.vertex = mul(vp, v[0]);
				pIn.color = p[0].color;
				triStream.Append(pIn);

				pIn.vertex = mul(vp, v[1]);
				pIn.color = p[0].color;
				triStream.Append(pIn);

				pIn.vertex = mul(vp, v[2]);
				pIn.color = p[0].color;
				triStream.Append(pIn);

				pIn.vertex = mul(vp, v[3]);
				pIn.color = p[0].color;
				triStream.Append(pIn);
			}

			float4 frag(v2f i) : SV_Target
			{
				return i.color;
			}
			ENDCG
		}
	}
}
