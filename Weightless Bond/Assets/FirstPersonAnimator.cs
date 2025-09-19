using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FirstPersonAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float animationSmoothing = 5f;
    public float idleThreshold = 0.1f;

    [Header("References")]
    public FirstPersonController playerController;

    // Animator component
    private Animator animator;

    // Animation parameter hashes (more efficient than strings)
    private int isWalkingHash;
    private int isRunningHash;
    private int isIdleHash;
    private int interactHash;
    private int punchHash;
    private int inspectHash;
    private int speedHash;

    // Animation states
    private bool isWalking;
    private bool isRunning;
    private bool isIdle;

    // Timers for one-shot animations
    private float interactTimer;
    private float punchTimer;
    private float inspectTimer;

    void Start()
    {
        // Get components
        animator = GetComponent<Animator>();

        if (playerController == null)
            playerController = GetComponentInParent<FirstPersonController>();

        // Cache animation parameter hashes
        isWalkingHash = Animator.StringToHash("IsWalking");
        isRunningHash = Animator.StringToHash("IsRunning");
        isIdleHash = Animator.StringToHash("IsIdle");
        interactHash = Animator.StringToHash("Interact");
        punchHash = Animator.StringToHash("Punch");
        inspectHash = Animator.StringToHash("Inspect");
        speedHash = Animator.StringToHash("Speed");

        // Subscribe to controller events
        if (playerController != null)
        {
            playerController.OnMovingChanged += OnMovingChanged;
            playerController.OnRunningChanged += OnRunningChanged;
            playerController.OnInteract += OnInteract;
            playerController.OnPunch += OnPunch;
            playerController.OnInspect += OnInspect;
        }

        // Start in idle state
        SetIdleState(true);
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (playerController != null)
        {
            playerController.OnMovingChanged -= OnMovingChanged;
            playerController.OnRunningChanged -= OnRunningChanged;
            playerController.OnInteract -= OnInteract;
            playerController.OnPunch -= OnPunch;
            playerController.OnInspect -= OnInspect;
        }
    }

    void Update()
    {
        if (playerController == null) return;

        UpdateMovementAnimations();
        UpdateTimers();
    }

    void UpdateMovementAnimations()
    {
        float speed = playerController.MovementSpeed;
        bool isMoving = playerController.IsMoving;
        bool isRunningNow = playerController.IsRunning;

        // Smooth speed parameter
        animator.SetFloat(speedHash, speed, animationSmoothing * Time.deltaTime, Time.deltaTime);

        // Determine animation state based on movement
        if (!isMoving || speed < idleThreshold)
        {
            // Idle state
            SetIdleState(true);
            SetWalkingState(false);
            SetRunningState(false);
        }
        else if (isRunningNow)
        {
            // Running state
            SetIdleState(false);
            SetWalkingState(false);
            SetRunningState(true);
        }
        else
        {
            // Walking state
            SetIdleState(false);
            SetWalkingState(true);
            SetRunningState(false);
        }
    }

    void UpdateTimers()
    {
        // Update timers for one-shot animations
        if (interactTimer > 0)
            interactTimer -= Time.deltaTime;

        if (punchTimer > 0)
            punchTimer -= Time.deltaTime;

        if (inspectTimer > 0)
            inspectTimer -= Time.deltaTime;
    }

    void SetIdleState(bool state)
    {
        if (isIdle != state)
        {
            isIdle = state;
            animator.SetBool(isIdleHash, state);
        }
    }

    void SetWalkingState(bool state)
    {
        if (isWalking != state)
        {
            isWalking = state;
            animator.SetBool(isWalkingHash, state);
        }
    }

    void SetRunningState(bool state)
    {
        if (isRunning != state)
        {
            isRunning = state;
            animator.SetBool(isRunningHash, state);
        }
    }

    // Event handlers
    void OnMovingChanged(bool moving)
    {
        // This will be handled in UpdateMovementAnimations
        // but you can add additional logic here if needed
    }

    void OnRunningChanged(bool running)
    {
        // This will be handled in UpdateMovementAnimations
        // but you can add additional logic here if needed
    }

    void OnInteract()
    {
        animator.SetTrigger(interactHash);
        interactTimer = 1f; // Duration to prevent rapid triggering
    }

    void OnPunch()
    {
        if (punchTimer <= 0) // Prevent rapid fire punching
        {
            animator.SetTrigger(punchHash);
            punchTimer = 0.5f; // Cooldown time
        }
    }

    void OnInspect()
    {
        animator.SetTrigger(inspectHash);
        inspectTimer = 1f; // Duration to prevent rapid triggering
    }

    // Public methods for manual animation control
    public void PlayInteractAnimation()
    {
        OnInteract();
    }

    public void PlayPunchAnimation()
    {
        OnPunch();
    }

    public void PlayInspectAnimation()
    {
        OnInspect();
    }

    // Method to force specific animation states (useful for debugging)
    public void ForceAnimationState(string stateName)
    {
        switch (stateName.ToLower())
        {
            case "idle":
                SetIdleState(true);
                SetWalkingState(false);
                SetRunningState(false);
                break;
            case "walk":
                SetIdleState(false);
                SetWalkingState(true);
                SetRunningState(false);
                break;
            case "run":
                SetIdleState(false);
                SetWalkingState(false);
                SetRunningState(true);
                break;
        }
    }

    // Get current animation info
    public bool IsPlayingAnimation(string animationName)
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(animationName);
    }

    public float GetCurrentAnimationTime()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.normalizedTime;
    }
}