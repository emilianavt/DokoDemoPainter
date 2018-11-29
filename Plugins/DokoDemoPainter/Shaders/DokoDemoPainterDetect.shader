// Copyright (c) 2018 @emiliana_vt
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// https://forum.unity.com/threads/uv-visualization-with-surface-shader.226509/
Shader "DokoDemoPainter/Detect" {
	Properties {
		_MainTex ("Base (RGBA)", 2D) = "white" {}
		_SurfaceID ("Surface ID", Float) = -4.0
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" "DokoDemoPainterDetect"="True" }
		LOD 0
			   
		CGPROGRAM
		#pragma surface surf Lambert alpha
 
		sampler2D _MainTex;
		sampler2D _DDPPenLast;
		float _SurfaceID;
		float _DDPDontSwitch;
		float _DDPInvisibleAlpha;
 
		struct Input {
			float2 uv_MainTex;
		};
 
		void surf (Input IN, inout SurfaceOutput o) {
			float4 last = tex2D(_DDPPenLast, float2(0.5, 0.5));
			float4 col = tex2D(_MainTex, IN.uv_MainTex.rg);
			bool sameID = abs(last.b - _SurfaceID) < 0.1;
			bool sameIDorNone = last.b >= 2.0 ? sameID : true;
			bool accept = _DDPDontSwitch > 0.0 ? sameIDorNone : true;
			o.Emission = accept ? float3(IN.uv_MainTex.rg, _SurfaceID) : float3(0.0, 0.0, 0.0);
			float invisibleAlpha = (accept && (_DDPInvisibleAlpha > 0.0)) ? 1.0 : 0.0;
			o.Alpha = (accept && (sameID || col.a > 0.0)) ? 1.0 : invisibleAlpha;
		}
		ENDCG
	}
}