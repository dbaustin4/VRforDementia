using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUp : MonoBehaviour
{
    private Transform controller;
    private bool isTouching = false;
    private bool isPickedUp = false;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (isPickedUp && controller != null)
        {
            // Make the object follow the controller
            transform.position = controller.position;
            transform.rotation = controller.rotation;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Controller"))
        {
            isTouching = true;
            controller = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Controller"))
        {
            isTouching = false;
            controller = null;
        }
    }

    public void TryPickup(bool triggerPressed)
    {
        if (triggerPressed && isTouching)
        {
            // Pick up
            isPickedUp = true;
            if (rb != null) rb.isKinematic = true; // disable physics while held
            Debug.Log($"{name} picked up");
        }
        else if (!triggerPressed && isPickedUp)
        {
            // Drop
            isPickedUp = false;
            if (rb != null) rb.isKinematic = false; // re-enable physics
            controller = null;
            Debug.Log($"{name} dropped");
        }
    }
}

