using UnityEngine;
using System.Collections.Generic;

public class CitizenDebugProvider : MonoBehaviour, IDebugProvider
{
    private Citizen citizen;
    private Transform player;
    
    void Start()
    {
        citizen = GetComponent<Citizen>();
        if (citizen == null)
        {
            Debug.LogWarning($"[CitizenDebugProvider] No Citizen component found on {gameObject.name}");
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
        string rarityText = citizen != null ? citizen.rarity.ToString() : "Unknown";
        return $"Citizen ({rarityText}): {gameObject.name}";
    }
    
    public string GetCurrentState()
    {
        if (citizen == null) return "Unknown";
        
        if (citizen.isHypnotized)
            return "Hypnotized";
        
        if (citizen.isDrained)
            return "Drained";
        
        if (citizen.isSleeping)
            return "Sleeping";
        
        // Determine state based on citizen's internal flags
        var agent = citizen.GetComponent<UnityEngine.AI.NavMeshAgent>();
        
        if (IsAlerting())
            return "Alerting";
        
        if (IsPanicking())
            return "Panicking";
        
        if (IsRunningToGuard())
            return "Running to Guard";
        
        if (IsSuspicious())
            return "Suspicious";
        
        if (IsDoingSocialInteraction())
            return "Social Interaction";
        
        if (agent != null && !agent.isStopped && agent.hasPath)
            return "Patrolling";
        
        return "Idle";
    }
    
    public float GetDetectionProgress()
    {
        if (citizen == null) return 0f;
        return citizen.detectionProgress;
    }
    
    public Vector3 GetPosition()
    {
        return transform.position;
    }
    
    public Dictionary<string, object> GetDebugData()
    {
        var debugData = new Dictionary<string, object>();
        
        if (citizen == null || player == null)
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
        
        // Citizen specific properties
        debugData["Rarity"] = citizen.rarity.ToString();
        debugData["Blood Amount"] = citizen.bloodAmount;
        debugData["Personality"] = citizen.personality.ToString();
        
        // Personality traits
        debugData["Bravery"] = $"{citizen.braveryLevel:F2}";
        debugData["Curiosity"] = $"{citizen.curiosityLevel:F2}";
        debugData["Social Level"] = $"{citizen.socialLevel:F2}";
        
        // View settings
        debugData["View Distance"] = $"{citizen.viewDistance:F1}m";
        debugData["Field of View"] = $"{citizen.fieldOfView:F1}°";
        debugData["Detection Time"] = $"{citizen.detectionTime:F2}s";
        
        // State flags
        debugData["Is Drained"] = citizen.isDrained;
        debugData["Is Hypnotized"] = citizen.isHypnotized;
        debugData["Is Sleeping"] = citizen.isSleeping;
        
        // Social behavior
        debugData["Social Range"] = $"{citizen.socialInteractionRange:F1}m";
        debugData["Social Cooldown"] = $"{citizen.socialInteractionCooldown:F1}s";
        
        // Find nearest citizen for social interaction
        if (CitizenManager.Instance != null)
        {
            var nearestCitizen = CitizenManager.Instance.GetNearestCitizen(transform.position, citizen.socialInteractionRange);
            if (nearestCitizen != null && nearestCitizen != citizen)
            {
                float distanceToNearest = Vector3.Distance(transform.position, nearestCitizen.transform.position);
                debugData["Nearest Citizen"] = $"{nearestCitizen.name} ({distanceToNearest:F1}m)";
            }
            else
            {
                debugData["Nearest Citizen"] = "None in range";
            }
        }
        
        // Schedule info
        if (citizen.schedule != null)
        {
            debugData["Has Schedule"] = true;
        }
        else
        {
            debugData["Has Schedule"] = false;
        }
        
        // Memory system
        debugData["Memory Slots"] = $"{GetMemoryCount()}/{citizen.maxMemorySlots}";
        
        // Environmental awareness
        debugData["Reacts to Noise"] = citizen.reactToNoises;
        debugData["Noise Range"] = $"{citizen.noiseReactionRange:F1}m";
        debugData["Light Range"] = $"{citizen.lightReactionRange:F1}m";
        
        // Line of sight check
        bool hasLineOfSight = CheckLineOfSight();
        debugData["Line of Sight"] = hasLineOfSight;
        
        // Movement info
        var agent = citizen.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            debugData["Agent Speed"] = $"{agent.speed:F1}";
            debugData["Remaining Distance"] = $"{agent.remainingDistance:F1}m";
            debugData["Is Stopped"] = agent.isStopped;
            debugData["Has Path"] = agent.hasPath;
        }
        
        return debugData;
    }
    
    private bool IsAlerting()
    {
        // Use reflection or check internal state
        // For now, check if agent is stopped and we're not patrolling
        var agent = citizen.GetComponent<UnityEngine.AI.NavMeshAgent>();
        return agent != null && agent.isStopped;
    }
    
    private bool IsPanicking()
    {
        // Infer from behavior - if moving fast away from player
        var agent = citizen.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.hasPath && player != null)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            Vector3 moveDirection = agent.velocity.normalized;
            float dot = Vector3.Dot(directionToPlayer, moveDirection);
            return dot < -0.5f && agent.velocity.magnitude > citizen.GetComponent<UnityEngine.AI.NavMeshAgent>().speed * 0.8f;
        }
        return false;
    }
    
    private bool IsRunningToGuard()
    {
        // Would need to be exposed from Citizen class
        // For now, check if moving quickly toward any guard
        if (SpatialGrid.Instance != null)
        {
            var nearbyGuards = SpatialGrid.Instance.GetEntitiesInRange<GuardAI>(transform.position, 30f);
            if (nearbyGuards.Count > 0)
            {
                var agent = citizen.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null && agent.hasPath)
                {
                    foreach (var guard in nearbyGuards)
                    {
                        float distanceToGuard = Vector3.Distance(transform.position, guard.transform.position);
                        if (distanceToGuard < 20f && agent.velocity.magnitude > agent.speed * 0.7f)
                        {
                            Vector3 directionToGuard = (guard.transform.position - transform.position).normalized;
                            Vector3 moveDirection = agent.velocity.normalized;
                            float dot = Vector3.Dot(directionToGuard, moveDirection);
                            if (dot > 0.7f) return true;
                        }
                    }
                }
            }
        }
        return false;
    }
    
    private bool IsSuspicious()
    {
        // Would need to be exposed from Citizen class
        // For now, check if view distance is increased
        return citizen.viewDistance > 35f; // Assuming base view distance is 35f
    }
    
    private bool IsDoingSocialInteraction()
    {
        // Check if there's a nearby citizen and we're facing them
        if (CitizenManager.Instance != null)
        {
            var nearestCitizen = CitizenManager.Instance.GetNearestCitizen(transform.position, citizen.socialInteractionRange);
            if (nearestCitizen != null && nearestCitizen != citizen)
            {
                Vector3 directionToOther = (nearestCitizen.transform.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, directionToOther);
                return dot > 0.8f; // Facing the other citizen
            }
        }
        return false;
    }
    
    private bool CheckLineOfSight()
    {
        if (player == null) return false;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        return !Physics.Raycast(transform.position + Vector3.up * 1.5f, directionToPlayer, distanceToPlayer, citizen.obstacleLayer);
    }
    
    private int GetMemoryCount()
    {
        // Would need to be exposed from Citizen class
        // For now, return placeholder
        return 0;
    }
}