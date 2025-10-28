using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OVRHeadProxy : MonoBehaviour
{
    public Transform centerEyeAnchor;

    void LateUpdate()
    {
        if (centerEyeAnchor)
        {
            transform.position = centerEyeAnchor.position;
            transform.rotation = centerEyeAnchor.rotation;
        }
    }
}

