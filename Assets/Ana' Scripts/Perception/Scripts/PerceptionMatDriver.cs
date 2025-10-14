using UnityEngine;

public class PerceptionMatDriver : MonoBehaviour
{
    [Header("Assign the same material used in the Full Screen Pass")]
    public Material perceptionMat;

    [Range(0, 1)] public float symptomIntensity = 0f;

    // Curves: tweak to taste
    public AnimationCurve vignetteByIntensity = AnimationCurve.Linear(0, 0.0f, 1, 0.7f);
    public AnimationCurve contrastByIntensity = AnimationCurve.Linear(0, 0.0f, 1, -0.35f);
    public AnimationCurve desatByIntensity = AnimationCurve.Linear(0, 0.0f, 1, 0.45f);

    void Reset()
    {
        // Try auto-find a material named Mat_PerceptionPost
        if (!perceptionMat)
            perceptionMat = Resources.Load<Material>("Mat_PerceptionPost");
    }

    void Update()
    {
        if (!perceptionMat) return;

        perceptionMat.SetFloat("_VignetteStrength", vignetteByIntensity.Evaluate(symptomIntensity));
        perceptionMat.SetFloat("_Contrast", contrastByIntensity.Evaluate(symptomIntensity));
        perceptionMat.SetFloat("_Desaturation", desatByIntensity.Evaluate(symptomIntensity));
    }
}
