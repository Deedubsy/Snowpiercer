using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class WaypointGroup : MonoBehaviour
{
    [Header("Group Configuration")]
    public WaypointType groupType = WaypointType.Peasant;
    public Waypoint[] waypoints;
    
    [Header("Multi-Entity Support")]
    public int maxEntities = 1; // How many entities can use this group
    public float entitySpacing = 2f; // Minimum distance between entities on same route
    public bool allowSharedWaypoints = false; // Whether entities can share exact waypoint positions
    
    [Header("Patrol Patterns")]
    public PatrolPattern patrolPattern = PatrolPattern.Distributed; // Changed default to Distributed
    public bool reverseDirection = false;
    public float staggerDelay = 1f; // Delay between entities starting patrol
    
    [Header("Debug")]
    public bool showGizmos = true;
    public Color gizmoColor = Color.green;
    
    // Runtime tracking
    private List<GameObject> assignedEntities = new List<GameObject>();
    private Dictionary<GameObject, int> entityStartWaypoints = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, bool> entityDirections = new Dictionary<GameObject, bool>();
    
    void Start()
    {
        // Validate waypoints
        if (waypoints != null)
        {
            waypoints = waypoints.Where(w => w != null).ToArray();
        }
    }
    
    public bool CanAssignEntity(GameObject entity)
    {
        if (assignedEntities.Contains(entity)) return true; // Already assigned
        return assignedEntities.Count < maxEntities;
    }
    
    public bool AssignEntity(GameObject entity)
    {
        if (assignedEntities.Contains(entity)) return true; // Already assigned
        
        if (assignedEntities.Count >= maxEntities)
        {
            Debug.LogWarning($"Cannot assign {entity.name} to {name}: max entities ({maxEntities}) reached");
            return false;
        }
        
        assignedEntities.Add(entity);
        
        // Assign starting waypoint and direction based on patrol pattern
        int startWaypoint = GetStartingWaypointForEntity(assignedEntities.Count - 1);
        bool direction = GetDirectionForEntity(assignedEntities.Count - 1);
        
        entityStartWaypoints[entity] = startWaypoint;
        entityDirections[entity] = direction;
        
        Debug.Log($"Assigned {entity.name} to {name} starting at waypoint {startWaypoint}");
        return true;
    }
    
    public void UnassignEntity(GameObject entity)
    {
        if (assignedEntities.Remove(entity))
        {
            entityStartWaypoints.Remove(entity);
            entityDirections.Remove(entity);
            Debug.Log($"Unassigned {entity.name} from {name}");
        }
    }
    
    public int GetStartingWaypointForEntity(int entityIndex)
    {
        if (waypoints == null || waypoints.Length == 0) return 0;
        
        switch (patrolPattern)
        {
            case PatrolPattern.Sequential:
                // All start at the same point
                return 0;
                
            case PatrolPattern.Distributed:
                // Spread entities evenly across waypoints
                // Ensure each entity starts at a different waypoint
                if (waypoints.Length >= maxEntities)
                {
                    // If we have enough waypoints, give each entity a unique starting point
                    return entityIndex % waypoints.Length;
                }
                else
                {
                    // If we have fewer waypoints than entities, distribute as evenly as possible
                    return (entityIndex * waypoints.Length / maxEntities) % waypoints.Length;
                }
                
            case PatrolPattern.RandomStart:
                // Each entity starts at a random waypoint
                return Random.Range(0, waypoints.Length);
                
            case PatrolPattern.Opposite:
                // Alternate between start and middle
                return entityIndex % 2 == 0 ? 0 : waypoints.Length / 2;
                
            default:
                return 0;
        }
    }
    
    public bool GetDirectionForEntity(int entityIndex)
    {
        switch (patrolPattern)
        {
            case PatrolPattern.Opposite:
                // Alternate directions
                return entityIndex % 2 == 0 ? !reverseDirection : reverseDirection;
                
            case PatrolPattern.Sequential:
            case PatrolPattern.Distributed:
            case PatrolPattern.RandomStart:
            default:
                return reverseDirection;
        }
    }
    
    public Vector3 GetAdjustedWaypointPosition(GameObject entity, int waypointIndex)
    {
        if (waypoints == null || waypointIndex >= waypoints.Length || waypoints[waypointIndex] == null)
            return transform.position;
        
        Vector3 basePosition = waypoints[waypointIndex].transform.position;
        
        if (!allowSharedWaypoints && assignedEntities.Count > 1)
        {
            int entityIndex = assignedEntities.IndexOf(entity);
            if (entityIndex >= 0)
            {
                // Offset position to avoid crowding
                Vector3 offset = GetEntityOffset(entityIndex);
                return basePosition + offset;
            }
        }
        
        return basePosition;
    }
    
    Vector3 GetEntityOffset(int entityIndex)
    {
        if (entityIndex == 0) return Vector3.zero; // First entity uses exact position
        
        // Create offsets in a circle pattern
        float angle = (2f * Mathf.PI * entityIndex) / maxEntities;
        float distance = entitySpacing * ((entityIndex - 1) / 2 + 1); // Increase distance for outer rings
        
        return new Vector3(
            Mathf.Cos(angle) * distance,
            0f,
            Mathf.Sin(angle) * distance
        );
    }
    
    // Public getters for entity management
    public List<GameObject> GetAssignedEntities() => new List<GameObject>(assignedEntities);
    public int GetAssignedEntityCount() => assignedEntities.Count;
    public int GetAvailableSlots() => maxEntities - assignedEntities.Count;
    
    // Methods for waypoint access with entity context
    public int GetStartingWaypointIndex(GameObject entity)
    {
        return entityStartWaypoints.ContainsKey(entity) ? entityStartWaypoints[entity] : 0;
    }
    
    public bool GetPatrolDirection(GameObject entity)
    {
        return entityDirections.ContainsKey(entity) ? entityDirections[entity] : reverseDirection;
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos || waypoints == null) return;
        
        Gizmos.color = gizmoColor;
        
        // Draw waypoint connections
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].transform.position, waypoints[i + 1].transform.position);
            }
        }
        
        // Draw waypoints
        foreach (var waypoint in waypoints)
        {
            if (waypoint != null)
            {
                Gizmos.DrawWireSphere(waypoint.transform.position, 0.5f);
            }
        }
        
        // Draw entity capacity indicator
        if (Application.isPlaying)
        {
            Gizmos.color = assignedEntities.Count >= maxEntities ? Color.red : Color.green;
            if (waypoints.Length > 0 && waypoints[0] != null)
            {
                Vector3 pos = waypoints[0].transform.position + Vector3.up * 2f;
                Gizmos.DrawWireCube(pos, Vector3.one * 0.5f);
            }
        }
    }
    
    // Context menu methods for testing
    [ContextMenu("Log Group Status")]
    void LogGroupStatus()
    {
        Debug.Log($"WaypointGroup {name}: {assignedEntities.Count}/{maxEntities} entities assigned");
        foreach (var entity in assignedEntities)
        {
            if (entity != null)
            {
                int startWP = entityStartWaypoints.ContainsKey(entity) ? entityStartWaypoints[entity] : -1;
                Debug.Log($"  - {entity.name}: start waypoint {startWP}");
            }
        }
    }
    
    [ContextMenu("Clear All Assignments")]
    void ClearAllAssignments()
    {
        assignedEntities.Clear();
        entityStartWaypoints.Clear();
        entityDirections.Clear();
        Debug.Log($"Cleared all entity assignments from {name}");
    }
}

public enum PatrolPattern
{
    Sequential,    // All entities follow the same route in order
    Distributed,   // Entities spread out evenly across waypoints
    RandomStart,   // Each entity starts at a random waypoint
    Opposite       // Entities patrol in opposite directions
}