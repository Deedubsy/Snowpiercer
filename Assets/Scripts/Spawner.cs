using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject guardPrefab;
    public GameObject peasantPrefab;
    public GameObject merchantPrefab;
    public GameObject priestPrefab;
    public GameObject noblePrefab;
    public GameObject royaltyPrefab;

    [Header("Migration Notice")]
    [TextArea(3, 5)]
    public string migrationNotice = "This is the legacy Spawner. Consider upgrading to EnhancedSpawner for better AI features, personality system, and visual/audio feedback.";

    [Header("Use Enhanced Spawner")]
    public bool useEnhancedSpawner = true;

    void Start()
    {
        if (useEnhancedSpawner)
        {
            // Try to find EnhancedSpawner and use it
            EnhancedSpawner enhancedSpawner = FindObjectOfType<EnhancedSpawner>();
            if (enhancedSpawner != null)
            {
                // Transfer prefab assignments to EnhancedSpawner
                TransferPrefabsToEnhancedSpawner(enhancedSpawner);

                // Disable this spawner since EnhancedSpawner will handle spawning
                enabled = false;
                Debug.Log("Legacy Spawner disabled - using EnhancedSpawner instead.");
                return;
            }
            else
            {
                Debug.LogWarning("EnhancedSpawner not found. Using legacy spawning method. Consider adding EnhancedSpawner to your scene.");
            }
        }

        // Fallback to legacy spawning
        SpawnEntitiesAtWaypointGroups();
    }

    void TransferPrefabsToEnhancedSpawner(EnhancedSpawner enhancedSpawner)
    {
        // Transfer prefab assignments
        if (enhancedSpawner.guardPrefab == null) enhancedSpawner.guardPrefab = guardPrefab;
        if (enhancedSpawner.peasantPrefab == null) enhancedSpawner.peasantPrefab = peasantPrefab;
        if (enhancedSpawner.merchantPrefab == null) enhancedSpawner.merchantPrefab = merchantPrefab;
        if (enhancedSpawner.priestPrefab == null) enhancedSpawner.priestPrefab = priestPrefab;
        if (enhancedSpawner.noblePrefab == null) enhancedSpawner.noblePrefab = noblePrefab;
        if (enhancedSpawner.royaltyPrefab == null) enhancedSpawner.royaltyPrefab = royaltyPrefab;

        Debug.Log("Prefab assignments transferred to EnhancedSpawner.");
    }

    void SpawnEntitiesAtWaypointGroups()
    {
        WaypointGroup[] groups = FindObjectsOfType<WaypointGroup>();
        foreach (WaypointGroup group in groups)
        {
            GameObject prefabToSpawn = GetPrefabForGroup(group.groupType);
            if (prefabToSpawn != null && group.waypoints != null && group.waypoints.Length > 0)
            {
                GameObject entity = Instantiate(prefabToSpawn, group.waypoints[0].transform.position, group.waypoints[0].transform.rotation);

                // Basic initialization for legacy compatibility
                Citizen citizen = entity.GetComponent<Citizen>();
                if (citizen != null)
                {
                    citizen.rarity = ConvertGroupTypeToCitizenRarity(group.groupType);
                    citizen.patrolGroup = group;

                    // Assign a basic personality for compatibility
                    citizen.personality = CitizenPersonality.Normal;
                    citizen.braveryLevel = 0.5f;
                    citizen.curiosityLevel = 0.5f;
                    citizen.socialLevel = 0.5f;
                }

                // Basic guard initialization
                GuardAI guard = entity.GetComponent<GuardAI>();
                if (guard != null && group.waypoints != null && group.waypoints.Length > 0)
                {
                    Waypoint[] patrolPoints = new Waypoint[group.waypoints.Length];
                    for (int i = 0; i < group.waypoints.Length; i++)
                    {
                        patrolPoints[i] = group.waypoints[i];
                    }
                    guard.patrolPoints = patrolPoints;
                }
            }
        }
    }

    GameObject GetPrefabForGroup(WaypointType type)
    {
        switch (type)
        {
            case WaypointType.Guard:
                return guardPrefab;
            case WaypointType.Peasant:
                return peasantPrefab;
            case WaypointType.Merchant:
                return merchantPrefab;
            case WaypointType.Priest:
                return priestPrefab;
            case WaypointType.Noble:
                return noblePrefab;
            case WaypointType.Royalty:
                return royaltyPrefab;
            default:
                return null;
        }
    }

    CitizenRarity ConvertGroupTypeToCitizenRarity(WaypointType type)
    {
        switch (type)
        {
            case WaypointType.Peasant:
                return CitizenRarity.Peasant;
            case WaypointType.Merchant:
                return CitizenRarity.Merchant;
            case WaypointType.Priest:
                return CitizenRarity.Priest;
            case WaypointType.Noble:
                return CitizenRarity.Noble;
            case WaypointType.Royalty:
                return CitizenRarity.Royalty;
            default:
                return CitizenRarity.Peasant;
        }
    }

    [ContextMenu("Upgrade to EnhancedSpawner")]
    void UpgradeToEnhancedSpawner()
    {
        // Check if EnhancedSpawner already exists
        EnhancedSpawner enhancedSpawner = FindObjectOfType<EnhancedSpawner>();
        if (enhancedSpawner == null)
        {
            // Create EnhancedSpawner
            GameObject enhancedSpawnerObj = new GameObject("EnhancedSpawner");
            enhancedSpawner = enhancedSpawnerObj.AddComponent<EnhancedSpawner>();
        }

        // Transfer prefab assignments
        TransferPrefabsToEnhancedSpawner(enhancedSpawner);

        // Enable enhanced features
        enhancedSpawner.enableVisualFeedback = true;
        enhancedSpawner.enableAudioFeedback = true;
        enhancedSpawner.enableGuardCommunication = true;
        enhancedSpawner.enableCitizenSocialBehavior = true;
        enhancedSpawner.enableMemorySystem = true;
        enhancedSpawner.debugMode = true;

        // Disable this spawner
        useEnhancedSpawner = true;
        enabled = false;

        Debug.Log("Successfully upgraded to EnhancedSpawner! The legacy Spawner has been disabled.");
    }
    
    // Method for DifficultyProgression integration
    public void SetTargetGuardCount(int targetCount)
    {
        // For legacy spawner, we delegate to EnhancedSpawner if available
        EnhancedSpawner enhancedSpawner = FindObjectOfType<EnhancedSpawner>();
        if (enhancedSpawner != null)
        {
            enhancedSpawner.AdjustGuardCount(targetCount);
        }
        else
        {
            // Legacy behavior - just log the request
            Debug.Log($"[Spawner] Target guard count set to {targetCount} (legacy spawner - consider upgrading to EnhancedSpawner for full functionality)");
        }
    }
}