using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullScreenHud : MonoBehaviour {
    GameObject Tunner;
    GameObject Tunner2;
    GameObject dotedCircle;
    GameObject halfInnerCircle;
    GameObject faddedCircle;
    GameObject gaddet;
    // Use this for initialization
    void Start () {
        Tunner = transform.GetChild(0).gameObject;
        Tunner2 = transform.GetChild(5).gameObject;
        dotedCircle = transform.GetChild(1).gameObject;
        halfInnerCircle = transform.GetChild(2).gameObject;
        faddedCircle = transform.GetChild(3).gameObject;
        gaddet = transform.GetChild(4).gameObject;

    }

    // Update is called once per frame
    void Update () {
        Tunner.transform.Rotate(new Vector3(Tunner.transform.rotation.x, Tunner.transform.rotation.y, 
            Tunner.transform.rotation.z - 2f));

        Tunner2.transform.Rotate(new Vector3(Tunner.transform.rotation.x, Tunner.transform.rotation.y,
            Tunner.transform.rotation.z + 2f));

        halfInnerCircle.transform.Rotate(new Vector3(halfInnerCircle.transform.rotation.x, halfInnerCircle.transform.rotation.y,
            halfInnerCircle.transform.rotation.z - 5f));

        faddedCircle.transform.Rotate(new Vector3(faddedCircle.transform.rotation.x, faddedCircle.transform.rotation.y,
            faddedCircle.transform.rotation.z + 3f));
    }
}
