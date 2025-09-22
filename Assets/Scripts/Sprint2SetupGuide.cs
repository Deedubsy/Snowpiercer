using UnityEngine;

/// <summary>
/// Sprint 2 Setup Guide
/// Provides step-by-step instructions for implementing SP-011 through SP-020
/// </summary>
public class Sprint2SetupGuide : MonoBehaviour
{
    [Header("Sprint 2 Progress Tracking")]
    [SerializeField] private bool[] sprintTasksCompleted = new bool[10];

    [Header("Setup Instructions")]
    [TextArea(10, 20)]
    public string setupInstructions = @"
=== SPRINT 2 SETUP GUIDE ===

WEEK 1: Foundation (SP-011 → SP-020 → SP-013)

DAY 1-2: SP-011 Scene Building
☐ 1. Add GamePlaySceneBuilder component to any GameObject
☐ 2. Assign prefab references (Player, Managers, Guard, Citizen)
☐ 3. Run 'Build GamePlay Scene' from context menu
☐ 4. Verify scene structure created:
    - Castle area with basic structure
    - Town districts with placeholder buildings
    - Terrain with castle hill
    - Basic lighting setup
☐ 5. Save scene as Assets/Scenes/GamePlay.unity

DAY 3: SP-020 Physics Validation
☐ 1. Add PhysicsLayerValidator to scene
☐ 2. Configure layer assignments in Project Settings:
    - Layer 8: Player
    - Layer 9: Guard
    - Layer 10: Citizen
    - Layer 11: Interactive
    - Layer 12: Shadow
    - Layer 13: IndoorArea
☐ 3. Run 'Validate Physics Setup' to check configuration
☐ 4. Use 'Auto-Fix Layer Assignments' if needed
☐ 5. Configure collision matrix in Physics settings

DAY 4-5: SP-013 NavMesh Setup
☐ 1. Select all walkable surfaces
☐ 2. Mark as 'Navigation Static' in Inspector
☐ 3. Window → AI → Navigation → Bake
☐ 4. Verify blue NavMesh overlay covers all areas
☐ 5. Test NPC navigation with existing prefabs

WEEK 2: Integration & Polish (SP-014 → SP-019)

DAY 1: SP-014 Enhanced Spawner Setup
☐ 1. Place EnhancedSpawner components in each district
☐ 2. Configure spawn settings for Guards/Citizens
☐ 3. Connect to existing manager systems
☐ 4. Test spawning with performance monitoring

DAY 2: SP-012 Scene Transitions
☐ 1. Enhance CityGateTrigger component
☐ 2. Test castle ↔ town transitions
☐ 3. Verify save/load state persistence
☐ 4. Add transition animations/effects

DAY 3: SP-015 Lighting + SP-017 Interactive Objects
☐ 1. Implement DayNightLightingController
☐ 2. Place interactive objects (doors, bells, hiding spots)
☐ 3. Configure audio triggers and feedback
☐ 4. Test lighting impact on gameplay

DAY 4: SP-018 Performance + SP-019 Testing
☐ 1. Performance optimization pass
☐ 2. Run comprehensive testing suite
☐ 3. Validate all systems working together
☐ 4. Prepare for Sprint 3

=== VALIDATION CHECKLIST ===

Scene Architecture:
☐ Castle area complete with proper layout
☐ Town districts with distinct characteristics
☐ Terrain supports navigation and gameplay
☐ Scene transitions working smoothly

Technical Systems:
☐ NavMesh covers all playable areas
☐ Physics layers properly configured
☐ NPCs spawn and navigate correctly
☐ Performance targets met (30+ FPS)

Gameplay Features:
☐ Player can move between castle and town
☐ Interactive objects respond correctly
☐ Lighting affects stealth gameplay
☐ Save/load preserves all state

=== TROUBLESHOOTING ===

Common Issues:
• NavMesh won't bake → Check Navigation Static flags
• NPCs get stuck → Verify NavMesh connections
• Performance drops → Use PerformanceStressTest.cs
• Scene transitions fail → Check SaveSystem integration
• Physics glitches → Run PhysicsLayerValidator

Emergency Fallbacks:
• Single scene approach if transitions fail
• Manual waypoints if NavMesh issues persist
• Reduced NPC count if performance problems
• Simplified lighting if complexity too high
";

    void Start()
    {
        DisplayCurrentProgress();
    }

    [ContextMenu("Display Setup Instructions")]
    public void DisplaySetupInstructions()
    {
        Debug.Log("=== SPRINT 2 SETUP GUIDE ===");
        Debug.Log(setupInstructions);
    }

    [ContextMenu("Check Sprint 2 Progress")]
    public void DisplayCurrentProgress()
    {
        Debug.Log("=== SPRINT 2 PROGRESS ===");

        string[] taskNames = {
            "SP-011: Design and build main GamePlay.unity scene",
            "SP-012: Implement CityGateTrigger system",
            "SP-013: Complete NavMesh setup for all areas",
            "SP-014: Configure EnhancedSpawner system",
            "SP-015: Implement DayNightLightingController",
            "SP-016: Set up Waypoint system with WaypointGenerator",
            "SP-017: Place and configure interactive objects",
            "SP-018: Optimize scene for performance",
            "SP-019: Test scene transitions and loading",
            "SP-020: Validate collision layers and physics"
        };

        int completed = 0;
        for (int i = 0; i < sprintTasksCompleted.Length; i++)
        {
            string status = sprintTasksCompleted[i] ? "✅" : "⬜";
            Debug.Log($"{status} {taskNames[i]}");
            if (sprintTasksCompleted[i]) completed++;
        }

        float progressPercent = (float)completed / sprintTasksCompleted.Length * 100f;
        Debug.Log($"Sprint 2 Progress: {completed}/{sprintTasksCompleted.Length} ({progressPercent:F0}%)");
    }

    [ContextMenu("Mark Task Complete")]
    public void MarkTaskComplete()
    {
        Debug.Log("Use Inspector to manually mark tasks as complete, or use specific validation methods");
    }

    public void CompleteTask(int taskIndex)
    {
        if (taskIndex >= 0 && taskIndex < sprintTasksCompleted.Length)
        {
            sprintTasksCompleted[taskIndex] = true;
            Debug.Log($"✅ Task {taskIndex + 1} marked complete");
            DisplayCurrentProgress();
        }
    }

    [ContextMenu("Mark Completed Sprint 2 Tasks")]
    public void MarkCompletedTasks()
    {
        Debug.Log("=== Marking completed Sprint 2 tasks ===");

        // Mark completed tasks:
        // SP-011: Design and build main GamePlay.unity scene (index 0)
        // SP-012: Implement CityGateTrigger system (index 1)
        // SP-013: Complete NavMesh setup for all areas (index 2)
        // SP-014: Configure EnhancedSpawner system (index 3)
        // SP-020: Validate collision layers and physics (index 9)

        int[] completedTaskIndices = { 0, 1, 2, 3, 9 };

        foreach (int index in completedTaskIndices)
        {
            if (index < sprintTasksCompleted.Length)
            {
                sprintTasksCompleted[index] = true;
            }
        }

        Debug.Log("✅ Completed tasks marked");
        DisplayCurrentProgress();
    }

    [ContextMenu("Validate Scene Requirements")]
    public void ValidateSceneRequirements()
    {
        Debug.Log("=== Scene Requirements Validation ===");

        // Check for required components
        bool hasGameManager = FindObjectOfType<GameManager>() != null;
        bool hasPlayer = FindObjectOfType<PlayerController>() != null;
        bool hasTerrain = FindObjectOfType<Terrain>() != null;
        bool hasMainCamera = Camera.main != null;

        Debug.Log($"GameManager present: {(hasGameManager ? "✅" : "❌")}");
        Debug.Log($"Player present: {(hasPlayer ? "✅" : "❌")}");
        Debug.Log($"Terrain present: {(hasTerrain ? "✅" : "❌")}");
        Debug.Log($"Main Camera present: {(hasMainCamera ? "✅" : "❌")}");

        // Check scene organization
        bool hasOrganization = GameObject.Find("--- ENVIRONMENT ---") != null;
        Debug.Log($"Scene organization: {(hasOrganization ? "✅" : "❌")}");

        // Check for NavMesh
        UnityEngine.AI.NavMeshTriangulation navMesh = UnityEngine.AI.NavMesh.CalculateTriangulation();
        bool hasNavMesh = navMesh.vertices.Length > 0;
        Debug.Log($"NavMesh baked: {(hasNavMesh ? "✅" : "❌")}");

        if (hasGameManager && hasPlayer && hasTerrain && hasMainCamera && hasOrganization)
        {
            Debug.Log("🎉 Scene requirements met - ready for Sprint 2 tasks!");
        }
        else
        {
            Debug.LogWarning("⚠️ Some scene requirements missing - run GamePlaySceneBuilder first");
        }
    }

    [ContextMenu("Quick Scene Setup")]
    public void QuickSceneSetup()
    {
        Debug.Log("=== Quick Scene Setup ===");
        Debug.Log("1. Add GamePlaySceneBuilder component");
        Debug.Log("2. Run 'Build GamePlay Scene'");
        Debug.Log("3. Add PhysicsLayerValidator component");
        Debug.Log("4. Run 'Validate Physics Setup'");
        Debug.Log("5. Bake NavMesh (Window → AI → Navigation → Bake)");
        Debug.Log("6. Run 'Validate Scene Requirements'");
    }
}