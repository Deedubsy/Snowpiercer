using System.Collections.Generic;
using UnityEngine;

public class AITestSceneController : MonoBehaviour
{
    [Header("Test Scene Setup")]
    public GameObject guardPrefab;
    public GameObject citizenPrefab;
    public GameObject playerPrefab;
    public GameObject waypointPrefab;

    [Header("Spawn Settings")]
    public Vector3 guardSpawnPosition = new Vector3(0, 0, 10);
    public Vector3 citizenSpawnPosition = new Vector3(0, 0, -10);
    public Vector3 playerSpawnPosition = Vector3.zero;

    [Header("Random Positioning")]
    public bool randomizeGroupPositions = true;
    public float minGroupDistance = 30f;
    public Vector2 spawnAreaSize = new Vector2(50f, 50f);

    [Header("Waypoint Generation")]
    public int numberOfWaypoints = 4;
    public float waypointRadius = 15f;
    public float waypointHeight = 0.1f;
    public float waypointSpacing = 3f;

    [Header("Test Controls")]
    public KeyCode resetSceneKey = KeyCode.R;
    public KeyCode togglePlayerMovementKey = KeyCode.T;
    public KeyCode teleportPlayerKey = KeyCode.Space;
    public KeyCode toggleRandomGroupsKey = KeyCode.G;

    [Header("Debug Settings")]
    public bool enableDebugUI = true;
    public bool showWaypointConnections = true;
    public bool autoSetupManagers = true;

    private GameObject spawnedGuard;
    private GameObject spawnedCitizen;
    private GameObject spawnedPlayer;
    private List<GameObject> spawnedWaypoints = new List<GameObject>();
    private WaypointGroup guardWaypointGroup;
    private WaypointGroup citizenWaypointGroup;

    private bool playerMovementEnabled = true;
    private CharacterController playerController;

    void Start()
    {
        SetupTestScene();
    }

    void Update()
    {
        HandleInput();

        if (playerMovementEnabled && spawnedPlayer != null)
        {
            //HandlePlayerMovement();
        }
    }

    void SetupTestScene()
    {
        Debug.Log("[AITestSceneController] Setting up test scene...");

        // Setup managers first
        if (autoSetupManagers)
        {
            SetupManagers();
        }

        // Clear existing objects
        ClearScene();

        // Create waypoints
        CreateWaypoints();

        // Spawn entities
        SpawnGuard();
        SpawnCitizen();
        SpawnPlayer();

        // Setup debug UI
        if (enableDebugUI)
        {
            SetupDebugUI();
        }

        Debug.Log("[AITestSceneController] Test scene setup complete!");
    }

    void SetupManagers()
    {
        // Create DebugUIManager if it doesn't exist
        if (DebugUIManager.Instance == null)
        {
            GameObject debugUIManager = new GameObject("DebugUIManager");
            debugUIManager.AddComponent<DebugUIManager>();
        }

        // Create CitizenManager if it doesn't exist
        if (CitizenManager.Instance == null)
        {
            GameObject citizenManager = new GameObject("CitizenManager");
            citizenManager.AddComponent<CitizenManager>();
        }

        // Create SpatialGrid if it doesn't exist
        if (SpatialGrid.Instance == null)
        {
            GameObject spatialGrid = new GameObject("SpatialGrid");
            SpatialGrid grid = spatialGrid.AddComponent<SpatialGrid>();
            grid.cellSize = 5f; // Smaller cells for test scene
            grid.gridWidth = 20;
            grid.gridHeight = 20;
            grid.showGrid = true;
            grid.showEntities = true;
        }

        // Create GameManager if it doesn't exist
        if (GameManager.instance == null)
        {
            GameObject gameManager = new GameObject("GameManager");
            gameManager.AddComponent<GameManager>();
        }
    }

    void ClearScene()
    {
        if (spawnedGuard != null) DestroyImmediate(spawnedGuard);
        if (spawnedCitizen != null) DestroyImmediate(spawnedCitizen);
        if (spawnedPlayer != null) DestroyImmediate(spawnedPlayer);

        foreach (var waypoint in spawnedWaypoints)
        {
            if (waypoint != null) DestroyImmediate(waypoint);
        }
        spawnedWaypoints.Clear();
    }

    void CreateWaypoints()
    {
        // Determine group center positions
        Vector3 guardGroupCenter = guardSpawnPosition;
        Vector3 citizenGroupCenter = citizenSpawnPosition;

        if (randomizeGroupPositions)
        {
            // Generate random positions that are far enough apart
            guardGroupCenter = GetRandomPositionInArea();

            // Ensure citizen group is far enough from guard group
            int attempts = 0;
            do
            {
                citizenGroupCenter = GetRandomPositionInArea();
                attempts++;
            } while (Vector3.Distance(guardGroupCenter, citizenGroupCenter) < minGroupDistance && attempts < 20);

            Debug.Log($"[AITestSceneController] Guard group at {guardGroupCenter}, Citizen group at {citizenGroupCenter}, Distance: {Vector3.Distance(guardGroupCenter, citizenGroupCenter):F1}m");
        }

        // Update spawn positions for entities
        guardSpawnPosition = guardGroupCenter;
        citizenSpawnPosition = citizenGroupCenter;

        // Create waypoint parent objects at group centers
        GameObject guardWaypointsParent = new GameObject("GuardWaypoints");
        GameObject citizenWaypointsParent = new GameObject("CitizenWaypoints");

        guardWaypointsParent.transform.position = guardGroupCenter;
        citizenWaypointsParent.transform.position = citizenGroupCenter;

        guardWaypointGroup = guardWaypointsParent.AddComponent<WaypointGroup>();
        citizenWaypointGroup = citizenWaypointsParent.AddComponent<WaypointGroup>();

        List<Waypoint> guardWaypoints = new List<Waypoint>();
        List<Waypoint> citizenWaypoints = new List<Waypoint>();

        // Create clustered waypoint pattern for guard
        Vector3[] guardPositions = GenerateClusteredWaypoints(guardGroupCenter, numberOfWaypoints, waypointRadius, waypointSpacing);
        for (int i = 0; i < guardPositions.Length; i++)
        {
            GameObject waypointObj = CreateWaypoint(guardPositions[i], $"GuardWaypoint_{i}");
            waypointObj.transform.SetParent(guardWaypointsParent.transform);
            Waypoint waypoint = waypointObj.GetComponent<Waypoint>();
            guardWaypoints.Add(waypoint);
            spawnedWaypoints.Add(waypointObj);
        }

        // Create clustered waypoint pattern for citizen
        Vector3[] citizenPositions = GenerateClusteredWaypoints(citizenGroupCenter, numberOfWaypoints, waypointRadius * 0.7f, waypointSpacing);
        for (int i = 0; i < citizenPositions.Length; i++)
        {
            GameObject waypointObj = CreateWaypoint(citizenPositions[i], $"CitizenWaypoint_{i}");
            waypointObj.transform.SetParent(citizenWaypointsParent.transform);
            Waypoint waypoint = waypointObj.GetComponent<Waypoint>();
            citizenWaypoints.Add(waypoint);
            spawnedWaypoints.Add(waypointObj);
        }

        // Assign waypoints to groups
        guardWaypointGroup.waypoints = guardWaypoints.ToArray();
        citizenWaypointGroup.waypoints = citizenWaypoints.ToArray();

        Debug.Log($"[AITestSceneController] Created {spawnedWaypoints.Count} waypoints in 2 groups");
    }

    GameObject CreateWaypoint(Vector3 position, string name)
    {
        GameObject waypointObj;

        if (waypointPrefab != null)
        {
            waypointObj = Instantiate(waypointPrefab, position, Quaternion.identity);
            waypointObj.name = name;
        }
        else
        {
            waypointObj = new GameObject(name);
            waypointObj.transform.position = position;

            // Add Waypoint component
            Waypoint waypoint = waypointObj.AddComponent<Waypoint>();
            waypoint.minWaitTime = 2f;
            waypoint.maxWaitTime = 5f;

            // Add visual representation
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.transform.SetParent(waypointObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(1f, 0.1f, 1f);

            // Remove collider from visual
            Collider collider = visual.GetComponent<Collider>();
            if (collider != null) DestroyImmediate(collider);

            // Set color
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = name.Contains("Guard") ? Color.red : Color.blue;
                renderer.material = mat;
            }
        }

        return waypointObj;
    }

    Vector3 GetRandomPositionInArea()
    {
        float halfWidth = spawnAreaSize.x * 0.5f;
        float halfHeight = spawnAreaSize.y * 0.5f;

        float x = Random.Range(-halfWidth, halfWidth);
        float z = Random.Range(-halfHeight, halfHeight);

        return new Vector3(x, waypointHeight, z);
    }

    Vector3[] GenerateClusteredWaypoints(Vector3 center, int count, float maxRadius, float spacing)
    {
        Vector3[] positions = new Vector3[count];

        if (count == 1)
        {
            positions[0] = center;
            return positions;
        }

        // For small numbers, use circular arrangement
        if (count <= 6)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * 2f * Mathf.PI;
                float radius = Mathf.Min(maxRadius, spacing * count / (2f * Mathf.PI));

                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );

                positions[i] = center + offset;
                positions[i].y = waypointHeight;
            }
        }
        else
        {
            // For larger numbers, use spiral pattern
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float angle = t * 4f * Mathf.PI; // Two full spirals
                float radius = t * maxRadius;

                // Add some randomness to avoid perfect spiral
                angle += Random.Range(-0.3f, 0.3f);
                radius += Random.Range(-spacing * 0.3f, spacing * 0.3f);
                radius = Mathf.Clamp(radius, 0, maxRadius);

                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );

                positions[i] = center + offset;
                positions[i].y = waypointHeight;
            }
        }

        // Ensure minimum spacing between waypoints
        for (int i = 0; i < positions.Length; i++)
        {
            for (int j = i + 1; j < positions.Length; j++)
            {
                Vector3 direction = positions[j] - positions[i];
                float distance = direction.magnitude;

                if (distance < spacing && distance > 0)
                {
                    // Push waypoints apart
                    direction = direction.normalized;
                    float pushDistance = (spacing - distance) * 0.5f;

                    positions[i] -= direction * pushDistance;
                    positions[j] += direction * pushDistance;
                }
            }
        }

        return positions;
    }

    void SpawnGuard()
    {
        if (guardPrefab != null)
        {
            spawnedGuard = Instantiate(guardPrefab, guardSpawnPosition, Quaternion.identity);
            spawnedGuard.name = "TestGuard";

            // Assign waypoint group
            GuardAI guardAI = spawnedGuard.GetComponent<GuardAI>();
            if (guardAI != null)
            {
                guardAI.assignedWaypointGroup = guardWaypointGroup;
                if (guardWaypointGroup.waypoints.Length > 0)
                {
                    guardAI.patrolPoints = guardWaypointGroup.waypoints;
                }
            }
        }
        else
        {
            Debug.LogWarning("[AITestSceneController] No guard prefab assigned!");
        }
    }

    void SpawnCitizen()
    {
        if (citizenPrefab != null)
        {
            spawnedCitizen = Instantiate(citizenPrefab, citizenSpawnPosition, Quaternion.identity);
            spawnedCitizen.name = "TestCitizen";

            // Assign waypoint group
            Citizen citizen = spawnedCitizen.GetComponent<Citizen>();
            if (citizen != null)
            {
                citizen.assignedWaypointGroup = citizenWaypointGroup;
            }
        }
        else
        {
            Debug.LogWarning("[AITestSceneController] No citizen prefab assigned!");
        }
    }

    void SpawnPlayer()
    {
        if (playerPrefab != null)
        {
            spawnedPlayer = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
            spawnedPlayer.name = "TestPlayer";
        }
        else
        {
            // Create simple player if no prefab
            spawnedPlayer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            spawnedPlayer.name = "TestPlayer";
            spawnedPlayer.tag = "Player";
            spawnedPlayer.transform.position = playerSpawnPosition;

            // Add character controller for movement
            playerController = spawnedPlayer.AddComponent<CharacterController>();

            // Add VampireStats if needed
            VampireStats vampireStats = spawnedPlayer.GetComponent<VampireStats>();
            if (vampireStats == null)
            {
                vampireStats = spawnedPlayer.AddComponent<VampireStats>();
            }
        }

        if (playerController == null)
        {
            playerController = spawnedPlayer.GetComponent<CharacterController>();
        }
    }

    void SetupDebugUI()
    {
        // Add debug components to AI entities
        if (spawnedGuard != null)
        {
            if (spawnedGuard.GetComponent<GuardAIDebugProvider>() == null)
                spawnedGuard.AddComponent<GuardAIDebugProvider>();

            if (spawnedGuard.GetComponent<AIDebugUI>() == null)
                spawnedGuard.AddComponent<AIDebugUI>();
        }

        if (spawnedCitizen != null)
        {
            if (spawnedCitizen.GetComponent<CitizenDebugProvider>() == null)
                spawnedCitizen.AddComponent<CitizenDebugProvider>();

            if (spawnedCitizen.GetComponent<AIDebugUI>() == null)
                spawnedCitizen.AddComponent<AIDebugUI>();
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(resetSceneKey))
        {
            SetupTestScene();
        }

        if (Input.GetKeyDown(togglePlayerMovementKey))
        {
            playerMovementEnabled = !playerMovementEnabled;
            Debug.Log($"[AITestSceneController] Player movement: {(playerMovementEnabled ? "Enabled" : "Disabled")}");
        }

        if (Input.GetKeyDown(teleportPlayerKey) && spawnedPlayer != null)
        {
            // Teleport player to a random position
            Vector3 randomPos = new Vector3(
                Random.Range(-20f, 20f),
                playerSpawnPosition.y,
                Random.Range(-20f, 20f)
            );
            spawnedPlayer.transform.position = randomPos;
            Debug.Log($"[AITestSceneController] Teleported player to {randomPos}");
        }

        if (Input.GetKeyDown(toggleRandomGroupsKey))
        {
            randomizeGroupPositions = !randomizeGroupPositions;
            Debug.Log($"[AITestSceneController] Random group positioning: {(randomizeGroupPositions ? "Enabled" : "Disabled")}");
        }
    }

    void HandlePlayerMovement()
    {
        if (spawnedPlayer == null) return;

        Vector3 movement = Vector3.zero;

        // WASD movement
        if (Input.GetKey(KeyCode.W)) movement += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) movement += Vector3.back;
        if (Input.GetKey(KeyCode.A)) movement += Vector3.left;
        if (Input.GetKey(KeyCode.D)) movement += Vector3.right;

        // Apply movement
        if (movement != Vector3.zero)
        {
            float speed = Input.GetKey(KeyCode.LeftShift) ? 8f : 4f; // Sprint with shift
            movement = movement.normalized * speed * Time.deltaTime;

            if (playerController != null)
            {
                playerController.Move(movement);
            }
            else
            {
                spawnedPlayer.transform.position += movement;
            }
        }

        // Mouse look (simple)
        if (Input.GetMouseButton(1)) // Right click to look
        {
            float mouseX = Input.GetAxis("Mouse X") * 2f;
            spawnedPlayer.transform.Rotate(0, mouseX, 0);
        }
    }

    void OnDrawGizmos()
    {
        if (!showWaypointConnections) return;

        // Draw waypoint connections
        if (guardWaypointGroup != null && guardWaypointGroup.waypoints != null)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < guardWaypointGroup.waypoints.Length; i++)
            {
                if (guardWaypointGroup.waypoints[i] != null)
                {
                    int nextIndex = (i + 1) % guardWaypointGroup.waypoints.Length;
                    if (guardWaypointGroup.waypoints[nextIndex] != null)
                    {
                        Gizmos.DrawLine(
                            guardWaypointGroup.waypoints[i].transform.position,
                            guardWaypointGroup.waypoints[nextIndex].transform.position
                        );
                    }
                }
            }
        }

        if (citizenWaypointGroup != null && citizenWaypointGroup.waypoints != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < citizenWaypointGroup.waypoints.Length; i++)
            {
                if (citizenWaypointGroup.waypoints[i] != null)
                {
                    int nextIndex = (i + 1) % citizenWaypointGroup.waypoints.Length;
                    if (citizenWaypointGroup.waypoints[nextIndex] != null)
                    {
                        Gizmos.DrawLine(
                            citizenWaypointGroup.waypoints[i].transform.position,
                            citizenWaypointGroup.waypoints[nextIndex].transform.position
                        );
                    }
                }
            }
        }
    }

    void OnGUI()
    {
        // Show controls
        GUI.Box(new Rect(10, 10, 350, 140),
            $"AI Test Scene Controls:\n" +
            $"{resetSceneKey}: Reset Scene\n" +
            $"{togglePlayerMovementKey}: Toggle Player Movement ({(playerMovementEnabled ? "ON" : "OFF")})\n" +
            $"{teleportPlayerKey}: Teleport Player\n" +
            $"WASD: Move Player\n" +
            $"Shift: Sprint\n" +
            $"Right Click: Look Around\n" +
            $"{toggleRandomGroupsKey}: Toggle Random Groups ({(randomizeGroupPositions ? "ON" : "OFF")}))");
    }
}