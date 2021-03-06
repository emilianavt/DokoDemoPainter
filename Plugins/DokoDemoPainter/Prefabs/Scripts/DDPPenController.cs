﻿// Copyright (c) 2018 Emiliana (twitter.com/Emiliana_vt)
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

public class DDPPenController : MonoBehaviour {
    [Header("Pen prefab settings")]
    [Tooltip("This field is optional. If set, it has to be the default pen prefab's MeshRenderer.")]
    public MeshRenderer penMesh = null;
    [Tooltip("This field determines how often to update the pen's appearance. Its value is given in seconds.")]
    public float updateInterval = 5.0f;
    private DokoDemoPainterPen pen;
    private float delta;

    void setPenVisuals() {
        if (penMesh != null) {
            delta += Time.deltaTime;
            if (delta < updateInterval) {
                return;
            }
            Material[] materials = penMesh.materials;
            materials[1].color = pen.color / 3.0f;
            materials[2].color = pen.color;
            penMesh.materials = materials;
            delta = 0.0f;
        }
    }

    void Start () {
        pen = GetComponent<DokoDemoPainterPen>();
        delta = updateInterval;
        setPenVisuals();
    }
    
    void Update () {
        setPenVisuals();
    }
}
