using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayColor : MonoBehaviour {

    private Material mat;

	// Use this for initialization
	void Start () {
        mat = gameObject.GetComponent<Renderer>().material;
        mat.SetTextureScale("_MainTex", new Vector2(-1, 1));
	}
	
	// Update is called once per frame
	void Update () {
	}
}
