using UnityEngine;
using UnityEngine.AI;

public class AISearchBehavior : MonoBehaviour
{
    [Header("Search Settings")]
    [SerializeField] private float searchRadius = 20f;
    [SerializeField] private float searchSpeed = 8f;
    [SerializeField] private float sniffRange = 5f;
    [SerializeField] private float trackingAccuracy = 0.8f;
    
    [Header("Search Pattern")]
    [SerializeField] private int searchPoints = 8;
    [SerializeField] private float spiralExpansionRate = 2f;
    
    private Vector3 searchTarget;
    private NavMeshAgent agent;
    private bool isSearching = false;
    private int currentSearchPoint = 0;
    private float currentSearchRadius = 5f;
    
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }
        
        agent.speed = searchSpeed;
    }
    
    public void SetSearchTarget(Vector3 target)
    {
        searchTarget = target;
        isSearching = true;
        currentSearchRadius = 5f;
        currentSearchPoint = 0;
        
        StartSearch();
    }
    
    private void StartSearch()
    {
        if (!isSearching) return;
        
        // Move to search target first
        agent.SetDestination(searchTarget);
    }
    
    private void Update()
    {
        if (!isSearching) return;
        
        // Check if we've reached the current destination
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            // Sniff for player scent
            if (CheckForPlayerScent())
            {
                // Found the player!
                OnPlayerFound();
            }
            else
            {
                // Continue search pattern
                MoveToNextSearchPoint();
            }
        }
    }
    
    private bool CheckForPlayerScent()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        
        // Dogs can smell the player from further away
        if (distanceToPlayer <= sniffRange)
        {
            // Tracking accuracy check
            if (Random.value <= trackingAccuracy)
            {
                return true;
            }
        }
        
        return false;
    }
    
    private void MoveToNextSearchPoint()
    {
        // Create a spiral search pattern
        float angle = (currentSearchPoint / (float)searchPoints) * 360f * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Sin(angle) * currentSearchRadius,
            0,
            Mathf.Cos(angle) * currentSearchRadius
        );
        
        Vector3 searchPosition = searchTarget + offset;
        
        // Ensure the position is on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(searchPosition, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        
        currentSearchPoint++;
        
        // Expand the search radius after completing a circle
        if (currentSearchPoint >= searchPoints)
        {
            currentSearchPoint = 0;
            currentSearchRadius += spiralExpansionRate;
            
            // Stop searching if we've exceeded max radius
            if (currentSearchRadius > searchRadius)
            {
                isSearching = false;
                OnSearchFailed();
            }
        }
    }
    
    private void OnPlayerFound()
    {
        isSearching = false;
        
        // Alert nearby guards
        GuardAI[] guards = FindObjectsOfType<GuardAI>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        foreach (var guard in guards)
        {
            float distance = Vector3.Distance(transform.position, guard.transform.position);
            if (distance <= 30f)
            {
                guard.AlertToPlayer(player.transform.position);
            }
        }
        
        // Make loud barking noise
        NoiseManager.MakeNoise(transform.position, 50f, 1f);
        
        GameLogger.Log(LogCategory.AI, "Search dog found the player!", this);
    }
    
    private void OnSearchFailed()
    {
        // Return to patrol or idle behavior
        GameLogger.Log(LogCategory.AI, "Search dog lost the trail", this);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sniffRange);
        
        if (isSearching)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(searchTarget, currentSearchRadius);
        }
    }
}