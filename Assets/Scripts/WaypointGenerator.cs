using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaypointGenerator : MonoBehaviour
{
    [Header("Waypoint Prefab")]
    public GameObject waypointPrefab;
    [Header("Waypoint Group Prefab (optional)")]
    public GameObject waypointGroupPrefab;
    [Header("Random Seed (0 = random)")]
    public int randomSeed = 0;
    [Header("Options")]
    public bool autoClearBeforeGenerate = true;

    [Header("Collision Detection")]
    public LayerMask obstacleLayerMask = -1; // What counts as obstacles
    public float minDistanceBetweenWaypoints = 2f;
    public float waypointRadius = 0.5f; // Radius for collision checking
    public int maxPlacementAttempts = 50; // Max attempts before giving up on a waypoint

    [Header("Waypoint Clustering")]
    public bool clusterWaypoints = true; // Group waypoints closer together
    public float clusterRadius = 10f; // Maximum radius from center for clustered waypoints
    public float clusterSpacing = 3f; // Preferred spacing between waypoints in a cluster

    [Header("Terrain Settings")]
    public float terrainHeightOffset = 0.2f; // How far above terrain to place waypoints
    public float maxTerrainSlope = 30f; // Maximum slope angle in degrees
    public bool validateNavMesh = true; // Check if position is on NavMesh

    [Header("Debug")]
    public bool debugMode = false;
    public bool showDebugSpheres = false;

    private List<GameObject> generatedWaypoints = new List<GameObject>();
    private List<GameObject> generatedGroups = new List<GameObject>();

    public void RegenerateWaypoints()
    {
        if (autoClearBeforeGenerate)
            ClearGeneratedWaypoints();
        GenerateWaypointsInAreas();
    }

    public void ClearGeneratedWaypoints()
    {
        foreach (var go in generatedWaypoints)
            if (go != null) DestroyImmediate(go);
        generatedWaypoints.Clear();
        foreach (var go in generatedGroups)
            if (go != null) DestroyImmediate(go);
        generatedGroups.Clear();
    }

    public void GenerateWaypointsInAreas()
    {
        if (randomSeed != 0)
            Random.InitState(randomSeed);

        WaypointArea[] areas = FindObjectsByType<WaypointArea>(FindObjectsSortMode.None);
        int totalWaypointsGenerated = 0;
        int totalWaypointsAttempted = 0;

        foreach (var area in areas)
        {
            List<Waypoint> areaWaypoints = new List<Waypoint>();
            List<Vector3> placedPositions = new List<Vector3>();

            for (int i = 0; i < area.waypointCount; i++)
            {
                Vector3 validPos;
                bool foundValidPosition = TryFindValidPosition(area, i, area.waypointCount, placedPositions, out validPos);
                totalWaypointsAttempted++;

                if (foundValidPosition)
                {
                    GameObject wpObj = (waypointPrefab != null)
                        ? Instantiate(waypointPrefab)
                        : new GameObject("Waypoint");
                    wpObj.transform.position = validPos;
                    wpObj.name = $"Waypoint_{area.areaType}_{i}";

                    Waypoint wp = wpObj.GetComponent<Waypoint>() ?? wpObj.AddComponent<Waypoint>();
                    areaWaypoints.Add(wp);
                    generatedWaypoints.Add(wpObj);
                    placedPositions.Add(validPos);
                    totalWaypointsGenerated++;

                    if (debugMode)
                    {
                        Debug.Log($"Successfully placed waypoint {i} for {area.areaType} at {validPos}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Failed to find valid position for waypoint {i} in {area.areaType} area after {maxPlacementAttempts} attempts");
                }
            }

            // Only create group if we have waypoints
            if (areaWaypoints.Count > 0)
            {
                GameObject groupObj = (waypointGroupPrefab != null)
                    ? Instantiate(waypointGroupPrefab)
                    : new GameObject($"{area.areaType}Group");

                groupObj.transform.position = area.transform.position;
                groupObj.transform.parent = area.transform;
                WaypointGroup group = groupObj.GetComponent<WaypointGroup>() ?? groupObj.AddComponent<WaypointGroup>();

                // Move all waypoints to be children of the group instead of the area
                foreach (Waypoint wp in areaWaypoints)
                {
                    wp.transform.parent = groupObj.transform;
                }

                // Configure group from area settings
                group.groupType = area.areaType;
                group.waypoints = areaWaypoints.ToArray();
                group.maxEntities = area.maxEntitiesPerGroup;
                group.entitySpacing = area.entitySpacing;
                group.patrolPattern = area.patrolPattern;
                group.allowSharedWaypoints = area.allowSharedWaypoints;

                groupObj.name = $"{area.areaType}Group_{areaWaypoints.Count}wp_{group.maxEntities}max";
                generatedGroups.Add(groupObj);

                if (debugMode)
                {
                    Debug.Log($"Created waypoint group {groupObj.name} with {areaWaypoints.Count} waypoints, max {group.maxEntities} entities");
                }
            }
        }

        if (debugMode)
        {
            Debug.Log($"Waypoint generation complete: {totalWaypointsGenerated}/{totalWaypointsAttempted} waypoints successfully placed");
        }
    }

    Vector3 GetRandomPositionInArea(WaypointArea area, int index, int total, int attempt = 0)
    {
        bool useLinearPlacement = area.placementMode == WaypointPlacementMode.Linear && total > 1;

        if (area.shape == WaypointAreaShape.Box)
        {
            Vector3 half = area.size * 0.5f;

            // If clustering is enabled, constrain waypoints to a smaller area
            if (clusterWaypoints)
            {
                half.x = Mathf.Min(half.x, clusterRadius);
                half.z = Mathf.Min(half.z, clusterRadius);
            }

            if (useLinearPlacement && attempt < 10)
            {
                // Linear pattern - waypoints in a line
                float t = (float)index / (total - 1);

                // For clustered waypoints, use smaller spacing
                if (clusterWaypoints)
                {
                    float lineLength = clusterSpacing * (total - 1);
                    half.x = Mathf.Min(half.x, lineLength * 0.5f);
                }

                // Rotate the line based on the area's transform rotation
                Vector3 localStart = new Vector3(-half.x, 0, 0);
                Vector3 localEnd = new Vector3(half.x, 0, 0);
                Vector3 localPos1 = Vector3.Lerp(localStart, localEnd, t);

                // Transform to world space
                Vector3 basePos = area.transform.TransformPoint(area.center + localPos1);

                // Add small random offset for collision avoidance on retry attempts
                if (attempt > 0)
                {
                    float offsetRange = Mathf.Min(2f, minDistanceBetweenWaypoints * 0.5f);
                    basePos += new Vector3(
                        Random.Range(-offsetRange, offsetRange),
                        0,
                        Random.Range(-offsetRange, offsetRange)
                    );
                }

                return basePos;
            }

            // Random scatter - use grid-based approach for clustering
            if (clusterWaypoints && total > 1)
            {
                // Calculate grid dimensions
                int gridSize = Mathf.CeilToInt(Mathf.Sqrt(total));
                int row = index / gridSize;
                int col = index % gridSize;

                // Calculate position in grid
                float gridX = (col - (gridSize - 1) * 0.5f) * clusterSpacing;
                float gridZ = (row - (gridSize - 1) * 0.5f) * clusterSpacing;

                // Add some randomness to avoid perfect grid
                gridX += Random.Range(-clusterSpacing * 0.3f, clusterSpacing * 0.3f);
                gridZ += Random.Range(-clusterSpacing * 0.3f, clusterSpacing * 0.3f);

                // Clamp to area bounds
                gridX = Mathf.Clamp(gridX, -half.x, half.x);
                gridZ = Mathf.Clamp(gridZ, -half.z, half.z);

                Vector3 localPos = area.center + new Vector3(gridX, 0, gridZ);
                return area.transform.TransformPoint(localPos);
            }
            else
            {
                // Random scatter within bounds
                float x = Random.Range(-half.x, half.x);
                float z = Random.Range(-half.z, half.z);
                Vector3 localPos = area.center + new Vector3(x, 0, z);
                return area.transform.TransformPoint(localPos);
            }
        }
        else // Sphere
        {
            float maxRadius = clusterWaypoints ? Mathf.Min(area.radius, clusterRadius) : area.radius;

            if (useLinearPlacement && attempt < 10)
            {
                // Place in a circle for linear mode
                float angle = 2 * Mathf.PI * index / total;
                float radius = clusterWaypoints ? Mathf.Min(maxRadius, clusterSpacing * total / (2 * Mathf.PI)) : maxRadius;

                // Add random offset for collision avoidance on retry attempts
                if (attempt > 0)
                {
                    angle += Random.Range(-0.2f, 0.2f); // Small angle variation
                    radius *= Random.Range(0.8f, 1.0f); // Vary radius slightly
                }

                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                return area.transform.position + area.center + offset;
            }

            // Random scatter - use spiral pattern for clustering
            if (clusterWaypoints && total > 1)
            {
                // Spiral pattern for better distribution
                float goldenAngle = 137.5f * Mathf.Deg2Rad;
                float angle = index * goldenAngle;
                float normalizedIndex = (float)index / total;
                float radius = normalizedIndex * maxRadius;

                // Add some randomness
                angle += Random.Range(-0.3f, 0.3f);
                radius = Mathf.Min(radius + Random.Range(-clusterSpacing * 0.3f, clusterSpacing * 0.3f), maxRadius);

                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                return area.transform.position + area.center + offset;
            }
            else
            {
                // Random scatter
                Vector2 circle = Random.insideUnitCircle * maxRadius;
                return area.transform.position + area.center + new Vector3(circle.x, 0, circle.y);
            }
        }
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showDebugSpheres) return;

        // Draw spheres around generated waypoints
        Gizmos.color = Color.yellow;
        foreach (GameObject waypoint in generatedWaypoints)
        {
            if (waypoint != null)
            {
                Gizmos.DrawWireSphere(waypoint.transform.position, waypointRadius);
            }
        }

        // Draw minimum distance spheres
        Gizmos.color = Color.red;
        foreach (GameObject waypoint in generatedWaypoints)
        {
            if (waypoint != null)
            {
                Gizmos.DrawWireSphere(waypoint.transform.position, minDistanceBetweenWaypoints * 0.5f);
            }
        }
    }

    // Context menu methods for testing
    [ContextMenu("Generate Waypoints")]
    void GenerateFromContext() => GenerateWaypointsInAreas();

    [ContextMenu("Clear Waypoints")]
    void ClearFromContext() => ClearGeneratedWaypoints();

    [ContextMenu("Regenerate Waypoints")]
    void RegenerateFromContext() => RegenerateWaypoints();

    [ContextMenu("Validate All Waypoints")]
    void ValidateAllWaypoints()
    {
        int validCount = 0;
        int invalidCount = 0;

        foreach (GameObject waypoint in generatedWaypoints)
        {
            if (waypoint != null)
            {
                Vector3 pos = waypoint.transform.position;
                bool isValid = IsValidWaypointPosition(pos, new List<Vector3>());

                if (isValid)
                {
                    validCount++;
                }
                else
                {
                    invalidCount++;
                    Debug.LogWarning($"Invalid waypoint found at {pos} on object {waypoint.name}", waypoint);
                }
            }
        }

        Debug.Log($"Waypoint validation complete: {validCount} valid, {invalidCount} invalid");
    }

    bool TryFindValidPosition(WaypointArea area, int index, int total, List<Vector3> existingPositions, out Vector3 validPosition)
    {
        validPosition = Vector3.zero;

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            Vector3 candidatePos = GetRandomPositionInArea(area, index, total, attempt);
            candidatePos = AdjustPositionToTerrain(candidatePos);

            if (IsValidWaypointPosition(candidatePos, existingPositions))
            {
                validPosition = candidatePos;
                return true;
            }
        }

        return false;
    }

    bool IsValidWaypointPosition(Vector3 position, List<Vector3> existingPositions)
    {
        // Check collision with obstacles
        if (Physics.CheckSphere(position, waypointRadius, obstacleLayerMask))
        {
            if (debugMode) Debug.Log($"Position {position} failed obstacle check");
            return false;
        }

        // Check minimum distance from existing waypoints
        foreach (Vector3 existingPos in existingPositions)
        {
            if (Vector3.Distance(position, existingPos) < minDistanceBetweenWaypoints)
            {
                if (debugMode) Debug.Log($"Position {position} too close to existing waypoint at {existingPos}");
                return false;
            }
        }

        // Check terrain slope
        if (!IsTerrainSlopeValid(position))
        {
            if (debugMode) Debug.Log($"Position {position} failed slope check");
            return false;
        }

        // Check NavMesh if enabled
        if (validateNavMesh && !IsOnNavMesh(position))
        {
            if (debugMode) Debug.Log($"Position {position} not on NavMesh");
            return false;
        }

        return true;
    }

    bool IsTerrainSlopeValid(Vector3 position)
    {
        // Sample terrain at multiple points around the position to calculate slope
        float sampleDistance = 1f;
        Vector3[] samplePoints = {
            position + Vector3.forward * sampleDistance,
            position + Vector3.back * sampleDistance,
            position + Vector3.left * sampleDistance,
            position + Vector3.right * sampleDistance
        };

        float centerHeight = GetTerrainHeight(position);

        foreach (Vector3 samplePoint in samplePoints)
        {
            float sampleHeight = GetTerrainHeight(samplePoint);
            float heightDiff = Mathf.Abs(sampleHeight - centerHeight);
            float angle = Mathf.Atan(heightDiff / sampleDistance) * Mathf.Rad2Deg;

            if (angle > maxTerrainSlope)
            {
                return false;
            }
        }

        return true;
    }

    bool IsOnNavMesh(Vector3 position)
    {
        NavMeshHit hit;
        return NavMesh.SamplePosition(position, out hit, 2f, NavMesh.AllAreas);
    }

    float GetTerrainHeight(Vector3 worldPosition)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            Vector3 terrainPos = terrain.transform.position;
            Vector3 localPos = worldPosition - terrainPos;
            float normX = Mathf.Clamp01(localPos.x / terrain.terrainData.size.x);
            float normZ = Mathf.Clamp01(localPos.z / terrain.terrainData.size.z);
            return terrain.terrainData.GetInterpolatedHeight(normX, normZ) + terrainPos.y;
        }

        // Fallback: use raycast
        RaycastHit hit;
        if (Physics.Raycast(worldPosition + Vector3.up * 100f, Vector3.down, out hit, 200f))
        {
            return hit.point.y;
        }

        return worldPosition.y;
    }

    Vector3 AdjustPositionToTerrain(Vector3 pos)
    {
        float terrainHeight = GetTerrainHeight(pos);
        pos.y = terrainHeight + terrainHeightOffset;
        return pos;
    }
}