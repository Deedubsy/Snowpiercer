using UnityEngine;
using System.Collections.Generic;

public class GuardAIDebugProvider : MonoBehaviour, IDebugProvider
{
    private GuardAI guardAI;
    private Transform player;
    
    void Start()
    {
        guardAI = GetComponent<GuardAI>();
        if (guardAI == null)
        {
            Debug.LogWarning($"[GuardAIDebugProvider] No GuardAI component found on {gameObject.name}");
            enabled = false;
            return;
        }
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }
    
    public string GetEntityName()
    {
        return $"Guard: {gameObject.name}";
    }
    
    public string GetCurrentState()
    {
        if (guardAI == null) return "Unknown";
        
        if (guardAI.isHypnotized)
            return "Hypnotized";
        
        return guardAI.currentState.ToString();
    }
    
    public float GetDetectionProgress()
    {
        if (guardAI == null) return 0f;
        return guardAI.DetectionProgress;
    }
    
    public Vector3 GetPosition()
    {
        return transform.position;
    }
    
    public Dictionary<string, object> GetDebugData()
    {
        var debugData = new Dictionary<string, object>();
        
        if (guardAI == null || player == null)
        {
            debugData["Error"] = "Missing References";
            return debugData;
        }
        
        // Basic positional info
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        debugData["Distance to Player"] = $"{distanceToPlayer:F1}m";
        
        // Calculate angle to player
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        debugData["Angle to Player"] = $"{angleToPlayer:F1}°";
        
        // View settings
        debugData["View Distance"] = $"{guardAI.viewDistance:F1}m";
        debugData["Field of View"] = $"{guardAI.fieldOfView:F1}°";
        
        // Detection settings
        debugData["Detection Time"] = $"{guardAI.detectionTime:F2}s";
        debugData["Close Range Dist"] = $"{guardAI.closeRangeDistance:F1}m";
        
        // Chase settings
        debugData["Chase Speed"] = $"{guardAI.chaseSpeed:F1}";
        debugData["Patrol Speed"] = $"{guardAI.patrolSpeed:F1}";
        debugData["Attack Range"] = $"{guardAI.attackRange:F1}m";
        
        // Current behavior info
        debugData["Can Attack"] = guardAI.canAttack;
        debugData["Has Spotted"] = guardAI.HasSpottedPlayer;
        debugData["Hypnotized"] = guardAI.isHypnotized;
        debugData["Lost Timer"] = $"{guardAI.LostTimer:F1}s";
        debugData["Alert Timer"] = $"{guardAI.AlertTimer:F1}s";
        
        // Alertness info
        debugData["Alertness Level"] = guardAI.currentAlertness.ToString();
        
        // Patrol info
        if (guardAI.patrolPoints != null && guardAI.patrolPoints.Length > 0)
        {
            debugData["Patrol Points"] = guardAI.patrolPoints.Length;
            // Current patrol point would need to be exposed from GuardAI
        }
        
        // Last known player position
        Vector3 lastKnownPos = guardAI.LastKnownPlayerPosition;
        if (lastKnownPos != Vector3.zero)
        {
            debugData["Last Known Pos"] = $"({lastKnownPos.x:F1}, {lastKnownPos.z:F1})";
        }
        
        // Line of sight check
        bool hasLineOfSight = CheckLineOfSight();
        debugData["Line of Sight"] = hasLineOfSight;
        
        // Movement info
        var agent = guardAI.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            debugData["Agent Speed"] = $"{agent.speed:F1}";
            debugData["Remaining Distance"] = $"{agent.remainingDistance:F1}m";
            debugData["Is Stopped"] = agent.isStopped;
        }
        
        return debugData;
    }
    
    private bool CanSeePlayerDirect()
    {
        if (guardAI == null || player == null) return false;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        
        // Use guardAI's public fields
        if (distanceToPlayer <= guardAI.viewDistance && angle <= guardAI.fieldOfView * 0.5f)
        {
            // Check for obstacles
            if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, guardAI.obstacleLayer))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private bool CheckLineOfSight()
    {
        if (player == null) return false;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        return !Physics.Raycast(transform.position + Vector3.up * 1.5f, directionToPlayer, distanceToPlayer, guardAI.obstacleLayer);
    }
    
    private bool GetHasSpottedStatus()
    {
        // This would need to be exposed from GuardAI as a public property
        // For now, infer from state
        return guardAI.currentState == GuardAI.GuardState.Chase || 
               guardAI.currentState == GuardAI.GuardState.Attack ||
               guardAI.currentState == GuardAI.GuardState.Alert;
    }
    
    private Vector3 GetLastKnownPlayerPosition()
    {
        // This would need to be exposed from GuardAI as a public property
        // For now, return current player position if we can see them
        if (CanSeePlayerDirect() && player != null)
        {
            return player.position;
        }
        
        return Vector3.zero;
    }
}