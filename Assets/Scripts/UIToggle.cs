using UnityEngine;
using UnityEngine.UI;

public class ToggleHandler : MonoBehaviour
{
    public Toggle myToggle;

    public void ActivateToggle()
    {
        if (myToggle != null)
        {
            myToggle.isOn = true;
        }
    }

    public void DeactivateToggle()
    {
        if (myToggle != null)
        {
            myToggle.isOn = false;
        }
    }
}
