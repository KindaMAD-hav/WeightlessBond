using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float loseTargetRange = 15f;

    [Header("Combat Settings")]
    public float attackCooldown = 2f;
    public float attackDamage = 20f;

    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Movement Settings")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;

    [Header("References")]
    public Transform player;
    public LayerMask playerLayer = 1;

    // Private references
    private NavMeshAgent agent;
    private Animator animator;

    // State
    private float lastAttackTime = Mathf.NegativeInfinity;
    private bool isDead = false;
    private bool hasTarget = false;

    // Animator parameters (must match Animator Controller)
    private const string IS_RUNNING = "IsRunning";
    private const string ATTACK = "Attack";
    private const string GET_HIT = "GetHit";
    private const string DEATH = "Death";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (!hasTarget && distance <= detectionRange && CanSeePlayer())
        {
            hasTarget = true;
        }

        if (hasTarget)
        {
            if (distance > loseTargetRange)
            {
                LoseTarget();
            }
            else if (distance <= attackRange)
            {
                AttackBehavior();
            }
            else
            {
                ChaseBehavior();
            }
        }
        else
        {
            IdleBehavior();
        }
    }

    #region Behaviors

    private void IdleBehavior()
    {
        StopAgent();
        animator.SetBool(IS_RUNNING, false);
    }

    private void ChaseBehavior()
    {
        if (!agent.enabled) return;

        agent.speed = runSpeed;
        agent.SetDestination(player.position);
        animator.SetBool(IS_RUNNING, true);
    }

    private void AttackBehavior()
    {
        StopAgent();
        LookAtPlayer();
        animator.SetBool(IS_RUNNING, false);

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            animator.SetTrigger(ATTACK);

            // Apply damage
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }

    private void LoseTarget()
    {
        hasTarget = false;
        StopAgent();
        animator.SetBool(IS_RUNNING, false);
    }

    #endregion

    #region Utilities

    private void StopAgent()
    {
        if (agent.enabled)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    private void LookAtPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 8f);
        }
    }

    private bool CanSeePlayer()
    {
        Vector3 origin = transform.position + Vector3.up; // eye level
        Vector3 dir = (player.position - origin).normalized;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, detectionRange, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == player;
        }
        return false;
    }

    #endregion

    #region Combat & Health

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        animator.SetTrigger(GET_HIT);

        if (!hasTarget) hasTarget = true;

        Debug.Log($"{name} took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        hasTarget = false;

        StopAgent();
        agent.enabled = false;

        animator.SetTrigger(DEATH);

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        Debug.Log($"{name} has died!");
        Destroy(gameObject, 3f);
    }

    public float GetHealthPercentage() => currentHealth / maxHealth;

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
    }

    #endregion
}
