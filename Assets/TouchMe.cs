using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchMe : MonoBehaviour
{
    public Light light1;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "RightIndex")
        {
            if (light1.enabled == false)
            {
                light1.enabled = true;
            }
            else
            {
                light1.enabled = false;
            }
        }
    }
}
