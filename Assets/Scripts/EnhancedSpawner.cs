using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GuardAI;

public class EnhancedSpawner : MonoBehaviour
{
    [Header("Object Pooling")]
    public bool useObjectPool = true;
    public bool autoInitializePools = true;

    [Header("Prefabs")]
    public GameObject guardPrefab;
    public GameObject peasantPrefab;
    public GameObject merchantPrefab;
    public GameObject priestPrefab;
    public GameObject noblePrefab;
    public GameObject royaltyPrefab;

    [Header("Pool Configuration")]
    public List<PooledObject> entityPools = new List<PooledObject>();

    [Header("Personality Distribution")]
    [Range(0f, 1f)]
    public float cowardlyChance = 0.15f;
    [Range(0f, 1f)]
    public float normalChance = 0.4f;
    [Range(0f, 1f)]
    public float braveChance = 0.1f;
    [Range(0f, 1f)]
    public float curiousChance = 0.15f;
    [Range(0f, 1f)]
    public float socialChance = 0.15f;
    [Range(0f, 1f)]
    public float lonerChance = 0.05f;

    [Header("Personality Traits by Rarity")]
    [Header("Peasant Traits")]
    public float peasantBraveryRange = 0.3f; // 0.2-0.5
    public float peasantCuriosityRange = 0.4f; // 0.3-0.7
    public float peasantSocialRange = 0.6f; // 0.4-1.0

    [Header("Merchant Traits")]
    public float merchantBraveryRange = 0.4f; // 0.3-0.7
    public float merchantCuriosityRange = 0.5f; // 0.4-0.9
    public float merchantSocialRange = 0.8f; // 0.6-1.0

    [Header("Priest Traits")]
    public float priestBraveryRange = 0.6f; // 0.5-1.0
    public float priestCuriosityRange = 0.3f; // 0.2-0.5
    public float priestSocialRange = 0.7f; // 0.5-1.0

    [Header("Noble Traits")]
    public float nobleBraveryRange = 0.5f; // 0.4-0.9
    public float nobleCuriosityRange = 0.4f; // 0.3-0.7
    public float nobleSocialRange = 0.6f; // 0.4-1.0

    [Header("Royalty Traits")]
    public float royaltyBraveryRange = 0.7f; // 0.6-1.0
    public float royaltyCuriosityRange = 0.6f; // 0.5-1.0
    public float royaltySocialRange = 0.5f; // 0.3-0.8

    [Header("Spawn Settings")]
    public bool enableVisualFeedback = true;
    public bool enableAudioFeedback = true;
    public bool enableGuardCommunication = true;
    public bool enableCitizenSocialBehavior = true;
    public bool enableMemorySystem = true;

    [Header("Debug")]
    public bool debugMode = false;
    public bool logSpawnDetails = false;

    private List<GameObject> spawnedEntities = new List<GameObject>();

    void Start()
    {
        if (autoInitializePools && useObjectPool)
        {
            InitializePools();
        }

        SpawnEntitiesAtWaypointGroups();
        if (debugMode)
        {
            LogSpawnSummary();
        }
    }

    void InitializePools()
    {
        if (ObjectPool.Instance == null)
        {
            Debug.LogError("ObjectPool not found! Make sure ObjectPool is in the scene.");
            return;
        }

        // Initialize pools for all entity types
        InitializeEntityPools();

        if (debugMode)
        {
            Debug.Log("EnhancedSpawner: Initialized object pools");
        }
    }

    void InitializeEntityPools()
    {
        // Add prefabs to entity pools if not already configured
        if (entityPools.Count == 0)
        {
            AddPrefabToPools(guardPrefab, "Guard", 10, 50);
            AddPrefabToPools(peasantPrefab, "Peasant", 20, 100);
            AddPrefabToPools(merchantPrefab, "Merchant", 15, 75);
            AddPrefabToPools(priestPrefab, "Priest", 10, 50);
            AddPrefabToPools(noblePrefab, "Noble", 8, 40);
            AddPrefabToPools(royaltyPrefab, "Royalty", 5, 25);
        }

        // Create pools in ObjectPool
        foreach (var pooledObject in entityPools)
        {
            if (pooledObject.prefab != null)
            {
                ObjectPool.Instance.CreatePool(
                    pooledObject.prefab.name,
                    pooledObject.prefab,
                    pooledObject.initialPoolSize,
                    pooledObject.maxPoolSize,
                    pooledObject.expandable
                );

                if (debugMode)
                {
                    Debug.Log($"Initialized pool for {pooledObject.prefab.name}");
                }
            }
        }
    }

    void AddPrefabToPools(GameObject prefab, string name, int initialSize, int maxSize)
    {
        if (prefab != null)
        {
            PooledObject pooledObject = new PooledObject
            {
                prefab = prefab,
                initialPoolSize = initialSize,
                maxPoolSize = maxSize,
                expandable = true
            };
            entityPools.Add(pooledObject);
        }
    }

    void SpawnEntitiesAtWaypointGroups()
    {
        WaypointGroup[] groups = FindObjectsByType<WaypointGroup>(FindObjectsSortMode.None);
        foreach (WaypointGroup group in groups)
        {
            GameObject prefabToSpawn = GetPrefabForGroup(group.groupType);
            if (prefabToSpawn != null && group.waypoints != null && group.waypoints.Length > 0)
            {
                // Spawn multiple entities based on group capacity
                int entitiesToSpawn = Mathf.Min(group.maxEntities, GetInitialEntityCountForGroup(group));

                for (int i = 0; i < entitiesToSpawn; i++)
                {
                    // Get starting position for this entity
                    int startWaypointIndex = group.GetStartingWaypointForEntity(i);

                    // Skip if we're trying to spawn more entities than waypoints (unless allowed)
                    if (!group.allowSharedWaypoints && i >= group.waypoints.Length)
                    {
                        Debug.LogWarning($"Skipping spawn {i + 1} for group {group.name}: not enough unique waypoints");
                        continue;
                    }

                    Vector3 spawnPosition = group.waypoints[startWaypointIndex].transform.position;

                    // Add offset to prevent overlapping spawns
                    if (i > 0 && group.allowSharedWaypoints)
                    {
                        spawnPosition += GetSpawnOffset(i);
                    }

                    GameObject entity = SpawnEntity(prefabToSpawn, spawnPosition, group.waypoints[startWaypointIndex].transform.rotation);
                    if (entity != null)
                    {
                        spawnedEntities.Add(entity);

                        // Assign entity to the group
                        if (group.AssignEntity(entity))
                        {
                            // Initialize based on entity type
                            if (group.groupType == WaypointType.Guard)
                            {
                                InitializeGuard(entity, group);
                            }
                            else
                            {
                                InitializeCitizen(entity, group);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Failed to assign {entity.name} to waypoint group {group.name}");
                        }
                    }
                }
            }
        }
    }

    int GetInitialEntityCountForGroup(WaypointGroup group)
    {
        // Determine how many entities to spawn initially based on group type
        switch (group.groupType)
        {
            case WaypointType.Guard:
                return 1; // Start with 1 guard, difficulty progression will add more
            case WaypointType.Peasant:
                return Mathf.Min(group.maxEntities, 2); // Start with 2 peasants
            case WaypointType.Merchant:
                return 1; // Merchants are usually alone
            case WaypointType.Priest:
                return 1; // Priests usually patrol alone
            case WaypointType.Noble:
                return Mathf.Min(group.maxEntities, 2); // Nobles might have companions
            case WaypointType.Royalty:
                return Mathf.Min(group.maxEntities, 3); // Royalty might have entourage
            default:
                return 1;
        }
    }

    Vector3 GetSpawnOffset(int entityIndex)
    {
        // Create spawn offsets to prevent entities spawning on top of each other
        float angle = (2f * Mathf.PI * entityIndex) / 8f; // Distribute around circle
        float distance = 1.5f; // Small spawn offset

        return new Vector3(
            Mathf.Cos(angle) * distance,
            0f,
            Mathf.Sin(angle) * distance
        );
    }

    public GameObject SpawnEntity(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (useObjectPool && ObjectPool.Instance != null)
        {
            return SpawnFromPool(prefab.name, position, rotation);
        }
        else
        {
            return SpawnDirect(prefab, position, rotation);
        }
    }

    GameObject SpawnFromPool(string entityName, Vector3 position, Quaternion rotation)
    {
        GameObject obj = ObjectPool.Instance.GetObject(entityName, position, rotation);

        if (obj != null && logSpawnDetails)
        {
            Debug.Log($"Spawned {entityName} from pool at {position}");
        }

        return obj;
    }

    GameObject SpawnDirect(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject obj = Instantiate(prefab, position, rotation);

        if (logSpawnDetails)
        {
            Debug.Log($"Spawned {prefab.name} directly at {position}");
        }

        return obj;
    }

    void InitializeGuard(GameObject guard, WaypointGroup group)
    {
        GuardAI guardAI = guard.GetComponent<GuardAI>();
        if (guardAI == null)
        {
            GameLogger.LogError(LogCategory.AI, $"Guard prefab {guard.name} missing GuardAI component!", guard);
            return;
        }

        // Set up patrol points
        if (group.waypoints != null && group.waypoints.Length > 0)
        {
            guardAI.patrolPoints = group.waypoints;

            // Set starting waypoint and direction based on group assignment
            guardAI.currentPatrolIndex = group.GetStartingWaypointIndex(guard);
            guardAI.patrolForward = group.GetPatrolDirection(guard);
        }

        // Store reference to waypoint group for dynamic updates
        guardAI.assignedWaypointGroup = group;

        // Set up visual feedback
        if (enableVisualFeedback)
        {
            SetupGuardVisualFeedback(guard);
        }

        // Set up audio feedback
        if (enableAudioFeedback)
        {
            SetupGuardAudioFeedback(guard);
        }

        // Configure guard communication
        guardAI.enableGuardCommunication = enableGuardCommunication;

        if (logSpawnDetails)
        {
            Debug.Log($"Spawned Guard at {guard.transform.position} with {guardAI.patrolPoints.Length} patrol points, starting at waypoint {guardAI.currentPatrolIndex}");
        }
    }

    void InitializeCitizen(GameObject citizen, WaypointGroup group)
    {
        Citizen Citizen = citizen.GetComponent<Citizen>();
        if (Citizen == null)
        {
            Debug.LogError($"Citizen prefab {citizen.name} missing Citizen component!");
            return;
        }

        // Set rarity and patrol group
        Citizen.rarity = ConvertGroupTypeToCitizenRarity(group.groupType);
        Citizen.patrolGroup = group;

        // Store reference to waypoint group for position adjustments
        Citizen.assignedWaypointGroup = group;

        // Assign random personality
        AssignRandomPersonality(Citizen);

        // Set personality traits based on rarity
        SetPersonalityTraitsByRarity(Citizen);

        // Set up visual feedback
        if (enableVisualFeedback)
        {
            SetupCitizenVisualFeedback(citizen);
        }

        // Set up audio feedback
        if (enableAudioFeedback)
        {
            SetupCitizenAudioFeedback(citizen);
        }

        // Configure social behavior
        Citizen.socialLevel = enableCitizenSocialBehavior ? Citizen.socialLevel : 0f;

        // Configure memory system
        if (!enableMemorySystem)
        {
            Citizen.maxMemorySlots = 0;
        }

        if (logSpawnDetails)
        {
            Debug.Log($"Spawned {Citizen.rarity} Citizen with {Citizen.personality} personality at {citizen.transform.position}");
        }
    }

    void AssignRandomPersonality(Citizen citizen)
    {
        float random = Random.value;
        float cumulative = 0f;

        cumulative += cowardlyChance;
        if (random <= cumulative)
        {
            citizen.personality = CitizenPersonality.Cowardly;
            return;
        }

        cumulative += normalChance;
        if (random <= cumulative)
        {
            citizen.personality = CitizenPersonality.Normal;
            return;
        }

        cumulative += braveChance;
        if (random <= cumulative)
        {
            citizen.personality = CitizenPersonality.Brave;
            return;
        }

        cumulative += curiousChance;
        if (random <= cumulative)
        {
            citizen.personality = CitizenPersonality.Curious;
            return;
        }

        cumulative += socialChance;
        if (random <= cumulative)
        {
            citizen.personality = CitizenPersonality.Social;
            return;
        }

        cumulative += lonerChance;
        if (random <= cumulative)
        {
            citizen.personality = CitizenPersonality.Loner;
            return;
        }

        // Fallback to normal
        citizen.personality = CitizenPersonality.Normal;
    }

    void SetPersonalityTraitsByRarity(Citizen citizen)
    {
        switch (citizen.rarity)
        {
            case CitizenRarity.Peasant:
                citizen.braveryLevel = Random.Range(0.2f, 0.5f);
                citizen.curiosityLevel = Random.Range(0.3f, 0.7f);
                citizen.socialLevel = Random.Range(0.4f, 1.0f);
                break;
            case CitizenRarity.Merchant:
                citizen.braveryLevel = Random.Range(0.3f, 0.7f);
                citizen.curiosityLevel = Random.Range(0.4f, 0.9f);
                citizen.socialLevel = Random.Range(0.6f, 1.0f);
                break;
            case CitizenRarity.Priest:
                citizen.braveryLevel = Random.Range(0.5f, 1.0f);
                citizen.curiosityLevel = Random.Range(0.2f, 0.5f);
                citizen.socialLevel = Random.Range(0.5f, 1.0f);
                break;
            case CitizenRarity.Noble:
                citizen.braveryLevel = Random.Range(0.4f, 0.9f);
                citizen.curiosityLevel = Random.Range(0.3f, 0.7f);
                citizen.socialLevel = Random.Range(0.4f, 1.0f);
                break;
            case CitizenRarity.Royalty:
                citizen.braveryLevel = Random.Range(0.6f, 1.0f);
                citizen.curiosityLevel = Random.Range(0.5f, 1.0f);
                citizen.socialLevel = Random.Range(0.3f, 0.8f);
                break;
        }
    }

    void SetupGuardVisualFeedback(GameObject guard)
    {
        // Add or find light component
        Light guardLight = guard.GetComponent<Light>();
        if (guardLight == null)
        {
            guardLight = guard.AddComponent<Light>();
        }

        // Configure light
        guardLight.type = LightType.Point;
        guardLight.range = 3f;
        guardLight.intensity = 0.5f;
        guardLight.color = Color.green; // Start in patrol state

        // Assign to GuardAI
        GuardAI guardAI = guard.GetComponent<GuardAI>();
        if (guardAI != null)
        {
            guardAI.guardLight = guardLight;
        }
    }

    void SetupGuardAudioFeedback(GameObject guard)
    {
        // Add or find audio source
        AudioSource audioSource = guard.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = guard.AddComponent<AudioSource>();
        }

        // Configure audio source
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.volume = 0.7f;
        audioSource.maxDistance = 15f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        // Note: Audio clips should be assigned in the inspector or loaded dynamically
    }

    void SetupCitizenVisualFeedback(GameObject citizen)
    {
        // Add or find light component
        Light citizenLight = citizen.GetComponent<Light>();
        if (citizenLight == null)
        {
            citizenLight = citizen.AddComponent<Light>();
        }

        // Configure light
        citizenLight.type = LightType.Point;
        citizenLight.range = 2f;
        citizenLight.intensity = 0.3f;
        citizenLight.color = Color.white; // Start in normal state

        // Assign to Citizen
        Citizen Citizen = citizen.GetComponent<Citizen>();
        if (Citizen != null)
        {
            Citizen.citizenLight = citizenLight;
        }
    }

    void SetupCitizenAudioFeedback(GameObject citizen)
    {
        // Add or find audio source
        AudioSource audioSource = citizen.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = citizen.AddComponent<AudioSource>();
        }

        // Configure audio source
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.volume = 0.5f;
        audioSource.maxDistance = 10f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        // Note: Audio clips should be assigned in the inspector or loaded dynamically
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

    void LogSpawnSummary()
    {
        int guardCount = 0;
        int citizenCount = 0;
        Dictionary<CitizenPersonality, int> personalityCounts = new Dictionary<CitizenPersonality, int>();
        Dictionary<CitizenRarity, int> rarityCounts = new Dictionary<CitizenRarity, int>();

        foreach (GameObject entity in spawnedEntities)
        {
            if (entity.GetComponent<GuardAI>() != null)
            {
                guardCount++;
            }
            else if (entity.GetComponent<Citizen>() != null)
            {
                citizenCount++;
                Citizen citizen = entity.GetComponent<Citizen>();

                // Count personalities
                if (!personalityCounts.ContainsKey(citizen.personality))
                    personalityCounts[citizen.personality] = 0;
                personalityCounts[citizen.personality]++;

                // Count rarities
                if (!rarityCounts.ContainsKey(citizen.rarity))
                    rarityCounts[citizen.rarity] = 0;
                rarityCounts[citizen.rarity]++;
            }
        }

        Debug.Log("=== SPAWN SUMMARY ===");
        Debug.Log($"Total entities spawned: {spawnedEntities.Count}");
        Debug.Log($"Guards: {guardCount}");
        Debug.Log($"Citizens: {citizenCount}");

        Debug.Log("Personality Distribution:");
        foreach (var kvp in personalityCounts)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value}");
        }

        Debug.Log("Rarity Distribution:");
        foreach (var kvp in rarityCounts)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value}");
        }
        Debug.Log("=====================");
    }

    // Public methods for runtime spawning
    public GameObject SpawnGuard(Vector3 position, Quaternion rotation)
    {
        if (guardPrefab == null) return null;

        GameObject guard = SpawnEntity(guardPrefab, position, rotation);
        if (guard != null)
        {
            spawnedEntities.Add(guard);

            // Create a temporary waypoint group for the guard
            GameObject tempGroup = new GameObject("TempGuardGroup");
            WaypointGroup waypointGroup = tempGroup.AddComponent<WaypointGroup>();
            waypointGroup.groupType = WaypointType.Guard;

            // Create a single waypoint at the spawn position
            GameObject waypoint = new GameObject("GuardWaypoint");
            waypoint.transform.position = position;
            waypoint.transform.SetParent(tempGroup.transform);
            waypointGroup.waypoints = new Waypoint[] { waypoint.GetComponent<Waypoint>() };

            InitializeGuard(guard, waypointGroup);
        }

        return guard;
    }

    public GameObject SpawnCitizen(Vector3 position, Quaternion rotation, CitizenRarity rarity = CitizenRarity.Peasant)
    {
        GameObject prefab = GetPrefabForRarity(rarity);
        if (prefab == null) return null;

        GameObject citizen = SpawnEntity(prefab, position, rotation);
        if (citizen != null)
        {
            spawnedEntities.Add(citizen);

            // Create a temporary waypoint group for the citizen
            GameObject tempGroup = new GameObject($"Temp{rarity}Group");
            WaypointGroup waypointGroup = tempGroup.AddComponent<WaypointGroup>();
            waypointGroup.groupType = ConvertRarityToWaypointType(rarity);

            // Create a single waypoint at the spawn position
            GameObject waypoint = new GameObject($"{rarity}Waypoint");
            waypoint.transform.position = position;
            waypoint.transform.SetParent(tempGroup.transform);
            waypointGroup.waypoints = new Waypoint[] { waypoint.GetComponent<Waypoint>() };

            InitializeCitizen(citizen, waypointGroup);
        }

        return citizen;
    }

    GameObject GetPrefabForRarity(CitizenRarity rarity)
    {
        switch (rarity)
        {
            case CitizenRarity.Peasant:
                return peasantPrefab;
            case CitizenRarity.Merchant:
                return merchantPrefab;
            case CitizenRarity.Priest:
                return priestPrefab;
            case CitizenRarity.Noble:
                return noblePrefab;
            case CitizenRarity.Royalty:
                return royaltyPrefab;
            default:
                return peasantPrefab;
        }
    }

    WaypointType ConvertRarityToWaypointType(CitizenRarity rarity)
    {
        switch (rarity)
        {
            case CitizenRarity.Peasant:
                return WaypointType.Peasant;
            case CitizenRarity.Merchant:
                return WaypointType.Merchant;
            case CitizenRarity.Priest:
                return WaypointType.Priest;
            case CitizenRarity.Noble:
                return WaypointType.Noble;
            case CitizenRarity.Royalty:
                return WaypointType.Royalty;
            default:
                return WaypointType.Peasant;
        }
    }

    // Add methods for returning entities to pool
    public void ReturnEntityToPool(GameObject entity)
    {
        // Unassign from waypoint group if assigned
        GuardAI guardAI = entity.GetComponent<GuardAI>();
        if (guardAI?.assignedWaypointGroup != null)
        {
            guardAI.assignedWaypointGroup.UnassignEntity(entity);
        }

        Citizen citizen = entity.GetComponent<Citizen>();
        if (citizen?.assignedWaypointGroup != null)
        {
            citizen.assignedWaypointGroup.UnassignEntity(entity);
        }

        if (useObjectPool && ObjectPool.Instance != null)
        {
            ObjectPool.Instance.ReturnObject(entity);
            spawnedEntities.Remove(entity);
        }
        else
        {
            Destroy(entity);
            spawnedEntities.Remove(entity);
        }
    }

    public void ReturnAllEntitiesToPool()
    {
        if (useObjectPool && ObjectPool.Instance != null)
        {
            ObjectPool.Instance.ReturnAllObjects();
        }
        else
        {
            foreach (var entity in spawnedEntities)
            {
                if (entity != null)
                {
                    Destroy(entity);
                }
            }
        }

        spawnedEntities.Clear();
    }

    // Add statistics methods
    public int GetSpawnedEntityCount()
    {
        return spawnedEntities.Count;
    }

    public Dictionary<string, int> GetEntityTypeCounts()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();

        foreach (var entity in spawnedEntities)
        {
            if (entity != null)
            {
                string type = "Unknown";
                if (entity.GetComponent<GuardAI>() != null)
                    type = "Guard";
                else if (entity.GetComponent<Citizen>() != null)
                    type = "Citizen";

                if (!counts.ContainsKey(type))
                    counts[type] = 0;
                counts[type]++;
            }
        }

        return counts;
    }

    // Debug methods
    [ContextMenu("Log Entity Statistics")]
    public void LogEntityStatistics()
    {
        Debug.Log("=== Enhanced Spawner Entity Statistics ===");
        Debug.Log($"Total spawned entities: {GetSpawnedEntityCount()}");

        var typeCounts = GetEntityTypeCounts();
        foreach (var kvp in typeCounts)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value}");
        }
    }

    [ContextMenu("Return All Entities")]
    public void ReturnAllEntitiesFromContext()
    {
        ReturnAllEntitiesToPool();
    }

    // Dynamic Guard Management Methods for DifficultyProgression integration
    public void AdjustGuardCount(int targetCount)
    {
        int currentGuardCount = GetCurrentGuardCount();
        int difference = targetCount - currentGuardCount;

        if (debugMode)
        {
            Debug.Log($"[EnhancedSpawner] Adjusting guard count: current={currentGuardCount}, target={targetCount}, difference={difference}");
        }

        if (difference > 0)
        {
            // Spawn additional guards
            SpawnAdditionalGuards(difference);
        }
        else if (difference < 0)
        {
            // Remove excess guards
            RemoveExcessGuards(-difference);
        }
    }

    public int GetCurrentGuardCount()
    {
        int guardCount = 0;
        foreach (GameObject entity in spawnedEntities)
        {
            if (entity != null && entity.GetComponent<GuardAI>() != null)
                guardCount++;
        }
        return guardCount;
    }

    private void SpawnAdditionalGuards(int count)
    {
        if (guardPrefab == null)
        {
            Debug.LogError("[EnhancedSpawner] Cannot spawn guards: guardPrefab is null!");
            return;
        }

        // Find guard waypoint groups with available slots
        WaypointGroup[] allGroups = FindObjectsOfType<WaypointGroup>();
        List<WaypointGroup> availableGroups = new List<WaypointGroup>();

        foreach (var group in allGroups)
        {
            if (group.groupType == WaypointType.Guard &&
                group.waypoints != null &&
                group.waypoints.Length > 0 &&
                group.GetAvailableSlots() > 0)
            {
                availableGroups.Add(group);
            }
        }

        if (availableGroups.Count == 0)
        {
            Debug.LogWarning("[EnhancedSpawner] No guard waypoint groups with available slots found! Consider increasing maxEntities on existing groups or creating new groups.");
            return;
        }

        int guardsSpawned = 0;

        // Spawn guards, filling groups evenly
        while (guardsSpawned < count && availableGroups.Count > 0)
        {
            // Use round-robin to distribute guards evenly
            WaypointGroup selectedGroup = availableGroups[guardsSpawned % availableGroups.Count];

            // Get the next available entity index for this group
            int entityIndex = selectedGroup.GetAssignedEntityCount();
            int startWaypointIndex = selectedGroup.GetStartingWaypointForEntity(entityIndex);

            // Get spawn position with offset
            Vector3 spawnPosition = selectedGroup.waypoints[startWaypointIndex].transform.position;
            if (entityIndex > 0)
            {
                spawnPosition += GetSpawnOffset(entityIndex);
            }

            Quaternion spawnRotation = selectedGroup.waypoints[startWaypointIndex].transform.rotation;

            GameObject newGuard = SpawnEntity(guardPrefab, spawnPosition, spawnRotation);
            if (newGuard != null)
            {
                spawnedEntities.Add(newGuard);

                // Assign to the group
                if (selectedGroup.AssignEntity(newGuard))
                {
                    InitializeGuard(newGuard, selectedGroup);
                    guardsSpawned++;

                    if (debugMode)
                    {
                        Debug.Log($"[EnhancedSpawner] Spawned additional guard at {spawnPosition} in group {selectedGroup.name} (slot {entityIndex + 1}/{selectedGroup.maxEntities})");
                    }

                    // Remove group from available list if it's now full
                    if (selectedGroup.GetAvailableSlots() == 0)
                    {
                        availableGroups.Remove(selectedGroup);
                    }
                }
                else
                {
                    // Failed to assign, remove the spawned guard
                    ReturnEntityToPool(newGuard);
                    Debug.LogWarning($"[EnhancedSpawner] Failed to assign guard to group {selectedGroup.name}");
                }
            }
            else
            {
                Debug.LogError($"[EnhancedSpawner] Failed to spawn guard at {spawnPosition}");
                break;
            }
        }

        if (guardsSpawned < count)
        {
            Debug.LogWarning($"[EnhancedSpawner] Only spawned {guardsSpawned}/{count} requested guards due to capacity limitations");
        }
    }

    private void RemoveExcessGuards(int count)
    {
        List<GameObject> guardsToRemove = new List<GameObject>();
        Dictionary<WaypointGroup, List<GameObject>> guardsByGroup = new Dictionary<WaypointGroup, List<GameObject>>();

        // Group guards by their waypoint groups
        foreach (GameObject entity in spawnedEntities)
        {
            if (entity != null && entity.GetComponent<GuardAI>() != null)
            {
                GuardAI guard = entity.GetComponent<GuardAI>();
                WaypointGroup group = guard.assignedWaypointGroup;

                if (group != null)
                {
                    if (!guardsByGroup.ContainsKey(group))
                        guardsByGroup[group] = new List<GameObject>();
                    guardsByGroup[group].Add(entity);
                }
            }
        }

        // Remove guards intelligently, prioritizing:
        // 1. Groups with multiple guards (keep at least 1 per group if possible)
        // 2. Guards in patrol state over those in alert states

        foreach (var groupGuards in guardsByGroup.OrderByDescending(kvp => kvp.Value.Count))
        {
            if (guardsToRemove.Count >= count) break;

            WaypointGroup group = groupGuards.Key;
            List<GameObject> guards = groupGuards.Value;

            // Always keep at least 1 guard per group unless we really need to remove them all
            int maxToRemoveFromGroup = guards.Count > 1 ? guards.Count - 1 :
                                     (guardsToRemove.Count + guards.Count <= count ? guards.Count : 0);

            // Sort guards by priority (patrol state first, then by distance from start)
            guards.Sort((a, b) =>
            {
                GuardAI guardA = a.GetComponent<GuardAI>();
                GuardAI guardB = b.GetComponent<GuardAI>();

                // Prioritize guards in patrol state
                if (guardA.currentState == GuardState.Patrol && guardB.currentState != GuardState.Patrol)
                    return -1;
                if (guardA.currentState != GuardState.Patrol && guardB.currentState == GuardState.Patrol)
                    return 1;

                return 0; // Equal priority
            });

            // Remove guards from this group
            for (int i = 0; i < maxToRemoveFromGroup && guardsToRemove.Count < count; i++)
            {
                guardsToRemove.Add(guards[i]);
            }
        }

        // Remove the selected guards and unassign them from groups
        foreach (GameObject guard in guardsToRemove)
        {
            GuardAI guardAI = guard.GetComponent<GuardAI>();
            if (guardAI?.assignedWaypointGroup != null)
            {
                guardAI.assignedWaypointGroup.UnassignEntity(guard);
            }

            if (debugMode)
            {
                Debug.Log($"[EnhancedSpawner] Removing excess guard at {guard.transform.position}");
            }

            ReturnEntityToPool(guard);
        }
    }

    // Context menu methods for testing
    [ContextMenu("Spawn 3 Additional Guards")]
    public void SpawnThreeGuards() => AdjustGuardCount(GetCurrentGuardCount() + 3);

    [ContextMenu("Remove 2 Guards")]
    public void RemoveTwoGuards() => AdjustGuardCount(Mathf.Max(1, GetCurrentGuardCount() - 2));

    [ContextMenu("Log Current Guard Count")]
    public void LogCurrentGuardCount() => Debug.Log($"Current Guard Count: {GetCurrentGuardCount()}");

    void OnDestroy()
    {
        // Clean up all spawned entities
        ReturnAllEntitiesToPool();
    }
}