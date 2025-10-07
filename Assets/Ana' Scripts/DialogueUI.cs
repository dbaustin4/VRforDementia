using UnityEngine;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [Header("Bindings")]
    public Canvas rootCanvas;           // Set Render Mode: World Space
    public TMP_Text speakerText;
    public TMP_Text bodyText;

    [Header("Follow Settings")]
    public Transform playerHead;        // Assign MainCamera from your XR rig (OVRCameraRig/CenterEyeAnchor)
    public Vector3 offset = new Vector3(0, -0.4f, 1.5f);  // position below center view
    public float followSpeed = 8f;      // how quickly it smooths to new position

    bool readyForAutoAdvance;

    void Awake()
    {
        if (rootCanvas != null) rootCanvas.enabled = false;
        if (playerHead == null && Camera.main != null)
            playerHead = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (playerHead == null || rootCanvas == null) return;

        // desired position = playerHead position + offset in head space
        Vector3 targetPos = playerHead.TransformPoint(offset);
        rootCanvas.transform.position = Vector3.Lerp(
            rootCanvas.transform.position,
            targetPos,
            Time.deltaTime * followSpeed
        );

        // look at player (keep upright)
        Vector3 lookDir = rootCanvas.transform.position - playerHead.position;
        lookDir.y = 0; // optional: keep horizontal
        rootCanvas.transform.rotation = Quaternion.LookRotation(lookDir);
    }

    // ========== Dialogue handling ==========

    public void ShowLine(string speaker, string text)
    {
        if (rootCanvas != null) rootCanvas.enabled = true;
        if (speakerText != null) speakerText.text = string.IsNullOrEmpty(speaker) ? "" : speaker;
        if (bodyText != null) bodyText.text = text;
        readyForAutoAdvance = false;
    }

    public void ShowHint(string hint)
    {
        if (rootCanvas != null) rootCanvas.enabled = true;
        if (speakerText != null) speakerText.text = "";
        if (bodyText != null) bodyText.text = hint;
        readyForAutoAdvance = false;
    }

    public void Hide()
    {
        if (rootCanvas != null) rootCanvas.enabled = false;
        if (speakerText != null) speakerText.text = "";
        if (bodyText != null) bodyText.text = "";
        readyForAutoAdvance = false;
    }

    public void MarkAutoAdvanceWindow()
    {
        readyForAutoAdvance = true;
    }

    public bool ReadyForAutoAdvance() => readyForAutoAdvance;
}
