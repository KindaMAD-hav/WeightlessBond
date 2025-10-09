using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class ButtonDoorOpener : MonoBehaviour
{
    [Header("Button Settings")]
    public Transform button;             // The visible moving button part
    public float pressDepth = 0.2f;      // How much the button moves down (local Y)
    public float pressSpeed = 5f;        // Speed of press animation
    public float pressCooldown = 0.15f;  // Debounce to avoid double triggers

    [Header("Door Settings")]
    public Transform door;               // Door object to open
    public float raiseAmount = 5f;       // World Y raise amount
    public float raiseSpeed = 2f;        // Units per second

    [Header("Impact Detection")]
    [Tooltip("Minimum relative speed of the impact needed to press the button.")]
    public float impactSpeedThreshold = 3.0f;

    [Tooltip("Optional: require some momentum (mass * speed) too, useful if tiny objects move fast.")]
    public float momentumThreshold = 2.0f; // kg * m/s; set 0 to ignore

    [Tooltip("Only allow objects with G_Interactable (thrown props) to trigger.")]
    public bool requireGInteractable = false;

    [Tooltip("Layers that are allowed to press the button. Leave empty to allow all.")]
    public LayerMask allowedLayers = ~0;

    [Header("Audio Settings")]
    public bool playDoorSfx = true;
    public AudioClip doorOpenSfx;
    [Range(0f, 1f)] public float doorSfxVolume = 1f;

    private Vector3 buttonStartPos;
    private Vector3 buttonPressedPos;

    private Vector3 doorStartPos;
    private Vector3 doorTargetPos;

    private bool isPressedVisual = false; // purely for animation state
    private bool doorOpening = false;
    private bool doorOpened = false;

    private float lastPressTime = -999f;
    private Collider myCollider;
    private AudioSource audioSource;

    void Awake()
    {
        myCollider = GetComponent<Collider>();
        if (!myCollider) Debug.LogError("[ButtonDoorOpener] Missing Collider on the button root.");

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    void Start()
    {
        if (button != null)
        {
            buttonStartPos = button.localPosition;
            buttonPressedPos = buttonStartPos + Vector3.down * Mathf.Abs(pressDepth);
        }
        else
        {
            Debug.LogWarning("[ButtonDoorOpener] 'button' is not assigned.");
        }

        if (door != null)
        {
            doorStartPos = door.position;
            doorTargetPos = doorStartPos + Vector3.up * raiseAmount;
        }
        else
        {
            Debug.LogWarning("[ButtonDoorOpener] 'door' is not assigned.");
        }
    }

    void Update()
    {
        // Animate button
        if (button != null)
        {
            Vector3 target = isPressedVisual ? buttonPressedPos : buttonStartPos;
            button.localPosition = Vector3.MoveTowards(button.localPosition, target, pressSpeed * Time.deltaTime);
        }

        // Animate door
        if (doorOpening && door != null)
        {
            door.position = Vector3.MoveTowards(door.position, doorTargetPos, raiseSpeed * Time.deltaTime);
            if (Vector3.Distance(door.position, doorTargetPos) < 0.01f)
            {
                door.position = doorTargetPos;
                doorOpening = false;
                doorOpened = true;
            }
        }
    }

    // =========================
    // Impact-driven activation
    // =========================

    void OnCollisionEnter(Collision collision)
    {
        if (!IsLayerAllowed(collision.collider.gameObject.layer)) return;
        if (!PassesInteractableFilter(collision.collider)) return;

        float relativeSpeed = collision.relativeVelocity.magnitude;
        TryPressFromImpact(collision.collider, relativeSpeed);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!myCollider || !myCollider.isTrigger) return;
        if (!IsLayerAllowed(other.gameObject.layer)) return;
        if (!PassesInteractableFilter(other)) return;

        float relativeSpeed = 0f;
        var rb = other.attachedRigidbody;
        if (rb != null)
        {
            relativeSpeed = rb.linearVelocity.magnitude;
        }
        else
        {
            return;
        }

        TryPressFromImpact(other, relativeSpeed);
    }

    private bool IsLayerAllowed(int layer)
    {
        return (allowedLayers.value & (1 << layer)) != 0;
    }

    private bool PassesInteractableFilter(Collider col)
    {
        if (!requireGInteractable) return true;
        return col.GetComponentInParent<G_Interactable>() != null;
    }

    private void TryPressFromImpact(Collider other, float relativeSpeed)
    {
        if (doorOpened) return;
        if (Time.time - lastPressTime < pressCooldown) return;

        var rb = other.attachedRigidbody;
        if (rb == null) return;

        if (relativeSpeed < impactSpeedThreshold) return;

        if (momentumThreshold > 0f)
        {
            float momentum = rb.mass * relativeSpeed;
            if (momentum < momentumThreshold) return;
        }

        Press();
    }

    // =========================
    // Core press/open logic
    // =========================
    private void Press()
    {
        lastPressTime = Time.time;
        isPressedVisual = true;
        doorOpening = true;

        Debug.Log("[ButtonDoorOpener] Button pressed by impact! Opening door...");

        // Play SFX once when door starts opening
        if (playDoorSfx && doorOpenSfx != null)
        {
            audioSource.volume = doorSfxVolume;
            audioSource.PlayOneShot(doorOpenSfx, doorSfxVolume);
        }

        CancelInvoke(nameof(ReleaseVisual));
        Invoke(nameof(ReleaseVisual), 0.12f);
    }

    private void ReleaseVisual()
    {
        isPressedVisual = false;
    }
}
