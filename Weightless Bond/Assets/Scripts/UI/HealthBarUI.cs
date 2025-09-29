using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Wiring")]
    public PlayerHealth player;     // Drag your Player here (or auto-find by tag "Player")
    public Image fillImage;         // The foreground Image set to Filled → Horizontal
    public Text valueText;          // Optional: 75/100 etc.

    [Header("Look & Feel")]
    public Gradient colorByHealth;  // 0 = red, 0.5 = yellow, 1 = green
    public float lerpSpeed = 8f;    // Smoothness of fill & color
    public bool pulseOnLowHP = true;
    [Range(0f, 0.5f)] public float lowHpThreshold = 0.25f; // 25% and below
    public float pulseSpeed = 6f;   // Speed of pulsing at low HP
    public float pulseAmplitude = 0.15f;

    float targetFill = 1f;

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.GetComponent<PlayerHealth>();
        }
    }

    void OnEnable()
    {
        if (player != null)
        {
            // Subscribe to live updates
            player.OnHealthChanged += OnHealthChanged;
            // Initialize once in case scene starts not-full
            OnHealthChanged(player.currentHealth);
        }
    }

    void OnDisable()
    {
        if (player != null)
            player.OnHealthChanged -= OnHealthChanged;
    }

    void OnHealthChanged(float current)
    {
        if (player == null || fillImage == null) return;

        float pct = Mathf.Approximately(player.maxHealth, 0f) ? 0f : current / player.maxHealth;
        targetFill = Mathf.Clamp01(pct);

        if (valueText != null)
            valueText.text = $"{Mathf.RoundToInt(current)}/{Mathf.RoundToInt(player.maxHealth)}";
    }

    void Update()
    {
        if (fillImage == null) return;

        // Smooth fill
        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * lerpSpeed);

        // Smooth color
        fillImage.color = Color.Lerp(fillImage.color, colorByHealth.Evaluate(fillImage.fillAmount), Time.deltaTime * lerpSpeed);

        // Optional low-HP pulse
        if (pulseOnLowHP && fillImage.fillAmount <= lowHpThreshold)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
            fillImage.transform.localScale = new Vector3(pulse, pulse, 1f);
        }
        else
        {
            // Smoothly return to normal scale
            fillImage.transform.localScale = Vector3.Lerp(fillImage.transform.localScale, Vector3.one, Time.deltaTime * lerpSpeed);
        }
    }
}
