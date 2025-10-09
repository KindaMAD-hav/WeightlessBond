using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool canRegenerate = true;
    public float regenRate = 5f; // Health per second
    public float regenDelay = 3f; // Delay after taking damage before regen starts

    [Header("Damage Settings")]
    public float invincibilityDuration = 1f; // Time player is invincible after taking damage
    public bool showDamageEffect = true;

    [Header("UI References")]
    public Slider healthBar;
    public Text healthText;
    public Image damageOverlay; // Red overlay for damage effect

    [Header("Audio")]
    public AudioClip damageSound;
    public AudioClip healSound;
    public AudioClip deathSound;

    [Header("Death Settings")]
    public bool respawnOnDeath = true;
    public float respawnDelay = 3f;
    public Transform respawnPoint;

    // Private variables
    private bool isInvincible = false;
    private bool isDead = false;
    private float lastDamageTime;
    private AudioSource audioSource;
    private Renderer playerRenderer;
    private Color originalColor;

    // Events
    public System.Action<float> OnHealthChanged;
    public System.Action OnPlayerDeath;
    public System.Action OnPlayerRespawn;

    void Start()
    {
        // Initialize health
        currentHealth = maxHealth;

        // Get components
        audioSource = GetComponent<AudioSource>();
        playerRenderer = GetComponent<Renderer>();

        if (playerRenderer != null)
            originalColor = playerRenderer.material.color;

        // Set respawn point to current position if not set
        if (respawnPoint == null)
            respawnPoint = transform;

        // Initialize UI
        UpdateHealthUI();

        if (damageOverlay != null)
        {
            Color overlayColor = damageOverlay.color;
            overlayColor.a = 0f;
            damageOverlay.color = overlayColor;
        }
        OnHealthChanged?.Invoke(currentHealth);
    }

    void Update()
    {
        if (isDead) return;

        // Handle health regeneration
        if (canRegenerate && currentHealth < maxHealth && Time.time - lastDamageTime >= regenDelay)
        {
            Heal(regenRate * Time.deltaTime);
        }

        // Update UI
        UpdateHealthUI();
    }
    // Back-compat overload so existing code compiles:
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, false); // respect invincibility by default
    }

    public void TakeDamage(float damage, bool ignoreInvincibility)
    {
        if (isDead) return;
        if (!ignoreInvincibility && isInvincible) return;

        // Apply damage
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);

        lastDamageTime = Time.time;
        OnHealthChanged?.Invoke(currentHealth);

        if (!ignoreInvincibility)
        {
            // Only play damage FX + i-frames if not bypassing
            StartCoroutine(DamageEffect());
            PlaySound(damageSound);
            StartCoroutine(InvincibilityCoroutine());
        }

        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0) Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        float oldHealth = currentHealth;
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Only trigger events and sound if actually healed
        if (currentHealth > oldHealth)
        {
            OnHealthChanged?.Invoke(currentHealth);

            if (amount >= 1f) // Only play sound for significant healing
                PlaySound(healSound);
        }
    }

    public void SetHealth(float newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void FullHeal()
    {
        Heal(maxHealth);
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        currentHealth = 0;

        Debug.Log("Player died!");

        // Trigger events
        OnPlayerDeath?.Invoke();

        // Play death sound
        PlaySound(deathSound);

        // Disable player controls
        DisablePlayerControls();

        if (respawnOnDeath)
        {
            StartCoroutine(RespawnCoroutine());
        }
        else
        {
            // Instead of just HandleGameOver, reload the scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void DisablePlayerControls()
    {
        // Disable common player components
        var playerController = GetComponent<CharacterController>();
        if (playerController != null) playerController.enabled = false;

        var rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null) rigidbody.isKinematic = true;

        // You might need to disable your specific player movement script here
        var movementScript = GetComponent<MonoBehaviour>(); // Replace with your movement script name
        if (movementScript != null) movementScript.enabled = false;
    }

    void EnablePlayerControls()
    {
        // Re-enable player components
        var playerController = GetComponent<CharacterController>();
        if (playerController != null) playerController.enabled = true;

        var rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null) rigidbody.isKinematic = false;

        // Re-enable your specific player movement script here
        var movementScript = GetComponent<MonoBehaviour>(); // Replace with your movement script name
        if (movementScript != null) movementScript.enabled = true;
    }

    IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    public void Respawn()
    {
        // Reset position
        transform.position = respawnPoint.position;
        transform.rotation = respawnPoint.rotation;

        // Reset health
        currentHealth = maxHealth;
        isDead = false;
        isInvincible = false;

        // Re-enable controls
        EnablePlayerControls();

        // Reset visuals
        if (playerRenderer != null)
            playerRenderer.material.color = originalColor;

        // Trigger event
        OnPlayerRespawn?.Invoke();
        OnHealthChanged?.Invoke(currentHealth);


        Debug.Log("Player respawned!");
    }

    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;

        // Visual feedback for invincibility (flashing effect)
        if (playerRenderer != null && showDamageEffect)
        {
            float flashTime = 0.1f;
            int flashes = Mathf.RoundToInt(invincibilityDuration / (flashTime * 2));

            for (int i = 0; i < flashes; i++)
            {
                playerRenderer.material.color = Color.red;
                yield return new WaitForSeconds(flashTime);
                playerRenderer.material.color = originalColor;
                yield return new WaitForSeconds(flashTime);
            }
        }
        else
        {
            yield return new WaitForSeconds(invincibilityDuration);
        }

        isInvincible = false;
    }

    IEnumerator DamageEffect()
    {
        if (damageOverlay != null && showDamageEffect)
        {
            // Fade in damage overlay
            Color overlayColor = damageOverlay.color;
            float fadeSpeed = 3f;

            // Fade in
            overlayColor.a = 0.3f;
            damageOverlay.color = overlayColor;

            // Fade out
            while (overlayColor.a > 0)
            {
                overlayColor.a -= fadeSpeed * Time.deltaTime;
                damageOverlay.color = overlayColor;
                yield return null;
            }
        }
    }

    void UpdateHealthUI()
    {
        // Update health bar
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }

        // Update health text
        if (healthText != null)
        {
            healthText.text = $"{Mathf.RoundToInt(currentHealth)}/{Mathf.RoundToInt(maxHealth)}";
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void HandleGameOver()
    {
        // Implement game over logic here
        Debug.Log("Game Over!");

        // You could:
        // - Show game over screen
        // - Restart current level
        // - Return to main menu
        // - etc.
    }

    // Public getters for other scripts
    public bool IsAlive() => !isDead;
    public bool IsInvincible() => isInvincible;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public bool IsAtFullHealth() => currentHealth >= maxHealth;

    // Method to add max health (for power-ups)
    public void IncreaseMaxHealth(float amount)
    {
        maxHealth += amount;
        OnHealthChanged?.Invoke(currentHealth);
    }

    void OnValidate()
    {
        // Ensure health values are valid in inspector
        if (maxHealth <= 0) maxHealth = 100f;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        if (currentHealth < 0) currentHealth = 0;
    }
}