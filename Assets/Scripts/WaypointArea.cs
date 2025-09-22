using UnityEngine;

public enum WaypointAreaShape { Box, Sphere }
public enum WaypointPlacementMode { Random, Linear }

public class WaypointArea : MonoBehaviour
{
    [Header("Area Configuration")]
    public WaypointType areaType = WaypointType.Peasant;
    public WaypointAreaShape shape = WaypointAreaShape.Box;
    public Vector3 center = Vector3.zero;
    public Vector3 size = new Vector3(10, 0, 10); // For box
    public float radius = 5f; // For sphere
    public int waypointCount = 5;
    
    [Header("Guard Patrol Settings")]
    [Tooltip("Linear: Waypoints placed in a line/circle. Random: Waypoints scattered randomly")]
    public WaypointPlacementMode placementMode = WaypointPlacementMode.Random;
    
    [Header("Multi-Entity Support")]
    public int maxEntitiesPerGroup = 1; // How many entities can use the generated group
    public float entitySpacing = 2f; // Spacing between entities
    public PatrolPattern patrolPattern = PatrolPattern.Sequential;
    public bool allowSharedWaypoints = false;
    
    [Header("Visual")]
    public Color gizmoColor = Color.green;

    public int GetRecommendedEntityCount()
    {
        // Recommend entity count based on area type and size
        switch (areaType)
        {
            case WaypointType.Guard:
                return Mathf.Min(maxEntitiesPerGroup, GetAreaCapacity() / 3); // Guards need more space
            case WaypointType.Peasant:
                return Mathf.Min(maxEntitiesPerGroup, GetAreaCapacity()); // Peasants can be crowded
            case WaypointType.Merchant:
                return Mathf.Min(maxEntitiesPerGroup, 2); // Merchants usually 1-2
            case WaypointType.Priest:
                return 1; // Priests usually alone
            case WaypointType.Noble:
                return Mathf.Min(maxEntitiesPerGroup, 3); // Nobles with small entourage
            case WaypointType.Royalty:
                return Mathf.Min(maxEntitiesPerGroup, 4); // Royalty with larger entourage
            default:
                return 1;
        }
    }
    
    int GetAreaCapacity()
    {
        // Estimate how many entities could reasonably fit in this area
        float area = shape == WaypointAreaShape.Box ? size.x * size.z : Mathf.PI * radius * radius;
        float entitySpace = entitySpacing * entitySpacing;
        return Mathf.Max(1, Mathf.FloorToInt(area / entitySpace));
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        if (shape == WaypointAreaShape.Box)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = oldMatrix;
        }
        else if (shape == WaypointAreaShape.Sphere)
        {
            Gizmos.DrawWireSphere(transform.position + center, radius);
        }
        
        // Draw entity capacity indicator
        Gizmos.color = Color.white;
        Vector3 labelPos = transform.position + center + Vector3.up * 1f;
        
        // Draw a small indicator showing max entities
        for (int i = 0; i < Mathf.Min(maxEntitiesPerGroup, 5); i++)
        {
            Vector3 dotPos = labelPos + Vector3.right * (i * 0.3f - (maxEntitiesPerGroup - 1) * 0.15f);
            Gizmos.DrawWireSphere(dotPos, 0.1f);
        }
    }
    
    // Context menu for testing
    [ContextMenu("Log Area Info")]
    void LogAreaInfo()
    {
        Debug.Log($"WaypointArea {name}:");
        Debug.Log($"  Type: {areaType}");
        Debug.Log($"  Waypoints: {waypointCount}");
        Debug.Log($"  Max Entities: {maxEntitiesPerGroup}");
        Debug.Log($"  Recommended Entities: {GetRecommendedEntityCount()}");
        Debug.Log($"  Area Capacity: {GetAreaCapacity()}");
    }
} 