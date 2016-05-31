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
			Cull Off

			CGPROGRAM
            #pragma target 5.0
			#pragma vertex vert
			//#pragma geometry GS_Main
            #pragma geometry geo
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
				float3 direction = p[1].vertex.xyz - p[0].vertex.xyz;

				float3 look = _WorldSpaceCameraPos - p[0].vertex;
				float distanceFromCamera = length(look) * 0.01f;
				//look.y = 0;
				look = normalize(look);

				float3 right = cross(direction, look);
				right = normalize(right);

				float smallSize = _Size * distanceFromCamera;

				float4 upLeft      = float4(p[0].vertex.xyz + right * smallSize, 1.0f);
				float4 bottomLeft  = float4(p[0].vertex.xyz - right * smallSize, 1.0f);
				float4 upRight     = float4(p[1].vertex.xyz + right * smallSize, 1.0f);
				float4 bottomRight = float4(p[1].vertex.xyz - right * smallSize, 1.0f);

				float4x4 vp = mul(UNITY_MATRIX_MVP, _World2Object);

				v2f pIn;

				pIn.vertex = mul(vp, upRight);
				pIn.color = p[1].color;
				triStream.Append(pIn);

				pIn.vertex = mul(vp, bottomRight);
				pIn.color = p[1].color;
				triStream.Append(pIn);

				pIn.vertex = mul(vp, upLeft);
				pIn.color = p[0].color;
				triStream.Append(pIn);

				pIn.vertex = mul(vp, bottomLeft);
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
