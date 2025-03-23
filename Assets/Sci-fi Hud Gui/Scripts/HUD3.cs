using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUD3 : MonoBehaviour {
    GameObject outerCircle;
    GameObject middleCircle;
    GameObject innerCircle;

    
    // Use this for initialization
    void Start () {
        outerCircle = transform.GetChild(0).gameObject;
        middleCircle = transform.GetChild(1).gameObject;
        innerCircle = transform.GetChild(2).gameObject;
    }
	
	// Update is called once per frame
	void Update () {
        outerCircle.transform.Rotate(new Vector3(innerCircle.transform.rotation.x, innerCircle.transform.rotation.y,
            innerCircle.transform.rotation.z + 2f));
        innerCircle.transform.Rotate(new Vector3(innerCircle.transform.rotation.x, innerCircle.transform.rotation.y,
            innerCircle.transform.rotation.z - 2f));
        middleCircle.transform.Rotate(new Vector3(middleCircle.transform.rotation.x, middleCircle.transform.rotation.y,
            middleCircle.transform.rotation.z - Random.Range(-2f,2f)));

    }
}
