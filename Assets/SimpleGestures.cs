using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SimpleGestures : MonoBehaviour
{
    public GameObject RightIndex;
    public GameObject RightMiddle;
    public GameObject RightThumb;
    public GameObject RightPalm;

    public TMP_Text debugData;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        debugData.text = Vector3.Distance(RightIndex.transform.position, RightPalm.transform.position).ToString("0.000");

        if (collision(RightIndex, RightPalm, 0.026f))
        {
            debugData.text += "\n Closed Hand";
        }

        if (collision(RightIndex, RightPalm, 0.035f) == false && collision(RightMiddle, RightPalm, 0.035f) && collision(RightMiddle, RightThumb, 0.035f))
        {
            debugData.text += "\n Pointing";
        }

        if (collision(RightIndex, RightPalm, 0.035f) == false && collision(RightMiddle, RightPalm, 0.035f) == true && collision(RightMiddle, RightThumb, 0.035f) == false)
        {
            debugData.text += "\n BARCOLA";
        }
    }

    bool collision(GameObject go1, GameObject go2, float distance)
    {
        if(Vector3.Distance(go1.transform.position, go2.transform.position) < distance)
        {
            return true;
        }
        return false;
    }
}
