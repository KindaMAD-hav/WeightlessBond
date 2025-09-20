// Highlighter.cs
// URP-friendly emission highlighter with debug toggle & context menu.
// - Add to any interactable (or let G_Interactable add it).
// - Call SetHighlighted(true) each frame while aimed.
// - Or tick "Debug Highlight" in the Inspector to test without gameplay code.

using UnityEngine;

[DisallowMultipleComponent]
public class Highlighter : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Renderers to highlight. If empty, collected from children on Awake().")]
    [SerializeField] private Renderer[] renderers;

    [Header("Appearance")]
    [Tooltip("Emission color when highlighted (intensity > 1 looks brighter).")]
    [ColorUsage(showAlpha: true, hdr: true)]
    [SerializeField] private Color onColor = new Color(0.3f, 0.7f, 1f, 1f) * 2f;

    [Tooltip("How fast the highlight fades in/out.")]
    [SerializeField, Range(1f, 30f)] private float fadeSpeed = 12f;

    [Header("Debug")]
    [Tooltip("When checked, stays highlighted in Play Mode without any other script.")]
    [SerializeField] private bool debugHighlight = false;

    // Shader property name (URP/Lit uses _EmissionColor)
    [SerializeField, Tooltip("Shader emission color property.")]
    private string emissionColorName = "_EmissionColor";

    // Internals
    private MaterialPropertyBlock _mpb;
    private float _current;  // 0..1 current visual intensity
    private float _target;   // 0..1 desired intensity for this frame

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(includeInactive: false);

        _mpb = new MaterialPropertyBlock();

        // Ensure emission is enabled on all materials (URP strips otherwise)
        foreach (var r in renderers)
        {
            if (!r) continue;
            // Using .materials here is fine in editor; at runtime this creates instances.
            // We only need to ensure the keyword once; subsequent frames use MPB.
            foreach (var m in r.materials)
            {
                if (!m) continue;
                m.EnableKeyword("_EMISSION");
            }
        }

        // Start fully off
        ApplyEmission(0f, forceAll: true);
    }

    void Update()
    {
        // If in debug mode, force target on.
        if (debugHighlight) _target = 1f;

        // Smooth step toward target
        _current = Mathf.MoveTowards(_current, _target, fadeSpeed * Time.deltaTime);

        // Apply and then reset target so other scripts must call SetHighlighted(true) each frame
        ApplyEmission(_current, forceAll: false);
        _target = 0f; // IMPORTANT: resets unless someone calls SetHighlighted(true) again this frame
    }

    /// <summary>
    /// Request highlight this frame. Call every frame while aimed/selected.
    /// </summary>
    public void SetHighlighted(bool on)
    {
        if (on) _target = 1f;
        // we don't set target to 0 here; leaving Update() to decay naturally
    }

    /// <summary>
    /// Immediately forces highlight ON for quick testing (Inspector context menu).
    /// </summary>
    [ContextMenu("Force Highlight On")]
    private void ForceOn()
    {
        _current = 1f;
        _target = 1f;
        ApplyEmission(_current, forceAll: true);
    }

    /// <summary>
    /// Immediately forces highlight OFF for quick testing (Inspector context menu).
    /// </summary>
    [ContextMenu("Force Highlight Off")]
    private void ForceOff()
    {
        _current = 0f;
        _target = 0f;
        ApplyEmission(_current, forceAll: true);
    }

    private void ApplyEmission(float t, bool forceAll)
    {
        // Lerp from black (off) to the chosen onColor
        Color c = Color.Lerp(Color.black, onColor, t);

        foreach (var r in renderers)
        {
            if (!r) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(emissionColorName, c);
            r.SetPropertyBlock(_mpb);
        }

        // Optionally push once more in case some renderers were missing a block
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
