using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;
    public float groundCheckDistance = 0.3f;

    [Header("Movement States")]
    public float walkThreshold = 0.1f; // Minimum input to start walking

    [Header("Input Settings")]
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode interactKey = KeyCode.E;
    public KeyCode punchKey = KeyCode.Mouse0;
    public KeyCode inspectKey = KeyCode.F;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundMask = 1;

    [Header("Audio")]
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.5f;

    // Components
    private CharacterController controller;
    private AudioSource audioSource;
    private PlayerAnimationController animationController;

    // Movement variables
    private Vector3 velocity;
    private bool isGrounded;
    private bool isRunning;
    private bool isWalking;
    private float currentSpeed;
    private float inputMagnitude;

    // Audio variables
    private float footstepTimer;

    // Input variables
    private float horizontal;
    private float vertical;
    private Vector3 moveDirection;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        animationController = GetComponentInChildren<PlayerAnimationController>();

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentSpeed = walkSpeed;
    }

    void Update()
    {
        HandleInput();
        HandleMovement();
        HandleActions();
        HandleAudio();

        // Send movement data to animation controller
        if (animationController != null)
        {
            animationController.SetMovementData(inputMagnitude, isWalking, isRunning, isGrounded);
        }
    }

    void HandleInput()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        // Calculate input magnitude for movement states
        Vector2 inputVector = new Vector2(horizontal, vertical);
        inputMagnitude = Mathf.Clamp01(inputVector.magnitude);

        // Determine movement states
        isRunning = Input.GetKey(runKey) && inputMagnitude > walkThreshold;
        isWalking = !isRunning && inputMagnitude > walkThreshold;

        // Set current speed based on state
        if (isRunning)
            currentSpeed = runSpeed;
        else if (isWalking)
            currentSpeed = walkSpeed;
        else
            currentSpeed = 0f;
    }

    void HandleMovement()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to keep grounded
        }

        // Movement
        moveDirection = transform.right * horizontal + transform.forward * vertical;

        // Only apply speed if we have input above threshold
        if (inputMagnitude > walkThreshold)
        {
            moveDirection = moveDirection.normalized * currentSpeed;
        }
        else
        {
            moveDirection = Vector3.zero;
        }

        controller.Move(moveDirection * Time.deltaTime);

        // Jumping
        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleActions()
    {
        if (Input.GetKeyDown(interactKey))
        {
            PerformInteract();
        }

        if (Input.GetKeyDown(punchKey))
        {
            PerformPunch();
        }

        if (Input.GetKeyDown(inspectKey))
        {
            PerformInspect();
        }
    }

    void HandleAudio()
    {
        if (isGrounded && (isWalking || isRunning))
        {
            footstepTimer += Time.deltaTime;

            float currentFootstepInterval = isRunning ? footstepInterval * 0.6f : footstepInterval;

            if (footstepTimer >= currentFootstepInterval)
            {
                PlayFootstepSound();
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    void PlayFootstepSound()
    {
        if (footstepSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            audioSource.PlayOneShot(clip, 0.7f);
        }
    }

    void PerformInteract()
    {
        if (animationController != null)
        {
            animationController.TriggerInteract();
        }

        // Add your interaction logic here
        Debug.Log("Interact performed");
    }

    void PerformPunch()
    {
        if (animationController != null)
        {
            animationController.TriggerPunch();
        }

        // Add your punch logic here
        Debug.Log("Punch performed");
    }

    void PerformInspect()
    {
        if (animationController != null)
        {
            animationController.TriggerInspect();
        }

        // Add your inspect logic here
        Debug.Log("Inspect performed");
    }

    // Public methods for external access
    public bool IsGrounded() => isGrounded;
    public bool IsRunning() => isRunning;
    public bool IsWalking() => isWalking;
    public float GetInputMagnitude() => inputMagnitude;
    public float GetMovementSpeed() => currentSpeed;
    public Vector3 GetVelocity() => controller.velocity;

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
        }
    }
}