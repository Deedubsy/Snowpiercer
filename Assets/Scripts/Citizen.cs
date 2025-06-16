using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Citizen : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Waypoint[] patrolPoints;

    [Header("Vision Settings")]
    public float fieldOfView = 45f;       // Half-angle for the cone of vision.
    public float viewDistance = 10f;      // Maximum distance at which the player can be seen.
    public float detectionTime = 2f;      // Time (in seconds) the player must be in view before alerting.
    public LayerMask playerLayer;         // Layer for the player.
    public LayerMask obstacleLayer;       // Layer(s) that could block line-of-sight (walls, etc).

    [Header("Alert Settings")]
    public float guardAlertRadius = 15f;  // How far away the citizen can alert guards.

    [Header("Alerted State Settings")]
    public float alertStateDuration = 5f; // How long (in seconds) the citizen will search if the player is lost.
    public float scanningRotationSpeed = 45f; // Degrees per second when scanning.

    [HideInInspector]
    public bool isDrained = false;

    private NavMeshAgent agent;
    private int currentPointIndex = 0;
    private bool waiting = false;
    private float detectionTimer = 0f;
    private Transform player;
    private bool isAlerting = false;
    private float alertTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[currentPointIndex].transform.position);

        // Assume the player GameObject is tagged "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
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

        // If in alert state...
        if (isAlerting)
        {
            // Stop moving.
            if (agent != null)
                agent.isStopped = true;

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
                    // Resume movement.
                    if (agent != null)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(patrolPoints[currentPointIndex].transform.position);
                    }
                }
            }
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

        // Rotate instantly to face the waypoint's desired direction.
        transform.rotation = currentWaypoint.transform.rotation;


        float waitTime = Random.Range(currentWaypoint.minWaitTime, currentWaypoint.maxWaitTime);
        yield return new WaitForSeconds(waitTime);
        waiting = false;
        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        agent.SetDestination(currentWaypoint.transform.position);
    }

    // Checks for the player within the citizen's cone of vision.
    void DetectPlayer()
    {
        if (player == null)
            return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if player is within view distance and within the cone.
        if (distanceToPlayer < viewDistance && angle < fieldOfView)
        {
            // Optionally, perform a raycast to see if view is obstructed.
            if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer))
            {
                // If not already alerting, accumulate detection time.
                if (!isAlerting)
                {
                    detectionTimer += Time.deltaTime;
                    if (detectionTimer >= detectionTime)
                    {
                        isAlerting = true;
                        detectionTimer = 0f;
                        alertTimer = 0f;
                        AlertGuards();
                    }
                }
                else
                {
                    // If already alerting and player is visible, reset the alert timer.
                    alertTimer = 0f;
                }
                return;
            }
        }
        // If not in view and not already alerting, reset the detection timer.
        if (!isAlerting)
        {
            detectionTimer = 0f;
        }
    }

    // Alerts nearby guards if the player is detected.
    void AlertGuards()
    {
        Debug.Log("Citizen spotted the player! Alerting nearby guards!");
        // Find guards within the alert radius that are on a specific "Guard" layer.
        Collider[] guardColliders = Physics.OverlapSphere(transform.position, guardAlertRadius, LayerMask.GetMask("Guard"));
        foreach (Collider guardCol in guardColliders)
        {
            GuardAI guard = guardCol.GetComponent<GuardAI>();
            if (guard != null)
            {
                guard.Alert(player.position);
            }
        }
        // Optionally, reset the detection timer after alerting.
        detectionTimer = 0f;
    }

    bool IsPlayerVisible()
    {
        if (player == null)
            return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < viewDistance && angle < fieldOfView)
        {
            if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer))
                return true;
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
        // Optionally, play a draining animation or effect.
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        // Draw a yellow wire sphere to represent the view distance.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
        // Calculate the left and right boundary directions.
        Vector3 forward = transform.forward;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-fieldOfView, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(fieldOfView, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;
        // Draw red rays for the boundaries.
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, leftRayDirection * viewDistance);
        Gizmos.DrawRay(transform.position, rightRayDirection * viewDistance);
    }

    void OnDrawGizmosSelected()
    {
        // Draw a yellow wire sphere to represent the view distance.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        // Calculate the left and right boundary directions.
        Vector3 forward = transform.forward;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-fieldOfView, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(fieldOfView, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;

        // Draw red rays for the boundaries.
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, leftRayDirection * viewDistance);
        Gizmos.DrawRay(transform.position, rightRayDirection * viewDistance);
    }
}
