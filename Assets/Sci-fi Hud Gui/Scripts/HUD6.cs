using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUD6 : MonoBehaviour {
    GameObject Glow;

    // Use this for initialization
    void Start () {
        Glow = transform.GetChild(0).gameObject;

    }

    // Update is called once per frame
    void Update () {
        Glow.transform.Rotate(new Vector3(Glow.transform.rotation.x, Glow.transform.rotation.y, Glow.transform.rotation.z - 10f));

    }
}
