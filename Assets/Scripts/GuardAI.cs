using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class GuardAI : MonoBehaviour, ISpatialEntity
{
    // Patrol settings
    public Waypoint[] patrolPoints;
    public float waitTimeMin = 2f;        // Minimum waiting time at a patrol point.
    public float waitTimeMax = 5f;        // Maximum waiting time at a patrol point.

    private int currentPointIndex = 0;
    private NavMeshAgent agent;

    // Multi-entity waypoint support
    [Header("Waypoint Group Support")]
    public WaypointGroup assignedWaypointGroup; // Reference to the waypoint group
    public int currentPatrolIndex = 0;          // Current waypoint index in the patrol
    public bool patrolForward = true;           // Direction of patrol

    // State machine
    public enum GuardState { Patrol, Chase, Attack, Alert, Search }
    public GuardState currentState = GuardState.Patrol;

    // Vision settings
    [Header("Vision Settings")]
    public float fieldOfView = 90f;         // Full cone angle (degrees) - increased from 75f
    public float viewDistance = 40f;        // Maximum distance to see the player - increased from 35f
    public float detectionTime = 0.25f;     // Time to detect player (reduced for faster detection)
    public float closeRangeDetectionTime = 0.05f; // Near-instant detection when very close
    public float closeRangeDistance = 6f;   // Distance for instant detection
    public bool enablePeripheralVision = true; // Detect movement in peripheral vision
    public float peripheralVisionAngle = 140f; // Wider angle for peripheral detection
    public float peripheralDetectionTime = 0.8f; // Slower detection for peripheral vision
    public LayerMask playerLayer;           // Should match the Player's layer
    public LayerMask obstacleLayer;         // Layers that block vision (walls, etc.)

    [Header("Enhanced Detection")]
    public bool useFlashlight = true;       // Guards can use flashlights
    public Light flashlight;                // Reference to flashlight
    public float flashlightRange = 20f;     // Additional range when using flashlight
    public float flashlightAngle = 30f;     // Flashlight cone angle
    public float soundDetectionRange = 15f; // Range to detect player sounds
    public float suspicionDecayRate = 0.5f; // How fast suspicion decreases

    [Header("Sound Detection Settings")]
    public float walkingSoundRange = 8f;    // Range to hear walking
    public float runningSoundRange = 15f;   // Range to hear running
    public float crouchSoundReduction = 0.1f; // Sound multiplier when crouching (90% quieter)
    public float minSoundThreshold = 2f;    // Minimum speed to make detectable sound

    // Chase settings
    [Header("Chase Settings")]
    public float timeToLosePlayer = 5f;     // Time the guard must lose sight before switching to alert mode - increased from 3f
    public float chaseSpeed = 8f;           // Speed when chasing player
    public float patrolSpeed = 3.5f;        // Speed when patrolling
    public float attackRange = 2f;          // Distance at which guard stops and attacks
    public float attackCooldown = 1.5f;     // Time between attacks
    public float attackDamage = 25f;        // Damage dealt per attack
    public float persistentChaseTime = 10f; // How long to keep chasing even if player is lost

    // Alert settings
    [Header("Alert Settings")]
    public float alertDuration = 5f;        // Time guard stays in alert mode before resuming patrol

    private float lostTimer = 0f;           // Tracks how long the player has been out of sight
    private float alertTimer = 0f;          // Tracks time in alert mode
    private Vector3 lastKnownPlayerPos;     // Remember the last seen position
    private float detectionTimer = 0f;      // Tracks detection progress
    private float lastAttackTime = 0f;      // Tracks when last attack occurred
    private float attackTimer = 0f;         // Tracks attack state timing
    private bool isAttacking = false;       // Whether currently attacking
    private bool hasSpotted = false;        // Flag to ensure we count a spotting event only once per loss.
    private bool waiting = false;
    private float persistentChaseTimer = 0f; // Tracks persistent chase time

    // Reference to the player transform
    private Transform player;

    // Enhanced AI variables
    private Vector3 lastPlayerPosition;
    private float playerMovementSpeed;
    private float suspicionLevel = 0f;      // 0-1 suspicion level
    private bool isInvestigatingNoise = false;
    private Vector3 lastHeardSound;
    private float lastSoundTime;
    private List<Vector3> playerTrail = new List<Vector3>(); // Track player movement
    private float trailUpdateTime = 0f;
    private Vector3 predictedPlayerPosition;

    private VampireStats playerStats;

    public bool isHypnotized = false;

    [Header("Alertness Settings")]
    public GuardAlertnessLevel currentAlertness = GuardAlertnessLevel.Normal;
    private float baseViewDistance;
    private float baseFieldOfView;
    private float baseDetectionTime;

    // Additional AI variables
    public float walkSpeed = 3.5f;
    public float soundSensitivity = 1f;
    public float reactionTime = 0.5f;
    private float baseSoundSensitivity;
    private float baseReactionTime;
    private Vector3 lastKnownPlayerPosition;
    private float currentSpeed;
    private float baseAlertRadius;
    private GuardAlertnessManager alertnessManager;

    [Header("Event Effects")]
    private float speedMultiplier = 1f;
    private float patrolFrequencyMultiplier = 1f;
    private float baseWaitTimeMin;
    private float baseWaitTimeMax;

    [Header("Advanced AI")]
    public bool enableGuardCommunication = true;
    public float communicationRange = 25f;
    public float searchRadius = 12f;
    public int maxSearchPoints = 8;
    public float investigationTime = 2f;
    public bool enablePredictiveChasing = true;
    public float predictionTime = 1.5f;     // How far ahead to predict player movement

    [Header("Coordination")]
    public bool canCallReinforcements = true;
    public float reinforcementRadius = 30f;
    public float formationDistance = 3f;    // Distance to maintain from other guards
    public bool enableFlankingManeuvers = true;

    [Header("Visual Feedback")]
    public Light guardLight;
    public Color patrolColor = Color.green;
    public Color chaseColor = Color.red;
    public Color alertColor = Color.yellow;
    public Color searchColor = Color.blue;

    [Header("Audio")]
    public AudioClip detectionSound;
    public AudioClip alertSound;
    public AudioClip searchSound;
    public AudioClip communicationSound;
    public AudioClip attackSound;

    // Advanced state variables
    private Vector3[] searchPoints;
    private int currentSearchIndex = 0;
    private float investigationTimer = 0f;
    private bool isInvestigating = false;
    private GuardAI[] nearbyGuards;
    private AudioSource audioSource;
    private Animator animator;

    // ISpatialEntity implementation
    public Vector3 Position => transform.position;
    public Transform Transform => transform;

    // Debug properties - exposed for debugging purposes
    public float DetectionProgress { get; private set; }
    public Vector3 LastKnownPlayerPosition => lastKnownPlayerPos;
    public bool HasSpottedPlayer => hasSpotted;
    public float LostTimer => lostTimer;
    public float AlertTimer => alertTimer;

    // Combat settings
    [Header("Combat Settings")]
    public bool canAttack = true;
    public float attackWindupTime = 0.3f;   // Time to wind up attack
    public float attackRecoveryTime = 0.5f; // Time to recover after attack
    public bool useMeleeWeapon = true;
    public GameObject meleeWeapon;          // Weapon model to show/hide
    public Transform weaponHand;            // Where to attach weapon

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Start patrolling if patrol points are set
        if (patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[currentPointIndex].transform.position);

        // Find the player by tag (make sure your player is tagged "Player")
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerStats = playerObj.GetComponent<VampireStats>();
        }

        // Store base values
        baseViewDistance = viewDistance;
        baseFieldOfView = fieldOfView;
        baseDetectionTime = detectionTime;
        baseAlertRadius = 15f; // Assuming this is the base alert radius

        // Register with alertness manager
        alertnessManager = GuardAlertnessManager.instance;
        if (alertnessManager != null)
            alertnessManager.RegisterGuard(this);

        // Store base wait times
        baseWaitTimeMin = waitTimeMin;
        baseWaitTimeMax = waitTimeMax;

        // Initialize search points array
        searchPoints = new Vector3[maxSearchPoints];

        // Set up melee weapon
        SetupMeleeWeapon();

        // Set initial speeds
        agent.speed = patrolSpeed;

        // Set initial visual state
        UpdateVisualState();

        // Register with spatial grid for performance optimization
        if (SpatialGrid.Instance != null)
        {
            SpatialGrid.Instance.RegisterEntity(this);
        }
    }

    void OnDestroy()
    {
        // Unregister from spatial grid
        if (SpatialGrid.Instance != null)
        {
            SpatialGrid.Instance.UnregisterEntity(this);
        }

        // Unregister from alertness manager
        if (alertnessManager != null)
        {
            alertnessManager.UnregisterGuard(this);
        }
    }

    void Update()
    {
        if (isHypnotized)
        {
            Patrol();
            UpdateAnimator();
            return;
        }

        GuardState previousState = currentState;

        // Run detection regardless of state
        DetectPlayer();

        switch (currentState)
        {
            case GuardState.Patrol:
                Patrol();
                if (CanSeePlayer())
                {
                    ImprovedStateTransition(GuardState.Chase);
                    lostTimer = 0f;
                    persistentChaseTimer = 0f;

                    if (!hasSpotted)
                    {
                        GameManager.instance?.IncrementSpotted();
                        if (DynamicObjectiveSystem.Instance != null)
                        {
                            DynamicObjectiveSystem.Instance.OnPlayerDetected();
                        }
                        hasSpotted = true;
                    }
                }
                break;

            case GuardState.Chase:
                Chase();
                // Check if player is in attack range
                if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange && canAttack)
                {
                    ImprovedStateTransition(GuardState.Attack);
                    attackTimer = 0f;
                    isAttacking = false;
                }
                // If player is visible, reset the lost timer
                else if (CanSeePlayer())
                {
                    lostTimer = 0f;
                    persistentChaseTimer = 0f;
                }
                else
                {
                    lostTimer += Time.deltaTime;
                    persistentChaseTimer += Time.deltaTime;
                    // If player is out of sight for too long, switch to Alert state
                    if (lostTimer >= timeToLosePlayer && persistentChaseTimer >= persistentChaseTime)
                    {
                        ImprovedStateTransition(GuardState.Alert);
                        alertTimer = 0f;
                        // Continue to head to the last known player position
                        agent.SetDestination(lastKnownPlayerPos);
                    }
                }
                break;

            case GuardState.Attack:
                Attack();
                // If player moves out of attack range, resume chase
                if (player != null && Vector3.Distance(transform.position, player.position) > attackRange)
                {
                    ImprovedStateTransition(GuardState.Chase);
                }
                break;

            case GuardState.Alert:
                AlertMode();
                // If the player is spotted again, resume chase
                if (CanSeePlayer())
                {
                    ImprovedStateTransition(GuardState.Chase);
                    lostTimer = 0f;
                    persistentChaseTimer = 0f;
                }
                else
                {
                    alertTimer += Time.deltaTime;
                    // After alert duration, return to patrol
                    if (alertTimer >= alertDuration)
                    {
                        ImprovedStateTransition(GuardState.Patrol);
                        lostTimer = 0f;
                        persistentChaseTimer = 0f;
                        if (patrolPoints.Length > 0)
                            agent.SetDestination(patrolPoints[currentPointIndex].transform.position);
                    }
                }
                break;

            case GuardState.Search:
                SearchMode();
                // If the player is spotted, switch to chase
                if (CanSeePlayer())
                {
                    ImprovedStateTransition(GuardState.Chase);
                    lostTimer = 0f;
                    isInvestigating = false;
                }
                break;
        }

        // Update suspicion decay
        if (currentState == GuardState.Patrol && suspicionLevel > 0f)
        {
            suspicionLevel = Mathf.Max(0f, suspicionLevel - Time.deltaTime * suspicionDecayRate);
        }

        // Update visual state
        UpdateVisualState();

        // Update animator
        UpdateAnimator();

        // Update spatial grid position for performance optimization
        if (SpatialGrid.Instance != null)
        {
            SpatialGrid.Instance.UpdateEntity(this);
        }
    }

    // Checks if the player is within the guard's vision cone
    bool CanSeePlayer()
    {
        if (isHypnotized) return false;
        if (player == null)
            return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        float effectiveSpotDistance = playerStats != null ? playerStats.spotDistance : viewDistance;

        // Use half of the full FOV for the cone check
        if (distanceToPlayer <= effectiveSpotDistance && angle <= fieldOfView * 0.5f)
        {
            // Optionally, perform a raycast to ensure there are no obstacles blocking the view
            if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer))
            {
                lastKnownPlayerPos = player.position; // update last known position

                // Notify alertness manager if player is spotted
                if (alertnessManager != null && currentState == GuardState.Patrol)
                {
                    alertnessManager.OnPlayerSpotted(player.position);
                }

                return true;
            }
        }
        return false;
    }

    // Enhanced detection with peripheral vision and progressive detection
    void DetectPlayer()
    {
        if (isHypnotized || player == null) return;

        // Calculate player movement
        if (lastPlayerPosition != Vector3.zero)
        {
            playerMovementSpeed = (player.position - lastPlayerPosition).magnitude / Time.deltaTime;

            // Update player trail for prediction
            if (Time.time - trailUpdateTime > 0.5f)
            {
                playerTrail.Add(player.position);
                if (playerTrail.Count > 5) playerTrail.RemoveAt(0);
                trailUpdateTime = Time.time;
            }
        }
        lastPlayerPosition = player.position;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float effectiveSpotDistance = playerStats != null ? playerStats.spotDistance : viewDistance;

        // Flashlight extends range in darkness
        bool usingFlashlight = useFlashlight && flashlight != null && flashlight.enabled;
        if (usingFlashlight)
        {
            float flashlightDistance = Vector3.Distance(transform.position, player.position);
            Vector3 flashlightDirection = flashlight.transform.forward;
            float flashlightAngleToPlayer = Vector3.Angle(flashlightDirection, directionToPlayer);

            if (flashlightAngleToPlayer < flashlightAngle * 0.5f && flashlightDistance <= flashlightRange)
            {
                effectiveSpotDistance = Mathf.Max(effectiveSpotDistance, flashlightRange);
            }
        }

        // Check if player is within view distance and within the cone
        bool inDirectView = distanceToPlayer < effectiveSpotDistance && angle < fieldOfView * 0.5f;
        bool inPeripheralView = false;
        bool inFlashlightBeam = false;

        // Check peripheral vision if enabled
        if (enablePeripheralVision && distanceToPlayer < effectiveSpotDistance && angle < peripheralVisionAngle * 0.5f)
        {
            inPeripheralView = true;
        }

        // Check flashlight beam
        if (usingFlashlight)
        {
            Vector3 flashlightDirection = flashlight.transform.forward;
            float flashlightAngleToPlayer = Vector3.Angle(flashlightDirection, directionToPlayer);
            if (flashlightAngleToPlayer < flashlightAngle * 0.5f && distanceToPlayer <= flashlightRange)
            {
                inFlashlightBeam = true;
            }
        }

        // Check for line of sight
        bool hasLineOfSight = !Physics.Raycast(transform.position + Vector3.up * 1.5f, directionToPlayer, distanceToPlayer, obstacleLayer);

        if ((inDirectView || inPeripheralView || inFlashlightBeam) && hasLineOfSight)
        {
            // Increase suspicion
            suspicionLevel = Mathf.Min(1f, suspicionLevel + Time.deltaTime * 2f);

            // If not already in chase or attack mode, accumulate detection time
            if (currentState == GuardState.Patrol || currentState == GuardState.Alert)
            {
                // Determine detection time based on distance, view type, and movement
                float effectiveDetectionTime = detectionTime;

                // Close range detection is much faster
                if (distanceToPlayer <= closeRangeDistance)
                {
                    effectiveDetectionTime = closeRangeDetectionTime;
                }
                // Peripheral vision is slower
                else if (inPeripheralView && !inDirectView && !inFlashlightBeam)
                {
                    effectiveDetectionTime = peripheralDetectionTime;
                }
                else
                {
                    // Distance-based scaling
                    float distanceRatio = distanceToPlayer / effectiveSpotDistance;
                    effectiveDetectionTime = detectionTime * (0.5f + distanceRatio * 0.5f);
                }

                // Movement affects detection (moving targets are easier to spot)
                if (playerMovementSpeed > 5f) // Running
                {
                    effectiveDetectionTime *= 0.4f;
                }
                else if (playerMovementSpeed > 2f) // Walking
                {
                    effectiveDetectionTime *= 0.7f;
                }
                // Crouching/still is harder to detect

                // Flashlight makes detection faster
                if (inFlashlightBeam)
                {
                    effectiveDetectionTime *= 0.5f;
                }

                // Lighting affects detection
                float lightingModifier = GetLightingModifier();
                effectiveDetectionTime /= lightingModifier;

                detectionTimer += Time.deltaTime;
                DetectionProgress = Mathf.Clamp01(detectionTimer / effectiveDetectionTime);

                if (detectionTimer >= effectiveDetectionTime)
                {
                    ImprovedStateTransition(GuardState.Chase);
                    lostTimer = 0f;
                    persistentChaseTimer = 0f;
                    detectionTimer = 0f;
                    DetectionProgress = 1f;
                    suspicionLevel = 1f;

                    if (!hasSpotted)
                    {
                        GameManager.instance?.IncrementSpotted();
                        if (DynamicObjectiveSystem.Instance != null)
                        {
                            DynamicObjectiveSystem.Instance.OnPlayerDetected();
                        }
                        hasSpotted = true;
                    }
                }
            }

            lastKnownPlayerPos = player.position;

            // Update prediction
            if (enablePredictiveChasing)
            {
                UpdatePlayerPrediction();
            }

            return;
        }

        // Decrease suspicion when player is not visible
        suspicionLevel = Mathf.Max(0f, suspicionLevel - Time.deltaTime * suspicionDecayRate);

        // Check for sound detection
        DetectPlayerSounds();

        // If not in view and in patrol mode, reset the detection timer
        if (currentState == GuardState.Patrol)
        {
            detectionTimer = 0f;
            DetectionProgress = 0f;
        }
    }

    // Patrol mode: move between patrol points
    void Patrol()
    {
        // Set patrol speed (affected by suspicion)
        float effectivePatrolSpeed = patrolSpeed * (1f + suspicionLevel * 0.5f);
        agent.speed = effectivePatrolSpeed;
        agent.isStopped = false;

        // More frequent stops when suspicious
        if (suspicionLevel > 0.5f && Random.value < 0.02f)
        {
            // Pause and look around
            agent.isStopped = true;
            transform.Rotate(0f, 30f * Time.deltaTime, 0f);

            if (Random.value < 0.1f)
            {
                agent.isStopped = false;
            }
        }

        // Turn off flashlight during normal patrol
        if (useFlashlight && flashlight != null && suspicionLevel < 0.3f)
        {
            flashlight.enabled = false;
        }
        else if (useFlashlight && flashlight != null && suspicionLevel > 0.5f)
        {
            // Use flashlight when suspicious
            flashlight.enabled = true;
        }

        if (waiting || patrolPoints.Length == 0)
            return;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            StartCoroutine(WaitAtPoint());
        }
    }

    IEnumerator WaitAtPoint()
    {
        waiting = true;
        // Look in the direction of the waypoint's forward vector.
        if (patrolPoints[currentPointIndex].transform != null)
        {
            transform.rotation = patrolPoints[currentPointIndex].transform.rotation;
        }

        // Wait for a random time between min and max.
        float waitTime = Random.Range(waitTimeMin, waitTimeMax);
        yield return new WaitForSeconds(waitTime);

        waiting = false;
        // Move to the next point in the array.
        MoveToNextPatrolPoint();
        Vector3 targetPosition = GetCurrentWaypointPosition();
        agent.SetDestination(targetPosition);
    }

    // Chase mode: head towards the player's current position or last known position
    void Chase()
    {
        // Set chase speed
        agent.speed = chaseSpeed;
        agent.isStopped = false;

        Vector3 targetPosition = lastKnownPlayerPos;

        if (player != null && CanSeePlayer())
        {
            targetPosition = player.position;

            // Use predictive chasing if enabled
            if (enablePredictiveChasing && playerMovementSpeed > 1f)
            {
                targetPosition = GetPredictedPlayerPosition();
            }

            // Use flanking maneuvers if enabled and other guards are nearby
            if (enableFlankingManeuvers && Random.value < 0.3f)
            {
                Collider[] nearbyGuards = Physics.OverlapSphere(transform.position, communicationRange, LayerMask.GetMask("Guard"));
                if (nearbyGuards.Length > 1) // Other guards present
                {
                    Vector3 flankPosition = GetFlankingPosition(targetPosition);
                    if (Vector3.Distance(flankPosition, targetPosition) > 3f) // Only flank if it's a significant change
                    {
                        targetPosition = flankPosition;
                    }
                }
            }
        }

        // Avoid other guards to prevent clustering
        targetPosition = AvoidOtherGuards(targetPosition);

        agent.SetDestination(targetPosition);

        // Enable flashlight during chase
        if (useFlashlight && flashlight != null)
        {
            flashlight.enabled = true;
            // Point flashlight towards target
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            flashlight.transform.rotation = Quaternion.LookRotation(directionToTarget);
        }
    }

    // Rotates the guard to face the player
    void FacePlayer()
    {
        if (player == null) return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f; // Only rotate around the Y axis
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    // Alert mode: guard remains at (or searches around) the last known player position
    void AlertMode()
    {
        if (!isInvestigating && !isInvestigatingNoise)
        {
            // Generate search points around last known position
            GenerateSearchPoints();
            isInvestigating = true;
            currentSearchIndex = 0;
            investigationTimer = 0f;

            if (searchSound != null)
                audioSource.PlayOneShot(searchSound);

            // Enable flashlight during search
            if (useFlashlight && flashlight != null)
            {
                flashlight.enabled = true;
            }
        }

        // Handle noise investigation
        if (isInvestigatingNoise)
        {
            InvestigateNoise();
            return;
        }

        // Move to current search point
        if (currentSearchIndex < searchPoints.Length)
        {
            if (agent.remainingDistance < 0.5f)
            {
                // Investigate current point - look around
                investigationTimer += Time.deltaTime;

                // Slowly rotate to scan area
                transform.Rotate(0f, 60f * Time.deltaTime, 0f);

                // Point flashlight in direction we're looking
                if (useFlashlight && flashlight != null)
                {
                    flashlight.transform.rotation = transform.rotation;
                }

                if (investigationTimer >= investigationTime)
                {
                    // Move to next search point
                    currentSearchIndex++;
                    investigationTimer = 0f;

                    if (currentSearchIndex < searchPoints.Length)
                    {
                        agent.SetDestination(searchPoints[currentSearchIndex]);
                    }
                }
            }
        }
        else
        {
            // Search complete, return to patrol
            isInvestigating = false;
            ImprovedStateTransition(GuardState.Patrol);
            lostTimer = 0f;
            suspicionLevel = Mathf.Max(0.3f, suspicionLevel); // Remain slightly suspicious

            // Turn off flashlight
            if (useFlashlight && flashlight != null)
            {
                flashlight.enabled = false;
            }

            if (patrolPoints.Length > 0)
                agent.SetDestination(patrolPoints[currentPointIndex].transform.position);
        }
    }

    // External method to be called by a citizen when alerting guards.
    // This immediately puts the guard into chase mode.
    public void Alert(Vector3 playerPos)
    {
        if (isHypnotized) return;
        currentState = GuardState.Chase;
        lastKnownPlayerPos = playerPos;
        lostTimer = 0f;
        Debug.Log("Guard alerted by citizen chasing the player!");
    }

    // Method to investigate noise
    public void InvestigateNoise(Vector3 noisePosition, float intensity)
    {
        // Don't investigate if already chasing or if noise is too weak
        if (currentState == GuardState.Chase || intensity < 0.3f) return;

        // Higher intensity = more likely to investigate
        if (Random.value < intensity)
        {
            lastKnownPlayerPos = noisePosition;
            if (currentState == GuardState.Patrol)
            {
                currentState = GuardState.Search;
                lostTimer = 0f;

                if (searchSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(searchSound);
                }

                Debug.Log($"Guard investigating noise at {noisePosition} with intensity {intensity}");
            }
        }
    }

    // Called by citizens to make guard investigate specific location
    public void InvestigateLocation(Vector3 location, float searchRadius = 10f)
    {
        Debug.Log($"[GuardAI] {name} received report to investigate location {location}");

        lastKnownPlayerPos = location;
        this.searchRadius = searchRadius;

        // Immediately switch to search state
        currentState = GuardState.Search;
        lostTimer = 0f;
        alertTimer = 0f;

        // Generate search points around the reported location
        GenerateSearchPoints(location, searchRadius);
        currentSearchIndex = 0;
        isInvestigating = true;

        // Move to the reported location first
        if (agent != null)
        {
            agent.speed = chaseSpeed; // Move quickly to investigate
            agent.SetDestination(location);
        }

        // Play communication sound to acknowledge report
        if (communicationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(communicationSound);
        }

        // Alert nearby guards about the sighting
        if (enableGuardCommunication)
        {
            CommunicateWithNearbyGuards();
        }

        // Increase alertness
        suspicionLevel = 1f;

        // Enable flashlight if available
        if (useFlashlight && flashlight != null)
        {
            flashlight.enabled = true;
        }
    }

    // Generate search points in a pattern around a location
    void GenerateSearchPoints(Vector3 center, float radius)
    {
        searchPoints = new Vector3[maxSearchPoints];

        for (int i = 0; i < maxSearchPoints; i++)
        {
            float angle = (360f / maxSearchPoints) * i;
            float distance = Random.Range(radius * 0.5f, radius);

            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;
            Vector3 searchPoint = center + offset;

            // Make sure the point is on the NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(searchPoint, out hit, radius, NavMesh.AllAreas))
            {
                searchPoints[i] = hit.position;
            }
            else
            {
                searchPoints[i] = center; // Fallback to center if no valid position
            }
        }

        Debug.Log($"[GuardAI] Generated {searchPoints.Length} search points around {center}");
    }

    private void OnDrawGizmos()
    {
        // Draw a yellow wire sphere to represent the view distance.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        // Calculate the left and right boundary directions for direct vision.
        Vector3 forward = transform.forward;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-fieldOfView * 0.5f, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(fieldOfView * 0.5f, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;

        // Draw red rays for the direct vision boundaries.
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, leftRayDirection * viewDistance);
        Gizmos.DrawRay(transform.position, rightRayDirection * viewDistance);

        // Draw peripheral vision if enabled
        if (enablePeripheralVision)
        {
            // Calculate the left and right boundary directions for peripheral vision.
            Quaternion leftPeripheralRotation = Quaternion.AngleAxis(-peripheralVisionAngle * 0.5f, Vector3.up);
            Quaternion rightPeripheralRotation = Quaternion.AngleAxis(peripheralVisionAngle * 0.5f, Vector3.up);
            Vector3 leftPeripheralDirection = leftPeripheralRotation * forward;
            Vector3 rightPeripheralDirection = rightPeripheralRotation * forward;

            // Draw orange rays for the peripheral vision boundaries.
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, leftPeripheralDirection * viewDistance);
            Gizmos.DrawRay(transform.position, rightPeripheralDirection * viewDistance);
        }

        // Draw close range detection sphere
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, closeRangeDistance);

        // Draw attack range sphere
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw lines between patrol points
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].transform.position, 0.3f);
                    if (i > 0 && patrolPoints[i - 1] != null)
                        Gizmos.DrawLine(patrolPoints[i - 1].transform.position, patrolPoints[i].transform.position);
                    else if (patrolPoints.Length > 1 && patrolPoints[patrolPoints.Length - 1] != null) // Draw line from last to first
                        Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1].transform.position, patrolPoints[0].transform.position);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw a yellow wire sphere to represent the view distance.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        // Calculate the left and right boundary directions.
        Vector3 forward = transform.forward;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-fieldOfView, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(fieldOfView, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;

        // Draw red rays for the boundaries.
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, leftRayDirection * viewDistance);
        Gizmos.DrawRay(transform.position, rightRayDirection * viewDistance);
    }

    public void SetHypnotized(bool value)
    {
        isHypnotized = value;
        if (isHypnotized)
        {
            // Reset to base values
            viewDistance = baseViewDistance;
            fieldOfView = baseFieldOfView;
        }
    }

    public void UpdateAlertness(GuardAlertnessLevel newAlertness)
    {
        currentAlertness = newAlertness;
        var alertnessLevel = alertnessManager != null ? alertnessManager.GetCurrentAlertnessLevel() : null;

        if (alertnessLevel != null)
        {
            viewDistance = baseViewDistance * alertnessLevel.spotDistanceMultiplier;
            fieldOfView = baseFieldOfView * alertnessLevel.patrolSpeedMultiplier; // Using patrol speed for FOV
                                                                                  // detectionTime = baseDetectionTime * alertnessLevel.detectionTimeMultiplier;
                                                                                  // guardAlertRadius = baseAlertRadius * alertnessLevel.alertRadiusMultiplier;

            // Update agent speed based on alertness
            if (agent != null)
            {
                agent.speed *= alertnessLevel.patrolSpeedMultiplier;
            }
        }
    }

    // Event effect methods
    public void ApplySpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
        if (agent != null)
        {
            agent.speed *= multiplier;
        }
    }

    public void ResetSpeedMultiplier()
    {
        if (agent != null)
        {
            agent.speed /= speedMultiplier;
        }
        speedMultiplier = 1f;
    }

    public void IncreasePatrolFrequency()
    {
        patrolFrequencyMultiplier = 0.5f; // Reduce wait times by half
        waitTimeMin = baseWaitTimeMin * patrolFrequencyMultiplier;
        waitTimeMax = baseWaitTimeMax * patrolFrequencyMultiplier;
    }

    public void ResetPatrolFrequency()
    {
        waitTimeMin = baseWaitTimeMin;
        waitTimeMax = baseWaitTimeMax;
        patrolFrequencyMultiplier = 1f;
    }

    void GenerateSearchPoints()
    {
        Vector3 center = lastKnownPlayerPos;

        for (int i = 0; i < searchPoints.Length; i++)
        {
            // Create a spiral pattern around the last known position
            float angle = (i * 360f / searchPoints.Length) * Mathf.Deg2Rad;
            float radius = searchRadius * (0.5f + (i * 0.5f / searchPoints.Length));

            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            Vector3 searchPoint = center + offset;

            // Ensure the point is on the NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(searchPoint, out hit, searchRadius, 1))
            {
                searchPoints[i] = hit.position;
            }
            else
            {
                searchPoints[i] = searchPoint;
            }
        }
    }

    void CommunicateWithNearbyGuards()
    {
        if (!enableGuardCommunication) return;

        // Find nearby guards
        Collider[] guardColliders = Physics.OverlapSphere(transform.position, communicationRange, LayerMask.GetMask("Guard"));

        foreach (Collider guardCol in guardColliders)
        {
            GuardAI nearbyGuard = guardCol.GetComponent<GuardAI>();
            if (nearbyGuard != null && nearbyGuard != this)
            {
                // Share information about player position
                if (currentState == GuardState.Chase || currentState == GuardState.Alert)
                {
                    nearbyGuard.ReceiveAlert(lastKnownPlayerPos);
                }
            }
        }

        if (communicationSound != null)
            audioSource.PlayOneShot(communicationSound);
    }

    public void ReceiveAlert(Vector3 alertPosition)
    {
        // Only respond if we're in patrol mode and the alert is close enough
        if (currentState == GuardState.Patrol)
        {
            float distanceToAlert = Vector3.Distance(transform.position, alertPosition);
            if (distanceToAlert <= communicationRange * 1.5f)
            {
                lastKnownPlayerPos = alertPosition;
                currentState = GuardState.Alert;
                alertTimer = 0f;
                agent.SetDestination(alertPosition);

                if (alertSound != null)
                    audioSource.PlayOneShot(alertSound);
            }
        }
    }

    void UpdateVisualState()
    {
        if (guardLight == null) return;

        switch (currentState)
        {
            case GuardState.Patrol:
                // Show suspicion level during patrol
                if (suspicionLevel > 0.3f)
                {
                    guardLight.color = Color.Lerp(patrolColor, alertColor, suspicionLevel);
                    guardLight.intensity = 0.5f + (suspicionLevel * 0.3f);
                }
                else
                {
                    guardLight.color = patrolColor;
                    guardLight.intensity = 0.5f;
                }
                break;
            case GuardState.Chase:
                // Pulsing red light during chase
                float pulseFactor = Mathf.PingPong(Time.time * 3f, 1f);
                guardLight.color = Color.Lerp(chaseColor, Color.white, pulseFactor * 0.3f);
                guardLight.intensity = 1f + (pulseFactor * 0.5f);
                break;
            case GuardState.Attack:
                guardLight.color = Color.red;
                guardLight.intensity = 1.5f;
                break;
            case GuardState.Alert:
                if (isInvestigatingNoise)
                {
                    guardLight.color = searchColor;
                    guardLight.intensity = 0.9f;
                }
                else
                {
                    guardLight.color = alertColor;
                    guardLight.intensity = 0.8f;
                }
                break;
        }

        // Flashlight state affects intensity
        if (useFlashlight && flashlight != null && flashlight.enabled)
        {
            guardLight.intensity *= 1.2f; // Brighter when using flashlight
        }
    }

    void ImprovedStateTransition(GuardState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        // Update visual state
        UpdateVisualState();

        // Play appropriate sound
        switch (newState)
        {
            case GuardState.Chase:
                if (detectionSound != null)
                    audioSource.PlayOneShot(detectionSound);
                break;
            case GuardState.Attack:
                if (attackSound != null)
                    audioSource.PlayOneShot(attackSound);
                break;
            case GuardState.Alert:
                if (alertSound != null)
                    audioSource.PlayOneShot(alertSound);
                break;
        }

        // Communicate with nearby guards and call for reinforcements
        if (newState == GuardState.Chase || newState == GuardState.Alert || newState == GuardState.Attack)
        {
            CommunicateWithNearbyGuards();

            // Call reinforcements when first spotting player
            if (newState == GuardState.Chase && canCallReinforcements)
            {
                CallReinforcements();
            }
        }
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        // Set the speed of the agent
        float speed = agent.velocity.magnitude / agent.speed;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);

        // Set state-based parameters
        animator.SetBool("IsChasing", currentState == GuardState.Chase);
        animator.SetBool("IsAttacking", currentState == GuardState.Attack);
        animator.SetBool("IsAlert", currentState == GuardState.Alert);
        animator.SetBool("IsPatrolling", currentState == GuardState.Patrol);
        animator.SetBool("IsHypnotized", isHypnotized);

        // Set suspicion level
        animator.SetFloat("SuspicionLevel", suspicionLevel);

        // Set investigation state
        animator.SetBool("IsInvestigating", isInvestigating || isInvestigatingNoise);
    }

    void SetupMeleeWeapon()
    {
        if (useMeleeWeapon && meleeWeapon != null && weaponHand != null)
        {
            // Instantiate weapon and attach to hand
            GameObject weaponInstance = Instantiate(meleeWeapon, weaponHand);
            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;
            weaponInstance.transform.localScale = new Vector3(100f, 100f, 100f);
        }
    }

    // Attack mode: guard stops and attacks the player
    void Attack()
    {
        if (player == null) return;

        // Stop moving and face the player
        agent.isStopped = true;
        FacePlayer();

        // Attack logic
        if (!isAttacking && Time.time - lastAttackTime >= attackCooldown)
        {
            StartCoroutine(PerformAttack());
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        attackTimer = 0f;

        // Wind up phase
        while (attackTimer < attackWindupTime)
        {
            attackTimer += Time.deltaTime;
            yield return null;
        }

        // Attack phase
        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            // Deal damage to player
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }

            // Play attack sound
            if (attackSound != null)
                audioSource.PlayOneShot(attackSound);

            // Trigger attack animation
            animator?.SetTrigger("Attack");
        }

        // Recovery phase
        attackTimer = 0f;
        while (attackTimer < attackRecoveryTime)
        {
            attackTimer += Time.deltaTime;
            yield return null;
        }

        isAttacking = false;
        lastAttackTime = Time.time;
    }

    public void Initialize()
    {
        agent.enabled = true;
    }

    // Check lighting conditions for detection modifier
    float GetLightingModifier()
    {
        float lightLevel = 1f; // Default full visibility

        // Check for nearby lights
        Light[] nearbyLights = Physics.OverlapSphere(transform.position, 15f)
            .Select(col => col.GetComponent<Light>())
            .Where(light => light != null && light != guardLight && light != flashlight)
            .ToArray();

        if (nearbyLights.Length == 0)
        {
            // No nearby lights - harder to see
            lightLevel = 0.6f;
        }
        else
        {
            // Calculate light intensity at guard position
            float totalIntensity = 0f;
            foreach (Light light in nearbyLights)
            {
                float distance = Vector3.Distance(transform.position, light.transform.position);
                if (distance <= light.range)
                {
                    totalIntensity += light.intensity * (1f - distance / light.range);
                }
            }
            lightLevel = Mathf.Clamp01(totalIntensity);
        }

        // Guards are trained to work in low light - minimum 40% effectiveness
        return Mathf.Max(0.4f, lightLevel);
    }

    // Detect player through sound
    void DetectPlayerSounds()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Get player's crouch state
        PlayerController playerController = player.GetComponent<PlayerController>();
        bool isPlayerCrouching = playerController != null && playerController.IsCrouched;

        // Don't detect sound if player is moving too slowly
        if (playerMovementSpeed < minSoundThreshold) return;

        // Determine effective sound range based on movement speed
        float effectiveSoundRange = 0f;
        if (playerMovementSpeed > 6f) // Running
        {
            effectiveSoundRange = runningSoundRange;
        }
        else if (playerMovementSpeed > minSoundThreshold) // Walking
        {
            effectiveSoundRange = walkingSoundRange;
        }

        // Apply crouch reduction
        if (isPlayerCrouching)
        {
            effectiveSoundRange *= crouchSoundReduction; // Make detection range much smaller when crouching
        }

        // Check if player is within hearing range
        if (distanceToPlayer <= effectiveSoundRange)
        {
            // Calculate sound intensity based on distance and movement speed
            float soundIntensity = (effectiveSoundRange - distanceToPlayer) / effectiveSoundRange;
            soundIntensity *= (playerMovementSpeed / 10f); // Louder when moving faster

            // Crouching significantly reduces sound intensity
            if (isPlayerCrouching)
            {
                soundIntensity *= crouchSoundReduction;
            }

            // Check for obstacles that might muffle sound
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up, (player.position - transform.position).normalized, out hit, distanceToPlayer, obstacleLayer))
            {
                soundIntensity *= 0.5f; // Walls muffle sound
            }

            if (soundIntensity > 0.2f && Time.time - lastSoundTime > 1f)
            {
                lastHeardSound = player.position;
                lastSoundTime = Time.time;

                // Increase suspicion based on sound intensity
                float suspicionIncrease = soundIntensity * 0.3f;
                if (playerMovementSpeed > 6f && !isPlayerCrouching) // Running makes more noise
                {
                    suspicionIncrease *= 1.5f;
                }

                suspicionLevel = Mathf.Min(1f, suspicionLevel + suspicionIncrease);

                // If suspicious enough, investigate
                if (suspicionLevel > 0.5f && currentState == GuardState.Patrol)
                {
                    InvestigateSound(lastHeardSound);
                }

                // If very loud (running close by), immediate alert
                if (soundIntensity > 0.8f && playerMovementSpeed > 6f && !isPlayerCrouching)
                {
                    ImprovedStateTransition(GuardState.Alert);
                    lastKnownPlayerPos = player.position;
                }
            }
        }
    }

    // Investigate a heard sound
    void InvestigateSound(Vector3 soundPosition)
    {
        isInvestigatingNoise = true;
        lastKnownPlayerPos = soundPosition;
        ImprovedStateTransition(GuardState.Alert);

        // Communicate sound to other guards
        if (enableGuardCommunication)
        {
            CommunicateWithNearbyGuards();
        }
    }

    // Handle noise investigation
    void InvestigateNoise()
    {
        agent.SetDestination(lastHeardSound);

        if (Vector3.Distance(transform.position, lastHeardSound) < 2f)
        {
            // Reached the sound location, look around
            investigationTimer += Time.deltaTime;
            transform.Rotate(0f, 90f * Time.deltaTime, 0f);

            if (investigationTimer >= investigationTime * 2f)
            {
                isInvestigatingNoise = false;
                investigationTimer = 0f;
                ImprovedStateTransition(GuardState.Patrol);
            }
        }
    }

    // Update player movement prediction
    void UpdatePlayerPrediction()
    {
        if (playerTrail.Count >= 2)
        {
            Vector3 currentVelocity = (playerTrail[playerTrail.Count - 1] - playerTrail[playerTrail.Count - 2]) / 0.5f;
            predictedPlayerPosition = player.position + currentVelocity * predictionTime;

            // Ensure predicted position is on NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(predictedPlayerPosition, out hit, 10f, 1))
            {
                predictedPlayerPosition = hit.position;
            }
        }
    }

    // Get predicted player position for chasing
    Vector3 GetPredictedPlayerPosition()
    {
        return predictedPlayerPosition != Vector3.zero ? predictedPlayerPosition : player.position;
    }

    // Avoid clustering with other guards
    Vector3 AvoidOtherGuards(Vector3 targetPosition)
    {
        if (!enableGuardCommunication) return targetPosition;

        Collider[] nearbyGuards = Physics.OverlapSphere(transform.position, formationDistance * 2f, LayerMask.GetMask("Guard"));

        Vector3 avoidanceVector = Vector3.zero;
        int guardCount = 0;

        foreach (Collider guardCol in nearbyGuards)
        {
            GuardAI otherGuard = guardCol.GetComponent<GuardAI>();
            if (otherGuard != null && otherGuard != this && otherGuard.currentState == GuardState.Chase)
            {
                Vector3 directionAway = (transform.position - otherGuard.transform.position).normalized;
                float distance = Vector3.Distance(transform.position, otherGuard.transform.position);

                if (distance < formationDistance)
                {
                    avoidanceVector += directionAway * (formationDistance - distance);
                    guardCount++;
                }
            }
        }

        if (guardCount > 0)
        {
            avoidanceVector /= guardCount;
            Vector3 adjustedTarget = targetPosition + avoidanceVector;

            // Ensure target is on NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(adjustedTarget, out hit, 5f, 1))
            {
                return hit.position;
            }
        }

        return targetPosition;
    }

    // React to player hiding in shadows
    public void OnPlayerEnterShadows(Vector3 shadowPosition)
    {
        if (Vector3.Distance(transform.position, shadowPosition) <= viewDistance * 0.5f)
        {
            // Player disappeared into shadows nearby
            suspicionLevel = Mathf.Min(1f, suspicionLevel + 0.3f);

            if (currentState == GuardState.Patrol)
            {
                InvestigateSound(shadowPosition);
            }
        }
    }

    // Call for reinforcements
    void CallReinforcements()
    {
        if (!canCallReinforcements) return;

        Collider[] nearbyGuards = Physics.OverlapSphere(transform.position, reinforcementRadius, LayerMask.GetMask("Guard"));

        foreach (Collider guardCol in nearbyGuards)
        {
            GuardAI reinforcement = guardCol.GetComponent<GuardAI>();
            if (reinforcement != null && reinforcement != this && reinforcement.currentState == GuardState.Patrol)
            {
                reinforcement.ReceiveReinforcementCall(lastKnownPlayerPos);
            }
        }
    }

    // Respond to reinforcement call
    public void ReceiveReinforcementCall(Vector3 alertPosition)
    {
        if (currentState == GuardState.Patrol)
        {
            lastKnownPlayerPos = alertPosition;
            ImprovedStateTransition(GuardState.Alert);

            if (alertSound != null)
                audioSource.PlayOneShot(alertSound);
        }
    }

    // Enhanced flanking behavior
    Vector3 GetFlankingPosition(Vector3 playerPosition)
    {
        Vector3 playerDirection = (playerPosition - transform.position).normalized;
        Vector3 flankDirection = Vector3.Cross(playerDirection, Vector3.up).normalized;

        // Try flanking from both sides
        Vector3 leftFlank = playerPosition + flankDirection * 8f;
        Vector3 rightFlank = playerPosition - flankDirection * 8f;

        // Choose the flanking position that's accessible
        NavMeshHit leftHit, rightHit;
        bool leftValid = NavMesh.SamplePosition(leftFlank, out leftHit, 10f, 1);
        bool rightValid = NavMesh.SamplePosition(rightFlank, out rightHit, 10f, 1);

        if (leftValid && rightValid)
        {
            // Choose closer flank
            float leftDist = Vector3.Distance(transform.position, leftHit.position);
            float rightDist = Vector3.Distance(transform.position, rightHit.position);
            return leftDist < rightDist ? leftHit.position : rightHit.position;
        }
        else if (leftValid)
        {
            return leftHit.position;
        }
        else if (rightValid)
        {
            return rightHit.position;
        }

        return playerPosition; // Fallback to direct approach
    }

    // Waypoint group support methods
    void MoveToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (patrolForward)
        {
            currentPatrolIndex++;
            if (currentPatrolIndex >= patrolPoints.Length)
            {
                currentPatrolIndex = patrolPoints.Length - 2;
                patrolForward = false;
            }
        }
        else
        {
            currentPatrolIndex--;
            if (currentPatrolIndex < 0)
            {
                currentPatrolIndex = 1;
                patrolForward = true;
            }
        }

        // Ensure we don't go out of bounds
        currentPatrolIndex = Mathf.Clamp(currentPatrolIndex, 0, patrolPoints.Length - 1);

        // Update the legacy currentPointIndex for compatibility
        currentPointIndex = currentPatrolIndex;
    }

    Vector3 GetCurrentWaypointPosition()
    {
        if (patrolPoints == null || patrolPoints.Length == 0 || currentPatrolIndex >= patrolPoints.Length)
            return transform.position;

        // If we have a waypoint group, get the adjusted position
        if (assignedWaypointGroup != null)
        {
            return assignedWaypointGroup.GetAdjustedWaypointPosition(gameObject, currentPatrolIndex);
        }

        // Fallback to direct waypoint position
        return patrolPoints[currentPatrolIndex].transform.position;
    }

    // Method called by spawner to update waypoint group assignment
    public void UpdateWaypointGroupAssignment(WaypointGroup newGroup)
    {
        assignedWaypointGroup = newGroup;
        if (newGroup != null)
        {
            currentPatrolIndex = newGroup.GetStartingWaypointIndex(gameObject);
            patrolForward = newGroup.GetPatrolDirection(gameObject);
        }
    }

    // Search mode: systematically search an area
    void SearchMode()
    {
        agent.isStopped = false;
        agent.speed = patrolSpeed * 1.2f; // Move slightly faster while searching

        // Check if we have search points
        if (searchPoints == null || searchPoints.Length == 0)
        {
            // Generate search points if we don't have any
            GenerateSearchPoints(lastKnownPlayerPos, searchRadius);
            currentSearchIndex = 0;
        }

        // Check if we've reached current search point
        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            // Look around at this search point
            investigationTimer += Time.deltaTime;

            // Rotate to scan the area
            transform.Rotate(0f, 90f * Time.deltaTime, 0f);

            // Play investigation animation
            animator?.SetBool("IsInvestigating", true);

            if (investigationTimer >= investigationTime)
            {
                // Move to next search point
                investigationTimer = 0f;
                currentSearchIndex++;

                if (currentSearchIndex >= searchPoints.Length)
                {
                    // Finished searching all points
                    Debug.Log($"[GuardAI] {name} finished searching area");

                    // Return to alert state briefly before resuming patrol
                    ImprovedStateTransition(GuardState.Alert);
                    alertTimer = alertDuration * 0.5f; // Shorter alert time after search
                    isInvestigating = false;

                    // Reset search points
                    searchPoints = null;

                    // Disable flashlight if it was enabled for search
                    if (useFlashlight && flashlight != null)
                    {
                        flashlight.enabled = false;
                    }
                }
                else
                {
                    // Move to next search point
                    agent.SetDestination(searchPoints[currentSearchIndex]);
                    Debug.Log($"[GuardAI] {name} moving to search point {currentSearchIndex + 1}/{searchPoints.Length}");
                }
            }
        }
        else if (agent.hasPath)
        {
            // Reset investigation timer while moving
            investigationTimer = 0f;
            animator?.SetBool("IsInvestigating", false);
        }

        // Periodically communicate with other guards
        if (enableGuardCommunication && Random.value < 0.01f) // 1% chance per frame
        {
            CommunicateWithNearbyGuards();
        }
    }

    // New methods for integration with new systems
    public void SetOverrideDestination(Vector3 destination)
    {
        if (agent != null)
        {
            agent.SetDestination(destination);
            ImprovedStateTransition(GuardState.Alert);
            lastKnownPlayerPosition = destination;
        }
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        if (agent != null)
        {
            agent.speed = walkSpeed * multiplier;
            currentSpeed = agent.speed;
        }
    }

    public void SetDetectionRangeMultiplier(float multiplier)
    {
        viewDistance = baseViewDistance * multiplier;
    }

    public void SetAudioSensitivityMultiplier(float multiplier)
    {
        // Store base sensitivity if not already stored
        if (baseSoundSensitivity == 0)
            baseSoundSensitivity = soundSensitivity;

        soundSensitivity = baseSoundSensitivity * multiplier;
    }

    public void SetAggressiveMode(bool aggressive)
    {
        isAggressiveMode = aggressive;
        if (aggressive)
        {
            attackRange *= 1.5f;
            reactionTime *= 0.5f;
        }
        else
        {
            attackRange = baseAttackRange;
            reactionTime = baseReactionTime;
        }
    }
    private bool isAggressiveMode = false;
    private float baseAttackRange = 0f;

    public void SetHuntingMode(bool hunting)
    {
        isHuntingPlayer = hunting;
        if (hunting && player != null)
        {
            ImprovedStateTransition(GuardState.Chase);
        }
    }
    private bool isHuntingPlayer = false;

    public void SetBribed(bool bribed)
    {
        isBribed = bribed;
        if (bribed)
        {
            // Temporarily ignore the player
            StartCoroutine(BribedBehavior());
        }
    }
    private bool isBribed = false;

    private IEnumerator BribedBehavior()
    {
        float bribeDuration = 60f; // 1 minute of looking the other way
        float elapsed = 0f;

        while (elapsed < bribeDuration)
        {
            // Don't detect player while bribed
            viewDistance = 0f;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restore normal behavior
        isBribed = false;
        viewDistance = baseViewDistance;
    }

    private Ward patrolWard;
    public void SetPatrolWard(Ward ward)
    {
        patrolWard = ward;
    }

    public Ward GetPatrolWard()
    {
        return patrolWard;
    }

    public void AlertToPlayer(Vector3 playerPosition)
    {
        lastKnownPlayerPosition = playerPosition;
        ImprovedStateTransition(GuardState.Chase);
    }

    public void IncreaseAlertness()
    {
        // Increase detection sensitivity temporarily
        viewDistance *= 1.2f;
        fieldOfView += 10f;
        peripheralVisionAngle += 15f;

        // Schedule return to normal after some time
        StartCoroutine(ReturnToNormalAlertness());
    }

    private IEnumerator ReturnToNormalAlertness()
    {
        yield return new WaitForSeconds(120f); // 2 minutes

        viewDistance = baseViewDistance;
        fieldOfView = baseFieldOfView;
        peripheralVisionAngle = basePeripheralVisionAngle;
    }

    private float basePeripheralVisionAngle = 0f;

    private void CacheBaseValues()
    {
        if (baseViewDistance == 0f)
        {
            baseViewDistance = viewDistance;
            baseFieldOfView = fieldOfView;
            basePeripheralVisionAngle = peripheralVisionAngle;
            baseAttackRange = attackRange;
            baseReactionTime = reactionTime;
        }
    }

    void Awake()
    {
        CacheBaseValues();
    }

    public GuardState GetCurrentState()
    {
        return currentState;
    }
}