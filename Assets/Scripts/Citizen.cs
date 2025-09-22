using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public enum CitizenRarity
{
    Peasant,
    Merchant,
    Priest,
    Noble,
    Royalty
}

public enum CitizenState
{
    Walking,
    Fleeing,
    Hiding,
    Dead
}

public class Citizen : MonoBehaviour, ISpatialEntity
{
    [Header("Patrol Settings")]
    public WaypointGroup patrolGroup;
    private Waypoint[] patrolPoints;

    [Header("Wait Time Settings")]
    public bool useCustomWaitTimes = false; // Override waypoint wait times
    public float customMinWaitTime = 1f;     // Custom minimum wait time
    public float customMaxWaitTime = 3f;     // Custom maximum wait time
    public float waitTimeMultiplier = 1f;    // Multiplier for all wait times

    // Multi-entity waypoint support
    [Header("Waypoint Group Support")]
    public WaypointGroup assignedWaypointGroup; // Reference to the assigned waypoint group
    private int currentPatrolIndex = 0;         // Current waypoint index
    private bool patrolForward = true;          // Direction of patrol

    [Header("Vision Settings")]
    public float fieldOfView = 75f;       // Half-angle for the cone of vision (increased from 45f).
    public float viewDistance = 35f;      // Maximum distance at which the player can be seen (increased from 20f).
    private float baseViewDistance;
    public float detectionTime = 0.5f;    // Time (in seconds) the player must be in view before alerting.
    public float closeRangeDetectionTime = 0.01f; // Near-instant detection when player is very close
    public float closeRangeDistance = 7f; // Distance considered "close range" for instant detection
    public bool enablePeripheralVision = true; // Detect movement in peripheral vision
    public float peripheralVisionAngle = 120f; // Wider angle for peripheral detection
    public float peripheralDetectionTime = 1.0f; // Slower detection for peripheral vision
    public LayerMask playerLayer;         // Layer for the player.
    public LayerMask obstacleLayer;       // Layer(s) that could block line-of-sight (walls, etc).

    [Header("Alert Settings")]
    public float guardAlertRadius = 20f;  // How far away the citizen can alert guards.
    public float screamChance = 0.7f;     // Chance to scream when spotting player (alerts more guards)
    public float panicDuration = 3f;      // How long citizen panics after losing sight of player

    [Header("Alerted State Settings")]
    public float alertStateDuration = 5f; // How long (in seconds) the citizen will search if the player is lost.
    public float scanningRotationSpeed = 45f; // Degrees per second when scanning.

    [Header("Rarity & Blood Settings")]
    public CitizenRarity rarity = CitizenRarity.Peasant;
    //[ReadOnlyInInspector]
    public int bloodAmount;

    [HideInInspector]
    public bool isDrained = false;
    public bool isHypnotized = false;

    [Header("Schedule Settings")]
    public CitizenSchedule schedule;
    public bool isSleeping = false;
    private CitizenScheduleManager scheduleManager;

    [Header("Event Effects")]
    private float speedMultiplier = 1f;
    private bool isInsideHouse = false;
    private Vector3 lastOutsidePosition;

    [Header("Personality & Behavior")]
    public CitizenPersonality personality = CitizenPersonality.Normal;
    public float braveryLevel = 0.5f; // 0 = cowardly, 1 = brave
    public float curiosityLevel = 0.5f; // 0 = uninterested, 1 = very curious
    public float socialLevel = 0.5f; // 0 = loner, 1 = very social

    [Header("Memory System")]
    public int maxMemorySlots = 3;
    private List<MemoryEntry> memories = new List<MemoryEntry>();
    private float memoryDecayTime = 300f; // 5 minutes

    [Header("Social Behavior")]
    public float socialInteractionRange = 5f;
    public float socialInteractionCooldown = 30f;
    private float lastSocialInteraction = 0f;
    private Citizen nearestCitizen = null;
    
    [Header("Movement")]
    public float sprintSpeed = 6f;
    public CitizenState currentState = CitizenState.Walking;
    private Coroutine patrolCoroutine;

    [Header("Environmental Awareness")]
    public bool reactToNoises = true;
    public float noiseReactionRange = 12f; // Increased from 8f
    public bool reactToLights = true;
    public float lightReactionRange = 6f;
    public float noiseSuspicionIncrease = 0.3f; // How much noise increases suspicion
    public float noiseInvestigationChance = 0.6f; // Chance to investigate noise

    [Header("Visual Feedback")]
    public Light citizenLight;
    public Color normalColor = Color.white;
    public Color alertColor = Color.yellow;
    public Color scaredColor = Color.red;
    public Color socialColor = Color.blue;

    [Header("Audio")]
    public AudioClip alertSound;
    public AudioClip scaredSound;
    public AudioClip socialSound;

    private NavMeshAgent agent;
    private int currentPointIndex = 0;
    private bool waiting = false;
    private float detectionTimer = 0f;
    private Transform player;
    private bool isAlerting = false;
    private float alertTimer = 0f;
    private VampireStats playerStats;
    private GuardAlertnessManager alertnessManager;
    private Animator animator;

    // Movement detection
    private Vector3 lastPlayerPosition;
    private float playerMovementSpeed;
    private bool isPanicking = false;
    private float panicTimer = 0f;
    private bool isRunningToGuard = false;
    private GuardAI targetGuard = null;
    private Vector3 lastKnownPlayerLocation = Vector3.zero;

    // Suspicion state
    private bool isSuspicious = false;
    private float suspicionTimer = 0f;
    private float suspicionDuration = 10f;  // How long to remain suspicious

    // --- Detection Progress Visualization ---
    [HideInInspector]
    public float detectionProgress = 0f;
    [HideInInspector]
    public bool showDetectionProgress = false;

    // ISpatialEntity implementation
    public Vector3 Position => transform.position;
    public Transform Transform => transform;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (patrolGroup != null)
            patrolPoints = patrolGroup.waypoints != null ? patrolGroup.waypoints : new Waypoint[0];
        else
            patrolPoints = new Waypoint[0];
        if (patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[currentPointIndex].transform.position);

        // Assume the player GameObject is tagged "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerStats = playerObj.GetComponent<VampireStats>();
        }

        baseViewDistance = viewDistance;
        SetBloodAmountByRarity();

        scheduleManager = FindObjectOfType<CitizenScheduleManager>();
        if (scheduleManager != null)
            scheduleManager.RegisterCitizen(this);

        alertnessManager = GuardAlertnessManager.instance;

        // Register with NoiseManager for sound detection
        if (NoiseManager.Instance != null)
        {
            Debug.Log($"[Citizen] {name} registered for noise detection");
        }

        // Register with CitizenManager for performance optimization
        if (CitizenManager.Instance != null)
        {
            CitizenManager.Instance.RegisterCitizen(this);
        }

        // Register with spatial grid for performance optimization
        if (SpatialGrid.Instance != null)
        {
            SpatialGrid.Instance.RegisterEntity(this);
        }
    }

    void OnDestroy()
    {
        // Unregister from CitizenManager
        if (CitizenManager.Instance != null)
        {
            CitizenManager.Instance.UnregisterCitizen(this);
        }

        // Unregister from spatial grid
        if (SpatialGrid.Instance != null)
        {
            SpatialGrid.Instance.UnregisterEntity(this);
        }

        if (scheduleManager != null)
        {
            scheduleManager.UnregisterCitizen(this);
        }
    }

    void Update()
    {
        // If not alerting, patrol normally.
        if (!isAlerting)
        {
            Patrol();
        }

        // Run detection regardless of state.
        DetectPlayer();

        // Handle suspicion state
        if (isSuspicious && !isAlerting)
        {
            suspicionTimer += Time.deltaTime;
            if (suspicionTimer >= suspicionDuration)
            {
                isSuspicious = false;
                suspicionTimer = 0f;
                // Return to normal detection parameters
                viewDistance = baseViewDistance;
            }
            else
            {
                // Look around more while suspicious
                transform.Rotate(0f, 30f * Time.deltaTime, 0f);
            }
        }

        // Handle social interactions
        HandleSocialInteractions();

        // Handle environmental awareness
        HandleEnvironmentalAwareness();

        // Update memories
        UpdateMemories();

        // Update animator parameters
        UpdateAnimator();

        // If in alert state...
        if (isAlerting)
        {
            if (viewDistance == baseViewDistance)
            {
                viewDistance = baseViewDistance * 2f; // Double detection range when alerting
                Debug.Log($"[Citizen] Detection range increased to {viewDistance}");
            }
            // Stop moving.
            if (agent != null)
                agent.isStopped = true;

            animator?.SetBool("IsScared", true);

            // Handle panic state
            if (isPanicking)
            {
                panicTimer += Time.deltaTime;
                if (panicTimer >= panicDuration)
                {
                    isPanicking = false;
                    panicTimer = 0f;
                }

                // Panic movement - run away from last known player position
                if (agent != null && !agent.isStopped)
                {
                    Vector3 awayDirection = (transform.position - player.position).normalized;
                    Vector3 fleePosition = transform.position + awayDirection * 10f;

                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(fleePosition, out hit, 10f, 1))
                    {
                        agent.SetDestination(hit.position);
                        agent.speed = agent.speed * 1.5f; // Run faster when panicking
                    }
                }
            }

            // Check if the player is in view.
            if (IsPlayerVisible())
            {
                // If the player is visible, reset the alert timer and face the player.
                alertTimer = 0.1f;
                FacePlayer();
            }
            else
            {
                // If the player is lost, increment the alert timer.
                alertTimer += Time.deltaTime;
                // Rotate at a constant scanning speed.
                transform.Rotate(0f, scanningRotationSpeed * Time.deltaTime, 0f);

                // After alertStateDuration seconds of searching, exit alert state.
                if (alertTimer >= alertStateDuration)
                {
                    isAlerting = false;
                    alertTimer = 0f;
                    animator?.SetBool("IsScared", false);
                    // Resume movement.
                    if (agent != null)
                    {
                        agent.isStopped = false;
                        Vector3 targetPosition = GetCurrentWaypointPosition();
                        agent.SetDestination(targetPosition);
                    }
                    if (viewDistance != baseViewDistance)
                    {
                        viewDistance = baseViewDistance;
                        Debug.Log($"[Citizen] Detection range reset to {viewDistance}");
                    }
                }
            }
        }

        // Handle running to guard behavior
        if (isRunningToGuard && targetGuard != null)
        {
            float distanceToGuard = Vector3.Distance(transform.position, targetGuard.transform.position);

            // Check if we've reached the guard
            if (distanceToGuard < 3f)
            {
                // Share information with guard
                ShareInformationWithGuard(targetGuard);
                isRunningToGuard = false;
                targetGuard = null;

                // Reset speed
                if (agent != null)
                {
                    agent.speed = agent.speed / 1.5f;
                }
            }
            else if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                // We couldn't reach the guard, alert normally
                AlertGuards();
                isRunningToGuard = false;
                targetGuard = null;

                // Reset speed
                if (agent != null)
                {
                    agent.speed = agent.speed / 1.5f;
                }
            }
        }

        // Update visual state
        UpdateVisualState();

        // Update spatial grid position for performance optimization
        if (SpatialGrid.Instance != null)
        {
            SpatialGrid.Instance.UpdateEntity(this);
        }
    }

    // Handles movement between patrol points and waiting at each point.
    void Patrol()
    {
        if (waiting || patrolPoints.Length == 0)
            return;
        if (agent.remainingDistance < 0.5f && !agent.pathPending)
        {
            StartCoroutine(WaitAtPoint());
        }
    }

    IEnumerator WaitAtPoint()
    {
        waiting = true;
        Waypoint currentWaypoint = patrolPoints[currentPointIndex];
        transform.rotation = currentWaypoint.transform.rotation;

        // Calculate wait time based on settings
        float waitTime;
        if (useCustomWaitTimes)
        {
            // Use custom wait times
            waitTime = Random.Range(customMinWaitTime, customMaxWaitTime);
        }
        else
        {
            // Use waypoint-specific wait times
            waitTime = Random.Range(currentWaypoint.minWaitTime, currentWaypoint.maxWaitTime);
        }

        // Apply multiplier (personality can affect this)
        waitTime *= waitTimeMultiplier;

        // Personality affects wait time
        switch (personality)
        {
            case CitizenPersonality.Curious:
                waitTime *= 1.5f; // Curious citizens linger longer
                break;
            case CitizenPersonality.Cowardly:
                waitTime *= 0.7f; // Cowardly citizens move more quickly
                break;
            case CitizenPersonality.Social:
                // Social citizens might wait longer if other citizens are nearby
                if (nearestCitizen != null && Vector3.Distance(transform.position, nearestCitizen.transform.position) < socialInteractionRange)
                {
                    waitTime *= 1.3f;
                }
                break;
        }

        yield return new WaitForSeconds(waitTime);
        waiting = false;
        MoveToNextPatrolPoint();
        Vector3 targetPosition = GetCurrentWaypointPosition();
        agent.SetDestination(targetPosition);
    }

    // Checks for the player within the citizen's cone of vision.
    void DetectPlayer()
    {
        if (isHypnotized || isSleeping)
        {
            Debug.Log("[Citizen.DetectPlayer] Skipped: Hypnotized or Sleeping");
            return;
        }
        if (player == null)
        {
            Debug.Log("[Citizen.DetectPlayer] Skipped: No player reference");
            return;
        }

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float effectiveSpotDistance = playerStats != null ? playerStats.spotDistance : viewDistance;

        // Personality affects detection
        float personalityModifier = 1f;
        switch (personality)
        {
            case CitizenPersonality.Cowardly:
                personalityModifier = 1.3f; // More alert (increased from 1.2f)
                break;
            case CitizenPersonality.Curious:
                personalityModifier = 1.2f; // Slightly more alert (increased from 1.1f)
                break;
            case CitizenPersonality.Brave:
                personalityModifier = 0.8f; // Less easily startled (reduced from 0.9f)
                break;
        }

        effectiveSpotDistance *= personalityModifier;

        // Check if player is within view distance and within the cone.
        bool inDirectView = distanceToPlayer < effectiveSpotDistance && angle < fieldOfView;
        bool inPeripheralView = false;

        // Check peripheral vision if enabled
        if (enablePeripheralVision && distanceToPlayer < effectiveSpotDistance && angle < peripheralVisionAngle)
        {
            inPeripheralView = true;
        }

        // Debug.Log($"[Citizen.DetectPlayer] directView={inDirectView}, peripheralView={inPeripheralView}, dist={distanceToPlayer:F2}, angle={angle:F2}, effectiveSpotDist={effectiveSpotDistance:F2}, isAlerting={isAlerting}");

        // Store detection progress for visualization
        detectionProgress = 0f;
        showDetectionProgress = false;

        if ((inDirectView || inPeripheralView) && !Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer))
        {
            // Calculate player movement speed
            if (lastPlayerPosition != Vector3.zero)
            {
                playerMovementSpeed = (player.position - lastPlayerPosition).magnitude / Time.deltaTime;
            }
            lastPlayerPosition = player.position;

            // If not already alerting, accumulate detection time.
            if (!isAlerting)
            {
                // Determine detection time based on distance, view type, and personality
                float effectiveDetectionTime = detectionTime;

                // Close range detection is much faster
                if (distanceToPlayer <= closeRangeDistance)
                {
                    effectiveDetectionTime = closeRangeDetectionTime;
                }
                // Peripheral vision is slower
                else if (inPeripheralView && !inDirectView)
                {
                    effectiveDetectionTime = peripheralDetectionTime;
                }
                else
                {
                    // Distance-based scaling for medium range
                    float distanceRatio = distanceToPlayer / effectiveSpotDistance;
                    effectiveDetectionTime = detectionTime * (0.5f + distanceRatio * 0.5f); // Faster detection when closer
                }

                // Movement speed affects detection (moving targets are easier to spot)
                if (playerMovementSpeed > 5f) // Running
                {
                    effectiveDetectionTime *= 0.5f;
                }
                else if (playerMovementSpeed > 2f) // Walking
                {
                    effectiveDetectionTime *= 0.75f;
                }
                // Crouching/still targets are harder to detect

                // Personality affects detection time
                if (personality == CitizenPersonality.Cowardly)
                    effectiveDetectionTime *= 0.5f; // Even faster detection (reduced from 0.7f)
                else if (personality == CitizenPersonality.Brave)
                    effectiveDetectionTime *= 1.5f; // Slower detection (increased from 1.3f)

                // Suspicious citizens detect faster
                if (isSuspicious)
                    effectiveDetectionTime *= 0.5f;

                // Lighting affects detection time
                float lightingModifier = GetLightingModifier();
                effectiveDetectionTime /= lightingModifier; // Harder to detect in darkness

                detectionTimer += Time.deltaTime;
                detectionProgress = Mathf.Clamp01(detectionTimer / effectiveDetectionTime);
                showDetectionProgress = true;
                Debug.Log($"[Citizen.DetectPlayer] detectionTimer={detectionTimer:F2}, effectiveDetectionTime={effectiveDetectionTime:F2}, progress={detectionProgress:F2}");
                if (detectionTimer >= effectiveDetectionTime)
                {
                    isAlerting = true;
                    detectionTimer = 0f;
                    alertTimer = 0f;

                    // Add memory of player sighting
                    float importance = personality == CitizenPersonality.Cowardly ? 0.9f : 0.7f;
                    string detectionType = inPeripheralView && !inDirectView ? "peripheral" : "direct";
                    AddMemory(MemoryEntry.MemoryType.PlayerSighting, player.position, importance,
                             $"Saw suspicious figure at {player.position} ({detectionType} vision)");

                    // Store last known player location
                    lastKnownPlayerLocation = player.position;

                    // Personality affects reaction
                    if (personality == CitizenPersonality.Cowardly)
                    {
                        // Cowardly citizens immediately run to guards
                        FindAndRunToNearestGuard();
                    }
                    else if (personality == CitizenPersonality.Curious)
                    {
                        // Curious citizens might investigate first
                        if (Random.value < 0.3f)
                        {
                            // Investigate instead of alerting
                            agent.SetDestination(player.position);
                            Debug.Log($"[Citizen] {name} (Curious) investigating player position");
                        }
                        else
                        {
                            FindAndRunToNearestGuard();
                        }
                    }
                    else
                    {
                        // Normal behavior - run to guard
                        FindAndRunToNearestGuard();
                    }
                }
            }
            else
            {
                // If already alerting and player is visible, reset the alert timer.
                alertTimer = 0f;
            }
            return;
        }

        // If not in view and not already alerting, gradually decrease the detection timer.
        if (!isAlerting)
        {
            // Check if we should become suspicious
            if (detectionTimer > detectionTime * 0.5f && !isSuspicious)
            {
                isSuspicious = true;
                suspicionTimer = 0f;
                viewDistance = baseViewDistance * 1.5f; // Increase view range when suspicious
                Debug.Log($"[Citizen] Becoming suspicious... Detection progress: {detectionProgress:F2}");
            }

            detectionTimer = Mathf.Max(0f, detectionTimer - Time.deltaTime * 0.5f);
        }
    }

    // Find and run to the nearest guard
    void FindAndRunToNearestGuard()
    {
        if (isHypnotized) return;

        // Find all guards in a large radius
        Collider[] guardColliders = Physics.OverlapSphere(transform.position, 50f, LayerMask.GetMask("Guard"));

        GuardAI nearestGuard = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider guardCol in guardColliders)
        {
            GuardAI guard = guardCol.GetComponent<GuardAI>();
            if (guard != null && !guard.isHypnotized)
            {
                float distance = Vector3.Distance(transform.position, guard.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestGuard = guard;
                }
            }
        }

        if (nearestGuard != null)
        {
            targetGuard = nearestGuard;
            isRunningToGuard = true;

            // Increase movement speed when running to guard
            if (agent != null)
            {
                agent.speed = agent.speed * 1.5f;
                agent.SetDestination(targetGuard.transform.position);
            }

            Debug.Log($"[Citizen] {name} running to guard {targetGuard.name} at distance {nearestDistance:F1}m");

            // Scream while running if cowardly
            if (personality == CitizenPersonality.Cowardly && Random.value < screamChance)
            {
                if (scaredSound != null)
                {
                    AudioSource audioSource = GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        audioSource.PlayOneShot(scaredSound);
                        audioSource.volume = 1f;
                    }
                }
            }
        }
        else
        {
            // No guards found - just alert normally
            Debug.Log($"[Citizen] {name} couldn't find any guards nearby, alerting with screams");
            AlertGuards();
        }
    }

    // Alerts nearby guards if the player is detected.
    void AlertGuards()
    {
        if (isHypnotized) return;

        isPanicking = true;
        panicTimer = 0f;

        // Determine if citizen screams (alerts more guards)
        bool screams = Random.value < screamChance;
        float effectiveAlertRadius = screams ? guardAlertRadius * 1.5f : guardAlertRadius;

        if (screams)
        {
            Debug.Log("Citizen SCREAMS in terror! Alerting all nearby guards!");
            if (scaredSound != null)
            {
                AudioSource audioSource = GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(scaredSound);
                    audioSource.volume = 1f; // Full volume for scream
                }
            }
        }
        else
        {
            Debug.Log("Citizen spotted the player! Alerting nearby guards!");
        }

        // Find guards within the alert radius that are on a specific "Guard" layer.
        Collider[] guardColliders = Physics.OverlapSphere(transform.position, effectiveAlertRadius, LayerMask.GetMask("Guard"));

        // Alert delay based on distance (guards further away take longer to respond)
        foreach (Collider guardCol in guardColliders)
        {
            GuardAI guard = guardCol.GetComponent<GuardAI>();
            if (guard != null)
            {
                float distance = Vector3.Distance(transform.position, guard.transform.position);
                float delay = screams ? 0f : (distance / effectiveAlertRadius) * 0.5f; // Up to 0.5s delay

                if (delay > 0f)
                {
                    StartCoroutine(DelayedAlert(guard, lastKnownPlayerLocation, delay));
                }
                else
                {
                    guard.Alert(lastKnownPlayerLocation);
                }
            }
        }

        // Alert other citizens nearby (panic spreads)
        Collider[] citizenColliders = Physics.OverlapSphere(transform.position, socialInteractionRange * 2f);
        foreach (Collider citizenCol in citizenColliders)
        {
            Citizen otherCitizen = citizenCol.GetComponent<Citizen>();
            if (otherCitizen != null && otherCitizen != this && !otherCitizen.isAlerting)
            {
                otherCitizen.ReactToPanic(transform.position);
            }
        }

        // Optionally, reset the detection timer after alerting.
        detectionTimer = 0f;
    }

    IEnumerator DelayedAlert(GuardAI guard, Vector3 alertPosition, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (guard != null)
        {
            guard.Alert(alertPosition);
        }
    }

    bool IsPlayerVisible()
    {
        if (player == null)
        {
            Debug.Log("[Citizen.IsPlayerVisible] No player reference");
            return false;
        }

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float effectiveSpotDistance = playerStats != null ? playerStats.spotDistance : viewDistance;

        // Apply personality modifier for consistency
        float personalityModifier = 1f;
        switch (personality)
        {
            case CitizenPersonality.Cowardly:
                personalityModifier = 1.3f;
                break;
            case CitizenPersonality.Curious:
                personalityModifier = 1.2f;
                break;
            case CitizenPersonality.Brave:
                personalityModifier = 0.8f;
                break;
        }
        effectiveSpotDistance *= personalityModifier;

        Debug.Log($"[Citizen.IsPlayerVisible] dist={distanceToPlayer:F2}, angle={angle:F2}, effectiveSpotDist={effectiveSpotDistance:F2}, fieldOfView={fieldOfView}, isAlerting={isAlerting}");

        if (distanceToPlayer < effectiveSpotDistance && angle < fieldOfView)
        {
            if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer))
            {
                Debug.Log("[Citizen.IsPlayerVisible] Player is visible!");
                return true;
            }
        }
        return false;
    }

    // Rotates the citizen to face the player.
    void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f; // Only rotate around the Y axis.
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    public void Drain()
    {
        isDrained = true;
        // Notify alertness manager
        if (alertnessManager != null)
            alertnessManager.OnCitizenMissing();
        // Optionally, play a draining animation or effect.
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        // Draw a yellow wire sphere to represent the view distance.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        // Calculate the left and right boundary directions for direct vision.
        Vector3 forward = transform.forward;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-fieldOfView, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(fieldOfView, Vector3.up);
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
            Quaternion leftPeripheralRotation = Quaternion.AngleAxis(-peripheralVisionAngle, Vector3.up);
            Quaternion rightPeripheralRotation = Quaternion.AngleAxis(peripheralVisionAngle, Vector3.up);
            Vector3 leftPeripheralDirection = leftPeripheralRotation * forward;
            Vector3 rightPeripheralDirection = rightPeripheralRotation * forward;

            // Draw orange rays for the peripheral vision boundaries.
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, leftPeripheralDirection * viewDistance);
            Gizmos.DrawRay(transform.position, rightPeripheralDirection * viewDistance);
        }

        // Draw close range detection sphere
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, closeRangeDistance);

        // Draw detection progress bar above the citizen's head if applicable
        if (showDetectionProgress)
        {
            Vector3 barPos = transform.position + Vector3.up * 2.2f;
            float barWidth = 1.2f;
            float barHeight = 0.15f;
            float progress = detectionProgress;
            Color barColor = Color.Lerp(Color.green, Color.red, progress);
            Vector3 left = barPos + Vector3.left * barWidth * 0.5f;
            Vector3 right = barPos + Vector3.right * barWidth * 0.5f;
            // Draw background
            Gizmos.color = Color.black;
            Gizmos.DrawCube(barPos, new Vector3(barWidth, barHeight, 0.01f));
            // Draw progress
            Gizmos.color = barColor;
            float filledWidth = barWidth * progress;
            Vector3 filledCenter = left + Vector3.right * (filledWidth * 0.5f);
            Gizmos.DrawCube(filledCenter, new Vector3(filledWidth, barHeight * 0.8f, 0.01f));
        }

        // Draw player distance-to-detection-range bar
        if (player != null && !isAlerting)
        {
            // Calculate effective detection range (including personality)
            float effectiveSpotDistance = playerStats != null ? playerStats.spotDistance : viewDistance;
            float personalityModifier = 1f;
            switch (personality)
            {
                case CitizenPersonality.Cowardly:
                    personalityModifier = 1.3f;
                    break;
                case CitizenPersonality.Curious:
                    personalityModifier = 1.2f;
                    break;
                case CitizenPersonality.Brave:
                    personalityModifier = 0.8f;
                    break;
            }
            effectiveSpotDistance *= personalityModifier;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            float rangeProgress = Mathf.Clamp01(1f - (distanceToPlayer / effectiveSpotDistance)); // 0 = far, 1 = at/inside range
            Color rangeColor = Color.Lerp(Color.green, Color.yellow, rangeProgress * 1.5f); // green to yellow
            if (rangeProgress > 0.95f) rangeColor = Color.red; // red if inside range

            Vector3 barPos2 = transform.position + Vector3.up * 2.4f;
            float barWidth2 = 1.2f;
            float barHeight2 = 0.12f;
            Vector3 left2 = barPos2 + Vector3.left * barWidth2 * 0.5f;
            Vector3 right2 = barPos2 + Vector3.right * barWidth2 * 0.5f;
            // Draw background
            Gizmos.color = Color.black;
            Gizmos.DrawCube(barPos2, new Vector3(barWidth2, barHeight2, 0.01f));
            // Draw progress
            Gizmos.color = rangeColor;
            float filledWidth2 = barWidth2 * rangeProgress;
            Vector3 filledCenter2 = left2 + Vector3.right * (filledWidth2 * 0.5f);
            Gizmos.DrawCube(filledCenter2, new Vector3(filledWidth2, barHeight2 * 0.8f, 0.01f));
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw a yellow wire sphere to represent the view distance.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        // Calculate the left and right boundary directions for direct vision.
        Vector3 forward = transform.forward;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-fieldOfView, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(fieldOfView, Vector3.up);
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
            Quaternion leftPeripheralRotation = Quaternion.AngleAxis(-peripheralVisionAngle, Vector3.up);
            Quaternion rightPeripheralRotation = Quaternion.AngleAxis(peripheralVisionAngle, Vector3.up);
            Vector3 leftPeripheralDirection = leftPeripheralRotation * forward;
            Vector3 rightPeripheralDirection = rightPeripheralRotation * forward;

            // Draw orange rays for the peripheral vision boundaries.
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, leftPeripheralDirection * viewDistance);
            Gizmos.DrawRay(transform.position, rightPeripheralDirection * viewDistance);
        }

        // Draw close range detection sphere
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, closeRangeDistance);
    }

    private void SetBloodAmountByRarity()
    {
        switch (rarity)
        {
            case CitizenRarity.Peasant:
                bloodAmount = 10;
                break;
            case CitizenRarity.Merchant:
                bloodAmount = 20;
                break;
            case CitizenRarity.Priest:
                bloodAmount = 30;
                break;
            case CitizenRarity.Noble:
                bloodAmount = 50;
                break;
            case CitizenRarity.Royalty:
                bloodAmount = 100;
                break;
            default:
                bloodAmount = 10;
                break;
        }

        // Also set default wait times based on rarity if using custom times
        SetDefaultWaitTimesByRarity();
    }

    private void SetDefaultWaitTimesByRarity()
    {
        if (!useCustomWaitTimes) return;

        switch (rarity)
        {
            case CitizenRarity.Peasant:
                // Peasants move frequently, short waits
                customMinWaitTime = 0.5f;
                customMaxWaitTime = 2f;
                break;
            case CitizenRarity.Merchant:
                // Merchants pause to "conduct business"
                customMinWaitTime = 2f;
                customMaxWaitTime = 5f;
                break;
            case CitizenRarity.Priest:
                // Priests move slowly and deliberately
                customMinWaitTime = 3f;
                customMaxWaitTime = 6f;
                break;
            case CitizenRarity.Noble:
                // Nobles take their time
                customMinWaitTime = 2f;
                customMaxWaitTime = 4f;
                break;
            case CitizenRarity.Royalty:
                // Royalty moves at a stately pace
                customMinWaitTime = 3f;
                customMaxWaitTime = 5f;
                break;
        }
    }

    public void SetHypnotized(bool value)
    {
        isHypnotized = value;
        if (isHypnotized)
        {
            isAlerting = false;
            waiting = false;
            // Optionally, play a visual effect or animation
        }
    }

    public void SwitchToWaypointGroup(WaypointGroup newGroup)
    {
        if (newGroup == null) return;

        patrolGroup = newGroup;
        if (patrolGroup != null)
            patrolPoints = patrolGroup.waypoints != null ? patrolGroup.waypoints : new Waypoint[0];
        else
            patrolPoints = new Waypoint[0];

        // Reset patrol state
        currentPointIndex = 0;
        waiting = false;
        isAlerting = false;

        if (patrolPoints.Length > 0 && agent != null)
        {
            agent.SetDestination(patrolPoints[currentPointIndex].transform.position);
        }

        // Check if this is a house (sleeping state)
        isSleeping = (patrolGroup.groupType == WaypointType.House && schedule != null && schedule.isSleeper);

        Debug.Log($"Citizen {name} switched to {newGroup.name}, sleeping: {isSleeping}");
    }

    public void WakeUp()
    {
        if (!isSleeping) return;

        isSleeping = false;
        Debug.Log($"Citizen {name} woke up!");

        // Optionally, make them more alert or aggressive
        // For now, just make them detect the player more easily
        detectionTime *= 0.5f;
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

    public void GoInside()
    {
        if (isInsideHouse) return;

        lastOutsidePosition = transform.position;
        isInsideHouse = true;

        // Find nearest house waypoint
        Waypoint[] houseWaypoints = FindObjectsOfType<Waypoint>();
        Waypoint nearestHouse = null;
        float nearestDistance = float.MaxValue;

        foreach (Waypoint waypoint in houseWaypoints)
        {
            if (waypoint.waypointType == WaypointType.House)
            {
                float distance = Vector3.Distance(transform.position, waypoint.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestHouse = waypoint;
                }
            }
        }

        if (nearestHouse != null)
        {
            // Move to house
            if (agent != null)
            {
                agent.SetDestination(nearestHouse.transform.position);
            }

            // Switch to house waypoint group if available
            if (nearestHouse.waypointGroup != null && nearestHouse.waypointGroup.groupType == WaypointType.House)
            {
                SwitchToWaypointGroup(nearestHouse.waypointGroup);
            }
        }
    }

    public void LeaveHouse()
    {
        if (!isInsideHouse) return;

        isInsideHouse = false;

        // Return to last outside position or find a new patrol point
        if (lastOutsidePosition != Vector3.zero)
        {
            if (agent != null)
            {
                agent.SetDestination(lastOutsidePosition);
            }
        }
        else
        {
            // Find a new patrol point outside
            Waypoint[] outsideWaypoints = FindObjectsOfType<Waypoint>();
            Waypoint nearestOutside = null;
            float nearestDistance = float.MaxValue;

            foreach (Waypoint waypoint in outsideWaypoints)
            {
                if (waypoint.waypointType != WaypointType.House)
                {
                    float distance = Vector3.Distance(transform.position, waypoint.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestOutside = waypoint;
                    }
                }
            }

            if (nearestOutside != null && nearestOutside.waypointGroup != null)
            {
                SwitchToWaypointGroup(nearestOutside.waypointGroup);
            }
        }
    }

    // Social interactions - Performance optimized
    void HandleSocialInteractions()
    {
        if (personality == CitizenPersonality.Loner || Time.time - lastSocialInteraction < socialInteractionCooldown)
            return;

        // Use CitizenManager for efficient citizen lookup
        if (CitizenManager.Instance != null)
        {
            nearestCitizen = CitizenManager.Instance.GetNearestCitizen(transform.position, socialInteractionRange);

            // Exclude self from the result
            if (nearestCitizen == this)
            {
                var nearbyCitizens = CitizenManager.Instance.GetCitizensInRangeExcluding(transform.position, socialInteractionRange, this);
                nearestCitizen = nearbyCitizens.Count > 0 ? nearbyCitizens[0] : null;
            }
        }
        else
        {
            // Fallback to old method if CitizenManager not available
            Citizen[] allCitizens = FindObjectsOfType<Citizen>();
            float nearestDistance = float.MaxValue;
            nearestCitizen = null;

            foreach (Citizen citizen in allCitizens)
            {
                if (citizen != this && citizen != null)
                {
                    float distance = Vector3.Distance(transform.position, citizen.transform.position);
                    if (distance < socialInteractionRange && distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestCitizen = citizen;
                    }
                }
            }
        }

        // Interact with nearest citizen
        if (nearestCitizen != null && Random.value < socialLevel * 0.1f)
        {
            SocialInteraction(nearestCitizen);
        }
    }

    void SocialInteraction(Citizen otherCitizen)
    {
        lastSocialInteraction = Time.time;

        // Share memories
        ShareMemories(otherCitizen);

        // Face each other
        Vector3 direction = (otherCitizen.transform.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
        }

        // Add memory of social interaction
        AddMemory(MemoryEntry.MemoryType.SocialInteraction, otherCitizen.transform.position, 0.3f,
                 $"Social interaction with {otherCitizen.name}");

        if (socialSound != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
                audioSource.PlayOneShot(socialSound);
        }
    }

    void ShareMemories(Citizen otherCitizen)
    {
        // Share important memories with other citizen
        foreach (MemoryEntry memory in memories)
        {
            if (memory.importance > 0.7f && memory.type == MemoryEntry.MemoryType.PlayerSighting)
            {
                otherCitizen.AddMemory(memory.type, memory.location, memory.importance * 0.5f,
                                     $"Heard about: {memory.description}");

                // Gossip spreads fear - make other citizen more alert
                if (otherCitizen.personality == CitizenPersonality.Cowardly)
                {
                    otherCitizen.viewDistance = Mathf.Min(otherCitizen.viewDistance * 1.2f, 50f);
                    otherCitizen.detectionTime *= 0.8f;
                }
            }
            else if (memory.type == MemoryEntry.MemoryType.DangerousEvent && memory.importance > 0.6f)
            {
                // Share memories of dangerous events too
                otherCitizen.AddMemory(memory.type, memory.location, memory.importance * 0.7f,
                                     $"Warning: {memory.description}");
            }
        }

        // If we have recent vampire sightings, both citizens become more cautious
        if (HasMemoryOfType(MemoryEntry.MemoryType.PlayerSighting))
        {
            braveryLevel = Mathf.Max(0.1f, braveryLevel - 0.1f);
            otherCitizen.braveryLevel = Mathf.Max(0.1f, otherCitizen.braveryLevel - 0.05f);
        }
    }

    // Environmental awareness
    void HandleEnvironmentalAwareness()
    {
        if (reactToNoises)
        {
            // Check for nearby noises (could be triggered by player actions, traps, etc.)
            // This would integrate with a noise system
        }

        if (reactToLights)
        {
            // Check for nearby light changes
            // This would integrate with lighting systems
        }
    }

    public void ReactToNoise(Vector3 noiseLocation, float noiseIntensity)
    {
        if (!reactToNoises || isAlerting || isRunningToGuard) return;

        float distance = Vector3.Distance(transform.position, noiseLocation);
        if (distance <= noiseReactionRange)
        {
            Debug.Log($"[Citizen] {name} heard noise at distance {distance:F1}m with intensity {noiseIntensity:F2}");

            // Increase suspicion based on noise intensity
            if (!isSuspicious && noiseIntensity > 0.3f)
            {
                isSuspicious = true;
                suspicionTimer = 0f;
                viewDistance = baseViewDistance * 1.5f;
                Debug.Log($"[Citizen] {name} became suspicious from noise");
            }

            // Check if the noise is from the player's direction
            bool isPlayerNearby = player != null && Vector3.Distance(noiseLocation, player.position) < 3f;

            // Personality affects reaction
            if (personality == CitizenPersonality.Cowardly)
            {
                // Cowardly citizens are more likely to panic from noise
                if (noiseIntensity > 0.7f || isPlayerNearby)
                {
                    // High intensity noise causes immediate panic
                    isAlerting = true;
                    isPanicking = true;
                    panicTimer = 0f;
                    lastKnownPlayerLocation = noiseLocation;
                    AlertGuards();
                }
                else
                {
                    // Run away from noise
                    Vector3 awayDirection = (transform.position - noiseLocation).normalized;
                    Vector3 fleePosition = transform.position + awayDirection * 10f;

                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(fleePosition, out hit, 10f, 1))
                    {
                        agent.SetDestination(hit.position);
                    }
                }
            }
            else if (personality == CitizenPersonality.Curious || personality == CitizenPersonality.Brave)
            {
                // Higher chance to investigate based on personality and intensity
                float investigateChance = personality == CitizenPersonality.Curious ?
                    noiseInvestigationChance * 1.5f : noiseInvestigationChance;

                if (Random.value < investigateChance * noiseIntensity)
                {
                    // Stop current patrol and investigate
                    waiting = false;
                    agent.SetDestination(noiseLocation);
                    Debug.Log($"[Citizen] {name} investigating noise at {noiseLocation}");
                }
            }
            else // Normal personality
            {
                // Normal citizens might investigate or flee based on intensity
                if (noiseIntensity > 0.8f)
                {
                    // Loud noise - flee
                    Vector3 awayDirection = (transform.position - noiseLocation).normalized;
                    Vector3 fleePosition = transform.position + awayDirection * 8f;

                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(fleePosition, out hit, 8f, 1))
                    {
                        agent.SetDestination(hit.position);
                    }
                }
                else if (noiseIntensity > 0.5f && Random.value < noiseInvestigationChance)
                {
                    // Medium noise - might investigate
                    agent.SetDestination(noiseLocation);
                }
            }

            AddMemory(MemoryEntry.MemoryType.Noise, noiseLocation, noiseIntensity,
                     $"Heard {(noiseIntensity > 0.8f ? "loud" : noiseIntensity > 0.5f ? "medium" : "soft")} noise");

            // Visual feedback
            UpdateVisualState();
        }
    }

    // Update memories
    void UpdateMemories()
    {
        // Remove expired memories
        memories.RemoveAll(memory => memory.IsExpired(memoryDecayTime));

        // Keep only the most important memories if we exceed max slots
        if (memories.Count > maxMemorySlots)
        {
            memories.Sort((a, b) => b.importance.CompareTo(a.importance));
            memories.RemoveRange(maxMemorySlots, memories.Count - maxMemorySlots);
        }
    }

    public void AddMemory(MemoryEntry.MemoryType type, Vector3 location, float importance, string description)
    {
        MemoryEntry memory = new MemoryEntry(type, location, importance, description);
        memories.Add(memory);
    }

    public bool HasMemoryOfType(MemoryEntry.MemoryType type)
    {
        return memories.Exists(memory => memory.type == type && !memory.IsExpired(memoryDecayTime));
    }

    public Vector3? GetMemoryLocation(MemoryEntry.MemoryType type)
    {
        MemoryEntry memory = memories.Find(m => m.type == type && !m.IsExpired(memoryDecayTime));
        return memory != null ? memory.location : null;
    }

    public void Initialize()
    {
        SetBloodAmountByRarity();
        memories.Clear();
    }

    // Public methods for configuring wait times
    public void SetWaitTimes(float minWait, float maxWait)
    {
        useCustomWaitTimes = true;
        customMinWaitTime = minWait;
        customMaxWaitTime = maxWait;
    }

    public void SetWaitTimeMultiplier(float multiplier)
    {
        waitTimeMultiplier = Mathf.Clamp(multiplier, 0.1f, 5f);
    }

    public void UseWaypointWaitTimes()
    {
        useCustomWaitTimes = false;
    }

    public void UseRarityBasedWaitTimes()
    {
        useCustomWaitTimes = true;
        SetDefaultWaitTimesByRarity();
    }

    // React to nearby citizen panic
    public void ReactToPanic(Vector3 panicSource)
    {
        // Personality affects reaction to panic
        float panicChance = 0.5f;

        switch (personality)
        {
            case CitizenPersonality.Cowardly:
                panicChance = 0.9f; // Very likely to panic
                break;
            case CitizenPersonality.Brave:
                panicChance = 0.2f; // Less likely to panic
                break;
            case CitizenPersonality.Curious:
                panicChance = 0.4f; // Might investigate instead
                break;
        }

        if (Random.value < panicChance)
        {
            // Start panicking
            isPanicking = true;
            panicTimer = 0f;
            isAlerting = true;

            // Run away from panic source
            if (agent != null)
            {
                Vector3 awayDirection = (transform.position - panicSource).normalized;
                Vector3 fleePosition = transform.position + awayDirection * 8f;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(fleePosition, out hit, 8f, 1))
                {
                    agent.SetDestination(hit.position);
                    agent.speed = agent.speed * 1.3f;
                }
            }

            // Add memory of panic
            AddMemory(MemoryEntry.MemoryType.DangerousEvent, panicSource, 0.8f,
                     "Witnessed panic at " + panicSource);
        }
        else if (personality == CitizenPersonality.Curious)
        {
            // Curious citizens might investigate
            if (agent != null)
            {
                agent.SetDestination(panicSource);
            }
        }
    }

    // Update visual state
    void UpdateVisualState()
    {
        if (citizenLight == null) return;

        if (isPanicking)
        {
            // Flashing red light when panicking
            float flashSpeed = 8f;
            citizenLight.color = Color.Lerp(scaredColor, Color.white, Mathf.PingPong(Time.time * flashSpeed, 1f));
            citizenLight.intensity = 1f;
        }
        else if (isAlerting)
        {
            citizenLight.color = alertColor;
            citizenLight.intensity = 0.8f;
        }
        else if (isSuspicious)
        {
            // Orange light when suspicious
            citizenLight.color = Color.Lerp(alertColor, normalColor, 0.5f);
            citizenLight.intensity = 0.6f;
        }
        else if (isHypnotized)
        {
            citizenLight.color = socialColor;
            citizenLight.intensity = 0.6f;
        }
        else if (nearestCitizen != null && Vector3.Distance(transform.position, nearestCitizen.transform.position) < socialInteractionRange)
        {
            citizenLight.color = socialColor;
            citizenLight.intensity = 0.5f;
        }
        else if (HasMemoryOfType(MemoryEntry.MemoryType.PlayerSighting))
        {
            // Cautious yellow if they remember seeing the vampire
            citizenLight.color = Color.Lerp(normalColor, alertColor, 0.5f);
            citizenLight.intensity = 0.4f;
        }
        else
        {
            citizenLight.color = normalColor;
            citizenLight.intensity = 0.3f;
        }
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        // Set the speed of the agent
        float speed = agent.velocity.magnitude / agent.speed;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);

        // Set panic state
        animator.SetBool("IsPanicking", isPanicking);
    }

    // Check if citizen is in darkness (harder to detect player in shadows)
    public float GetLightingModifier()
    {
        float lightLevel = 1f; // Default full visibility

        // Check for nearby lights
        Light[] nearbyLights = Physics.OverlapSphere(transform.position, 10f)
            .Select(col => col.GetComponent<Light>())
            .Where(light => light != null && light != citizenLight)
            .ToArray();

        if (nearbyLights.Length == 0)
        {
            // No nearby lights - harder to see
            lightLevel = 0.5f;
        }
        else
        {
            // Calculate light intensity at citizen position
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

        // Personality affects how darkness impacts detection
        switch (personality)
        {
            case CitizenPersonality.Cowardly:
                // Cowardly citizens are more afraid in the dark
                return lightLevel * 0.8f + 0.2f; // Min 20% visibility
            case CitizenPersonality.Brave:
                // Brave citizens handle darkness better
                return lightLevel * 0.5f + 0.5f; // Min 50% visibility
            default:
                return lightLevel * 0.7f + 0.3f; // Min 30% visibility
        }
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

        // If we have an assigned waypoint group, get the adjusted position
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

    // Share vampire sighting information with a guard
    void ShareInformationWithGuard(GuardAI guard)
    {
        if (guard == null) return;

        Debug.Log($"[Citizen] {name} sharing vampire location with guard {guard.name}");

        // Face the guard
        Vector3 directionToGuard = (guard.transform.position - transform.position).normalized;
        directionToGuard.y = 0;
        if (directionToGuard != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(directionToGuard);
        }

        // Animate talking/pointing
        animator?.SetTrigger("Point");

        // Play alert sound
        if (alertSound != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(alertSound);
            }
        }

        // Tell the guard to investigate the last known player location
        guard.InvestigateLocation(lastKnownPlayerLocation, searchRadius: 15f);

        // Alert other nearby guards too
        Collider[] nearbyGuardColliders = Physics.OverlapSphere(transform.position, 10f, LayerMask.GetMask("Guard"));
        foreach (Collider guardCol in nearbyGuardColliders)
        {
            GuardAI nearbyGuard = guardCol.GetComponent<GuardAI>();
            if (nearbyGuard != null && nearbyGuard != guard)
            {
                nearbyGuard.Alert(lastKnownPlayerLocation);
            }
        }

        // Add memory of reporting to guard
        AddMemory(MemoryEntry.MemoryType.SocialInteraction, guard.transform.position, 0.9f,
                 $"Reported vampire sighting to guard at {lastKnownPlayerLocation}");

        // Stop being alerted after reporting
        isAlerting = false;
        isPanicking = false;
        panicTimer = 0f;
    }

    // New methods for integration with new systems
    public void FleeToSafety()
    {
        // Find nearest house or safe location
        GameObject[] houses = GameObject.FindGameObjectsWithTag("House");
        GameObject nearestHouse = null;
        float nearestDistance = float.MaxValue;

        foreach (var house in houses)
        {
            float distance = Vector3.Distance(transform.position, house.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestHouse = house;
            }
        }

        if (nearestHouse != null)
        {
            // Set destination to nearest house
            if (agent != null)
            {
                agent.SetDestination(nearestHouse.transform.position);
                agent.speed = sprintSpeed;
            }
            
            currentState = CitizenState.Fleeing;
            isPanicking = true;
            panicTimer = panicDuration * 2f; // Extended panic when fleeing
            
            // Stop current activities
            StopPatrolling();
            
            GameLogger.Log(LogCategory.AI, $"{name} fleeing to safety!", this);
        }
    }

    public void SetOverrideDestination(Vector3 destination)
    {
        if (agent != null)
        {
            agent.SetDestination(destination);
            StopPatrolling();
            currentState = CitizenState.Walking;
        }
    }

    private Ward homeWard;
    public void SetHomeWard(Ward ward)
    {
        homeWard = ward;
    }

    public Ward GetHomeWard()
    {
        return homeWard;
    }

    private void StopPatrolling()
    {
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
            patrolCoroutine = null;
        }
    }
}
