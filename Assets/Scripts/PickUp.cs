using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUp : MonoBehaviour
{
    private Transform controller;
    private bool isTouching = false;
    private bool isPickedUp = false;
    private Vector3 startPos;
    private Quaternion startRot;

    void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation;
    }

    void Update()
    {
        // If object is currently picked up, follow controller
        if (isPickedUp && controller != null)
        {
            transform.position = controller.position;
            transform.rotation = controller.rotation;
        }
        else if (!isPickedUp)
        {
            // Optional: reset to start position
            transform.position = startPos;
            transform.rotation = startRot;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Controller"))
        {
            controller = other.transform;
            isTouching = true;
            Debug.Log($"{name} touched by controller");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Controller"))
        {
            isTouching = false;
            controller = null;
            Debug.Log($"{name} released from controller trigger");
        }
    }

    public void TryPickup(bool triggerPressed)
    {
        if (triggerPressed && isTouching)
        {
            isPickedUp = true;
            Debug.Log($"{name} picked up");
        }
        else if (!triggerPressed && isPickedUp)
        {
            isPickedUp = false;
            Debug.Log($"{name} dropped");
        }
    }
}
