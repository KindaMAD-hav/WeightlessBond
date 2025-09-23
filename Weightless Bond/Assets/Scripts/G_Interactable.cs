// G_Interactable.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class G_Interactable : MonoBehaviour
{
    public Rigidbody Body { get; private set; }
    public Highlighter Highlighter { get; private set; }

    [Header("Interactable Settings")]
    public bool allowRotation = true;
    public float maxPickupDistance = 8f;
    public float maxHoldDistance = 12f;
    public float massClamp = 50f;

    [Header("Throwback (player knockback tuning)")]
    [Tooltip("If ON, use Rigidbody.mass for momentum transfer. If OFF, use Throwback Weight below.")]
    public bool useMassForThrowback = true;

    [Tooltip("Acts like a designer mass for player knockback when not using real mass.")]
    public float throwbackWeight = 1f;

    void Awake()
    {
        Body = GetComponent<Rigidbody>();
        Highlighter = GetComponent<Highlighter>() ?? gameObject.AddComponent<Highlighter>();
    }

    public void OnFocus(bool focused)
    {
        if (Highlighter) Highlighter.SetHighlighted(focused);
    }

    // Effective 'mass' used for player knockback
    public float GetThrowbackMassLike() =>
        Mathf.Max(0.01f, useMassForThrowback ? Body.mass : throwbackWeight);
}
