using UnityEngine;
using UnityEngine.AI;

public class VampireHunter : MonoBehaviour
{
    [Header("Hunter Stats")]
    public float health = 100f;
    public float maxHealth = 100f;
    public float damage = 25f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;

    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;
    public float turnSpeed = 120f;

    [Header("Detection")]
    public float detectionRange = 20f;
    public float fieldOfView = 90f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    [Header("Tracking")]
    public float trackingRange = 15f;
    public float trackingTime = 10f;
    public float searchRadius = 8f;

    [Header("Equipment")]
    public GameObject crossbowPrefab;
    public GameObject holyWaterPrefab;
    public GameObject garlicBombPrefab;
    public Transform weaponHand;
    [HideInInspector] public Projectile crossbow;
    [HideInInspector] public AreaEffect holyWater;
    [HideInInspector] public AreaEffect garlicBomb;

    [Header("Visual Effects")]
    public GameObject detectionEffect;
    public GameObject attackEffect;
    public GameObject deathEffect;

    [Header("Audio")]
    public AudioClip detectionSound;
    public AudioClip attackSound;
    public AudioClip deathSound;
    public AudioClip footstepsSound;

    // AI States
    private enum HunterState { Patrol, Chase, Attack, Search, Retreat }
    private HunterState currentState = HunterState.Patrol;

    // Components
    private NavMeshAgent agent;
    private Animator animator;
    private AudioSource audioSource;
    private Transform player;
    private VampireStats playerStats;
    private PlayerHealth playerHealth;

    // State variables
    private Vector3 lastKnownPlayerPos;
    private float lastAttackTime;
    private float searchTimer;
    private float trackingTimer;
    private bool isDead = false;

    // Patrol points
    private Vector3[] patrolPoints;
    private int currentPatrolIndex = 0;
    private Vector3 spawnPosition;

    // Equipment cooldowns
    private float crossbowCooldown = 0f;
    private float holyWaterCooldown = 0f;
    private float garlicBombCooldown = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerStats = playerObj.GetComponent<VampireStats>();
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        // Initialize patrol
        spawnPosition = transform.position;
        GeneratePatrolPoints();

        // Set initial state
        agent.speed = walkSpeed;
        currentState = HunterState.Patrol;

        // Start patrol
        SetNextPatrolDestination();
    }

    void Update()
    {
        if (isDead) return;

        // Update cooldowns
        UpdateCooldowns();

        // Update state machine
        UpdateStateMachine();

        // Update animations
        UpdateAnimations();
    }

    void UpdateCooldowns()
    {
        if (crossbowCooldown > 0) crossbowCooldown -= Time.deltaTime;
        if (holyWaterCooldown > 0) holyWaterCooldown -= Time.deltaTime;
        if (garlicBombCooldown > 0) garlicBombCooldown -= Time.deltaTime;
    }

    void UpdateStateMachine()
    {
        switch (currentState)
        {
            case HunterState.Patrol:
                Patrol();
                if (CanSeePlayer())
                {
                    TransitionToChase();
                }
                break;

            case HunterState.Chase:
                Chase();
                if (Vector3.Distance(transform.position, player.position) <= attackRange)
                {
                    TransitionToAttack();
                }
                else if (!CanSeePlayer())
                {
                    TransitionToSearch();
                }
                break;

            case HunterState.Attack:
                Attack();
                if (Vector3.Distance(transform.position, player.position) > attackRange)
                {
                    TransitionToChase();
                }
                break;

            case HunterState.Search:
                Search();
                if (CanSeePlayer())
                {
                    TransitionToChase();
                }
                else if (searchTimer <= 0)
                {
                    TransitionToPatrol();
                }
                break;

            case HunterState.Retreat:
                Retreat();
                break;
        }
    }

    void Patrol()
    {
        if (agent.remainingDistance < 0.5f)
        {
            SetNextPatrolDestination();
        }
    }

    void Chase()
    {
        if (player == null) return;

        lastKnownPlayerPos = player.position;
        agent.SetDestination(player.position);
        agent.speed = runSpeed;

        // Use ranged weapons if available
        if (crossbowCooldown <= 0 && Vector3.Distance(transform.position, player.position) <= trackingRange)
        {
            UseCrossbow();
        }
    }

    void Attack()
    {
        if (player == null) return;

        // Face the player
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // Attack if cooldown is ready
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            PerformMeleeAttack();
        }

        // Use special weapons
        if (holyWaterCooldown <= 0)
        {
            UseHolyWater();
        }

        if (garlicBombCooldown <= 0)
        {
            UseGarlicBomb();
        }
    }

    void Search()
    {
        searchTimer -= Time.deltaTime;

        if (agent.remainingDistance < 0.5f)
        {
            // Search in a random direction
            Vector3 randomDirection = Random.insideUnitSphere * searchRadius;
            randomDirection.y = 0;
            Vector3 searchPoint = lastKnownPlayerPos + randomDirection;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(searchPoint, out hit, searchRadius, 1))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    void Retreat()
    {
        if (agent.remainingDistance < 0.5f)
        {
            TransitionToPatrol();
        }
    }

    void TransitionToChase()
    {
        currentState = HunterState.Chase;
        agent.speed = runSpeed;

        if (detectionSound != null)
            audioSource.PlayOneShot(detectionSound);

        if (detectionEffect != null)
            Instantiate(detectionEffect, transform.position, Quaternion.identity);

        Debug.Log("Vampire Hunter detected the player!");
    }

    void TransitionToAttack()
    {
        currentState = HunterState.Attack;
        agent.isStopped = true;
    }

    void TransitionToSearch()
    {
        currentState = HunterState.Search;
        searchTimer = trackingTime;
        agent.speed = walkSpeed;
        agent.isStopped = false;
    }

    void TransitionToPatrol()
    {
        currentState = HunterState.Patrol;
        agent.speed = walkSpeed;
        agent.isStopped = false;
        SetNextPatrolDestination();
    }

    void TransitionToRetreat()
    {
        currentState = HunterState.Retreat;
        agent.speed = runSpeed;
        agent.SetDestination(spawnPosition);
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (distanceToPlayer <= detectionRange && angle <= fieldOfView * 0.5f)
        {
            if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer))
            {
                return true;
            }
        }

        return false;
    }

    void PerformMeleeAttack()
    {
        lastAttackTime = Time.time;

        // Damage player if in range
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        if (attackSound != null)
            audioSource.PlayOneShot(attackSound);

        if (attackEffect != null)
            Instantiate(attackEffect, transform.position + transform.forward, Quaternion.identity);

        Debug.Log($"Vampire Hunter attacked for {damage} damage!");
    }

    void UseCrossbow()
    {
        if (crossbowPrefab == null) return;

        crossbowCooldown = 3f;

        // Create crossbow bolt
        Vector3 spawnPos = transform.position + transform.forward + Vector3.up;
        GameObject bolt = Instantiate(crossbowPrefab, spawnPos, transform.rotation);

        // Add projectile component
        Projectile projectile = bolt.GetComponent<Projectile>();
        if (projectile == null)
        {
            projectile = bolt.AddComponent<Projectile>();
        }

        projectile.Initialize(player.position - spawnPos, 15f, damage * 0.5f, playerLayer, 5f);

        GameLogger.Log(LogCategory.AI, "Vampire Hunter fired crossbow!", this);
    }

    void UseHolyWater()
    {
        if (holyWaterPrefab == null) return;

        holyWaterCooldown = 8f;

        // Throw holy water at player
        Vector3 spawnPos = transform.position + transform.forward + Vector3.up;
        GameObject holyWaterObj = Instantiate(holyWaterPrefab, spawnPos, Quaternion.identity);

        // Add projectile component
        Projectile projectile = holyWaterObj.GetComponent<Projectile>();
        if (projectile == null)
        {
            projectile = holyWaterObj.AddComponent<Projectile>();
        }

        projectile.Initialize(player.position - spawnPos, 8f, damage * 0.3f, playerLayer, 5f);

        GameLogger.Log(LogCategory.AI, "Vampire Hunter threw holy water!", this);
    }

    void UseGarlicBomb()
    {
        if (garlicBombPrefab == null) return;

        garlicBombCooldown = 12f;

        // Throw garlic bomb
        Vector3 spawnPos = transform.position + transform.forward + Vector3.up;
        GameObject garlicBombObj = Instantiate(garlicBombPrefab, spawnPos, Quaternion.identity);

        // Add area effect component
        AreaEffect areaEffect = garlicBombObj.GetComponent<AreaEffect>();
        if (areaEffect == null)
        {
            areaEffect = garlicBombObj.AddComponent<AreaEffect>();
        }

        areaEffect.Initialize(5f, damage * 0.2f, 3f, playerLayer, true);

        GameLogger.Log(LogCategory.AI, "Vampire Hunter threw garlic bomb!", this);
    }

    void GeneratePatrolPoints()
    {
        patrolPoints = new Vector3[5];
        Vector3 center = spawnPosition;
        float radius = 15f;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float angle = i * (360f / patrolPoints.Length);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 point = center + direction * radius;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(point, out hit, radius, 1))
            {
                patrolPoints[i] = hit.position;
            }
            else
            {
                patrolPoints[i] = point;
            }
        }
    }

    void SetNextPatrolDestination()
    {
        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[currentPatrolIndex]);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // Set movement speed
        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);

        // Set state-based parameters
        animator.SetBool("IsChasing", currentState == HunterState.Chase);
        animator.SetBool("IsAttacking", currentState == HunterState.Attack);
        animator.SetBool("IsSearching", currentState == HunterState.Search);
        animator.SetBool("IsRetreating", currentState == HunterState.Retreat);
        animator.SetBool("IsPatrolling", currentState == HunterState.Patrol);
        animator.SetBool("IsDead", isDead);

        // Set attack trigger
        if (currentState == HunterState.Attack && Time.time - lastAttackTime < 0.5f)
        {
            animator.SetTrigger("Attack");
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;

        if (health <= 0)
        {
            Die();
        }
        else if (health < maxHealth * 0.3f)
        {
            // Retreat when low health
            TransitionToRetreat();
        }
    }

    void Die()
    {
        isDead = true;

        if (deathSound != null)
            audioSource.PlayOneShot(deathSound);

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        Debug.Log("Vampire Hunter defeated!");

        // Disable components
        if (agent != null) agent.enabled = false;
        if (animator != null) animator.SetTrigger("Die");

        // Destroy after delay
        Destroy(gameObject, 3f);
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw field of view
        Gizmos.color = Color.cyan;
        Vector3 forward = transform.forward;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-fieldOfView * 0.5f, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(fieldOfView * 0.5f, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;
        Gizmos.DrawRay(transform.position, leftRayDirection * detectionRange);
        Gizmos.DrawRay(transform.position, rightRayDirection * detectionRange);

        // Draw patrol points
        if (patrolPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (Vector3 point in patrolPoints)
            {
                Gizmos.DrawWireSphere(point, 0.5f);
            }
        }
    }

    public void Initialize()
    {
        if (crossbowPrefab != null)
        {
            GameObject crossbowInstance = Instantiate(crossbowPrefab, weaponHand);
            crossbow = crossbowInstance.GetComponent<Projectile>();
        }

        if (holyWaterPrefab != null)
        {
            GameObject holyWaterInstance = Instantiate(holyWaterPrefab, weaponHand);
            holyWater = holyWaterInstance.GetComponent<AreaEffect>();
        }

        if (garlicBombPrefab != null)
        {
            GameObject garlicBombInstance = Instantiate(garlicBombPrefab, weaponHand);
            garlicBomb = garlicBombInstance.GetComponent<AreaEffect>();
        }
    }
}