using System.Collections.Generic;
using UnityEngine;

public class AISystemIntegrator : MonoBehaviour
{
    [Header("System Integration")]
    public bool autoIntegrateOnStart = true;
    public bool validateComponents = true;
    public bool setupCrossReferences = true;

    [Header("Integration Settings")]
    public bool enableGuardAlertnessSystem = true;
    public bool enableCitizenScheduleSystem = true;
    public bool enableRandomEventSystem = true;
    public bool enableVampireHunterSystem = true;

    [Header("Debug")]
    public bool logIntegrationDetails = true;
    public bool showIntegrationStatus = true;

    private List<string> integrationLog = new List<string>();

    void Start()
    {
        if (autoIntegrateOnStart)
        {
            IntegrateAllSystems();
        }
    }

    [ContextMenu("Integrate All Systems")]
    public void IntegrateAllSystems()
    {
        try
        {
            integrationLog.Clear();
            LogMessage("Starting AI System Integration...");

            // Validate and integrate core systems
            ValidateAndIntegrateGameManager();
            ValidateAndIntegrateSpawner();
            ValidateAndIntegrateGuardSystem();
            ValidateAndIntegrateCitizenSystem();
            ValidateAndIntegrateWaypointSystem();
            ValidateAndIntegrateAlertnessSystem();
            ValidateAndIntegrateScheduleSystem();
            ValidateAndIntegrateRandomEventSystem();
            ValidateAndIntegrateVampireHunterSystem();

            // Setup cross-references
            if (setupCrossReferences)
            {
                SetupCrossReferences();
            }

            // Final validation
            if (validateComponents)
            {
                ValidateAllComponents();
            }

            LogMessage("AI System Integration Complete!");
        }
        catch (System.Exception ex)
        {
            LogMessage(ex.Message);
        }

        if (showIntegrationStatus)
        {
            ShowIntegrationStatus();
        }
    }

    void ValidateAndIntegrateGameManager()
    {
        LogMessage("Validating GameManager...");

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            LogMessage("WARNING: GameManager not found in scene!");
            return;
        }

        // Ensure GameManager has required components
        if (gameManager.GetComponent<GuardAlertnessManager>() == null && enableGuardAlertnessSystem)
        {
            gameManager.gameObject.AddComponent<GuardAlertnessManager>();
            LogMessage("Added GuardAlertnessManager to GameManager");
        }

        if (gameManager.GetComponent<CitizenScheduleManager>() == null && enableCitizenScheduleSystem)
        {
            gameManager.gameObject.AddComponent<CitizenScheduleManager>();
            LogMessage("Added CitizenScheduleManager to GameManager");
        }

        if (gameManager.GetComponent<RandomEventManager>() == null && enableRandomEventSystem)
        {
            gameManager.gameObject.AddComponent<RandomEventManager>();
            LogMessage("Added RandomEventManager to GameManager");
        }

        LogMessage("GameManager validation complete");
    }

    void ValidateAndIntegrateSpawner()
    {
        LogMessage("Validating Spawner System...");

        EnhancedSpawner enhancedSpawner = FindFirstObjectByType<EnhancedSpawner>();
        Spawner legacySpawner = FindFirstObjectByType<Spawner>();

        if (enhancedSpawner == null && legacySpawner == null)
        {
            LogMessage("WARNING: No spawner found in scene!");
            return;
        }

        if (enhancedSpawner != null)
        {
            // Configure EnhancedSpawner
            enhancedSpawner.enableVisualFeedback = true;
            enhancedSpawner.enableAudioFeedback = true;
            enhancedSpawner.enableGuardCommunication = true;
            enhancedSpawner.enableCitizenSocialBehavior = true;
            enhancedSpawner.enableMemorySystem = true;

            LogMessage("EnhancedSpawner configured");
        }
        else if (legacySpawner != null)
        {
            LogMessage("Legacy Spawner found - consider upgrading to EnhancedSpawner");
        }
    }

    void ValidateAndIntegrateGuardSystem()
    {
        LogMessage("Validating Guard System...");

        GuardAI[] guards = FindObjectsByType<GuardAI>(FindObjectsSortMode.None);
        LogMessage($"Found {guards.Length} guards in scene");

        foreach (GuardAI guard in guards)
        {
            // Ensure guard has required components
            if (guard.GetComponent<AudioSource>() == null)
            {
                guard.gameObject.AddComponent<AudioSource>();
            }

            if (guard.GetComponent<Light>() == null)
            {
                Light light = guard.gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 3f;
                light.intensity = 0.5f;
                light.color = Color.green;
            }

            // Validate patrol points
            if (guard.patrolPoints == null || guard.patrolPoints.Length == 0)
            {
                LogMessage($"WARNING: Guard {guard.name} has no patrol points!");
            }
        }
    }

    void ValidateAndIntegrateCitizenSystem()
    {
        LogMessage("Validating Citizen System...");

        Citizen[] citizens = FindObjectsByType<Citizen>(FindObjectsSortMode.None);
        LogMessage($"Found {citizens.Length} citizens in scene");

        foreach (Citizen citizen in citizens)
        {
            // Ensure citizen has required components
            if (citizen.GetComponent<AudioSource>() == null)
            {
                citizen.gameObject.AddComponent<AudioSource>();
            }

            if (citizen.GetComponent<Light>() == null)
            {
                Light light = citizen.gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 2f;
                light.intensity = 0.3f;
                light.color = Color.white;
            }
        }
    }

    void ValidateAndIntegrateWaypointSystem()
    {
        LogMessage("Validating Waypoint System...");

        WaypointGroup[] groups = FindObjectsByType<WaypointGroup>(FindObjectsSortMode.None);
        LogMessage($"Found {groups.Length} waypoint groups");

        foreach (WaypointGroup group in groups)
        {
            if (group.waypoints == null || group.waypoints.Length == 0)
            {
                LogMessage($"WARNING: WaypointGroup {group.name} has no waypoints!");
            }
            else
            {
                LogMessage($"WaypointGroup {group.name} has {group.waypoints.Length} waypoints");
            }
        }
    }

    void ValidateAndIntegrateAlertnessSystem()
    {
        if (!enableGuardAlertnessSystem) return;

        LogMessage("Validating Alertness System...");

        GuardAlertnessManager alertnessManager = FindFirstObjectByType<GuardAlertnessManager>();
        if (alertnessManager == null)
        {
            LogMessage("WARNING: GuardAlertnessManager not found!");
            return;
        }

        LogMessage("Alertness system validated");
    }

    void ValidateAndIntegrateScheduleSystem()
    {
        if (!enableCitizenScheduleSystem) return;

        LogMessage("Validating Schedule System...");

        CitizenScheduleManager scheduleManager = FindFirstObjectByType<CitizenScheduleManager>();
        if (scheduleManager == null)
        {
            LogMessage("WARNING: CitizenScheduleManager not found!");
            return;
        }

        // Ensure all citizens are registered
        Citizen[] citizens = FindObjectsByType<Citizen>(FindObjectsSortMode.None);
        foreach (Citizen citizen in citizens)
        {
            scheduleManager.RegisterCitizen(citizen);
        }

        LogMessage("Schedule system validated");
    }

    void ValidateAndIntegrateRandomEventSystem()
    {
        if (!enableRandomEventSystem) return;

        LogMessage("Validating Random Event System...");

        RandomEventManager eventManager = FindFirstObjectByType<RandomEventManager>();
        if (eventManager == null)
        {
            LogMessage("WARNING: RandomEventManager not found!");
            return;
        }

        if (eventManager.availableEvents == null || eventManager.availableEvents.Count == 0)
        {
            LogMessage("WARNING: RandomEventManager has no events assigned!");
        }

        LogMessage("Random Event System validated");
    }

    void ValidateAndIntegrateVampireHunterSystem()
    {
        if (!enableVampireHunterSystem) return;

        LogMessage("Validating Vampire Hunter System...");

        VampireHunter hunter = FindFirstObjectByType<VampireHunter>();
        if (hunter == null)
        {
            LogMessage("INFO: No VampireHunter found in scene.");
            return;
        }

        // Initialize the hunter's equipment
        hunter.Initialize();

        // Validate equipment references
        if (hunter.crossbowPrefab != null && hunter.crossbow == null)
        {
            LogMessage($"WARNING: VampireHunter {hunter.name} has crossbow prefab but failed to get Projectile component.");
        }

        if (hunter.holyWaterPrefab != null && hunter.holyWater == null)
        {
            LogMessage($"WARNING: VampireHunter {hunter.name} has holy water prefab but failed to get AreaEffect component.");
        }

        if (hunter.garlicBombPrefab != null && hunter.garlicBomb == null)
        {
            LogMessage($"WARNING: VampireHunter {hunter.name} has garlic bomb prefab but failed to get AreaEffect component.");
        }

        LogMessage("Vampire Hunter System validated");
    }

    void SetupCrossReferences()
    {
        LogMessage("Setting up cross-references...");

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        GuardAlertnessManager alertnessManager = FindFirstObjectByType<GuardAlertnessManager>();
        CitizenScheduleManager scheduleManager = FindFirstObjectByType<CitizenScheduleManager>();
        RandomEventManager eventManager = FindFirstObjectByType<RandomEventManager>();

        if (gameManager != null)
        {
            if (alertnessManager != null) gameManager.GetComponent<GuardAlertnessManager>();
            if (scheduleManager != null) gameManager.GetComponent<CitizenScheduleManager>();
            if (eventManager != null) gameManager.GetComponent<RandomEventManager>();
        }

        if (alertnessManager != null && eventManager != null)
        {
            // Example: eventManager.alertnessManager = alertnessManager;
        }

        if (scheduleManager != null && eventManager != null)
        {
            // Example: eventManager.scheduleManager = scheduleManager;
        }

        LogMessage("Cross-references setup complete");
    }

    void ValidateAllComponents()
    {
        LogMessage("Performing final component validation...");

        // Player validation
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (player.GetComponent<PlayerController>() == null) LogMessage("WARNING: Player missing PlayerController component");
            if (player.GetComponent<VampireStats>() == null) LogMessage("WARNING: Player missing VampireStats component");
            if (player.GetComponent<VampireAbilities>() == null) LogMessage("WARNING: Player missing VampireAbilities component");
        }
        else
        {
            LogMessage("ERROR: No GameObject with tag 'Player' found!");
        }

        LogMessage("Final validation complete.");
    }

    void ShowIntegrationStatus()
    {
        string status = "--- AI System Integration Status ---\n";
        foreach (string log in integrationLog)
        {
            status += log + "\n";
        }
        Debug.Log(status);
    }

    void LogMessage(string message)
    {
        if (logIntegrationDetails)
        {
            integrationLog.Add(message);
        }
    }

    [ContextMenu("Quick Fix Common Issues")]
    void QuickFixCommonIssues()
    {
        LogMessage("Running Quick Fix...");

        // Ensure player has correct tag
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null && player.tag != "Player")
        {
            player.tag = "Player";
            LogMessage("FIXED: Set Player GameObject tag to 'Player'");
        }

        IntegrateAllSystems();
    }
}