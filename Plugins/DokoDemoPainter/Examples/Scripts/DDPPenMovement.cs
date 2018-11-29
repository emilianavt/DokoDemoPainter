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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DDPPenMovement : MonoBehaviour {
	public Vector2 minPosition;
	public Vector2 maxPosition;
	public float stepSize = 0.1f;
	private bool lastJump = false;
	private DokoDemoPainterPen pen;

	void Start() {
		pen = GetComponent<DokoDemoPainterPen>();
	}

	void FixedUpdate() {
		Vector3 move = new Vector3();
		float axis;
		axis = Input.GetAxis("Horizontal");
		if (axis > 0.0f) {
			move.x += stepSize;
		}
		if (axis < 0.0f) {
			move.x -= stepSize;
		}
		axis = Input.GetAxis("Vertical");
		if (axis > 0.0f) {
			move.y += stepSize;
		}
		if (axis < 0.0f) {
			move.y -= stepSize;
		}
		Vector3 pos = move.normalized * stepSize + transform.localPosition;
		if (pos.x > maxPosition.x) {
			pos.x = maxPosition.x;
		}
		if (pos.x < minPosition.x) {
			pos.x = minPosition.x;
		}
		if (pos.y > maxPosition.y) {
			pos.y = maxPosition.y;
		}
		if (pos.y < minPosition.y) {
			pos.y = minPosition.y;
		}
		transform.localPosition = pos;
		if (Input.GetAxis("Jump") != 0) {
			if (!lastJump) {
				pen.penDown = !pen.penDown;
			}
			lastJump = true;
		} else {
			lastJump = false;
		}
	}
}
