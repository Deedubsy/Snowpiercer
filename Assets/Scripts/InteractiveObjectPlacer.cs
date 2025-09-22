using UnityEngine;
using System.Collections.Generic;

public class InteractiveObjectPlacer : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject doorPrefab;
    public GameObject bellTowerPrefab;
    public GameObject shadowTriggerPrefab;
    public GameObject hidingSpotPrefab;
    public GameObject wardPrefab;
    public GameObject trapPrefabs;

    [Header("Placement Settings")]
    public bool autoPlaceOnStart = false;
    public bool clearExistingObjects = true;
    public LayerMask groundLayerMask = 1;
    public LayerMask obstacleLayerMask = -1;

    [Header("Door Configuration")]
    [Range(5, 50)] public int houseDoorCount = 20;
    [Range(2, 15)] public int castleDoorCount = 8;
    [Range(1, 10)] public int specialDoorCount = 5;

    [Header("Bell Tower Configuration")]
    [Range(1, 5)] public int bellTowerCount = 3;

    [Header("Shadow & Hiding Spots")]
    [Range(10, 50)] public int shadowSpotCount = 25;
    [Range(5, 20)] public int hidingSpotCount = 12;

    [Header("Ward System")]
    [Range(3, 15)] public int wardObjectCount = 8;

    private List<GameObject> placedObjects = new List<GameObject>();

    void Start()
    {
        if (autoPlaceOnStart)
        {
            PlaceAllInteractiveObjects();
        }
    }

    [ContextMenu("Place All Interactive Objects")]
    public void PlaceAllInteractiveObjects()
    {
        Debug.Log("[InteractiveObjectPlacer] Starting placement of all interactive objects...");

        if (clearExistingObjects)
        {
            ClearExistingObjects();
        }

        // Place objects in logical order
        PlaceHouseDoors();
        PlaceCastleDoors();
        PlaceSpecialDoors();
        PlaceBellTowers();
        PlaceShadowTriggers();
        PlaceHidingSpots();
        PlaceWardObjects();

        Debug.Log($"[InteractiveObjectPlacer] Placed {placedObjects.Count} interactive objects total");
        ValidatePlacement();
    }

    void PlaceHouseDoors()
    {
        Debug.Log("[InteractiveObjectPlacer] Placing house doors...");

        // Get all house waypoint groups to place doors at
        WaypointGroup[] houseGroups = FindWaypointGroups(WaypointType.House);

        int doorsPlaced = 0;
        foreach (WaypointGroup houseGroup in houseGroups)
        {
            if (doorsPlaced >= houseDoorCount) break;

            // Place door at the house group center
            Vector3 doorPosition = houseGroup.transform.position;
            doorPosition = AdjustToGround(doorPosition);

            if (IsValidPlacementPosition(doorPosition, 2f))
            {
                GameObject door = CreateDoor(doorPosition, houseGroup, "House Door");
                if (door != null)
                {
                    door.transform.parent = houseGroup.transform;
                    placedObjects.Add(door);
                    doorsPlaced++;
                    Debug.Log($"Placed house door at {doorPosition} for {houseGroup.name}");
                }
            }
        }

        Debug.Log($"[InteractiveObjectPlacer] Placed {doorsPlaced} house doors");
    }

    void PlaceCastleDoors()
    {
        Debug.Log("[InteractiveObjectPlacer] Placing castle doors...");

        // Castle door positions (main entrances, towers, etc.)
        Vector3[] castleDoorPositions = {
            new Vector3(0, 0, 75),    // Main castle entrance
            new Vector3(-10, 0, 70),  // Side entrance
            new Vector3(10, 0, 70),   // Side entrance
            new Vector3(-15, 0, 65),  // Tower entrance
            new Vector3(15, 0, 65),   // Tower entrance
            new Vector3(0, 5, 80),    // Upper level entrance
            new Vector3(-8, 0, 85),   // Rear entrance
            new Vector3(8, 0, 85),    // Rear entrance
        };

        int doorsPlaced = 0;
        for (int i = 0; i < Mathf.Min(castleDoorPositions.Length, castleDoorCount); i++)
        {
            Vector3 position = AdjustToGround(castleDoorPositions[i]);

            if (IsValidPlacementPosition(position, 3f))
            {
                GameObject door = CreateDoor(position, null, $"Castle Door {i + 1}");
                if (door != null)
                {
                    door.transform.parent = transform;
                    placedObjects.Add(door);
                    doorsPlaced++;
                }
            }
        }

        Debug.Log($"[InteractiveObjectPlacer] Placed {doorsPlaced} castle doors");
    }

    void PlaceSpecialDoors()
    {
        Debug.Log("[InteractiveObjectPlacer] Placing special doors...");

        // Special locations (chapel, merchant shops, noble houses)
        Vector3[] specialPositions = {
            new Vector3(-15, 0, 30),  // Chapel
            new Vector3(5, 0, -5),    // Merchant shop
            new Vector3(-25, 0, -15), // Large house
            new Vector3(25, 0, -15),  // Artisan shop
            new Vector3(0, 0, 50),    // Noble residence
            new Vector3(-10, 0, 45),  // Noble residence
            new Vector3(10, 0, 45),   // Noble residence
            new Vector3(30, 0, 15),   // Artisan guild
            new Vector3(-30, 0, 15),  // Residential complex
            new Vector3(0, 0, -35),   // South gate building
        };

        int doorsPlaced = 0;
        for (int i = 0; i < Mathf.Min(specialPositions.Length, specialDoorCount); i++)
        {
            Vector3 position = AdjustToGround(specialPositions[i]);

            if (IsValidPlacementPosition(position, 2.5f))
            {
                GameObject door = CreateDoor(position, null, $"Special Door {i + 1}");
                if (door != null)
                {
                    door.transform.parent = transform;
                    placedObjects.Add(door);
                    doorsPlaced++;
                }
            }
        }

        Debug.Log($"[InteractiveObjectPlacer] Placed {doorsPlaced} special doors");
    }

    void PlaceBellTowers()
    {
        Debug.Log("[InteractiveObjectPlacer] Placing bell towers...");

        // Strategic bell tower positions
        Vector3[] bellPositions = {
            new Vector3(0, 0, 80),    // Castle bell tower
            new Vector3(-20, 0, 25),  // Chapel bell tower
            new Vector3(25, 0, 10),   // Town square bell tower
            new Vector3(0, 0, -20),   // South gate bell tower
            new Vector3(-35, 0, 0),   // West district bell tower
        };

        int bellsPlaced = 0;
        for (int i = 0; i < Mathf.Min(bellPositions.Length, bellTowerCount); i++)
        {
            Vector3 position = AdjustToGround(bellPositions[i]);

            if (IsValidPlacementPosition(position, 5f))
            {
                GameObject bellTower = CreateBellTower(position, $"Bell Tower {i + 1}");
                if (bellTower != null)
                {
                    bellTower.transform.parent = transform;
                    placedObjects.Add(bellTower);
                    bellsPlaced++;
                }
            }
        }

        Debug.Log($"[InteractiveObjectPlacer] Placed {bellsPlaced} bell towers");
    }

    void PlaceShadowTriggers()
    {
        Debug.Log("[InteractiveObjectPlacer] Placing shadow triggers...");

        // Strategic shadow locations for stealth gameplay
        List<Vector3> shadowPositions = GenerateShadowPositions();

        int shadowsPlaced = 0;
        foreach (Vector3 position in shadowPositions)
        {
            if (shadowsPlaced >= shadowSpotCount) break;

            Vector3 adjustedPos = AdjustToGround(position);
            if (IsValidPlacementPosition(adjustedPos, 1.5f))
            {
                GameObject shadowTrigger = CreateShadowTrigger(adjustedPos, $"Shadow Spot {shadowsPlaced + 1}");
                if (shadowTrigger != null)
                {
                    shadowTrigger.transform.parent = transform;
                    placedObjects.Add(shadowTrigger);
                    shadowsPlaced++;
                }
            }
        }

        Debug.Log($"[InteractiveObjectPlacer] Placed {shadowsPlaced} shadow triggers");
    }

    void PlaceHidingSpots()
    {
        Debug.Log("[InteractiveObjectPlacer] Placing hiding spots...");

        // Hiding spots near buildings and cover
        List<Vector3> hidingPositions = GenerateHidingSpotPositions();

        int hidingSpotsPlaced = 0;
        foreach (Vector3 position in hidingPositions)
        {
            if (hidingSpotsPlaced >= hidingSpotCount) break;

            Vector3 adjustedPos = AdjustToGround(position);
            if (IsValidPlacementPosition(adjustedPos, 2f))
            {
                GameObject hidingSpot = CreateHidingSpot(adjustedPos, $"Hiding Spot {hidingSpotsPlaced + 1}");
                if (hidingSpot != null)
                {
                    hidingSpot.transform.parent = transform;
                    placedObjects.Add(hidingSpot);
                    hidingSpotsPlaced++;
                }
            }
        }

        Debug.Log($"[InteractiveObjectPlacer] Placed {hidingSpotsPlaced} hiding spots");
    }

    void PlaceWardObjects()
    {
        Debug.Log("[InteractiveObjectPlacer] Placing ward objects...");

        // Ward objects at important locations
        Vector3[] wardPositions = {
            new Vector3(-15, 0, 30),  // Chapel
            new Vector3(0, 0, 75),    // Castle entrance
            new Vector3(0, 0, 0),     // Market center
            new Vector3(-25, 0, 0),   // Residential center
            new Vector3(25, 0, 0),    // Artisan center
            new Vector3(0, 0, 45),    // Noble quarter
            new Vector3(0, 0, -25),   // South gate
            new Vector3(-40, 0, 0),   // West edge
            new Vector3(40, 0, 0),    // East edge
            new Vector3(0, 0, 90),    // North castle
            new Vector3(-20, 0, 60),  // Castle west
            new Vector3(20, 0, 60),   // Castle east
        };

        int wardsPlaced = 0;
        for (int i = 0; i < Mathf.Min(wardPositions.Length, wardObjectCount); i++)
        {
            Vector3 position = AdjustToGround(wardPositions[i]);

            if (IsValidPlacementPosition(position, 3f))
            {
                GameObject ward = CreateWardObject(position, $"Ward Object {i + 1}");
                if (ward != null)
                {
                    ward.transform.parent = transform;
                    placedObjects.Add(ward);
                    wardsPlaced++;
                }
            }
        }

        Debug.Log($"[InteractiveObjectPlacer] Placed {wardsPlaced} ward objects");
    }

    GameObject CreateDoor(Vector3 position, WaypointGroup houseGroup, string doorName)
    {
        GameObject doorObj;

        if (doorPrefab != null)
        {
            doorObj = Instantiate(doorPrefab, position, Quaternion.identity);
        }
        else
        {
            // Create basic door if no prefab
            doorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorObj.transform.localScale = new Vector3(1f, 2f, 0.1f);
            doorObj.transform.position = position;
        }

        doorObj.name = doorName;

        // Add Door component if it doesn't exist
        Door doorScript = doorObj.GetComponent<Door>();
        if (doorScript == null)
        {
            doorScript = doorObj.AddComponent<Door>();
        }

        // Configure door
        doorScript.displayName = doorName;
        doorScript.interactionRange = 2f;
        doorScript.openAngle = 90f;
        doorScript.openSpeed = 3f;
        doorScript.houseGroup = houseGroup;

        // Add collider if needed
        if (doorObj.GetComponent<Collider>() == null)
        {
            doorObj.AddComponent<BoxCollider>();
        }

        return doorObj;
    }

    GameObject CreateBellTower(Vector3 position, string towerName)
    {
        GameObject towerObj;

        if (bellTowerPrefab != null)
        {
            towerObj = Instantiate(bellTowerPrefab, position, Quaternion.identity);
        }
        else
        {
            // Create basic bell tower if no prefab
            towerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            towerObj.transform.localScale = new Vector3(2f, 5f, 2f);
            towerObj.transform.position = position + Vector3.up * 2.5f;
        }

        towerObj.name = towerName;

        // Add BellTower component if it doesn't exist
        BellTower bellScript = towerObj.GetComponent<BellTower>();
        if (bellScript == null)
        {
            bellScript = towerObj.AddComponent<BellTower>();
        }

        // Configure bell tower
        bellScript.displayName = towerName;
        bellScript.interactionRange = 3f;

        return towerObj;
    }

    GameObject CreateShadowTrigger(Vector3 position, string shadowName)
    {
        GameObject shadowObj;

        if (shadowTriggerPrefab != null)
        {
            shadowObj = Instantiate(shadowTriggerPrefab, position, Quaternion.identity);
        }
        else
        {
            // Create basic shadow trigger
            shadowObj = new GameObject(shadowName);
            shadowObj.transform.position = position;
            BoxCollider trigger = shadowObj.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3f, 2f, 3f);
        }

        shadowObj.name = shadowName;

        // Add ShadowTrigger component if it doesn't exist
        ShadowTrigger shadowScript = shadowObj.GetComponent<ShadowTrigger>();
        if (shadowScript == null)
        {
            shadowScript = shadowObj.AddComponent<ShadowTrigger>();
        }

        // Configure shadow trigger
        shadowScript.isActive = true;
        shadowScript.requiresCrouching = true;
        shadowScript.shadowIntensity = 0.8f;
        shadowScript.affectsPlayerVisibility = true;
        shadowScript.affectsEnemyDetection = true;

        return shadowObj;
    }

    GameObject CreateHidingSpot(Vector3 position, string hidingName)
    {
        GameObject hidingObj;

        if (hidingSpotPrefab != null)
        {
            hidingObj = Instantiate(hidingSpotPrefab, position, Quaternion.identity);
        }
        else
        {
            // Create basic hiding spot (bush/barrel/crate)
            hidingObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hidingObj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            hidingObj.transform.position = position + Vector3.up * 0.75f;
        }

        hidingObj.name = hidingName;

        // Add shadow trigger for hiding functionality
        ShadowTrigger shadowScript = hidingObj.GetComponent<ShadowTrigger>();
        if (shadowScript == null)
        {
            shadowScript = hidingObj.AddComponent<ShadowTrigger>();
            BoxCollider trigger = hidingObj.GetComponent<BoxCollider>();
            if (trigger == null) trigger = hidingObj.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
        }

        shadowScript.isActive = true;
        shadowScript.requiresCrouching = false;
        shadowScript.shadowIntensity = 1f;
        shadowScript.isPermanentShadow = true;

        return hidingObj;
    }

    GameObject CreateWardObject(Vector3 position, string wardName)
    {
        GameObject wardObj;

        if (wardPrefab != null)
        {
            wardObj = Instantiate(wardPrefab, position, Quaternion.identity);
        }
        else
        {
            // Create basic ward object (cross/statue/shrine)
            wardObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            wardObj.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
            wardObj.transform.position = position + Vector3.up * 0.75f;
        }

        wardObj.name = wardName;

        // Add WardSystem component if it doesn't exist
        WardSystem wardScript = wardObj.GetComponent<WardSystem>();
        if (wardScript == null)
        {
            wardScript = wardObj.AddComponent<WardSystem>();
        }

        // Configure ward through the Ward component instead of WardSystem
        Ward wardComponent = wardObj.GetComponent<Ward>();
        if (wardComponent == null)
        {
            wardComponent = wardObj.AddComponent<Ward>();
        }

        return wardObj;
    }

    List<Vector3> GenerateShadowPositions()
    {
        List<Vector3> positions = new List<Vector3>();

        // Alleyway shadows
        for (int i = -40; i <= 40; i += 10)
        {
            for (int j = -30; j <= 30; j += 15)
            {
                if (Random.value > 0.6f) // 40% chance to place
                {
                    positions.Add(new Vector3(i + Random.Range(-3f, 3f), 0, j + Random.Range(-3f, 3f)));
                }
            }
        }

        // Building corners and walls
        Vector3[] buildingCorners = {
            new Vector3(-35, 0, -10), new Vector3(-25, 0, -20), new Vector3(-30, 0, 10),
            new Vector3(35, 0, -10), new Vector3(25, 0, -20), new Vector3(30, 0, 10),
            new Vector3(-15, 0, 40), new Vector3(15, 0, 40), new Vector3(0, 0, 35),
            new Vector3(-20, 0, 65), new Vector3(20, 0, 65), new Vector3(-10, 0, 75),
        };

        positions.AddRange(buildingCorners);

        return positions;
    }

    List<Vector3> GenerateHidingSpotPositions()
    {
        List<Vector3> positions = new List<Vector3>();

        // Near waypoint groups for strategic hiding
        WaypointGroup[] allGroups = FindObjectsByType<WaypointGroup>(FindObjectsSortMode.None);
        foreach (WaypointGroup group in allGroups)
        {
            if (group.groupType != WaypointType.House && Random.value > 0.5f)
            {
                Vector3 offset = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
                positions.Add(group.transform.position + offset);
            }
        }

        // Manual strategic positions
        Vector3[] strategicPositions = {
            new Vector3(-2, 0, 15), new Vector3(2, 0, 15), new Vector3(-8, 0, 25),
            new Vector3(8, 0, 25), new Vector3(-12, 0, 55), new Vector3(12, 0, 55),
            new Vector3(-5, 0, -10), new Vector3(5, 0, -10), new Vector3(-20, 0, 5),
            new Vector3(20, 0, 5), new Vector3(-30, 0, -5), new Vector3(30, 0, -5),
        };

        positions.AddRange(strategicPositions);

        return positions;
    }

    WaypointGroup[] FindWaypointGroups(WaypointType type)
    {
        WaypointGroup[] allGroups = FindObjectsByType<WaypointGroup>(FindObjectsSortMode.None);
        List<WaypointGroup> matchingGroups = new List<WaypointGroup>();

        foreach (WaypointGroup group in allGroups)
        {
            if (group.groupType == type)
            {
                matchingGroups.Add(group);
            }
        }

        return matchingGroups.ToArray();
    }

    Vector3 AdjustToGround(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit, 200f, groundLayerMask))
        {
            return hit.point;
        }
        return position;
    }

    bool IsValidPlacementPosition(Vector3 position, float checkRadius)
    {
        // Check for obstacles
        if (Physics.CheckSphere(position + Vector3.up * 0.5f, checkRadius, obstacleLayerMask))
        {
            return false;
        }

        // Check minimum distance from other placed objects
        foreach (GameObject obj in placedObjects)
        {
            if (obj != null && Vector3.Distance(position, obj.transform.position) < checkRadius * 2f)
            {
                return false;
            }
        }

        return true;
    }

    void ClearExistingObjects()
    {
        // Clear previously placed objects
        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        placedObjects.Clear();

        Debug.Log("[InteractiveObjectPlacer] Cleared existing interactive objects");
    }

    void ValidatePlacement()
    {
        Dictionary<string, int> objectCounts = new Dictionary<string, int>();

        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
            {
                string type = obj.GetComponent<Door>() != null ? "Doors" :
                             obj.GetComponent<BellTower>() != null ? "Bell Towers" :
                             obj.GetComponent<ShadowTrigger>() != null ? "Shadow Triggers" :
                             obj.GetComponent<WardSystem>() != null ? "Ward Objects" : "Other";

                if (!objectCounts.ContainsKey(type)) objectCounts[type] = 0;
                objectCounts[type]++;
            }
        }

        Debug.Log("[InteractiveObjectPlacer] Placement Summary:");
        foreach (var kvp in objectCounts)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value}");
        }
    }

    [ContextMenu("Clear All Interactive Objects")]
    public void ClearAllInteractiveObjects()
    {
        ClearExistingObjects();
    }

    [ContextMenu("Place Quick Test Setup")]
    public void PlaceQuickTestSetup()
    {
        // Reduced counts for testing
        houseDoorCount = 10;
        castleDoorCount = 4;
        specialDoorCount = 3;
        bellTowerCount = 2;
        shadowSpotCount = 15;
        hidingSpotCount = 8;
        wardObjectCount = 5;

        PlaceAllInteractiveObjects();
    }
}