using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRCameraSwitcher : MonoBehaviour
{
    public Transform[] viewpoints;
    private int currentIndex = 0;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentIndex = (currentIndex + 1) % viewpoints.Length;
            TeleportToViewpoint(viewpoints[currentIndex]);
        }
    }

    void TeleportToViewpoint(Transform target)
    {
        transform.position = target.position;
        transform.rotation = target.rotation;
    }
}