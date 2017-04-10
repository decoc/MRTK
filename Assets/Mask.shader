// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Mask" {

	Properties
	{
		_Color("Color", Color) = (1,1,1,0)
	}

	SubShader{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent+10" }

		LOD 200

		CGINCLUDE
		#include "UnityCG.cginc"

		fixed4 _Color;

		struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

				float4 vert(appdata v):SV_POSITION{
					return UnityObjectToClipPos(v.vertex);
				}

			float4 frag(v2f i): COLOR{
				return half4(0,1,0,0);
				//return _Color;
			}

		ENDCG

		Pass {

			Stencil
			{
				Ref 10
				Comp Always 
				Pass Replace 
			}


			Cull Front
			ZWrite On
			ZTest LEqual 
			ColorMask 0 

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0



			ENDCG
		}

			
	}
		
		FallBack "Diffuse"
}
