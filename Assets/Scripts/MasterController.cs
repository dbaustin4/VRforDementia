using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MasterController : MonoBehaviour
{
    void Update()
    {
        float rightPress = OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger);
        bool triggerPressed = rightPress > 0.9f;

        // Tell all pickable objects whether trigger is pressed
        foreach (PickUp pickable in FindObjectsOfType<PickUp>())
        {
            pickable.TryPickup(triggerPressed);
        }
    }
}
