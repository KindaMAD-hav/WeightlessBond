using UnityEngine;
using System.Collections;

[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(AudioSource))]
public class GravityInverter : MonoBehaviour
{
    private FirstPersonController playerController;
    private AudioSource audioSource;

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
        originalGravity = Mathf.Abs(playerController.gravity);
    }

    void Update()
    {
        if (Input.GetKeyDown(invertKey) && !isInverting)
        {
            gravityRoutine = StartCoroutine(SmoothInvertGravity());
        }
    }

    private IEnumerator SmoothInvertGravity()
    {
        isInverting = true;

        float startGravity = playerController.gravity;
        float targetGravity = gravityInverted ? originalGravity : -originalGravity;

        Debug.Log("🌀 Starting smooth gravity inversion...");

        // Step 1: Move gravity smoothly toward 0
        float velocity = 0f;
        while (Mathf.Abs(playerController.gravity) > 0.05f)
        {
            playerController.gravity = Mathf.SmoothDamp(playerController.gravity, 0, ref velocity, 1f / gravityTransitionSpeed);
            yield return null;
        }

        playerController.gravity = 0;
        Debug.Log("🌌 Zero gravity reached — pausing...");
        yield return new WaitForSeconds(zeroGravityPause);

        // Step 2: Smoothly move gravity toward target polarity with easing
        float t = 0f;
        float duration = 1.5f / gravityTransitionSpeed; // Duration of inversion phase
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            // Smooth in-out easing
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            playerController.gravity = Mathf.Lerp(0, targetGravity, easedT);
            yield return null;
        }

        playerController.gravity = targetGravity;
        gravityInverted = !gravityInverted;

        // Step 3: Damp vertical velocity for smoother feel
        Vector3 currentVel = playerController.GetVelocity();
        currentVel.y = -currentVel.y * 0.3f; // Slight rebound effect
        typeof(FirstPersonController)
            .GetField("worldVel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(playerController, currentVel);

        // Optional audio feedback
        if (gravityFlipSound && audioSource)
            audioSource.PlayOneShot(gravityFlipSound, 0.9f);

        Debug.Log($"✅ Gravity inversion complete! Gravity now {(gravityInverted ? "UP" : "DOWN")}. Value: {playerController.gravity}");

        isInverting = false;
    }
}
