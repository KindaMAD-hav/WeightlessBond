using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FirstPersonController : MonoBehaviour
{
    // =================== Core Physics / Momentum ===================
    [Header("Momentum / Gravity")]
    [Tooltip("Effective mass used when AddImpulse(J) is called (e.g., throwback).")]
    public float playerMass = 80f;
    [Tooltip("Gravity (negative). Applies to worldVel.y every frame.")]
    public float gravity = -26f;

    // Entire player velocity in world space (used for gravity, impulses, air accel)
    private Vector3 worldVel;

    // =================== Grounded Movement (definite speeds) ===================
    [Header("Ground Movement (Definite Speeds)")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;

    [Tooltip("How fast horizontal velocity snaps to target on ground (m/s^2). Use big value for near-instant.")]
    public float groundSnapAcceleration = 100f;

    [Tooltip("How fast we decelerate to zero on ground when no input (m/s^2).")]
    public float groundDeceleration = 50f;

    // =================== Jumping (impulse-based) ===================
    [Header("Jump (Impulse-Based)")]
    [Tooltip("Vertical impulse applied to worldVel.y when jumping (m/s).")]
    public float jumpImpulse = 7.5f;

    [Tooltip("Extra forgiveness time after walking off edges.")]
    public float coyoteTime = 0.10f;

    [Tooltip("Buffer window that remembers a jump press slightly before landing.")]
    public float jumpBufferTime = 0.10f;

    private float coyoteTimer;
    private float jumpBufferTimer;

    // =================== Air Control / Strafing ===================
    [Header("Air Control (Strafing only while airborne)")]
    [Tooltip("Forward/back acceleration in air.")]
    public float airAcceleration = 16f;
    [Tooltip("Pure A/D strafe acceleration in air.")]
    public float airStrafeAcceleration = 80f;
    [Tooltip("How well we can bend current velocity toward our wish direction.")]
    public float airControl = 0.40f;
    [Tooltip("Max speed cap for forward/back air accel.")]
    public float airMaxSpeed = 10f;
    [Tooltip("Max speed cap for pure strafe (A/D) air accel.")]
    public float airStrafeMaxSpeed = 30f;
    [Tooltip("Tiny damping in air to curb infinite drift (0..1 per second).")]
    public float airDrag = 0.04f;

    // =================== Input / Ground Check / Audio ===================
    [Header("Ground Check")]
    public Transform groundCheck;           // marker near feet (for gizmo only)
    public LayerMask groundMask = ~0;       // which layers count as ground
    public float groundCheckDistance = 0.3f;

    [Header("States / Input")]
    public float walkThreshold = 0.1f;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode interactKey = KeyCode.E;
    public KeyCode punchKey = KeyCode.Mouse0;
    public KeyCode inspectKey = KeyCode.F;

    [Header("Audio")]
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.5f;

    // Components
    private CharacterController controller;
    private AudioSource audioSource;
    private PlayerAnimationController animationController;

    // State
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private bool isRunning;
    private bool isWalking;
    private float inputMagnitude;

    // Input axes
    private float horizontal;
    private float vertical;

    // Audio
    private float footstepTimer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        animationController = GetComponentInChildren<PlayerAnimationController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        ReadInput();
        MovementUpdate();
        HandleActions();
        HandleAudio();

        if (animationController != null)
            animationController.SetMovementData(inputMagnitude, isWalking, isRunning, isGrounded);
    }

    // External impulse (e.g., from GravityHand throwback)
    public void AddImpulse(Vector3 impulseWorld)
    {
        worldVel += impulseWorld / Mathf.Max(0.01f, playerMass);
    }

    // ----------------------- INPUT -----------------------
    void ReadInput()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        Vector2 iv = new Vector2(horizontal, vertical);
        inputMagnitude = Mathf.Clamp01(iv.magnitude);

        isRunning = Input.GetKey(runKey) && inputMagnitude > walkThreshold;
        isWalking = !isRunning && inputMagnitude > walkThreshold;

        // Jump buffering
        if (Input.GetKeyDown(jumpKey))
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;
    }

    // ----------------------- MOVEMENT -----------------------
    void MovementUpdate()
    {
        // Ground check (simple sphere at feet)
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);

        // Coyote timer update
        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        // Build input wishdir (camera-relative, XZ plane)
        Vector3 wishdir = Vector3.zero;
        if (inputMagnitude > walkThreshold)
            wishdir = (transform.right * horizontal + transform.forward * vertical).normalized;

        // Desired ground speed (definite)
        float targetSpeed = isRunning ? runSpeed : (isWalking ? walkSpeed : 0f);

        // Split current velocity
        Vector3 horiz = new Vector3(worldVel.x, 0f, worldVel.z);

        if (isGrounded)
        {
            // Deterministic ground motion:
            // - Move horizontal speed toward exact target speed along wishdir
            // - Decelerate toward 0 when no input
            if (targetSpeed > 0f && wishdir.sqrMagnitude > 0.0f)
            {
                Vector3 targetVel = wishdir * targetSpeed;
                horiz = Vector3.MoveTowards(horiz, targetVel, groundSnapAcceleration * Time.deltaTime);
            }
            else
            {
                horiz = Vector3.MoveTowards(horiz, Vector3.zero, groundDeceleration * Time.deltaTime);
            }

            // Jump if buffered and coyote time valid
            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                worldVel.y = jumpImpulse;
                jumpBufferTimer = 0f; // consume
                isGrounded = false;   // will be airborne after Move
            }
        }
        else
        {
            // Air: light drag on existing horizontal
            horiz *= Mathf.Clamp01(1f - airDrag * Time.deltaTime);

            // Air acceleration (with pure A/D strafe detection)
            bool pureStrafe = Mathf.Abs(horizontal) > 0f && Mathf.Abs(vertical) <= 0.0001f;

            float wishspeed = (isRunning ? runSpeed : walkSpeed);
            float cap = pureStrafe ? airStrafeMaxSpeed : airMaxSpeed;
            if (wishspeed > cap) wishspeed = cap;

            float accel = pureStrafe ? airStrafeAcceleration : airAcceleration;

            Accelerate(ref horiz, wishdir, wishspeed, accel);
            AirControlTurn(ref horiz, wishdir, wishspeed);
        }

        // Recombine with vertical & apply gravity
        worldVel = new Vector3(horiz.x, worldVel.y, horiz.z);
        worldVel.y += gravity * Time.deltaTime;

        // Single move
        controller.Move(worldVel * Time.deltaTime);

        // Sticky ground fix after landing
        if (isGrounded && worldVel.y < 0f)
            worldVel.y = -2f;

        wasGroundedLastFrame = isGrounded;
    }

    // ----------------------- ACTIONS / AUDIO -----------------------
    void HandleActions()
    {
        if (Input.GetKeyDown(interactKey)) PerformInteract();
        if (Input.GetKeyDown(punchKey)) PerformPunch();
        if (Input.GetKeyDown(inspectKey)) PerformInspect();
    }

    void HandleAudio()
    {
        if (isGrounded && (isWalking || isRunning))
        {
            footstepTimer += Time.deltaTime;
            float stepInt = isRunning ? footstepInterval * 0.6f : footstepInterval;
            if (footstepTimer >= stepInt)
            {
                PlayFootstepSound();
                footstepTimer = 0f;
            }
        }
        else footstepTimer = 0f;
    }

    void PlayFootstepSound()
    {
        if (footstepSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            audioSource.PlayOneShot(clip, 0.7f);
        }
    }

    void PerformInteract() { if (animationController) animationController.TriggerInteract(); }
    void PerformPunch() { if (animationController) animationController.TriggerPunch(); }
    void PerformInspect() { if (animationController) animationController.TriggerInspect(); }

    // ----------------------- Public getters -----------------------
    public bool IsGrounded() => isGrounded;
    public bool IsRunning() => isRunning;
    public bool IsWalking() => isWalking;
    public float GetInputMagnitude() => inputMagnitude;
    public float GetMovementSpeed() => isRunning ? runSpeed : (isWalking ? walkSpeed : 0f);
    public Vector3 GetVelocity() => controller.velocity;

    // ----------------------- Helpers -----------------------
    // Quake-like accelerate in air toward wishdir up to wishspeed.
    void Accelerate(ref Vector3 horizVel, Vector3 wishdir, float wishspeed, float accel)
    {
        if (wishspeed <= 0f || wishdir.sqrMagnitude < 1e-6f) return;

        float currentspeed = Vector3.Dot(horizVel, wishdir);
        float addspeed = wishspeed - currentspeed;
        if (addspeed <= 0f) return;

        float accelspeed = accel * Time.deltaTime * wishspeed;
        if (accelspeed > addspeed) accelspeed = addspeed;

        horizVel += wishdir * accelspeed;
    }

    // Turn existing horizontal velocity toward wishdir mid-air.
    void AirControlTurn(ref Vector3 horizVel, Vector3 wishdir, float wishspeed)
    {
        if (airControl <= 0f || wishspeed <= 0f) return;
        if (horizVel.sqrMagnitude < 1e-6f || wishdir.sqrMagnitude < 1e-6f) return;

        float proj = Vector3.Dot(horizVel.normalized, wishdir);
        if (proj <= 0f) return;

        Vector3 newDir = Vector3.Slerp(horizVel.normalized, wishdir, airControl * Time.deltaTime);
        float speed = horizVel.magnitude;
        horizVel = newDir * speed;
    }

    // Debug gizmo for ground check
    void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
    }
}
