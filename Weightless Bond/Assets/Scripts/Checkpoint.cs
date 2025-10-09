using UnityEngine;

[DisallowMultipleComponent]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Optional: use a different transform (empty) for the exact spawn pose. If null, this object’s transform is used.")]
    public Transform spawnTransform;

    [Tooltip("Play once then stop reacting.")]
    public bool oneTime = false;

    [Tooltip("Optional SFX when checkpoint is reached.")]
    public AudioClip reachedSfx;

    void OnTriggerEnter(Collider other)
    {
        TrySetCheckpoint(other);
    }

    void OnCollisionEnter(Collision collision)
    {
        TrySetCheckpoint(collision.collider);
    }

    void TrySetCheckpoint(Collider col)
    {
        var health = col.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        health.SetRespawnPoint(spawnTransform != null ? spawnTransform : transform);

        if (reachedSfx) AudioSource.PlayClipAtPoint(reachedSfx, transform.position);
        if (oneTime) enabled = false;

        Debug.Log($"Checkpoint set to {(spawnTransform ? spawnTransform.name : name)}.");
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
