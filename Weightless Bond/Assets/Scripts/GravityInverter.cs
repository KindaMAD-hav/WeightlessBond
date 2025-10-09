using UnityEngine;
using System.Collections;

[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(AudioSource))]
public class GravityInverter : MonoBehaviour
{
    private FirstPersonController playerController;
    private AudioSource audioSource;

    // True means gravity currently pulling UP (positive), false means DOWN (negative)
    private bool gravityInverted = false;
    private bool isInverting = false;

    [Header("Invert Settings")]
    [Tooltip("Key to toggle gravity inversion.")]
    public KeyCode invertKey = KeyCode.F;

    [Tooltip("Overall speed multiplier of the gravity inversion.")]
    public float gravityTransitionSpeed = 2f;

    [Tooltip("Pause duration at zero gravity before flipping.")]
    public float zeroGravityPause = 0.4f;

    [Tooltip("Optional gravity flip sound.")]
    public AudioClip gravityFlipSound;

    private float originalGravity;
    private Coroutine gravityRoutine;

    void Start()
    {
        playerController = GetComponent<FirstPersonController>();
        audioSource = GetComponent<AudioSource>();

        // Cache the magnitude and infer the *current* polarity/state
        originalGravity = Mathf.Max(0.0001f, Mathf.Abs(playerController.gravity));
        gravityInverted = playerController.gravity > 0f; // positive means "up", negative means "down"
    }

    void Update()
    {
        if (Input.GetKeyDown(invertKey) && !isInverting)
        {
            if (gravityRoutine != null) StopCoroutine(gravityRoutine);
            gravityRoutine = StartCoroutine(SmoothInvertGravity());
        }
    }

    private IEnumerator SmoothInvertGravity()
    {
        isInverting = true;

        float startGravity = playerController.gravity;

        // If we're somehow at 0 already, fallback to current state to decide a sign
        if (Mathf.Approximately(startGravity, 0f))
            startGravity = gravityInverted ? +originalGravity : -originalGravity;

        // Flip the sign relative to what we *actually* have right now
        float targetGravity = -Mathf.Sign(startGravity) * originalGravity;

        // Step 1: Move gravity smoothly toward 0
        float velocity = 0f;
        while (Mathf.Abs(playerController.gravity) > 0.05f)
        {
            playerController.gravity = Mathf.SmoothDamp(playerController.gravity, 0f, ref velocity, 1f / Mathf.Max(0.0001f, gravityTransitionSpeed));
            yield return null;
        }

        playerController.gravity = 0f;
        yield return new WaitForSeconds(zeroGravityPause);

        // Step 2: Smoothly move gravity toward target polarity with easing
        float t = 0f;
        float duration = 1.5f / Mathf.Max(0.0001f, gravityTransitionSpeed);
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            playerController.gravity = Mathf.Lerp(0f, targetGravity, easedT);
            yield return null;
        }

        playerController.gravity = targetGravity;

        // Update our state based on the new sign
        gravityInverted = playerController.gravity > 0f;

        // Step 3: Damp vertical velocity for smoother feel (reflection is optional/hardening)
        try
        {
            Vector3 currentVel = playerController.GetVelocity();
            currentVel.y = -currentVel.y * 0.3f; // slight rebound
            typeof(FirstPersonController)
                .GetField("worldVel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(playerController, currentVel);
        }
        catch { /* safe no-op if internals differ */ }

        // Optional audio feedback
        if (gravityFlipSound && audioSource)
            audioSource.PlayOneShot(gravityFlipSound, 0.9f);

        isInverting = false;
    }
}
