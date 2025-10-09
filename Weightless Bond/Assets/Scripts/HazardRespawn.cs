using UnityEngine;

[DisallowMultipleComponent]
public class HazardRespawn : MonoBehaviour
{
    [Tooltip("If true, player gets full health again on respawn.")]
    public bool fullHealOnRespawn = true;

    [Tooltip("Seconds before the same collider can trigger another respawn (to avoid multiple triggers in one frame).")]
    public float contactCooldown = 0.2f;

    private float lastHitTime = -999f;

    void OnCollisionEnter(Collision collision)
    {
        // Only trigger on valid physical collisions
        if (Time.time - lastHitTime < contactCooldown)
            return;

        var health = collision.collider.GetComponentInParent<PlayerHealth>();
        if (health == null)
            return;

        lastHitTime = Time.time;
        health.RespawnImmediate(fullHealOnRespawn);

        Debug.Log($"Player hit {name} → Respawned at last checkpoint");
    }
}
