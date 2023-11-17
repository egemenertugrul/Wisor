Shader "Openwixr Lens Distortion Shader" {
	Properties {
		_MainTex("MainTex", 2D) = "white" {}
		[MaterialToggle] _isRight("isRight", Range(0, 1)) = 0.0
		_distortion("distortion", Range(-3, 3)) = -0.7
		_offsetX("offsetX", Range(-1, 1)) = 0.0
		_offsetY("offsetY", Range(-1, 1)) = 0.0
		_cubicDistortion("cubicDistortion", Range(0, 3)) = 0.4
		_scale("scale", Range(0, 3)) = 1
		_OutOfBoundColour ("OutOfBoundColour", Color) = (0, 0, 0, 0)
	}

	SubShader {
		Pass {
			Tags { "LightMode" = "ForwardBase" }
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.0
			#include "UnityCG.cginc"

			float _offsetX;
			float _offsetY;
			float _distortion;
			float _cubicDistortion;
			bool _isRight;
			float _scale;
			float4 _OutOfBoundColour;

			sampler2D _MainTex;
			fixed4 _MainTex_ST;

			struct v2f {
				fixed4 pos : SV_POSITION;
				fixed2 uv_MainTex : TEXCOORD0;
			};

			v2f vert(appdata_full v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv_MainTex = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}

			float2 barrel(float2 uv) {
				float2 h = uv.xy - float2(0.5, 0.5);
				h.x += _offsetX;
				h.y += _offsetY;

				float r2 = h.x * h.x; // + h.y * h.y;

				if (_isRight) {
					if (h.x > 0.12)
						return float2(-5, -5);

					if (h.x < 0)
						r2 *= 3;
				}

				if (!_isRight) {
					if (h.x < -0.12)
						return float2(-5, -5);

					if (h.x > 0)
						r2 *= 3;
				}

				float f = 1.0 + r2 * (_distortion + _cubicDistortion * r2);
				float dec = f * 0.5f;
				float2 ret = float2(f * h.x + 0.5f, h.y + dec);

				if (ret[0] < 0 || ret[0] > 1 || ret[1] < 0 || ret[1] > 1) {
					return float2(-5, -5); //uv out of bound so display out of bound color
				}

				return ret;
			}

			fixed4 frag(v2f i) : COLOR
			{
				float2 barreled = barrel(i.uv_MainTex);

				if (barreled[0] < -1 && barreled[1] < -1)
				{
					return _OutOfBoundColour;
				}

				fixed4 distorted = tex2D(_MainTex, barreled);
				return distorted;
			}
			ENDCG
		}
	}
}