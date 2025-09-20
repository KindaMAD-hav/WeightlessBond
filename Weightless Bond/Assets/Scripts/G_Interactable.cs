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

    void Awake()
    {
        Body = GetComponent<Rigidbody>();
        Highlighter = GetComponent<Highlighter>() ?? gameObject.AddComponent<Highlighter>();
    }

    public void OnFocus(bool focused)
    {
        if (Highlighter) Highlighter.SetHighlighted(focused);
    }
}
