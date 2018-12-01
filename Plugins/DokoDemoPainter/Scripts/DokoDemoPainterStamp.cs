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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DokoDemoPainterStamp : MonoBehaviour {
    [Header("Stamp settings")]
    [Tooltip("The texture to be stamped.")]
    public Texture2D stampTexture;
    [Tooltip("The size the stamp will occupy on the target textures. This value is scaled by the DokoDemoPainterPaintable component's stampScaleFactor setting.")]
    public Vector2 stampPixelSize = new Vector2(128.0f, 128.0f);
    [Tooltip("The angle to stamp at in degrees.")]
    public float stampAngle = 0.0f;
    [Tooltip("The opacity of the stamp.")]
    public float stampOpacity = 1.0f;
    [Tooltip("Whether the stamp should be color tinted or not.")]
    public bool enableTint;
    [Tooltip("The color to tint the stamp with. Tinting the stamp with an alpha value will result in the target texture becoming transparent.")]
    public Color tintColor = Color.black;
    [Tooltip("Each RGBA color channel can be tinted with a different strength. By default alpha tinting is set to 0 (off).")]
    public Vector4 tintStrength = new Vector4(1.0f, 1.0f, 1.0f, 0.0f);
    [Tooltip("The stamp will only stamp a texture while active.")]
    public bool stampActive = false;
    [Tooltip("When enabled, this option will turn the stamp inactive after stamping an object. This requires reading the detection result from the GPU back to the CPU, which can slow things down a lot. At the same time, this check should only be necessary rarely. As long as there isn't a number of stamps almost but not quite stamping things, performance should still be okay.")]
    public bool deactiveStampAfterAttempt = true;
    [Header("Stamp behaviour")]
    [Tooltip("When enabled, you can stamp on textures where they have an alpha value of 0.")]
    public bool paintInvisible = false;
    [Header("Required setup")]
    [Tooltip("This camera is used to find surfaces to stamp. It may not be used for any other purpose.")]
    public Camera uvcam;
    private Shader ddpdShader = null;
    
    void Start () {
        int layer = LayerMask.NameToLayer("DokoDemoPainter");
        if (layer == -1) {
            Debug.LogError("Attempting to initialize texture painting with missing Unity layer. Please add a layer named: DokoDemoPainter");
            return;
        }
        uvcam.cullingMask = 1 << layer;
        /*foreach (Transform transform in uvcam.gameObject.GetComponentsInChildren<Transform>(true))
        {
            transform.gameObject.layer = layer;
        }*/
        ddpdShader = Shader.Find("DokoDemoPainter/Detect");
    }

    void Update () {
        if (stampActive) {
            stampUpdate();
        }
    }

    public void stampUpdate () {
        if (!stampActive) {
            return;
        }
        uvcam.enabled = false;
        uvcam.gameObject.SetActive(true);
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(uvcam);
        bool detected = DokoDemoPainterPaintable.setGlobalUVMode(true, planes);
        if (detected) {
            RenderTexture prev = RenderTexture.active;
            RenderTexture renderTexture = RenderTexture.GetTemporary(1, 1, 16, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, 1);
            uvcam.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            Shader.SetGlobalTexture("_DDPPenLast", null);
            Shader.SetGlobalFloat("_DDPDontSwitch", 0.0f);
            Shader.SetGlobalFloat("_DDPInvisibleAlpha", paintInvisible ? 1.0f : 0.0f);
            uvcam.RenderWithShader(ddpdShader, "");
            uvcam.targetTexture = null;
            if (deactiveStampAfterAttempt) {
                RenderTexture.active = renderTexture;
                Texture2D readTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                readTex.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
                readTex.Apply();
                if (readTex.GetPixel(0, 0).b > 0.0) {
                    stampActive = false;
                }
            }
            RenderTexture.active = prev;
            DokoDemoPainterPaintable.setGlobalUVMode(false, planes);
            DokoDemoPainterPaintable.globalStampAt(renderTexture, stampTexture, stampPixelSize, stampAngle * Mathf.Deg2Rad, stampOpacity, enableTint, tintColor, tintStrength);
            RenderTexture.ReleaseTemporary(renderTexture);
        }
        uvcam.gameObject.SetActive(false);
    }
}