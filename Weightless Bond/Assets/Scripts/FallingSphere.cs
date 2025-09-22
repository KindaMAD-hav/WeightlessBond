using UnityEngine;

public class FallingSphere : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damage = 20f;
    public float explosionRadius = 2f;
    public bool damageOnImpact = true;
    public bool destroyOnImpact = true;

    [Header("Visual Effects")]
    public GameObject impactEffect; // Particle system for explosion
    public GameObject trailEffect; // Trail for the falling sphere

    [Header("Audio")]
    public AudioClip impactSound;
    public AudioClip whistleSound; // Sound while falling

    [Header("Physics")]
    public float fallSpeed = 10f;
    public float lifeTime = 10f; // Auto-destroy after this time

    private Rigidbody rb;
    private AudioSource audioSource;
    private bool hasImpacted = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // Set initial velocity
        if (rb != null)
        {
            rb.linearVelocity = Vector3.down * fallSpeed;
        }

        // Play whistle sound
        if (audioSource && whistleSound)
        {
            audioSource.PlayOneShot(whistleSound);
        }

        // Auto-destroy after lifetime
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasImpacted) return;

        // Check if hit player
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Debug.Log($"Player hit by falling sphere for {damage} damage!");
            }

            Impact(other.transform.position);
        }
        // Check if hit ground or other objects
        else if (other.CompareTag("Ground") || other.CompareTag("Terrain"))
        {
            Impact(transform.position);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasImpacted) return;

        // Alternative collision detection
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }

        Impact(collision.contacts[0].point);
    }

    void Impact(Vector3 impactPosition)
    {
        if (hasImpacted) return;
        hasImpacted = true;

        // Area damage (optional - damages player if within explosion radius)
        if (explosionRadius > 0)
        {
            Collider[] hitColliders = Physics.OverlapSphere(impactPosition, explosionRadius);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        // Calculate distance-based damage
                        float distance = Vector3.Distance(impactPosition, hitCollider.transform.position);
                        float damageMultiplier = Mathf.Clamp01(1 - (distance / explosionRadius));
                        float areaDamage = damage * 0.5f * damageMultiplier; // 50% damage for area effect

                        playerHealth.TakeDamage(areaDamage);
                        Debug.Log($"Player hit by explosion for {areaDamage} damage!");
                    }
                }
            }
        }

        // Visual effects
        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, impactPosition, Quaternion.identity);
            Destroy(effect, 3f);
        }

        // Audio
        if (audioSource && impactSound)
        {
            audioSource.PlayOneShot(impactSound);
        }

        // Destroy or hide the sphere
        if (destroyOnImpact)
        {
            // Delay destruction slightly to let sound play
            GetComponent<Renderer>().enabled = false;
            GetComponent<Collider>().enabled = false;
            Destroy(gameObject, 1f);
        }
    }

    // Visualize explosion radius in scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}