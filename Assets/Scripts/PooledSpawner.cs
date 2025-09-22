using System.Collections.Generic;
using UnityEngine;

public class PooledSpawner : MonoBehaviour
{
    [Header("Spawner Configuration")]
    public bool useObjectPool = true;
    public bool autoInitializePools = true;

    [Header("Spawn Settings")]
    public float spawnRadius = 5f;
    public LayerMask spawnLayerMask = -1;
    public int maxSpawnAttempts = 10;

    [Header("Pool Configuration")]
    public List<PooledObject> entityPools = new List<PooledObject>();

    [Header("Debug")]
    public bool debugMode = false;
    public bool showSpawnPoints = false;

    // Spawn statistics
    private Dictionary<string, int> spawnCounts = new Dictionary<string, int>();
    private List<GameObject> spawnedObjects = new List<GameObject>();

    void Start()
    {
        if (autoInitializePools && useObjectPool)
        {
            InitializePools();
        }
    }

    void InitializePools()
    {
        if (ObjectPool.Instance == null)
        {
            Debug.LogError("ObjectPool not found! Make sure ObjectPool is in the scene.");
            return;
        }

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

                spawnCounts[pooledObject.prefab.name] = 0;

                if (debugMode)
                {
                    Debug.Log($"Initialized pool for {pooledObject.prefab.name}");
                }
            }
        }
    }

    public GameObject SpawnEntity(string entityName, Vector3 position, Quaternion rotation = default)
    {
        if (useObjectPool && ObjectPool.Instance != null)
        {
            return SpawnFromPool(entityName, position, rotation);
        }
        else
        {
            return SpawnDirect(entityName, position, rotation);
        }
    }

    GameObject SpawnFromPool(string entityName, Vector3 position, Quaternion rotation)
    {
        GameObject obj = ObjectPool.Instance.GetObject(entityName, position, rotation);

        if (obj != null)
        {
            InitializeSpawnedObject(obj, entityName);
            spawnedObjects.Add(obj);
            spawnCounts[entityName]++;

            if (debugMode)
            {
                Debug.Log($"Spawned {entityName} from pool at {position}");
            }
        }
        else
        {
            Debug.LogWarning($"Failed to spawn {entityName} from pool - pool may be empty or not exist");
        }

        return obj;
    }

    GameObject SpawnDirect(string entityName, Vector3 position, Quaternion rotation)
    {
        // Find the prefab in our pool configuration
        GameObject prefab = null;
        foreach (var pooledObject in entityPools)
        {
            if (pooledObject.prefab != null && pooledObject.prefab.name == entityName)
            {
                prefab = pooledObject.prefab;
                break;
            }
        }

        if (prefab == null)
        {
            Debug.LogError($"Prefab '{entityName}' not found in pool configuration!");
            return null;
        }

        GameObject obj = Instantiate(prefab, position, rotation);
        InitializeSpawnedObject(obj, entityName);
        spawnedObjects.Add(obj);

        if (!spawnCounts.ContainsKey(entityName))
        {
            spawnCounts[entityName] = 0;
        }
        spawnCounts[entityName]++;

        if (debugMode)
        {
            Debug.Log($"Spawned {entityName} directly at {position}");
        }

        return obj;
    }

    void InitializeSpawnedObject(GameObject obj, string entityName)
    {
        var guard = obj.GetComponent<GuardAI>();
        if (guard != null)
        {
            guard.Initialize();
            return;
        }

        var citizen = obj.GetComponent<Citizen>();
        if (citizen != null)
        {
            //citizen.Initialize();
            // You might want to set personality here if the spawner is responsible for it
            return;
        }

        var hunter = obj.GetComponent<VampireHunter>();
        if (hunter != null)
        {
            hunter.Initialize();
            return;
        }
    }

    void InitializeGuard(GameObject obj)
    {
        GuardAI guardAI = obj.GetComponent<GuardAI>();
        if (guardAI != null)
        {
            guardAI.Initialize();
        }
    }

    void InitializeCitizen(GameObject obj)
    {
        Citizen citizen = obj.GetComponent<Citizen>();
        if (citizen != null)
        {
            citizen.Initialize();
        }

        // Set random personality
        SetRandomPersonality(obj);
    }

    void InitializeVampireHunter(GameObject obj)
    {
        VampireHunter vampireHunter = obj.GetComponent<VampireHunter>();
        if (vampireHunter != null)
        {
            vampireHunter.Initialize();
        }
    }

    //void InitializeGenericEntity(GameObject obj)
    //{
    //    // Generic initialization for any entity
    //    AIBase aiBase = obj.GetComponent<AIBase>();
    //    if (aiBase != null)
    //    {
    //        aiBase.Initialize();
    //    }
    //}

    void SetRandomPersonality(GameObject obj)
    {
        Citizen citizen = obj.GetComponent<Citizen>();
        if (citizen != null)
        {
            // Random personality assignment
            var personalities = System.Enum.GetValues(typeof(CitizenPersonality)) as CitizenPersonality[];
            if (personalities != null && personalities.Length > 1) // Ensure more than 'None' exists
            {
                // Start from index 1 to skip 'None'
                CitizenPersonality randomPersonality = personalities[Random.Range(1, personalities.Length)];
                citizen.personality = randomPersonality;
            }
        }
    }

    public GameObject SpawnAtWaypoint(string entityName, Waypoint waypoint)
    {
        if (waypoint == null)
        {
            Debug.LogError("Waypoint is null!");
            return null;
        }

        Vector3 spawnPosition = waypoint.transform.position;
        return SpawnEntity(entityName, spawnPosition, waypoint.transform.rotation);
    }

    public GameObject SpawnAtRandomWaypoint(string entityName, List<Waypoint> waypoints)
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogError("No waypoints provided for random spawning!");
            return null;
        }

        Waypoint randomWaypoint = waypoints[Random.Range(0, waypoints.Count)];
        return SpawnAtWaypoint(entityName, randomWaypoint);
    }

    public GameObject SpawnInArea(string entityName, Vector3 center, float radius)
    {
        Vector3 spawnPosition = GetRandomPositionInArea(center, radius);
        return SpawnEntity(entityName, spawnPosition);
    }

    public List<GameObject> SpawnMultiple(string entityName, int count, Vector3 center, float radius)
    {
        List<GameObject> spawned = new List<GameObject>();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = SpawnInArea(entityName, center, radius);
            if (obj != null)
            {
                spawned.Add(obj);
            }
        }

        return spawned;
    }

    public List<GameObject> SpawnMultipleAtWaypoints(string entityName, List<Waypoint> waypoints, int count = -1)
    {
        List<GameObject> spawned = new List<GameObject>();

        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogError("No waypoints provided for spawning!");
            return spawned;
        }

        int spawnCount = count > 0 ? Mathf.Min(count, waypoints.Count) : waypoints.Count;

        // Shuffle waypoints to randomize spawn order
        List<Waypoint> shuffledWaypoints = new List<Waypoint>(waypoints);
        for (int i = 0; i < shuffledWaypoints.Count; i++)
        {
            Waypoint temp = shuffledWaypoints[i];
            int randomIndex = Random.Range(i, shuffledWaypoints.Count);
            shuffledWaypoints[i] = shuffledWaypoints[randomIndex];
            shuffledWaypoints[randomIndex] = temp;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject obj = SpawnAtWaypoint(entityName, shuffledWaypoints[i]);
            if (obj != null)
            {
                spawned.Add(obj);
            }
        }

        return spawned;
    }

    Vector3 GetRandomPositionInArea(Vector3 center, float radius)
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 randomPosition = center + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Check if position is valid (not inside obstacles)
            if (IsValidSpawnPosition(randomPosition))
            {
                return randomPosition;
            }
        }

        // If no valid position found, return center
        Debug.LogWarning($"Could not find valid spawn position in area. Using center position.");
        return center;
    }

    bool IsValidSpawnPosition(Vector3 position)
    {
        // Check if position is on valid ground
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out hit, 20f, spawnLayerMask))
        {
            // Check if there are no obstacles at the spawn position
            Collider[] colliders = Physics.OverlapSphere(position, 1f);
            foreach (var collider in colliders)
            {
                if (collider.CompareTag("Obstacle") || collider.CompareTag("Wall"))
                {
                    return false;
                }
            }
            return true;
        }

        return false;
    }

    public void ReturnToPool(GameObject obj)
    {
        if (useObjectPool && ObjectPool.Instance != null)
        {
            ObjectPool.Instance.ReturnObject(obj);
            spawnedObjects.Remove(obj);
        }
        else
        {
            Destroy(obj);
            spawnedObjects.Remove(obj);
        }
    }

    public void ReturnAllSpawned()
    {
        if (useObjectPool && ObjectPool.Instance != null)
        {
            ObjectPool.Instance.ReturnAllObjects();
        }
        else
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }

        spawnedObjects.Clear();
    }

    public void ClearSpawnStatistics()
    {
        spawnCounts.Clear();
    }

    // Statistics methods
    public int GetSpawnCount(string entityName)
    {
        return spawnCounts.ContainsKey(entityName) ? spawnCounts[entityName] : 0;
    }

    public int GetActiveSpawnedCount()
    {
        return spawnedObjects.Count;
    }

    public Dictionary<string, int> GetAllSpawnCounts()
    {
        return new Dictionary<string, int>(spawnCounts);
    }

    // Debug methods
    [ContextMenu("Log Spawn Statistics")]
    public void LogSpawnStatistics()
    {
        Debug.Log("=== Pooled Spawner Statistics ===");
        Debug.Log($"Active spawned objects: {GetActiveSpawnedCount()}");
        foreach (var kvp in spawnCounts)
        {
            Debug.Log($"Spawned {kvp.Key}: {kvp.Value} times");
        }
    }

    [ContextMenu("Return All Spawned")]
    public void ReturnAllSpawnedFromContext()
    {
        ReturnAllSpawned();
    }

    void OnDrawGizmos()
    {
        if (showSpawnPoints)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);

            // Draw spawn area
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawSphere(transform.position, spawnRadius);
        }
    }

    void OnDestroy()
    {
        // Clean up spawned objects
        ReturnAllSpawned();
    }
}