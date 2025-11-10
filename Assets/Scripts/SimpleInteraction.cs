using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleInteraction : MonoBehaviour
{
    public Rigidbody Ball;
    bool fire = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float triggerLeft = OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger);
        float triggerRight = OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger);

        //Debug.Log($"Left Trigger: {triggerLeft}, Right Trigger: {triggerRight}");

        if (triggerRight > 0.9f && !fire)
        {
            fire = true;
            Debug.Log("Fire!");
            Instantiate(Ball, new Vector3(Random.Range(3, 5), Random.Range(1, 2), Random.Range(8, 9)), Quaternion.identity);
        }

        if (fire && triggerRight < 0.1f)
        {
            fire = false;
        }
        if (triggerLeft > 0.9f && !fire)
        {
            fire = true;
            Debug.Log("Fire!");
            Instantiate(Ball, new Vector3(Random.Range(3, 5), Random.Range(1, 2), Random.Range(8, 9)), Quaternion.identity);
        }

        if (fire && triggerLeft < 0.1f)
        {
            fire = false;
        }
    }
}
