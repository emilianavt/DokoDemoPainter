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
