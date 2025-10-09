// TMPWobble.cs – subtle vertex jitter to break legibility without blur
using UnityEngine;
using TMPro;
public class TMPWobble : MonoBehaviour
{
    public float amp = 0.6f, freq = 2.5f; TMP_Text tmp; TMP_TextInfo info;
    void Awake() { tmp = GetComponent<TMP_Text>(); }
    void LateUpdate()
    {
        if (!tmp) return; tmp.ForceMeshUpdate(); info = tmp.textInfo;
        for (int i = 0; i < info.characterCount; i++)
        {
            var ch = info.characterInfo[i]; if (!ch.isVisible) continue; int v = ch.vertexIndex; int m = ch.materialReferenceIndex;
            var verts = info.meshInfo[m].vertices; float t = Time.time + i * 0.13f;
            Vector3 off = new Vector3(Mathf.PerlinNoise(t, 0) - 0.5f, Mathf.PerlinNoise(0, t) - 0.5f, 0) * amp;
            verts[v + 0] += off; verts[v + 1] += off; verts[v + 2] += off; verts[v + 3] += off; info.meshInfo[m].vertices = verts;
        }
        for (int m = 0; m < info.meshInfo.Length; m++) { var mi = info.meshInfo[m]; mi.mesh.vertices = mi.vertices; tmp.UpdateGeometry(mi.mesh, m); }
    }
}