using System.Collections.Generic;
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

    // =====================  Thrown Damage  =====================
    [Header("Thrown Damage")]
    [Tooltip("If ON, any armed impact with an EnemyAI kills it instantly.")]
    public bool killOnImpact = true;

    [Tooltip("Minimum relative impact speed required to deal damage (ignored if killOnImpact = true).")]
    public float impactSpeedThreshold = 5f;

    [Tooltip("Damage added per 1 m/s above the threshold (ignored if killOnImpact = true).")]
    public float damagePerUnitSpeed = 2f;

    [Tooltip("Maximum damage an impact can deal (ignored if killOnImpact = true).")]
    public float maxImpactDamage = 50f;

    [Tooltip("Delay after being released before this object can deal impact damage.")]
    public float armingDelay = 0.12f;

    [Tooltip("Cooldown to avoid multiple hits on the same enemy from bounces.")]
    public float perTargetCooldown = 0.15f;

    [Tooltip("Multiply damage by this object's Rigidbody mass (ignored if killOnImpact = true).")]
    public bool scaleDamageByMass = false;

    // Runtime thrown state
    [HideInInspector] public bool IsHeld = false;
    [HideInInspector] public float LastReleaseTime = -999f;

    // Track last time this object damaged a given enemy to avoid rapid re-hits
    private readonly Dictionary<EnemyAI, float> _lastHitTime = new();

    void Awake()
    {
        Body = GetComponent<Rigidbody>();
        Highlighter = GetComponent<Highlighter>() ?? gameObject.AddComponent<Highlighter>();

        // Good defaults for thrown/fast objects
        if (Body != null)
        {
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public void OnFocus(bool focused)
    {
        if (Highlighter) Highlighter.SetHighlighted(focused);
    }

    // Effective 'mass' used for player knockback
    public float GetThrowbackMassLike() =>
        Mathf.Max(0.01f, useMassForThrowback ? Body.mass : throwbackWeight);

    // =====================  Impact Damage Hook  =====================
    void OnCollisionEnter(Collision collision)
    {
        // Never deal damage while being held
        if (IsHeld) return;

        // Small arm-time after release so you don't hit yourself instantly
        if (Time.time - LastReleaseTime < armingDelay) return;

        // Find an EnemyAI on what we hit (object or its parents)
        var enemy = collision.collider.GetComponentInParent<EnemyAI>();
        if (enemy == null) return;

        // Per-target cooldown to avoid repeated hits from the same bounce
        if (_lastHitTime.TryGetValue(enemy, out float lastTime) &&
            Time.time - lastTime < perTargetCooldown)
        {
            return;
        }

        if (killOnImpact)
        {
            // Instant kill: apply at least maxHealth damage
            enemy.TakeDamage(Mathf.Max(999999f, enemy.maxHealth));
            _lastHitTime[enemy] = Time.time;
            return;
        }

        // Speed-based damage path (if not using instant kill)
        float relSpeed = collision.relativeVelocity.magnitude;
        if (relSpeed < impactSpeedThreshold) return;

        float dmg = Mathf.Max(0f, (relSpeed - impactSpeedThreshold) * damagePerUnitSpeed);
        if (scaleDamageByMass && Body) dmg *= Mathf.Max(0.1f, Body.mass);
        dmg = Mathf.Min(dmg, maxImpactDamage);

        if (dmg > 0f)
        {
            enemy.TakeDamage(dmg);
            _lastHitTime[enemy] = Time.time;
            // Optional: add hit VFX/SFX here using collision.GetContact(0).point
        }
    }
}
