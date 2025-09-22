using UnityEngine;
using System.Collections.Generic;

public class WaypointSystemSetup : MonoBehaviour
{
    [Header("Waypoint Prefabs")]
    public GameObject waypointPrefab;
    public GameObject waypointGroupPrefab;

    [Header("Generation Settings")]
    public bool autoGenerateOnStart = false;
    public bool clearExistingWaypoints = true;
    public int randomSeed = 0;

    [Header("District Configuration")]
    [Range(1, 15)] public int marketSquareWaypoints = 8;
    [Range(1, 20)] public int residentialWaypoints = 12;
    [Range(1, 10)] public int artisanWaypoints = 6;
    [Range(1, 8)] public int nobleWaypoints = 5;
    [Range(1, 15)] public int castleGroundsWaypoints = 10;
    [Range(1, 12)] public int castleInteriorWaypoints = 8;

    [Header("Guard Patrol Configuration")]
    [Range(2, 8)] public int mainGateGuardWaypoints = 4;
    [Range(2, 6)] public int castleWallGuardWaypoints = 3;
    [Range(2, 8)] public int townPatrolWaypoints = 5;

    [Header("House Waypoints")]
    [Range(5, 30)] public int totalHouseWaypoints = 15;

    void Start()
    {
        if (autoGenerateOnStart)
        {
            SetupCompleteWaypointSystem();
        }
    }

    [ContextMenu("Setup Complete Waypoint System")]
    public void SetupCompleteWaypointSystem()
    {
        Debug.Log("[WaypointSystemSetup] Starting complete waypoint system setup...");

        if (clearExistingWaypoints)
        {
            ClearAllWaypoints();
        }

        // Set random seed for consistent generation
        if (randomSeed != 0)
        {
            Random.InitState(randomSeed);
        }

        // Create waypoint areas for all districts
        CreateCitizenWaypointAreas();
        CreateGuardWaypointAreas();
        CreateHouseWaypointAreas();

        // Generate waypoints for all areas
        GenerateAllWaypoints();

        Debug.Log("[WaypointSystemSetup] Complete waypoint system setup finished!");
    }

    void CreateCitizenWaypointAreas()
    {
        // Market Square - Central hub for peasants and merchants
        CreateWaypointArea("Market Square",
                          new Vector3(0, 0, 0),
                          new Vector3(25, 0, 25),
                          WaypointType.Peasant,
                          marketSquareWaypoints,
                          WaypointPlacementMode.Random,
                          3); // Max 3 peasants per group

        CreateWaypointArea("Market Merchant Area",
                          new Vector3(5, 0, -5),
                          new Vector3(15, 0, 15),
                          WaypointType.Merchant,
                          4,
                          WaypointPlacementMode.Linear,
                          2); // Max 2 merchants per group

        // Residential District - Mix of peasants and some merchants
        CreateWaypointArea("Residential District",
                          new Vector3(-30, 0, 0),
                          new Vector3(35, 0, 35),
                          WaypointType.Peasant,
                          residentialWaypoints,
                          WaypointPlacementMode.Random,
                          4);

        // Artisan Quarter - Mix of merchants and some nobles
        CreateWaypointArea("Artisan Quarter",
                          new Vector3(30, 0, 0),
                          new Vector3(25, 0, 25),
                          WaypointType.Merchant,
                          artisanWaypoints,
                          WaypointPlacementMode.Random,
                          2);

        // Noble Quarter - High-value citizens
        CreateWaypointArea("Noble Quarter",
                          new Vector3(0, 0, 35),
                          new Vector3(20, 0, 20),
                          WaypointType.Noble,
                          nobleWaypoints,
                          WaypointPlacementMode.Linear,
                          3);

        // Priest waypoints - Near chapel/church area
        CreateWaypointArea("Chapel Area",
                          new Vector3(-15, 0, 30),
                          new Vector3(12, 0, 12),
                          WaypointType.Priest,
                          3,
                          WaypointPlacementMode.Linear,
                          1);

        // Castle Grounds - Royalty and nobles
        CreateWaypointArea("Castle Grounds",
                          new Vector3(0, 0, 60),
                          new Vector3(30, 0, 25),
                          WaypointType.Noble,
                          castleGroundsWaypoints,
                          WaypointPlacementMode.Random,
                          2);

        CreateWaypointArea("Castle Courtyard",
                          new Vector3(0, 0, 70),
                          new Vector3(20, 0, 15),
                          WaypointType.Royalty,
                          4,
                          WaypointPlacementMode.Linear,
                          2);

        // Castle Interior - High-value targets
        CreateWaypointArea("Castle Interior",
                          new Vector3(0, 5, 75),
                          new Vector3(25, 0, 20),
                          WaypointType.Royalty,
                          castleInteriorWaypoints,
                          WaypointPlacementMode.Random,
                          3);
    }

    void CreateGuardWaypointAreas()
    {
        // Main Gate Guard Post
        CreateWaypointArea("Main Gate Guard Post",
                          new Vector3(0, 0, -25),
                          new Vector3(15, 0, 10),
                          WaypointType.Guard,
                          mainGateGuardWaypoints,
                          WaypointPlacementMode.Linear,
                          2);

        // Castle Wall Patrols
        CreateWaypointArea("Castle Wall East",
                          new Vector3(15, 0, 55),
                          new Vector3(8, 0, 25),
                          WaypointType.Guard,
                          castleWallGuardWaypoints,
                          WaypointPlacementMode.Linear,
                          1);

        CreateWaypointArea("Castle Wall West",
                          new Vector3(-15, 0, 55),
                          new Vector3(8, 0, 25),
                          WaypointType.Guard,
                          castleWallGuardWaypoints,
                          WaypointPlacementMode.Linear,
                          1);

        // Town Patrols
        CreateWaypointArea("Town Patrol Central",
                          new Vector3(0, 0, 15),
                          new Vector3(40, 0, 15),
                          WaypointType.Guard,
                          townPatrolWaypoints,
                          WaypointPlacementMode.Linear,
                          1);

        CreateWaypointArea("Town Patrol Residential",
                          new Vector3(-25, 0, 15),
                          new Vector3(20, 0, 20),
                          WaypointType.Guard,
                          4,
                          WaypointPlacementMode.Linear,
                          1);

        CreateWaypointArea("Town Patrol Artisan",
                          new Vector3(25, 0, 15),
                          new Vector3(20, 0, 20),
                          WaypointType.Guard,
                          4,
                          WaypointPlacementMode.Linear,
                          1);
    }

    void CreateHouseWaypointAreas()
    {
        // Distribute house waypoints across residential areas
        Vector3[] housePositions = {
            new Vector3(-35, 0, -10), // Residential homes
            new Vector3(-25, 0, -15),
            new Vector3(-40, 0, 5),
            new Vector3(-30, 0, 10),
            new Vector3(-45, 0, -5),
            new Vector3(35, 0, -10), // Artisan homes
            new Vector3(25, 0, -15),
            new Vector3(40, 0, 5),
            new Vector3(-10, 0, 45), // Noble homes
            new Vector3(10, 0, 45),
            new Vector3(-5, 0, 50),
            new Vector3(5, 0, 50),
            new Vector3(-20, 0, 40), // Additional scattered houses
            new Vector3(20, 0, 40),
            new Vector3(0, 0, -35)
        };

        for (int i = 0; i < Mathf.Min(totalHouseWaypoints, housePositions.Length); i++)
        {
            CreateWaypointArea($"House Area {i + 1}",
                              housePositions[i],
                              new Vector3(6, 0, 6),
                              WaypointType.House,
                              2, // 2 waypoints per house (inside/outside)
                              WaypointPlacementMode.Linear,
                              1); // Only 1 citizen per house at a time
        }
    }

    WaypointArea CreateWaypointArea(string areaName, Vector3 center, Vector3 size, WaypointType type, int waypointCount, WaypointPlacementMode placementMode, int maxEntities)
    {
        // Create the area GameObject
        GameObject areaObj = new GameObject($"WaypointArea_{areaName}");
        areaObj.transform.position = center;
        areaObj.transform.parent = transform; // Parent to this setup object

        // Add and configure WaypointArea component
        WaypointArea area = areaObj.AddComponent<WaypointArea>();
        area.areaType = type;
        area.shape = WaypointAreaShape.Box;
        area.center = Vector3.zero; // Center is handled by transform position
        area.size = size;
        area.waypointCount = waypointCount;
        area.placementMode = placementMode;
        area.maxEntitiesPerGroup = maxEntities;
        area.entitySpacing = type == WaypointType.Guard ? 3f : 2f;
        area.patrolPattern = type == WaypointType.Guard ? PatrolPattern.Sequential : PatrolPattern.Distributed;
        area.allowSharedWaypoints = type != WaypointType.House && type != WaypointType.Guard;

        // Set appropriate gizmo color
        switch (type)
        {
            case WaypointType.Guard:
                area.gizmoColor = Color.red;
                break;
            case WaypointType.Peasant:
                area.gizmoColor = Color.green;
                break;
            case WaypointType.Merchant:
                area.gizmoColor = Color.yellow;
                break;
            case WaypointType.Priest:
                area.gizmoColor = Color.cyan;
                break;
            case WaypointType.Noble:
                area.gizmoColor = Color.blue;
                break;
            case WaypointType.Royalty:
                area.gizmoColor = Color.magenta;
                break;
            case WaypointType.House:
                area.gizmoColor = Color.white;
                break;
        }

        Debug.Log($"[WaypointSystemSetup] Created {areaName} area for {type} with {waypointCount} waypoints");
        return area;
    }

    void GenerateAllWaypoints()
    {
        // Find or create WaypointGenerator
        WaypointGenerator generator = FindAnyObjectByType<WaypointGenerator>();
        if (generator == null)
        {
            GameObject generatorObj = new GameObject("WaypointGenerator");
            generatorObj.transform.parent = transform;
            generator = generatorObj.AddComponent<WaypointGenerator>();
        }

        // Configure generator
        generator.waypointPrefab = waypointPrefab;
        generator.waypointGroupPrefab = waypointGroupPrefab;
        generator.randomSeed = randomSeed;
        generator.autoClearBeforeGenerate = false; // We already cleared
        generator.clusterWaypoints = true;
        generator.clusterRadius = 8f;
        generator.clusterSpacing = 2.5f;
        generator.minDistanceBetweenWaypoints = 2f;
        generator.waypointRadius = 0.8f;
        generator.maxPlacementAttempts = 75;
        generator.validateNavMesh = true;
        generator.maxTerrainSlope = 25f;
        generator.terrainHeightOffset = 0.3f;
        generator.debugMode = true;

        // Generate waypoints for all areas
        Debug.Log("[WaypointSystemSetup] Generating waypoints for all areas...");
        generator.GenerateWaypointsInAreas();

        // Validate the results
        ValidateWaypointGeneration();
    }

    void ValidateWaypointGeneration()
    {
        WaypointGroup[] groups = FindObjectsByType<WaypointGroup>(FindObjectsSortMode.None);
        Dictionary<WaypointType, int> typeCounts = new Dictionary<WaypointType, int>();
        int totalWaypoints = 0;

        foreach (WaypointGroup group in groups)
        {
            if (!typeCounts.ContainsKey(group.groupType))
                typeCounts[group.groupType] = 0;

            typeCounts[group.groupType]++;
            totalWaypoints += group.waypoints != null ? group.waypoints.Length : 0;
        }

        Debug.Log($"[WaypointSystemSetup] Validation Results:");
        Debug.Log($"  Total Waypoint Groups: {groups.Length}");
        Debug.Log($"  Total Individual Waypoints: {totalWaypoints}");

        foreach (var kvp in typeCounts)
        {
            Debug.Log($"  {kvp.Key} groups: {kvp.Value}");
        }

        // Check for missing critical types
        var criticalTypes = new WaypointType[] { WaypointType.Guard, WaypointType.Peasant, WaypointType.House };
        foreach (var type in criticalTypes)
        {
            if (!typeCounts.ContainsKey(type) || typeCounts[type] == 0)
            {
                Debug.LogWarning($"[WaypointSystemSetup] Critical waypoint type missing: {type}");
            }
        }
    }

    void ClearAllWaypoints()
    {
        Debug.Log("[WaypointSystemSetup] Clearing existing waypoint areas and groups...");

        // Clear waypoint areas
        WaypointArea[] areas = FindObjectsByType<WaypointArea>(FindObjectsSortMode.None);
        foreach (WaypointArea area in areas)
        {
            if (area.transform.parent == transform) // Only clear our own areas
            {
                DestroyImmediate(area.gameObject);
            }
        }

        // Clear waypoint groups that are children of our areas
        WaypointGroup[] groups = GetComponentsInChildren<WaypointGroup>();
        foreach (WaypointGroup group in groups)
        {
            DestroyImmediate(group.gameObject);
        }

        // Clear individual waypoints that are children of our areas
        Waypoint[] waypoints = GetComponentsInChildren<Waypoint>();
        foreach (Waypoint waypoint in waypoints)
        {
            DestroyImmediate(waypoint.gameObject);
        }
    }

    [ContextMenu("Clear All Waypoints")]
    public void ClearAllWaypointsFromContext()
    {
        ClearAllWaypoints();
    }

    [ContextMenu("Validate Current Waypoints")]
    public void ValidateCurrentWaypoints()
    {
        ValidateWaypointGeneration();
    }

    [ContextMenu("Generate Quick Test Setup")]
    public void GenerateQuickTestSetup()
    {
        // Quick setup for testing
        marketSquareWaypoints = 4;
        residentialWaypoints = 6;
        artisanWaypoints = 3;
        nobleWaypoints = 2;
        castleGroundsWaypoints = 4;
        mainGateGuardWaypoints = 3;
        townPatrolWaypoints = 3;
        totalHouseWaypoints = 5;

        SetupCompleteWaypointSystem();
    }
}