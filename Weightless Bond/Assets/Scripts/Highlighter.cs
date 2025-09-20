// Highlighter.cs
using UnityEngine;

[DisallowMultipleComponent]
public class Highlighter : MonoBehaviour
{
    [SerializeField] Renderer[] renderers;
    [SerializeField] string emissionColorName = "_EmissionColor";
    [SerializeField] Color onColor = new Color(0.3f, 0.7f, 1f, 1f) * 2f; // bright
    [SerializeField] float fadeSpeed = 12f;

    MaterialPropertyBlock _mpb;
    float _t; // 0..1

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
        foreach (var r in renderers) r.material.EnableKeyword("_EMISSION");
        Set(0f, true);
    }

    public void SetHighlighted(bool on) => _t = Mathf.MoveTowards(_t, on ? 1f : 0f, Time.deltaTime * fadeSpeed);

    void Update() => SetHighlighted(false); // default fade; GravityHand will call true while aimed

    void Set(float val, bool force = false)
    {
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(emissionColorName, Color.Lerp(Color.black, onColor, val));
            r.SetPropertyBlock(_mpb);
        }
        if (!force) return;
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(emissionColorName, Color.black);
            r.SetPropertyBlock(_mpb);
        }
    }

    void LateUpdate()
    {
        // apply current interpolated value
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(emissionColorName, Color.Lerp(Color.black, onColor, _t));
            r.SetPropertyBlock(_mpb);
        }
    }
}
