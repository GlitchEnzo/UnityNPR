Shader "NPR/EdgeLines"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				int2 edgeID : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.edgeID = v.uv;
				return o;
			}

			int4 frag (v2f i) : SV_Target
			{
				return int4(i.edgeID, 0, 1);
			}
			ENDCG
		}
	}
}
