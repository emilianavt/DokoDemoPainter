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

public class DokoDemoPainterPen : MonoBehaviour {
    [Header("Pen settings")]
    [Tooltip("The color to paint with. Setting an alpha value will make the target texture transparent. To paint at a reduced opacity, use the opacity setting below.")]
    public Color color = Color.black;
    [Tooltip("The drawing size of the pen on the target texture in pixels. It will be multiplied by the DokoDemoPainterPaintable component's radiusFactor or eraserRadiusFactor.")]
    public float radius = 1.0f;
    [Tooltip("Allows painting at a reduced opacity. Setting this to a value other than 1.0 or enabling the smooth pen tip function will slightly reduce performance.")]
    public float opacity = 1.0f;
    [Tooltip("When enabled, the brush will have a smoother brush. Enabling this or setting an opacity other than 1.0 will slightly reduce performance.")]
    public bool smoothTip = false;
    [Tooltip("This exponent will determine how smooth or hard the brush is. Values above 1.0 make it harder. Values below make it softer.")]
    public float smoothTipExponent = 1.0f;
    [Tooltip("The pen will only paint while this flag is active.")]
    public bool penDown = false;
    [Tooltip("This flag turns the pen into an eraser. An eraser will blend the texture back to its original state rather than painting over it with a color.")]
    public bool eraser = false;
    [Header("Pen behaviour")]
    [Tooltip("When enabled, the pen tries to keep painting on the same texture, even when going underneath other objects.")]
    public bool keepTarget = false;
    [Tooltip("When enabled, you can start painting on textures where they have an alpha value of 0.")]
    public bool paintInvisible = false;
    [Header("Required setup")]
    [Tooltip("This camera is used to find surfaces to paint on. It may not be used for any other purpose.")]
    public Camera uvcam;
    private Shader ddpdShader = null;
    private RenderTexture lastPenTex = null;
    private int id = -1;
    
    private static int nextId = 0;
    
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
        id = nextId++;
    }

    void Update () {
        if (penDown) {
            penUpdate();
        } else if (lastPenTex != null) {
            RenderTexture.ReleaseTemporary(lastPenTex);
            lastPenTex = null;
        }
    }

    public void penUpdate () {
        if (!penDown) {
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
            Shader.SetGlobalTexture("_DDPPenLast", lastPenTex);
            Shader.SetGlobalFloat("_DDPDontSwitch", keepTarget ? 1.0f : 0.0f);
            Shader.SetGlobalFloat("_DDPInvisibleAlpha", paintInvisible ? 1.0f : 0.0f);
            uvcam.RenderWithShader(ddpdShader, "");
            uvcam.targetTexture = null;
            RenderTexture.active = prev;
            DokoDemoPainterPaintable.setGlobalUVMode(false, planes);
            DokoDemoPainterPaintable.globalPaintAt(id, renderTexture, color, smoothTip, smoothTipExponent, radius, opacity, eraser);
            if (lastPenTex != null) {
                RenderTexture.ReleaseTemporary(lastPenTex);
            }
            lastPenTex = renderTexture;
        }
        uvcam.gameObject.SetActive(false);
    }
}