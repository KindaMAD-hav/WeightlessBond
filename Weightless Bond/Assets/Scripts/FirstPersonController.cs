using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FirstPersonController : MonoBehaviour
{
    // =================== Momentum / Gravity ===================
    [Header("Momentum / Gravity")]
    [Tooltip("Effective mass when AddImpulse(J) is called (e.g., throwback).")]
    public float playerMass = 80f;
    [Tooltip("Gravity (negative). Applied to worldVel.y each frame.")]
    public float gravity = -26f;

    // Full player velocity in world space (used for gravity, impulses, air accel)
    private Vector3 worldVel;

    // =================== Ground (definite speeds; no slide) ===================
    [Header("Ground Movement (No Slide)")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;

    [Tooltip("How fast to snap to exact ground speed when input is present (m/s^2). Use high for near-instant.")]
    public float groundSnapAcceleration = 200f;

    // =================== Jump (impulse-based) ===================
    [Header("Jump (Impulse-Based)")]
    [Tooltip("Vertical impulse applied to worldVel.y (m/s).")]
    public float jumpImpulse = 7.5f;

    [Tooltip("Grace time after walking off edges.")]
    public float coyoteTime = 0.10f;

    [Tooltip("Remember a jump press slightly before landing.")]
    public float jumpBufferTime = 0.10f;

    private float coyoteTimer;
    private float jumpBufferTimer;

    // =================== Air Control / Strafing (air only) ===================
    [Header("Air Control (air only)")]
    [Tooltip("Forward/back acceleration in air.")]
    public float airAcceleration = 16f;
    [Tooltip("Pure A/D strafe acceleration in air.")]
    public float airStrafeAcceleration = 80f;
    [Tooltip("How well you can bend current velocity toward wish direction.")]
    [Range(0f, 1f)] public float airControl = 0.40f;
    [Tooltip("Max speed cap for forward/back air accel.")]
    public float airMaxSpeed = 10f;
    [Tooltip("Max speed cap for pure strafe (A/D) air accel.")]
    public float airStrafeMaxSpeed = 30f;
    [Tooltip("Tiny damping in air to curb infinite drift (0..1 per second).")]
    [Range(0f, 1f)] public float airDrag = 0.04f;

    // =================== Ground Check / Input / Audio ===================
    [Header("Ground Check")]
    public Transform groundCheck;           // kept for gizmo
    public LayerMask groundMask = ~0;       // used only if anyColliderIsGround = false
    public float groundCheckDistance = 0.3f;

    [Header("Grounding (walkable on ANY collider)")]
    [Tooltip("If true, anything underfoot below slope limit counts as ground, not just Ground layer.")]
    public bool anyColliderIsGround = true;
    [Tooltip("Layers that should NEVER count as ground (e.g., Player, Enemies, Triggers).")]
    public LayerMask neverGround = 0;
    [Range(0.5f, 1f)] public float groundProbeRadiusScale = 0.95f;
    [Tooltip("Extra cast distance below the capsule for the ground probe.")]
    public float groundProbeExtra = 0.15f;

    [Header("States / Input")]
    public float walkThreshold = 0.1f;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode interactKey = KeyCode.E;
    public KeyCode punchKey = KeyCode.Mouse0;
    public KeyCode inspectKey = KeyCode.F;

    [Header("Combat Settings")]
    public float punchRange = 2f;
    public float punchDamage = 25f;
    public LayerMask enemyLayerMask = 1 << 6; // Assuming enemies are on layer 6
    public float punchCooldown = 0.5f;

    [Header("Audio")]
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.5f;
    public AudioClip punchSound;

    // Components
    private CharacterController controller;
    private AudioSource audioSource;
    private PlayerAnimationController animationController;
    private Camera playerCamera;

    // State
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private bool isRunning;
    private bool isWalking;
    private float inputMagnitude;

    // Input axes
    private float horizontal;
    private float vertical;

    // Combat/audio timers
    private float lastPunchTime;
    private float footstepTimer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        animationController = GetComponentInChildren<PlayerAnimationController>();

        // Get the camera (child first, fallback to MainCamera)
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;

        // Lock and hide cursor
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
        // Layer-agnostic, slope-limited ground probe BEFORE movement
        isGrounded = ProbeWalkableGround(out _);

        // Coyote timer
        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        // Wishdir (camera-relative on XZ)
        Vector3 wishdir = Vector3.zero;
        if (inputMagnitude > walkThreshold)
            wishdir = (transform.right * horizontal + transform.forward * vertical).normalized;

        // Desired ground speed (definite)
        float targetSpeed = isRunning ? runSpeed : (isWalking ? walkSpeed : 0f);

        // Split current velocity
        Vector3 horiz = new Vector3(worldVel.x, 0f, worldVel.z);

        if (isGrounded)
        {
            // *** NO SLIDE RULES ***
            Vector3 targetVel = (targetSpeed > 0f && wishdir.sqrMagnitude > 0f) ? wishdir * targetSpeed : Vector3.zero;
            horiz = Vector3.MoveTowards(horiz, targetVel, groundSnapAcceleration * Time.deltaTime);

            // Jump using impulse (consistent with throwback)
            if (jumpBufferTimer > 0f && coyoteTimer > 0f && Input.GetKeyDown(jumpKey))
            {
                worldVel.y = jumpImpulse;
                jumpBufferTimer = 0f; // consume
                isGrounded = false;   // will be airborne after Move
            }
        }
        else
        {
            // Air: tiny damping on horizontal drift
            horiz *= Mathf.Clamp01(1f - airDrag * Time.deltaTime);

            // Air acceleration (A/D strafing only works here)
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

        // Backup: if CharacterController touched below, treat as grounded when walkable
        if (!isGrounded && (controller.collisionFlags & CollisionFlags.Below) != 0)
        {
            if (ProbeWalkableGround(out _))
                isGrounded = true;
        }

        // After landing, enforce no-slide immediately
        if (!wasGroundedLastFrame && isGrounded)
        {
            if (targetSpeed > 0f && wishdir.sqrMagnitude > 0f)
                worldVel = new Vector3((wishdir * targetSpeed).x, worldVel.y, (wishdir * targetSpeed).z);
            else
                worldVel = new Vector3(0f, worldVel.y, 0f);
        }

        // Sticky ground protection
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

    void PerformInteract()
    {
        if (animationController) animationController.TriggerInteract();
        // Add your interaction logic here
        Debug.Log("Interact performed");
    }

    void PerformPunch()
    {
        // Check cooldown
        if (Time.time - lastPunchTime < punchCooldown)
            return;

        lastPunchTime = Time.time;

        if (animationController != null)
            animationController.TriggerPunch();

        // Play punch sound
        if (punchSound != null && audioSource != null)
            audioSource.PlayOneShot(punchSound, 0.8f);

        // Camera fallback safety
        if (playerCamera == null) playerCamera = Camera.main;

        // Perform raycast from camera center
        Ray punchRay = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

        if (Physics.Raycast(punchRay, out RaycastHit hit, punchRange, enemyLayerMask))
        {
            // Check if we hit an enemy
            EnemyAI enemy = hit.collider.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                // Deal damage to the enemy
                enemy.TakeDamage(punchDamage);

                // Optional: Add punch force/impulse to the enemy
                Rigidbody enemyRb = hit.collider.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    Vector3 punchForce = punchRay.direction * 5f; // Adjust force as needed
                    enemyRb.AddForce(punchForce, ForceMode.Impulse);
                }

                Debug.Log($"Punched {enemy.name} for {punchDamage} damage!");
            }
        }
        else
        {
            Debug.Log("Punch missed - no enemy in range");
        }
    }

    void PerformInspect()
    {
        if (animationController != null)
            animationController.TriggerInspect();

        // Add your inspect logic here
        Debug.Log("Inspect performed");
    }

    // ----------------------- Public getters -----------------------
    public bool IsGrounded() => isGrounded;
    public bool IsRunning() => isRunning;
    public bool IsWalking() => isWalking;
    public float GetInputMagnitude() => inputMagnitude;
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

    // Probe for any walkable ground under the controller (layer-agnostic if enabled)
    bool ProbeWalkableGround(out RaycastHit hit)
    {
        float radius = controller.radius * groundProbeRadiusScale;
        Vector3 origin = controller.bounds.center + Vector3.up * 0.05f;
        float castDist = controller.skinWidth + groundCheckDistance + groundProbeExtra;

        int mask = anyColliderIsGround ? ~neverGround : groundMask;

        if (Physics.SphereCast(origin, radius, Vector3.down, out hit, castDist, mask, QueryTriggerInteraction.Ignore))
        {
            float slope = Vector3.Angle(hit.normal, Vector3.up);
            return slope <= controller.slopeLimit + 1f; // small tolerance
        }
        return false;
    }

    // Debug gizmos for ground and punch direction
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
        }

        if (playerCamera != null)
        {
            Gizmos.color = Color.blue;
            Vector3 punchDirection = playerCamera.transform.forward;
            Gizmos.DrawRay(playerCamera.transform.position, punchDirection * punchRange);
        }
    }
}
