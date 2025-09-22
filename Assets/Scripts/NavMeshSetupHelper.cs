using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AI;
#endif

/// <summary>
/// SP-013: NavMesh Setup Helper
/// Automates NavMesh setup for the complete GamePlay scene
/// </summary>
public class NavMeshSetupHelper : MonoBehaviour
{
    [Header("NavMesh Configuration")]
    [Tooltip("Agent radius - smaller = more precise paths")]
    public float agentRadius = 0.5f;
    [Tooltip("Agent height - must accommodate all NPCs")]
    public float agentHeight = 2.0f;
    [Tooltip("Max slope NPCs can walk on")]
    public float maxSlope = 45f;
    [Tooltip("Step height NPCs can climb")]
    public float stepHeight = 0.4f;

    [Header("Area Settings")]
    public bool autoMarkNavigationStatic = true;
    public bool createMultipleAreas = true;

    [Header("Validation")]
    public bool validateOnBake = true;
    public GameObject testNPCPrefab;

    [Header("Results")]
    [SerializeField] private bool navMeshValid = false;
    [SerializeField] private float navMeshArea = 0f;
    [SerializeField] private int navMeshTriangles = 0;

    void Start()
    {
        // Display current NavMesh status
        ValidateCurrentNavMesh();
    }

    [ContextMenu("Setup Complete NavMesh")]
    public void SetupCompleteNavMesh()
    {
        Debug.Log("=== SP-013: NavMesh Setup Starting ===");

#if UNITY_EDITOR
        // Step 1: Mark surfaces as Navigation Static
        if (autoMarkNavigationStatic)
        {
            MarkNavigationStatic();
        }

        // Step 2: Configure NavMesh settings
        ConfigureNavMeshSettings();

        // Step 3: Create multiple areas if needed
        if (createMultipleAreas)
        {
            SetupNavMeshAreas();
        }

        // Step 4: Bake NavMesh
        BakeNavMesh();

        // Step 5: Validate result
        if (validateOnBake)
        {
            ValidateNavMeshSetup();
        }

        Debug.Log("✅ SP-013 NavMesh setup complete!");
#else
        Debug.LogWarning("NavMesh setup only available in Unity Editor");
#endif
    }

#if UNITY_EDITOR
    void MarkNavigationStatic()
    {
        Debug.Log("--- Marking surfaces as Navigation Static ---");

        int markedCount = 0;

        // Find all renderers in scene (potential walkable surfaces)
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();

        foreach (Renderer renderer in allRenderers)
        {
            GameObject obj = renderer.gameObject;

            // Skip if already marked
            if (GameObjectUtility.GetStaticEditorFlags(obj).HasFlag(StaticEditorFlags.NavigationStatic))
                continue;

            // Mark terrain
            if (obj.GetComponent<Terrain>() != null)
            {
                GameObjectUtility.SetStaticEditorFlags(obj,
                    GameObjectUtility.GetStaticEditorFlags(obj) | StaticEditorFlags.NavigationStatic);
                markedCount++;
                Debug.Log($"✅ Marked terrain '{obj.name}' as Navigation Static");
                continue;
            }

            // Mark ground-level objects (likely walkable)
            if (IsLikelyWalkableSurface(obj))
            {
                GameObjectUtility.SetStaticEditorFlags(obj,
                    GameObjectUtility.GetStaticEditorFlags(obj) | StaticEditorFlags.NavigationStatic);
                markedCount++;
                Debug.Log($"✅ Marked '{obj.name}' as Navigation Static");
            }
        }

        Debug.Log($"✅ Marked {markedCount} objects as Navigation Static");
    }

    bool IsLikelyWalkableSurface(GameObject obj)
    {
        // Heuristics for identifying walkable surfaces
        string name = obj.name.ToLower();

        // Likely walkable
        if (name.Contains("ground") || name.Contains("floor") || name.Contains("plaza") ||
            name.Contains("street") || name.Contains("path") || name.Contains("courtyard"))
        {
            return true;
        }

        // Check position (ground-level objects)
        if (obj.transform.position.y < 5f && obj.transform.localScale.y < 2f)
        {
            return true;
        }

        // Buildings and walls should NOT be walkable
        if (name.Contains("wall") || name.Contains("building") || name.Contains("tower"))
        {
            return false;
        }

        return false;
    }

    void ConfigureNavMeshSettings()
    {
        Debug.Log("--- Configuring NavMesh settings ---");

#if UNITY_EDITOR
        // Get current NavMesh build settings
        NavMeshBuildSettings buildSettings = NavMesh.GetSettingsByID(0);

        // Configure agent settings
        buildSettings.agentRadius = agentRadius;
        buildSettings.agentHeight = agentHeight;
        buildSettings.agentSlope = maxSlope;
        buildSettings.agentClimb = stepHeight;

        // Advanced settings for better pathfinding
        buildSettings.minRegionArea = 2f; // Minimum area for a region
        buildSettings.overrideVoxelSize = true;
        buildSettings.voxelSize = agentRadius / 3f; // Higher resolution

        Debug.Log($"✅ NavMesh settings configured:");
        Debug.Log($"   Agent Radius: {agentRadius}");
        Debug.Log($"   Agent Height: {agentHeight}");
        Debug.Log($"   Max Slope: {maxSlope}°");
        Debug.Log($"   Step Height: {stepHeight}");
#else
        Debug.Log("NavMesh settings configuration only available in Unity Editor");
#endif
    }

    void SetupNavMeshAreas()
    {
        Debug.Log("--- Setting up NavMesh areas ---");

        // Define area types
        string[] areaNames = { "Walkable", "Guard Only", "Citizen Only", "Player Only", "Restricted" };

        Debug.Log("📋 NavMesh Areas to configure manually:");
        for (int i = 0; i < areaNames.Length; i++)
        {
            Debug.Log($"   Area {i}: {areaNames[i]}");
        }

        Debug.Log("⚠️ Manual setup required in Navigation → Areas");
    }

    void BakeNavMesh()
    {
        Debug.Log("--- Baking NavMesh ---");

#if UNITY_EDITOR
        // Clear existing NavMesh
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();

        // Bake new NavMesh
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

        Debug.Log("✅ NavMesh baking complete");
#else
        Debug.Log("NavMesh baking only available in Unity Editor");
#endif
    }

    void ValidateNavMeshSetup()
    {
        Debug.Log("--- Validating NavMesh setup ---");

        // Get NavMesh statistics
        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();
        navMeshTriangles = navMeshData.vertices.Length / 3;

        // Calculate total area
        navMeshArea = 0f;
        for (int i = 0; i < navMeshData.indices.Length; i += 3)
        {
            Vector3 v1 = navMeshData.vertices[navMeshData.indices[i]];
            Vector3 v2 = navMeshData.vertices[navMeshData.indices[i + 1]];
            Vector3 v3 = navMeshData.vertices[navMeshData.indices[i + 2]];
            navMeshArea += Vector3.Cross(v2 - v1, v3 - v1).magnitude * 0.5f;
        }

        Debug.Log($"📊 NavMesh Statistics:");
        Debug.Log($"   Triangles: {navMeshTriangles:N0}");
        Debug.Log($"   Total Area: {navMeshArea:F1} m²");

        // Validation checks
        bool hasNavMesh = navMeshTriangles > 0;
        bool reasonableSize = navMeshArea > 1000f; // At least 1000 m² for our scene
        bool canFindPath = TestNavMeshConnectivity();

        navMeshValid = hasNavMesh && reasonableSize && canFindPath;

        if (navMeshValid)
        {
            Debug.Log("✅ NavMesh validation PASSED");
        }
        else
        {
            Debug.LogWarning("⚠️ NavMesh validation issues detected");
            if (!hasNavMesh) Debug.LogWarning("   - No NavMesh found");
            if (!reasonableSize) Debug.LogWarning("   - NavMesh area too small");
            if (!canFindPath) Debug.LogWarning("   - Connectivity issues detected");
        }
    }

    bool TestNavMeshConnectivity()
    {
        Debug.Log("--- Testing NavMesh connectivity ---");

        // Define test points (castle and town)
        Vector3 castlePoint = new Vector3(200, 0, 150);
        Vector3 townPoint = new Vector3(400, 0, 300);

        // Sample points on NavMesh
        NavMeshHit castleHit, townHit;
        bool castleValid = NavMesh.SamplePosition(castlePoint, out castleHit, 10f, NavMesh.AllAreas);
        bool townValid = NavMesh.SamplePosition(townPoint, out townHit, 10f, NavMesh.AllAreas);

        if (!castleValid)
        {
            Debug.LogWarning("❌ Castle area not accessible on NavMesh");
            return false;
        }

        if (!townValid)
        {
            Debug.LogWarning("❌ Town area not accessible on NavMesh");
            return false;
        }

        // Test path between castle and town
        NavMeshPath path = new NavMeshPath();
        bool pathExists = NavMesh.CalculatePath(castleHit.position, townHit.position, NavMesh.AllAreas, path);

        if (pathExists && path.status == NavMeshPathStatus.PathComplete)
        {
            Debug.Log("✅ Castle ↔ Town connectivity verified");
            Debug.Log($"   Path length: {CalculatePathLength(path):F1} m");
            return true;
        }
        else
        {
            Debug.LogWarning("❌ No valid path between castle and town");
            return false;
        }
    }

    float CalculatePathLength(NavMeshPath path)
    {
        float length = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        return length;
    }
#endif

    [ContextMenu("Validate Current NavMesh")]
    public void ValidateCurrentNavMesh()
    {
        Debug.Log("=== Current NavMesh Status ===");

        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();
        int triangleCount = navMeshData.vertices.Length / 3;

        if (triangleCount > 0)
        {
            Debug.Log($"✅ NavMesh present: {triangleCount:N0} triangles");

            // Test basic functionality
            Vector3 testPoint = transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(testPoint, out hit, 5f, NavMesh.AllAreas))
            {
                Debug.Log($"✅ NavMesh functional at current position");
            }
            else
            {
                Debug.LogWarning($"⚠️ NavMesh not accessible at current position");
            }
        }
        else
        {
            Debug.LogWarning("❌ No NavMesh found - run 'Setup Complete NavMesh'");
        }
    }

    [ContextMenu("Test NPC Navigation")]
    public void TestNPCNavigation()
    {
        if (testNPCPrefab == null)
        {
            Debug.LogWarning("⚠️ No test NPC prefab assigned");
            return;
        }

        Debug.Log("--- Testing NPC navigation ---");

        // Spawn test NPC
        Vector3 spawnPos = transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPos, out hit, 5f, NavMesh.AllAreas))
        {
            GameObject testNPC = Instantiate(testNPCPrefab, hit.position, Quaternion.identity);
            testNPC.name = "NavMesh Test NPC";

            // Add simple movement script for testing
            NavMeshAgent agent = testNPC.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = testNPC.AddComponent<NavMeshAgent>();
            }

            // Set a destination
            Vector3 destination = hit.position + new Vector3(10, 0, 10);
            NavMeshHit destHit;
            if (NavMesh.SamplePosition(destination, out destHit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(destHit.position);
                Debug.Log("✅ Test NPC spawned and given navigation target");
                Debug.Log("   Watch the NPC to verify navigation works correctly");
            }
            else
            {
                Debug.LogWarning("⚠️ Could not find valid destination for test");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Could not find valid spawn position on NavMesh");
        }
    }

    [ContextMenu("Clear Test NPCs")]
    public void ClearTestNPCs()
    {
        GameObject[] testNPCs = GameObject.FindGameObjectsWithTag("Untagged");
        foreach (GameObject obj in testNPCs)
        {
            if (obj.name.Contains("NavMesh Test NPC"))
            {
                DestroyImmediate(obj);
            }
        }
        Debug.Log("✅ Test NPCs cleared");
    }

    void OnDrawGizmosSelected()
    {
        // Draw NavMesh bounds
        if (navMeshValid)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 10f);
        }
    }
}