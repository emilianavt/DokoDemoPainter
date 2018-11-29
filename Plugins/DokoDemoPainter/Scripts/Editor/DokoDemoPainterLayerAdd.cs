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