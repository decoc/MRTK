// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/StencilViewer" {

	Properties{
		_Color0("Color0", Color) = (1,0,0)
		_Color1("Color1", Color) = (0,1,0)
		_Color2("Color2", Color) = (0,0,1)
		_Color3("Color3", Color) = (0,0,1)
	}

	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Transparent+500" }

		CGINCLUDE
		#include "UnityCG.cginc"
				struct appdata {
					float4 vertex : POSITION;
					float3 normal : NORMAL;
				};

				struct v2f{
					float4 pos: SV_POSITION;
					float3 normal: TEXCOORD1;
					float4 uvgrab: TEXCOORD2;
				};

				float4 vert(appdata v):SV_POSITION{
					return UnityObjectToClipPos(v.vertex);
				}
		ENDCG

					Pass
				{
					ZWrite Off
						ZTest Always

						Stencil
					{
						Ref 1
						Comp Equal
						Pass Keep
					}

						CGPROGRAM
						#pragma vertex vert
						#pragma fragment frag
						#pragma target 3.0

						fixed4 _Color3;

					half4 frag(v2f i) : COLOR{
						return _Color3;
					}

						ENDCG
				}

		Pass
		{
			ZWrite Off
			ZTest Always

			Stencil
			{
				Ref 2
				Comp Equal
				Pass Keep
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			fixed4 _Color0;

			half4 frag(v2f i): COLOR{
					return _Color0;
				}

			ENDCG
		}

		Pass
		{
			ZWrite Off
			ZTest Always

			Stencil
			{
				Ref 3
				Comp Equal
				Pass Keep
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			fixed4 _Color1;

			half4 frag(v2f i): COLOR{
					return _Color1;
				}

			ENDCG
		}

		Pass
		{
			ZWrite Off
			ZTest Always

			Stencil
			{
				Ref 4
				Comp Equal
				Pass Keep
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			fixed4 _Color2;

			half4 frag(v2f i): COLOR{
					return _Color2;
				}

			ENDCG
		}
	}
}
