using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUD4 : MonoBehaviour {
    GameObject ocircle1;
    GameObject ocircle2;
    GameObject ocircle3;
    GameObject ocircle4;
    GameObject Curve;
    float value = 0.0f;

    // Use this for initialization
    void Start () {
        ocircle1 = transform.GetChild(0).gameObject;
        ocircle2 = transform.GetChild(1).gameObject;
        ocircle3 = transform.GetChild(2).gameObject;
        ocircle4 = transform.GetChild(3).gameObject;
        Curve = transform.GetChild(4).gameObject;

    }

    // Update is called once per frame
    void Update () {
        if (!this.transform.gameObject.name.Contains("HUD 5"))
        {
            ocircle1.transform.Rotate(new Vector3(ocircle1.transform.rotation.x, ocircle1.transform.rotation.y,
            ocircle1.transform.rotation.z + 2f));

            ocircle2.transform.Rotate(new Vector3(ocircle2.transform.rotation.x, ocircle2.transform.rotation.y,
            ocircle2.transform.rotation.z + 3f));

            ocircle3.transform.Rotate(new Vector3(ocircle3.transform.rotation.x, ocircle3.transform.rotation.y,
            ocircle3.transform.rotation.z - 2f));

            ocircle4.transform.Rotate(new Vector3(ocircle4.transform.rotation.x, ocircle4.transform.rotation.y,
            ocircle4.transform.rotation.z - 3f));

            if (value < 5)
                value++;
            else
                value = 0.0f;

            Curve.transform.Rotate(new Vector3(Curve.transform.rotation.x, Curve.transform.rotation.y,
            Curve.transform.rotation.z + value));
        }
        else
        {
            Curve.transform.Rotate(new Vector3(Curve.transform.rotation.x, Curve.transform.rotation.y,
            Curve.transform.rotation.z - 3));
        }
    }
}
