using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PooledObject
{
    public GameObject prefab;
    public int initialPoolSize = 10;
    public int maxPoolSize = 50;
    public bool expandable = true;
}

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [Header("Pool Configuration")]
    public List<PooledObject> pooledObjects = new List<PooledObject>();

    [Header("Debug")]
    public bool debugMode = false;
    public bool logPoolOperations = false;

    // Dictionary to store all pools
    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, PooledObject> poolConfigs = new Dictionary<string, PooledObject>();
    private Dictionary<GameObject, string> objectToPoolMap = new Dictionary<GameObject, string>();

    // Statistics
    private Dictionary<string, int> poolSizes = new Dictionary<string, int>();
    private Dictionary<string, int> activeObjects = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializePools()
    {
        foreach (var pooledObject in pooledObjects)
        {
            if (pooledObject.prefab != null)
            {
                string poolName = pooledObject.prefab.name;
                CreatePool(poolName, pooledObject.prefab, pooledObject.initialPoolSize, pooledObject.maxPoolSize, pooledObject.expandable);
                poolConfigs[poolName] = pooledObject;
            }
        }

        if (debugMode)
        {
            Debug.Log($"ObjectPool initialized with {pools.Count} pools");
        }
    }

    public void CreatePool(string poolName, GameObject prefab, int initialSize, int maxSize, bool expandable = true)
    {
        if (pools.ContainsKey(poolName))
        {
            if (debugMode)
            {
                Debug.LogWarning($"Pool '{poolName}' already exists. Skipping creation.");
            }
            return;
        }

        poolSizes[poolName] = initialSize;

        Queue<GameObject> pool = new Queue<GameObject>();

        // Pre-populate the pool
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = CreateNewObject(poolName, prefab);
            pool.Enqueue(obj);
        }

        pools[poolName] = pool;
        activeObjects[poolName] = 0;

        if (logPoolOperations)
        {
            Debug.Log($"Created pool '{poolName}' with {initialSize} objects");
        }
    }

    GameObject CreateNewObject(string poolName, GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);
        obj.name = $"{poolName}_Pooled_{poolSizes[poolName]}";
        obj.SetActive(false);

        // Store reference to pool
        objectToPoolMap[obj] = poolName;

        // Add PooledObject component if it doesn't exist
        PooledObjectComponent pooledComponent = obj.GetComponent<PooledObjectComponent>();
        if (pooledComponent == null)
        {
            pooledComponent = obj.AddComponent<PooledObjectComponent>();
        }
        pooledComponent.poolName = poolName;

        return obj;
    }

    public GameObject GetObject(string poolName, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(poolName))
        {
            Debug.LogError($"Pool '{poolName}' does not exist!");
            return null;
        }

        Queue<GameObject> pool = pools[poolName];
        GameObject obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            // Pool is empty, check if we can expand
            PooledObject config = poolConfigs[poolName];
            if (config.expandable && poolSizes[poolName] < config.maxPoolSize)
            {
                obj = CreateNewObject(poolName, config.prefab);
                poolSizes[poolName]++;

                if (logPoolOperations)
                {
                    Debug.Log($"Expanded pool '{poolName}' to {poolSizes[poolName]} objects");
                }
            }
            else
            {
                Debug.LogWarning($"Pool '{poolName}' is empty and cannot expand. Returning null.");
                return null;
            }
        }

        // Set position and rotation
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        // Update statistics
        activeObjects[poolName]++;

        // Notify the object it's been activated
        PooledObjectComponent pooledComponent = obj.GetComponent<PooledObjectComponent>();
        if (pooledComponent != null)
        {
            pooledComponent.OnActivated();
        }

        if (logPoolOperations)
        {
            Debug.Log($"Got object from pool '{poolName}'. Active: {activeObjects[poolName]}, Available: {pool.Count}");
        }

        return obj;
    }

    public GameObject GetObject(string poolName)
    {
        return GetObject(poolName, Vector3.zero, Quaternion.identity);
    }

    public void ReturnObject(GameObject obj)
    {
        if (obj == null) return;

        string poolName;
        if (objectToPoolMap.TryGetValue(obj, out poolName))
        {
            ReturnObjectToPool(obj, poolName);
        }
        else
        {
            // Object wasn't created by pool, destroy it
            Destroy(obj);
        }
    }

    void ReturnObjectToPool(GameObject obj, string poolName)
    {
        if (!pools.ContainsKey(poolName))
        {
            Debug.LogError($"Pool '{poolName}' does not exist!");
            return;
        }

        // Notify the object it's being deactivated
        PooledObjectComponent pooledComponent = obj.GetComponent<PooledObjectComponent>();
        if (pooledComponent != null)
        {
            pooledComponent.OnDeactivated();
        }

        // Reset object state
        obj.SetActive(false);
        obj.transform.SetParent(transform);

        // Return to pool
        pools[poolName].Enqueue(obj);
        activeObjects[poolName]--;

        if (logPoolOperations)
        {
            Debug.Log($"Returned object to pool '{poolName}'. Active: {activeObjects[poolName]}, Available: {pools[poolName].Count}");
        }
    }

    public void ReturnAllObjects(string poolName)
    {
        if (!pools.ContainsKey(poolName))
        {
            Debug.LogWarning($"Pool '{poolName}' does not exist!");
            return;
        }

        // Find all active objects from this pool and return them
        foreach (var kvp in objectToPoolMap)
        {
            if (kvp.Value == poolName && kvp.Key.activeInHierarchy)
            {
                ReturnObject(kvp.Key);
            }
        }
    }

    public void ReturnAllObjects()
    {
        foreach (var poolName in pools.Keys)
        {
            ReturnAllObjects(poolName);
        }
    }

    public void PrewarmPool(string poolName, int count)
    {
        if (!pools.ContainsKey(poolName))
        {
            Debug.LogError($"Pool '{poolName}' does not exist!");
            return;
        }

        PooledObject config = poolConfigs[poolName];
        Queue<GameObject> pool = pools[poolName];

        int currentSize = poolSizes[poolName];
        int targetSize = Mathf.Min(currentSize + count, config.maxPoolSize);
        int objectsToAdd = targetSize - currentSize;

        for (int i = 0; i < objectsToAdd; i++)
        {
            GameObject obj = CreateNewObject(poolName, config.prefab);
            pool.Enqueue(obj);
        }

        poolSizes[poolName] = targetSize;

        if (logPoolOperations)
        {
            Debug.Log($"Prewarmed pool '{poolName}' with {objectsToAdd} additional objects. Total: {targetSize}");
        }
    }

    public void ClearPool(string poolName)
    {
        if (!pools.ContainsKey(poolName))
        {
            Debug.LogWarning($"Pool '{poolName}' does not exist!");
            return;
        }

        // Return all active objects
        ReturnAllObjects(poolName);

        // Destroy all objects in the pool
        Queue<GameObject> pool = pools[poolName];
        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            objectToPoolMap.Remove(obj);
            Destroy(obj);
        }

        pools.Remove(poolName);
        poolSizes.Remove(poolName);
        activeObjects.Remove(poolName);

        if (logPoolOperations)
        {
            Debug.Log($"Cleared pool '{poolName}'");
        }
    }

    public void ClearAllPools()
    {
        List<string> poolNames = new List<string>(pools.Keys);
        foreach (string poolName in poolNames)
        {
            ClearPool(poolName);
        }
    }

    // Statistics methods
    public int GetPoolSize(string poolName)
    {
        return poolSizes.ContainsKey(poolName) ? poolSizes[poolName] : 0;
    }

    public int GetActiveObjects(string poolName)
    {
        return activeObjects.ContainsKey(poolName) ? activeObjects[poolName] : 0;
    }

    public int GetAvailableObjects(string poolName)
    {
        return pools.ContainsKey(poolName) ? pools[poolName].Count : 0;
    }

    public Dictionary<string, int> GetAllPoolSizes()
    {
        return new Dictionary<string, int>(poolSizes);
    }

    public Dictionary<string, int> GetAllActiveObjects()
    {
        return new Dictionary<string, int>(activeObjects);
    }

    // Debug methods
    [ContextMenu("Log Pool Statistics")]
    public void LogPoolStatistics()
    {
        Debug.Log("=== Object Pool Statistics ===");
        foreach (var poolName in pools.Keys)
        {
            Debug.Log($"Pool '{poolName}': Size={poolSizes[poolName]}, Active={activeObjects[poolName]}, Available={pools[poolName].Count}");
        }
    }

    [ContextMenu("Return All Objects")]
    public void ReturnAllObjectsFromContext()
    {
        ReturnAllObjects();
    }

    [ContextMenu("Clear All Pools")]
    public void ClearAllPoolsFromContext()
    {
        ClearAllPools();
    }

    void OnDestroy()
    {
        // Clean up when the pool is destroyed
        ClearAllPools();
    }
}

// Component to track pooled objects
public class PooledObjectComponent : MonoBehaviour
{
    public string poolName;

    public System.Action onActivated;
    public System.Action onDeactivated;

    public void OnActivated()
    {
        onActivated?.Invoke();
    }

    public void OnDeactivated()
    {
        onDeactivated?.Invoke();
    }

    // Auto-return to pool when disabled (optional)
    void OnDisable()
    {
        // Only auto-return if this is a pooled object
        if (!string.IsNullOrEmpty(poolName))
        {
            // Use a small delay to avoid issues with immediate re-enabling
            StartCoroutine(DelayedReturn());
        }
    }

    System.Collections.IEnumerator DelayedReturn()
    {
        yield return new WaitForEndOfFrame();

        // Only return if still disabled and not already returned
        if (!gameObject.activeInHierarchy && ObjectPool.Instance != null)
        {
            ObjectPool.Instance.ReturnObject(gameObject);
        }
    }
}