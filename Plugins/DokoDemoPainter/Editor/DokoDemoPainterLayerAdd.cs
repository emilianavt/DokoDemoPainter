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

using UnityEngine;
using System.Collections;
using UnityEditor;
 
[InitializeOnLoad]
public class DokoDemoPainterLayerAdd {
	static DokoDemoPainterLayerAdd() {
		if (LayerMask.NameToLayer("DokoDemoPainter") != -1) {
			return;
		}
		CreateLayer();
	}
	static void CreateLayer() {
		SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
 
		SerializedProperty layers = tagManager.FindProperty("layers");
		if (layers == null || !layers.isArray)
		{
			Debug.LogWarning("Can't set up the layers automatically.  It's possible the format of the layers and tags data has changed in this version of Unity.");
			Debug.LogWarning("Layers is null: " + (layers == null));
			return;
		}

		for (int i = 8; i < layers.arraySize; i++)
		{
			SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
			if (layerSP.stringValue == "") {
				layerSP.stringValue = "DokoDemoPainter";
				break;
			}
		}

		tagManager.ApplyModifiedProperties();
	}
}