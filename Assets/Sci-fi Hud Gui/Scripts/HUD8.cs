using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUD8 : MonoBehaviour {
    GameObject innerGlow;
    GameObject outerGlow;
    // Use this for initialization
    void Start () {
        innerGlow = transform.GetChild(0).gameObject;
        outerGlow = transform.GetChild(1).gameObject;
    }
	
	// Update is called once per frame
	void Update () {
        innerGlow.transform.Rotate(new Vector3(innerGlow.transform.rotation.x, innerGlow.transform.rotation.y,
            innerGlow.transform.rotation.z - 10f));
        outerGlow.transform.Rotate(new Vector3(outerGlow.transform.rotation.x, outerGlow.transform.rotation.y, 
            outerGlow.transform.rotation.z + 10f));

    }
}
