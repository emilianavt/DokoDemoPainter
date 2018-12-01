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

public class DDPStampController : MonoBehaviour {
    [Header("General settings")]
    [Tooltip("This value sets the minimum distance in world coordinates the stamp has to move before being automatically enabled again after stamping. Setting a negative value disables this function.")]
    public float minimumMovement = 0.1f;
    [Header("Stamp prefab settings")]
    [Tooltip("This field is optional. If set, it has to be the default stamp prefab's MeshRenderer.")]
    public MeshRenderer stampMesh = null;
    [Tooltip("This field determines how often to update the stamp's appearance. Its value is given in seconds.")]
    public float updateInterval = 5.0f;
    private DokoDemoPainterStamp stamp;
    private bool switched = false;
    private bool last;
    private Vector3 pos;
    private float delta;

    void setStampVisuals() {
        if (stampMesh != null) {
            delta += Time.deltaTime;
            if (delta < updateInterval) {
                return;
            }
            Material[] materials = stampMesh.materials;
            materials[2].mainTexture = stamp.stampTexture;
            if (!stamp.enableTint) {
                materials[2].color = Color.white;
            } else {
                materials[2].color = stamp.tintColor;
            }
            stampMesh.materials = materials;
            delta = 0.0f;
        }
    }

    void Start () {
        stamp = GetComponent<DokoDemoPainterStamp>();
        last = stamp.stampActive;
        delta = updateInterval;
        setStampVisuals();
    }
    
    void Update () {
        setStampVisuals();
        if (last && !stamp.stampActive) {
            switched = true;
            pos = transform.position;
        }
        last = stamp.stampActive;
        if (minimumMovement < 0.0f) {
            return;
        }
        if (switched && Vector3.Distance(pos, transform.position) > minimumMovement) {
            switched = false;
            stamp.stampActive = true;
            last = true;
        }
    }
}
