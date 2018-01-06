Shader ".Custom/Terrain Gradient"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_GradBalance("Gradient Brightness Balance", Float) = 1
		_GradStrength("Gradient Strength", Float) = 1.2
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM

			#pragma vertex vertexFunction
			#pragma fragment fragmentFunction

			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv2 : TEXCOORD1;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 uv2 : TEXCOORD1;
			};

			float4 _Color;
			float _GradBalance;
			float _GradStrength;

			v2f vertexFunction(appdata IN)
			{
				v2f OUT;

				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.uv2 = IN.uv2;

				return OUT;
			}

			fixed4 fragmentFunction(v2f IN) : SV_TARGET
			{
				float4 finalColor = _Color;

				finalColor = _Color + ((IN.uv2.y - _GradBalance) * _GradStrength);

				return finalColor;
			}

			ENDCG
		}
	}
}