using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GuardAI : MonoBehaviour
{
    // Patrol settings
    public Transform[] patrolPoints;
    public float waitTimeMin = 2f;        // Minimum waiting time at a patrol point.
    public float waitTimeMax = 5f;        // Maximum waiting time at a patrol point.

    private int currentPointIndex = 0;
    private NavMeshAgent agent;

    // State machine
    private enum GuardState { Patrol, Chase, Alert }
    private GuardState currentState = GuardState.Patrol;

    // Vision settings
    [Header("Vision Settings")]
    public float fieldOfView = 75f;         // Full cone angle (degrees)
    public float viewDistance = 25f;        // Maximum distance to see the player
    public LayerMask playerLayer;           // Should match the Player’s layer
    public LayerMask obstacleLayer;         // Layers that block vision (walls, etc.)

    // Chase settings
    [Header("Chase Settings")]
    public float timeToLosePlayer = 3f;     // Time the guard must lose sight before switching to alert mode

    // Alert settings
    [Header("Alert Settings")]
    public float alertDuration = 5f;        // Time guard stays in alert mode before resuming patrol

    private float lostTimer = 0f;           // Tracks how long the player has been out of sight
    private float alertTimer = 0f;          // Tracks time in alert mode
    private Vector3 lastKnownPlayerPos;     // Remember the last seen position

    // Reference to the player transform
    private Transform player;

    // Flag to ensure we count a spotting event only once per loss.
    private bool hasSpotted = false;
    private bool waiting = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Start patrolling if patrol points are set
        if (patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[currentPointIndex].position);

        // Find the player by tag (make sure your player is tagged "Player")
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void Update()
    {
        switch (currentState)
        {
            case GuardState.Patrol:
                Patrol();
                if (CanSeePlayer())
                {
                    currentState = GuardState.Chase;
                    lostTimer = 0f;

                    if (!hasSpotted)
                    {
                        GameManager.instance?.IncrementSpotted();
                        hasSpotted = true;
                    }
                }
                break;

            case GuardState.Chase:
                Chase();
                // If player is visible, reset the lost timer
                if (CanSeePlayer())
                {
                    lostTimer = 0f;
                }
                else
                {
                    lostTimer += Time.deltaTime;
                    // If player is out of sight for too long, switch to Alert state
                    if (lostTimer >= timeToLosePlayer)
                    {
                        currentState = GuardState.Alert;
                        alertTimer = 0f;
                        // Continue to head to the last known player position
                        agent.SetDestination(lastKnownPlayerPos);
                    }
                }
                break;

            case GuardState.Alert:
                AlertMode();
                // If the player is spotted again, resume chase
                if (CanSeePlayer())
                {
                    currentState = GuardState.Chase;
                    lostTimer = 0f;
                }
                else
                {
                    alertTimer += Time.deltaTime;
                    // After alert duration, return to patrol
                    if (alertTimer >= alertDuration)
                    {
                        currentState = GuardState.Patrol;
                        lostTimer = 0f;
                        if (patrolPoints.Length > 0)
                            agent.SetDestination(patrolPoints[currentPointIndex].position);
                    }
                }
                break;
        }
    }

    // Checks if the player is within the guard's vision cone
    bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        // Use half of the full FOV for the cone check
        if (distanceToPlayer <= viewDistance && angle <= fieldOfView * 0.5f)
        {
            // Optionally, perform a raycast to ensure there are no obstacles blocking the view
            if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer))
            {
                lastKnownPlayerPos = player.position; // update last known position
                return true;
            }
        }
        return false;
    }

    // Patrol mode: move between patrol points
    void Patrol()
    {
        if (waiting || patrolPoints.Length == 0)
            return;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            //currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
            //agent.SetDestination(patrolPoints[currentPointIndex].position);
            StartCoroutine(WaitAtPoint());
        }
    }

    IEnumerator WaitAtPoint()
    {
        waiting = true;
        float waitTime = Random.Range(waitTimeMin, waitTimeMax);
        yield return new WaitForSeconds(waitTime);
        waiting = false;
        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[currentPointIndex].position);
    }

    // Chase mode: head towards the player's current position or last known position
    void Chase()
    {
        if (player != null)
            agent.SetDestination(player.position);
        else
            agent.SetDestination(lastKnownPlayerPos);
    }

    // Alert mode: guard remains at (or searches around) the last known player position
    void AlertMode()
    {
        // Optionally, you could add behaviors like rotating in place or investigating nearby areas.
        // For now, the guard simply stays near the last known player position.
    }

    // External method to be called by a citizen when alerting guards.
    // This immediately puts the guard into chase mode.
    public void Alert(Vector3 playerPos)
    {
        currentState = GuardState.Chase;
        lastKnownPlayerPos = playerPos;
        lostTimer = 0f;
        Debug.Log("Guard alerted by citizen – chasing the player!");
    }

    private void OnDrawGizmos()
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
}
