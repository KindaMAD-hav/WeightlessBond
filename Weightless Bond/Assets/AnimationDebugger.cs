using UnityEngine;

public class AnimationDebugger : MonoBehaviour
{
    public FirstPersonController playerController;
    public FirstPersonAnimator fpsAnimator;
    public Animator animator;

    void Start()
    {
        if (playerController == null)
            playerController = FindObjectOfType<FirstPersonController>();

        if (fpsAnimator == null)
            fpsAnimator = GetComponent<FirstPersonAnimator>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (playerController != null)
        {
            Debug.Log($"Player Moving: {playerController.IsMoving}, Running: {playerController.IsRunning}, Speed: {playerController.MovementSpeed}");
        }

        if (animator != null)
        {
            Debug.Log($"Animator - IsWalking: {animator.GetBool("IsWalking")}, IsRunning: {animator.GetBool("IsRunning")}, IsIdle: {animator.GetBool("IsIdle")}");

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"Current Animation State: {stateInfo.shortNameHash}");
        }
    }
}