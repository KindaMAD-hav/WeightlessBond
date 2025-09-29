using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarDOTween : MonoBehaviour
{
    [Header("Wiring")]
    public PlayerHealth player;          // Drag your Player (or leave empty if tagged "Player")
    public Image fillLeft;               // Optional: left half (Image Type = Filled → Horizontal → Origin Left)
    public Image fillRight;              // Optional: right half (Image Type = Filled → Horizontal → Origin Right)
    public Image singleFill;             // Use this instead of left/right if you prefer 1 bar (Filled → Horizontal → Origin Left)

    [Header("Look & Feel")]
    public Gradient colorByHealth;       // 0 = red, 0.5 = yellow, 1 = green
    public float tweenDuration = 0.25f;  // seconds

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
            player.OnHealthChanged += OnHealthChanged;
        }

        // Initialize once
        if (player != null) ApplyInstant(player.currentHealth, player.maxHealth);
    }

    void OnDisable()
    {
        if (player != null)
            player.OnHealthChanged -= OnHealthChanged;
    }

    void OnHealthChanged(float current)
    {
        if (player == null) return;
        float pct = (player.maxHealth > 0f) ? Mathf.Clamp01(current / player.maxHealth) : 0f;
        TweenTo(pct);
    }

    void ApplyInstant(float current, float max)
    {
        float pct = (max > 0f) ? Mathf.Clamp01(current / max) : 0f;
        if (singleFill)
        {
            singleFill.fillAmount = pct;
            singleFill.color = colorByHealth.Evaluate(pct);
        }
        if (fillLeft)
        {
            fillLeft.fillAmount = pct;
            fillLeft.color = colorByHealth.Evaluate(pct);
        }
        if (fillRight)
        {
            fillRight.fillAmount = pct;
            fillRight.color = colorByHealth.Evaluate(pct);
        }
    }

    void TweenTo(float pct)
    {
        // Single bar mode
        if (singleFill)
        {
            DOTween.Kill(singleFill);
            singleFill.DOFillAmount(pct, tweenDuration);
            singleFill.DOColor(colorByHealth.Evaluate(pct), tweenDuration);
        }

        // Split bar mode (left/right)
        if (fillLeft)
        {
            DOTween.Kill(fillLeft);
            fillLeft.DOFillAmount(pct, tweenDuration);
            fillLeft.DOColor(colorByHealth.Evaluate(pct), tweenDuration);
        }
        if (fillRight)
        {
            DOTween.Kill(fillRight);
            fillRight.DOFillAmount(pct, tweenDuration);
            fillRight.DOColor(colorByHealth.Evaluate(pct), tweenDuration);
        }
    }
}
