// XRPerceptionEffectsController.cs
using UnityEngine;


[DefaultExecutionOrder(-100)]
public class XRPerceptionEffectsController : MonoBehaviour
{
    [Range(0, 1)] public float symptomIntensity = 0f; // master knob from gameplay


    [Header("Mappings")]
    public AnimationCurve vignetteByIntensity = AnimationCurve.Linear(0, 0, 1, 0.7f);
    public AnimationCurve contrastByIntensity = AnimationCurve.Linear(0, 0, 1, -0.35f);
    public AnimationCurve desatByIntensity = AnimationCurve.Linear(0, 0, 1, 0.45f);
    public AnimationCurve swayByIntensity = AnimationCurve.Linear(0, 0, 1, 0.35f); // world sway amp
    public AnimationCurve blurByIntensity = AnimationCurve.Linear(0, 0, 1, 0.85f);


    int _vignetteID = Shader.PropertyToID("_VignetteStrength");
    int _contrastID = Shader.PropertyToID("_Contrast");
    int _desatID = Shader.PropertyToID("_Desaturation");
    int _maskBlurID = Shader.PropertyToID("_MaskedBlurStrength");


    void Update()
    {
        Shader.SetGlobalFloat(_vignetteID, vignetteByIntensity.Evaluate(symptomIntensity));
        Shader.SetGlobalFloat(_contrastID, contrastByIntensity.Evaluate(symptomIntensity));
        Shader.SetGlobalFloat(_desatID, desatByIntensity.Evaluate(symptomIntensity));
        Shader.SetGlobalFloat(_maskBlurID, blurByIntensity.Evaluate(symptomIntensity));
    }
}