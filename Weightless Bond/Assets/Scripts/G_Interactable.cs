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
    public float massClamp = 50f; // heavier objects harder to move

    [Header("Throwback")]
    [Tooltip("If ON, use Rigidbody.mass to compute player knockback. If OFF, use Throwback Weight below.")]
    public bool useMassForThrowback = true;

    [Tooltip("Designer-tunable weight used for player knockback when useMassForThrowback is OFF.")]
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

    public float GetThrowbackFactor()
    {
        return useMassForThrowback ? Mathf.Max(0.1f, Body.mass) : Mathf.Max(0.1f, throwbackWeight);
    }
}
