using UnityEngine;

[DisallowMultipleComponent]
public class HazardRespawn : MonoBehaviour
{
    [Tooltip("If true, reacts to triggers; otherwise, reacts to solid collisions.")]
    public bool useTrigger = true;

    [Tooltip("Full heal on respawn.")]
    public bool fullHealOnRespawn = true;

    [Tooltip("Optional cooldown so a grazing contact doesn't spam multiple respawns.")]
    public float contactCooldown = 0.15f;

    private float lastHitTime = -999f;

    void OnTriggerEnter(Collider other)
    {
        if (useTrigger) TryRespawn(other);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!useTrigger) TryRespawn(collision.collider);
    }

    void TryRespawn(Collider col)
    {
        if (Time.time - lastHitTime < contactCooldown) return;
        var health = col.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        lastHitTime = Time.time;
        health.RespawnImmediate(fullHealOnRespawn);
        Debug.Log("Hazard touched -> instant respawn.");
    }
}
