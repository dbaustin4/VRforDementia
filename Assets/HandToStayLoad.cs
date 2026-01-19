using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HandStayToLoad : MonoBehaviour
{
    public float waitTime = 5.0f;
    public Renderer sphereRenderer;
    public Color neutralColor = Color.white;
    public Color chargedColor = Color.blue;

    public bool isCounting = false;
    private float timer = 0f;

    void Start()
    {
        sphereRenderer.material.color = neutralColor;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "RightPalm" || other.gameObject.tag == "LeftPalm")
        {
            if (isCounting == false)
            {
                isCounting = true;
                StartCoroutine(ChargeAndLoad());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "RightPalm" || other.gameObject.tag == "LeftPalm")
        {
            isCounting = false;
            timer = 0f;
            sphereRenderer.material.color = neutralColor;
            StopAllCoroutines();
        }
    }

    IEnumerator ChargeAndLoad()
    {
        timer = 0f;

        while(timer < waitTime)
        {
            timer += Time.deltaTime;

            float t = timer / waitTime;
            sphereRenderer.material.color = Color.Lerp(neutralColor, chargedColor, t);

            yield return null;

            if (!isCounting)
                yield break;
        }

            int currentScene = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene("Experience Scene");
    }

}