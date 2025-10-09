using UnityEngine;

[DisallowMultipleComponent]
public class HazardTouchResponder : MonoBehaviour
{
    [Header("What counts as a hazard")]
    [Tooltip("Any collider on these layers will trigger a respawn.")]
    public LayerMask hazardLayers;

    [Tooltip("Also treat objects with this component as hazards (optional).")]
    public bool useHazardComponentCheck = true;

    [Header("Respawn Settings")]
    public bool fullHealOnRespawn = true;
    public float contactCooldown = 0.15f;

    private float _lastHitTime = -999f;
    private PlayerHealth _health;

    void Awake()
    {
        _health = GetComponent<PlayerHealth>();
        if (_health == null)
            Debug.LogWarning("HazardTouchResponder: PlayerHealth not found on player.");
    }

    // Called only on the CharacterController object when it hits colliders
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (Time.time - _lastHitTime < contactCooldown) return;
        if (_health == null) return;

        var other = hit.collider;
        bool layerMatch = ((1 << other.gameObject.layer) & hazardLayers) != 0;
        bool hasHazardComponent = useHazardComponentCheck && other.GetComponent<HazardRespawn>() != null;

        if (!layerMatch && !hasHazardComponent) return;

        _lastHitTime = Time.time;
        _health.RespawnImmediate(fullHealOnRespawn);
        Debug.Log($"Hit hazard '{other.name}' → respawned at last checkpoint.");
    }
}
