// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/MaskTarget" {

		Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex ("Texture", 2D) = "white" {}
	}

		SubShader{
		Tags{ "RenderType" = "Transparent" "IgnoreProjector"="True" "Queue" = "Transparent+2" }

		LOD 200

		CGINCLUDE
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				UNITY_FOG_COORDS(1)
			};

			fixed4 _Color;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}
			
		ENDCG



		Pass{
			Stencil{
				Ref 2
				Comp Equal
				ZFail IncrSat
			}

			Cull Back
			Zwrite Off
			ZTest GEqual
			ColorMask 0

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			float4 frag(v2f i) : SV_Target{
			return half4(1,0,0,1);
		}

			ENDCG
		}


		Pass{
			Stencil
			{
				Ref 3
				Comp Equal
				ZFail IncrSat
			}

			Cull Front
			Zwrite Off
			ZTest LEqual
			ColorMask 0

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			float4 frag(v2f i) : SV_Target{
			return half4(1,0,0,1);
		}
			ENDCG
		}

		Pass{
			Stencil
			{
				Ref 2
				Comp Equal
				ZFail DecrSat
			}

			Cull Back
			Zwrite Off
			ZTest LEqual
			ColorMask 0

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			float4 frag(v2f i) : SV_Target{
			return half4(1,0,0,1);
		}
			ENDCG
		}

				
			Pass{
				Cull Back
				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.0
				#pragma multi_compile_fog


				float4 frag(v2f i) : SV_Target{
					fixed4 col = tex2D(_MainTex, i.uv) * _Color;
					//col = 1 - col;
						return col;
				}

				ENDCG
		}
				
		
		
	}
		FallBack "Diffuse"
}
