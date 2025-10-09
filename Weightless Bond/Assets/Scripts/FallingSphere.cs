using UnityEngine;

public class FallingSphere : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damage = 20f;
    public bool destroyOnImpact = true;
    public bool bypassIFrames = true;   // make hits always count (ignores invincibility)

    [Header("Visual / Audio")]
    public GameObject impactEffect;     // optional
    public GameObject trailEffect;      // optional
    public AudioClip impactSound;
    public AudioClip whistleSound;

    [Header("Physics")]
    public float fallSpeed = 10f;
    public float lifeTime = 10f;

    Rigidbody rb;
    AudioSource audioSource;
    bool hasImpacted = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.down * fallSpeed;                    // FIX
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        if (audioSource && whistleSound) audioSource.PlayOneShot(whistleSound);

        Destroy(gameObject, lifeTime);
    }

    // Make sure the sphere's SphereCollider is set to IsTrigger = true
    void OnTriggerEnter(Collider other)
    {
        if (hasImpacted) return;
        hasImpacted = true;

        // Damage player once
        if (other.CompareTag("Player"))
        {
            var ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                if (bypassIFrames) ph.TakeDamage(damage, ignoreInvincibility: true);
                else ph.TakeDamage(damage);
            }
        }

        // VFX/SFX (optional)
        if (impactEffect) { var fx = Instantiate(impactEffect, transform.position, Quaternion.identity); Destroy(fx, 3f); }
        if (audioSource && impactSound) audioSource.PlayOneShot(impactSound);

        // Clean up
        if (destroyOnImpact)
        {
            if (trailEffect) { trailEffect.transform.SetParent(null); Destroy(trailEffect, 2f); }
            var rend = GetComponent<Renderer>(); if (rend) rend.enabled = false;
            var col = GetComponent<Collider>(); if (col) col.enabled = false;
            Destroy(gameObject, 1f);
        }
    }
}
