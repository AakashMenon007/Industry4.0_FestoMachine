using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUD2 : MonoBehaviour {
    GameObject outerCircle;
    GameObject innerCircle;
    GameObject Glow;
    // Use this for initialization
    void Start () {
        outerCircle = transform.GetChild(0).gameObject;
        innerCircle = transform.GetChild(1).gameObject;
        Glow = transform.GetChild(2).gameObject;

    }

    // Update is called once per frame
    void Update () {
        innerCircle.transform.Rotate(new Vector3(innerCircle.transform.rotation.x, innerCircle.transform.rotation.y,
        innerCircle.transform.rotation.z + 2f));
        Glow.transform.Rotate(new Vector3(Glow.transform.rotation.x, Glow.transform.rotation.y, Glow.transform.rotation.z - 2f));

    }
}
