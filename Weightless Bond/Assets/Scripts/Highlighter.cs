using UnityEngine;
using UnityEngine.UI; // for RawImage

[DisallowMultipleComponent]
public class Highlighter : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Renderer[] renderers;

    [Header("Appearance")]
    [ColorUsage(true, true)]
    [SerializeField] private Color onColor = new Color(0.3f, 0.7f, 1f, 1f) * 2f;
    [SerializeField, Range(1f, 30f)] private float fadeSpeed = 12f;

    [Header("UI Feedback")]
    [Tooltip("UI element that appears when this object is highlighted.")]
    public RawImage highlightUI;

    [Header("Debug")]
    [SerializeField] private bool debugHighlight = false;

    [SerializeField] private string emissionColorName = "_EmissionColor";

    private MaterialPropertyBlock _mpb;
    private float _current;
    private float _target;

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(false);

        _mpb = new MaterialPropertyBlock();

        foreach (var r in renderers)
        {
            if (!r) continue;
            foreach (var m in r.materials)
                if (m) m.EnableKeyword("_EMISSION");
        }

        ApplyEmission(0f, true);

        // hide UI at start
        if (highlightUI != null)
            highlightUI.enabled = false;
    }

    void Update()
    {
        if (debugHighlight) _target = 1f;

        _current = Mathf.MoveTowards(_current, _target, fadeSpeed * Time.deltaTime);
        ApplyEmission(_current, false);

        // toggle UI
        if (highlightUI != null)
            highlightUI.enabled = _current > 0.01f;

        _target = 0f;
    }

    public void SetHighlighted(bool on)
    {
        if (on) _target = 1f;
    }

    private void ApplyEmission(float t, bool forceAll)
    {
        Color c = Color.Lerp(Color.black, onColor, t);
        foreach (var r in renderers)
        {
            if (!r) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(emissionColorName, c);
            r.SetPropertyBlock(_mpb);
        }

        if (forceAll)
        {
            foreach (var r in renderers)
            {
                if (!r) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(emissionColorName, c);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
