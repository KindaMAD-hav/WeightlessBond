using UnityEngine;

[DisallowMultipleComponent]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Optional: exact spawn pose. If null, this object's transform is used.")]
    public Transform spawnTransform;

    [Tooltip("Play only once, then stop reacting after it becomes the active checkpoint.")]
    public bool oneTime = false;

    [Tooltip("Optional SFX when a NEW checkpoint is reached.")]
    public AudioClip reachedSfx;

    void OnTriggerEnter(Collider other) { TrySetCheckpoint(other); }
    void OnCollisionEnter(Collision c) { TrySetCheckpoint(c.collider); }

    void TrySetCheckpoint(Collider col)
    {
        var health = col.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        Transform target = spawnTransform != null ? spawnTransform : transform;

        // Only react when switching to a DIFFERENT checkpoint
        if (health.GetRespawnPoint() == target)
            return;

        health.SetRespawnPoint(target);

        if (reachedSfx) AudioSource.PlayClipAtPoint(reachedSfx, target.position);
        if (oneTime) enabled = false;

        Debug.Log($"Checkpoint set → {target.name}");
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var t = spawnTransform ? spawnTransform : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(t.position, 0.25f);
        Gizmos.DrawLine(t.position, t.position + t.forward * 0.8f);
    }
#endif
}
