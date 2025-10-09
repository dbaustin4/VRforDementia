using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUp : MonoBehaviour
{
    private Transform controller;
    private Collider playerCapsuleCollider;
    private Rigidbody rb;
    private bool isTouching = false;
    private bool isPickedUp = false;
    private bool wasTriggerPressed = false;
    private Vector3 lastPosition;
    private Vector3 controllerVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (controller != null)
        {
            controllerVelocity = (controller.position - lastPosition) / Time.deltaTime;
            lastPosition = controller.position;
        }
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
            controller = other.transform;
            playerCapsuleCollider = other.GetComponent<Collider>();
            lastPosition = controller.position;
            isTouching = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Controller"))
        {
            isTouching = false;
        }
    }

    public void TryPickup(bool triggerPressed)
    {
        if (triggerPressed && !wasTriggerPressed)
        {
            if (isTouching && !isPickedUp)
            {
                isPickedUp = true;
                rb.isKinematic = true;
                rb.useGravity = false;
                if(playerCapsuleCollider && GetComponent<Collider>())
                {
                    Physics.IgnoreCollision(playerCapsuleCollider, GetComponent<Collider>(), true);
                }
            }
            else if (isPickedUp)
            {
                isPickedUp = false;
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.velocity = controllerVelocity;
                if(playerCapsuleCollider && GetComponent<Collider>())
                {
                    Physics.IgnoreCollision(playerCapsuleCollider, GetComponent<Collider>(), false);
                }
            }
        }
        wasTriggerPressed = triggerPressed;
    }
}

