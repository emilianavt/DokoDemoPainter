// Copyright (c) 2018 Emiliana (twitter.com/Emiliana_vt)
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

Shader "DokoDemoPainter/Render" {
Properties {
    _MainTex ("Current (RGB) Trans (A)", 2D) = "black" {}
    _OrigTex ("Original (RGB) Trans (A)", 2D) = "white" {}
    _HistTex ("History (RG)", 2D) = "black" {}
    _Fade ("Fade factor towards original", Range(0.0, 1.0)) = 0.0
    _SurfaceID ("Surface ID", Float) = -1.0
    _MaxDist ("Maximum distance between line drawing points", Float) = 0.0
    _PreserveAlpha ("Preserve original alpha or let pen/stamp paint it over", Range(0.0, 1.0)) = 0.0
    _CoordTexA ("Coord texture containing old line drawing point", 2D) = "black" {}
    _CoordTexB ("Coord texture containing new line drawing point", 2D) = "black" {}
    _PenThickness ("Pen thickness", Float) = 0.0
    _PenColor ("Pen color", Color) = (0.0, 0.0, 0.0, 1.0)
    _Opacity ("Pen opacity", Range(0.0, 1.0)) = 0.0
    _EraserStrength ("Eraser strength", Range(0.0, 1.0)) = 0.0
    _SmoothTip ("Smooth pen/eraser tip", Range(0.0, 1.0)) = 0.0
    _SmoothExp ("Exponent for tip smoothness", Float) = 1.0
    _StampTex ("Square stamp (RGB) Trans (A)", 2D) = "black" {}
    _StampSize ("Normalized stamp size in X and Y directions", Vector) = (0.0, 0.0, 0.0, 0.0)
    _StampAngle ("Rotation of the stamp in radian", Float) = 0.0
    _StampAlpha ("Stamp opacity", Range(0.0, 1.0)) = 0.0
    _StampTint ("Stamp tint color", Color) = (0.0, 0.0, 0.0, 0.0)
    _StampTintFactor ("Stamp tint factor", Vector) = (0.0, 0.0, 0.0, 0.0)
    _HistoryTime ("Amount of time to keep history in seconds", Float) = 0.0
    _HistoryDecay ("Amount to decay history by", Float) = 0.0
    _HistoryWrite ("Generate history", Range(0.0, 1.0)) = 0.0
}

SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "DokoDemoPainterRender"="True"}
    LOD 100

    ColorMask RGBA

    Pass {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float2 texcoord2 : TEXCOORD1;
                float2 texcoord3 : TEXCOORD2;
                float4 a : TEXCOORD3;
                float4 b : TEXCOORD4;
                float2 ba : TEXCOORD5;
                float2 dotBA : TEXCOORD6;
                float dID : TEXCOORD7;
                float4 stamp : TEXCOORD8;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _OrigTex;
            sampler2D _HistTex;
            sampler2D _StampTex;
            sampler2D _CoordTexA;
            sampler2D _CoordTexB;
            float4 _MainTex_ST;
            float4 _HistTex_ST;
            float4 _OrigTex_ST;
            float _Fade;
            float _SurfaceID;
            float _MaxDist;
            float _PenThickness;
            float4 _PenColor;
            float _Opacity;
            float _EraserStrength;
            float _SmoothTip;
            float _SmoothExp;
            float2 _StampSize;
            float _StampAngle;
            float _StampAlpha;
            float4 _StampTint;
            float4 _StampTintFactor;
            float _HistoryTime;
            float _HistoryDecay;
            float _HistoryWrite;
            float _PreserveAlpha;

            v2f vert (appdata_t v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.texcoord2 = TRANSFORM_TEX(v.texcoord, _OrigTex);
                o.texcoord3 = TRANSFORM_TEX(v.texcoord, _HistTex);
                o.a = tex2Dlod(_CoordTexA, float4(0.5, 0.5, 0.0, 0.0));
                o.a.xy = frac(o.a.xy);
                o.a.x = o.a.x * 2.0;
                o.b = tex2Dlod(_CoordTexB, float4(0.5, 0.5, 0.0, 0.0));
                float angle = radians(trunc(o.b.x) * 2.0) * sign(o.b.x);
                o.b.xy = frac(o.b.xy);
                o.b.x = o.b.x * 2.0;
                o.dID = abs(o.a.z - _SurfaceID);
                o.a = (o.dID < 0.1) ? o.a : o.b;
                o.dID = abs(o.b.z - _SurfaceID);
                float dist = distance(o.a, o.b);
                o.a = (_MaxDist > dist) ? o.a : o.b;
                o.ba = o.b - o.a;
                o.dotBA = dot(o.ba, o.ba);
                o.stamp.xy = o.b.xy;
                o.stamp.zw = float2(cos(_StampAngle + angle), sin(_StampAngle + angle));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 pa = i.texcoord - i.a;
                float h = clamp( dot(pa,i.ba)/i.dotBA, 0.0, 1.0 );
                float idk = (abs(i.dotBA) < 0.0000001) ? length(i.b.xy - i.texcoord) : length(pa - i.ba*h);

                float4 origColor = tex2D(_OrigTex, i.texcoord2);
                float4 curColor = tex2D(_MainTex, i.texcoord);
                float2 histColor = tex2D(_HistTex, i.texcoord3).rg;
                float4 blend = lerp(curColor, origColor, _Fade);

                
                float smoothAlpha = (_SmoothTip > 0.0) ? 1.0 - pow((idk / _PenThickness), _SmoothExp) : 1.0;
                float strength = (_EraserStrength > 0.0) ? _EraserStrength : _Opacity;
                float4 color = (_EraserStrength > 0.0) ? origColor : _PenColor;
                float4 hist = (curColor - histColor.g * strength * color) / (1.0 - histColor.g * strength);
                hist = lerp(hist, origColor, _Fade);
                
                float4 base = (histColor.r > 0.0 && histColor.g < smoothAlpha) ? hist : blend;
                float basehCr = max(0, histColor.r - _HistoryDecay);
                histColor.r = (histColor.g < smoothAlpha) ? 0.0 : histColor.r;
                
                float4 newColor = lerp(base, color, smoothAlpha * strength);
                float4 drawColor = (histColor.r > 0.0) ? blend : newColor;
                drawColor.a = (_PreserveAlpha > 0.0) ? blend.a : drawColor.a;
                drawColor = (i.dID < 0.1) ? drawColor : blend;  
                
                bool inLine = (idk < _PenThickness && i.dID < 0.1);
                bool hasHistory = basehCr >= 0.0000001;
                bool notMoving = length(i.texcoord.xy - i.b.xy) < _PenThickness && length(i.a.xy - i.b.xy) < 0.0000001;
                float4 historyData = float4(0.0, 0.0, 0.0, 0.0);
                historyData.r = (inLine && (!hasHistory || notMoving)) ? _HistoryTime : basehCr;
                historyData.g = inLine ? max(histColor.g, smoothAlpha) : ((historyData.r < 0.0000001) ? 0.0 : histColor.g);

                fixed4 col = inLine ? drawColor : blend;

                float2 stampPos = i.texcoord;
                float2 stampOffset = i.stamp.xy;
                float2 q = stampPos;
                stampPos = mul(float2((stampPos.x - stampOffset.x)/_StampSize.x, (stampPos.y - stampOffset.y)/_StampSize.y), float2x2(i.stamp.z, i.stamp.w, -i.stamp.w, i.stamp.z));
                stampPos += float2(0.5, 0.5);
                float4 stamp = tex2D(_StampTex, stampPos);
                float stampAlpha = stamp.a * _StampAlpha;
                stamp.a = (_PreserveAlpha > 0.0) ? col.a : stamp.a;
                
                float4 stampTinted;
                stampTinted.rgb = (stamp.x + stamp.y + stamp.z)/3.0 - (1.0 - _StampTint);
                stampTinted.a = _StampTint.a;
                stamp = lerp(stamp, stampTinted, _StampTintFactor);

                col = (stampPos.x > 0.0 && stampPos.y > 0.0 && stampPos.x < 1.0 && stampPos.y < 1.0 && abs(_StampSize.x) > 0.0 && abs(_StampSize.y) > 0.0 && i.dID < 0.1) ? lerp(col, stamp, stampAlpha) : col;
                col = (_HistoryWrite > 0.0) ? historyData : col;

                return col;
            }
        ENDCG
    }
}

}
