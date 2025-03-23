using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUD1 : MonoBehaviour {
     GameObject outerCircle;
     GameObject cntrCircle;
     GameObject innerCircle;
     GameObject vLine;
     GameObject vLineB;
     GameObject bLine;
    // Use this for initialization
    void Start () {
        outerCircle = transform.GetChild(0).gameObject;
        cntrCircle = transform.GetChild(1).gameObject;
        innerCircle = transform.GetChild(2).gameObject;
        vLine = transform.GetChild(3).gameObject;
        vLineB = transform.GetChild(4).gameObject;
        bLine = transform.GetChild(5).gameObject;
    }
	
	// Update is called once per frame
	void Update () {
        vLine.transform.Rotate(new Vector3(vLine.transform.rotation.x, vLine.transform.rotation.y, vLine.transform.rotation.z + 5f));
        vLineB.transform.Rotate(new Vector3(vLineB.transform.rotation.x, vLineB.transform.rotation.y, vLineB.transform.rotation.z - 5f));
        bLine.transform.Rotate(new Vector3(bLine.transform.rotation.x, bLine.transform.rotation.y, bLine.transform.rotation.z - 2f));
        innerCircle.transform.Rotate(new Vector3(innerCircle.transform.rotation.x, innerCircle.transform.rotation.y, innerCircle.transform.rotation.z + 2f));

    }
}
