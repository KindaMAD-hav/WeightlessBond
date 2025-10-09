using UnityEngine;

[DisallowMultipleComponent]
public class Highlighter : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Renderers to highlight. If empty, collected from children on Awake().")]
    [SerializeField] private Renderer[] renderers;

    [Header("Appearance")]
    [Tooltip("Material to use while highlighted.")]
    [SerializeField] private Material highlightMaterial;

    private Material[][] originalMaterials; // store original per renderer
    private bool isHighlighted = false;

    [Header("Debug")]
    [Tooltip("When checked, stays highlighted in Play Mode without any other script.")]
    [SerializeField] private bool debugHighlight = false;

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(includeInactive: false);

        // Save original materials
        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                originalMaterials[i] = renderers[i].materials;
        }
    }

    void Update()
    {
        if (debugHighlight)
        {
            SetHighlighted(true);
        }
        else if (!isHighlighted)
        {
            // Ensure reverted if no one called SetHighlighted this frame
            SetHighlighted(false);
        }

        isHighlighted = false; // reset, must be re-enabled each frame
    }

    /// <summary>Call this every frame while aimed/selected.</summary>
    public void SetHighlighted(bool on)
    {
        if (on)
        {
            ApplyHighlight();
            isHighlighted = true;
        }
        else
        {
            RestoreOriginal();
        }
    }

    [ContextMenu("Force Highlight On")]
    private void ForceOn()
    {
        ApplyHighlight();
        isHighlighted = true;
    }

    [ContextMenu("Force Highlight Off")]
    private void ForceOff()
    {
        RestoreOriginal();
        isHighlighted = false;
    }

    private void ApplyHighlight()
    {
        if (highlightMaterial == null) return;

        foreach (var r in renderers)
        {
            if (!r) continue;
            var mats = new Material[r.materials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = highlightMaterial;
            r.materials = mats;
        }
    }

    private void RestoreOriginal()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && originalMaterials[i] != null)
                renderers[i].materials = originalMaterials[i];
        }
    }
}
