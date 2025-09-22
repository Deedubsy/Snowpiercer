using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProjectilePoolConfig
{
    public GameObject projectilePrefab;
    public int initialPoolSize = 20;
    public int maxPoolSize = 100;
    public float autoReturnDelay = 5f;
    public bool useGravity = true;
    public bool useTrail = false;
    public Color trailColor = Color.white;
}

public class ProjectilePool : MonoBehaviour
{
    [Header("Projectile Pool Configuration")]
    public List<ProjectilePoolConfig> projectileConfigs = new List<ProjectilePoolConfig>();

    [Header("Effect Pool Configuration")]
    public List<PooledObject> effectPools = new List<PooledObject>();

    [Header("Settings")]
    public bool autoInitialize = true;
    public bool useObjectPool = true;

    [Header("Debug")]
    public bool debugMode = false;
    public bool logProjectileSpawns = false;

    // Statistics
    private Dictionary<string, int> projectileSpawnCounts = new Dictionary<string, int>();
    private Dictionary<string, int> effectSpawnCounts = new Dictionary<string, int>();

    void Start()
    {
        if (autoInitialize)
        {
            InitializeProjectilePools();
            InitializeEffectPools();
        }
    }

    void InitializeProjectilePools()
    {
        if (ObjectPool.Instance == null)
        {
            Debug.LogError("ObjectPool not found! Make sure ObjectPool is in the scene.");
            return;
        }

        foreach (var config in projectileConfigs)
        {
            if (config.projectilePrefab != null)
            {
                string poolName = config.projectilePrefab.name;
                ObjectPool.Instance.CreatePool(
                    poolName,
                    config.projectilePrefab,
                    config.initialPoolSize,
                    config.maxPoolSize,
                    true
                );

                projectileSpawnCounts[poolName] = 0;

                if (debugMode)
                {
                    Debug.Log($"Initialized projectile pool for {poolName}");
                }
            }
        }
    }

    void InitializeEffectPools()
    {
        if (ObjectPool.Instance == null) return;

        foreach (var pooledObject in effectPools)
        {
            if (pooledObject.prefab != null)
            {
                string poolName = pooledObject.prefab.name;
                ObjectPool.Instance.CreatePool(
                    poolName,
                    pooledObject.prefab,
                    pooledObject.initialPoolSize,
                    pooledObject.maxPoolSize,
                    pooledObject.expandable
                );

                effectSpawnCounts[poolName] = 0;

                if (debugMode)
                {
                    Debug.Log($"Initialized effect pool for {poolName}");
                }
            }
        }
    }

    public GameObject SpawnProjectile(string projectileName, Vector3 position, Vector3 direction, float speed = 10f)
    {
        if (!useObjectPool || ObjectPool.Instance == null)
        {
            Debug.LogError("ObjectPool not available for projectile spawning!");
            return null;
        }

        GameObject projectile = ObjectPool.Instance.GetObject(projectileName, position, Quaternion.LookRotation(direction));

        if (projectile != null)
        {
            InitializeProjectile(projectile, projectileName, direction, speed);
            projectileSpawnCounts[projectileName]++;

            if (logProjectileSpawns)
            {
                Debug.Log($"Spawned projectile {projectileName} at {position} with direction {direction}");
            }
        }

        return projectile;
    }

    void InitializeProjectile(GameObject projectile, string projectileName, Vector3 direction, float speed)
    {
        // Find the config for this projectile
        ProjectilePoolConfig config = GetProjectileConfig(projectileName);
        if (config == null) return;

        // Set up projectile component
        Projectile projectileComponent = projectile.GetComponent<Projectile>();
        if (projectileComponent != null)
        {
            projectileComponent.Initialize(direction, speed, projectileComponent.damage, LayerMask.GetMask("Player"), config.autoReturnDelay);
        }

        // Set up rigidbody if it exists
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = config.useGravity;
            rb.linearVelocity = direction * speed;
        }

        // Set up trail renderer if needed
        if (config.useTrail)
        {
            TrailRenderer trail = projectile.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = projectile.AddComponent<TrailRenderer>();
            }
            trail.material.color = config.trailColor;
            trail.startWidth = 0.1f;
            trail.endWidth = 0.01f;
            trail.time = 0.5f;
        }

        // Set up auto-return
        StartCoroutine(AutoReturnProjectile(projectile, config.autoReturnDelay));
    }

    ProjectilePoolConfig GetProjectileConfig(string projectileName)
    {
        foreach (var config in projectileConfigs)
        {
            if (config.projectilePrefab != null && config.projectilePrefab.name == projectileName)
            {
                return config;
            }
        }
        return null;
    }

    public System.Collections.IEnumerator AutoReturnProjectile(GameObject projectile, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (projectile != null && projectile.activeInHierarchy)
        {
            ReturnProjectile(projectile);
        }
    }

    public void ReturnProjectile(GameObject projectile)
    {
        if (useObjectPool && ObjectPool.Instance != null)
        {
            // Reset projectile state
            ResetProjectile(projectile);
            ObjectPool.Instance.ReturnObject(projectile);
        }
        else
        {
            Destroy(projectile);
        }
    }

    void ResetProjectile(GameObject projectile)
    {
        // Reset rigidbody
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Reset trail renderer
        TrailRenderer trail = projectile.GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear();
        }

        // Reset projectile component
        Projectile projectileComponent = projectile.GetComponent<Projectile>();
        if (projectileComponent != null)
        {
            projectileComponent.Reset();
        }
    }

    public GameObject SpawnEffect(string effectName, Vector3 position, Quaternion rotation = default)
    {
        if (!useObjectPool || ObjectPool.Instance == null)
        {
            Debug.LogError("ObjectPool not available for effect spawning!");
            return null;
        }

        GameObject effect = ObjectPool.Instance.GetObject(effectName, position, rotation);

        if (effect != null)
        {
            InitializeEffect(effect, effectName);
            effectSpawnCounts[effectName]++;

            if (debugMode)
            {
                Debug.Log($"Spawned effect {effectName} at {position}");
            }
        }

        return effect;
    }

    void InitializeEffect(GameObject effect, string effectName)
    {
        // Set up particle system if it exists
        ParticleSystem particles = effect.GetComponent<ParticleSystem>();
        if (particles != null)
        {
            particles.Play();

            // Auto-return when particles finish
            StartCoroutine(AutoReturnEffect(effect, particles.main.duration));
        }
        else
        {
            // Default auto-return for effects without particle systems
            StartCoroutine(AutoReturnEffect(effect, 2f));
        }

        // Set up audio if it exists
        AudioSource audio = effect.GetComponent<AudioSource>();
        if (audio != null)
        {
            audio.Play();
        }
    }

    System.Collections.IEnumerator AutoReturnEffect(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (effect != null && effect.activeInHierarchy)
        {
            ReturnEffect(effect);
        }
    }

    public void ReturnEffect(GameObject effect)
    {
        if (useObjectPool && ObjectPool.Instance != null)
        {
            // Reset effect state
            ResetEffect(effect);
            ObjectPool.Instance.ReturnObject(effect);
        }
        else
        {
            Destroy(effect);
        }
    }

    void ResetEffect(GameObject effect)
    {
        // Reset particle system
        ParticleSystem particles = effect.GetComponent<ParticleSystem>();
        if (particles != null)
        {
            particles.Stop();
            particles.Clear();
        }

        // Reset audio
        AudioSource audio = effect.GetComponent<AudioSource>();
        if (audio != null)
        {
            audio.Stop();
        }
    }

    // Specialized spawning methods for different projectile types
    public GameObject SpawnCrossbowBolt(Vector3 position, Vector3 direction, float speed = 15f)
    {
        return SpawnProjectile("CrossbowBolt", position, direction, speed);
    }

    public GameObject SpawnHolyWater(Vector3 position, Vector3 direction, float speed = 8f)
    {
        return SpawnProjectile("HolyWater", position, direction, speed);
    }

    public GameObject SpawnGarlicBomb(Vector3 position, Vector3 direction, float speed = 6f)
    {
        return SpawnProjectile("GarlicBomb", position, direction, speed);
    }

    public GameObject SpawnBloodEffect(Vector3 position)
    {
        return SpawnEffect("BloodEffect", position);
    }

    public GameObject SpawnExplosionEffect(Vector3 position)
    {
        return SpawnEffect("ExplosionEffect", position);
    }

    public GameObject SpawnSmokeEffect(Vector3 position)
    {
        return SpawnEffect("SmokeEffect", position);
    }

    // Batch spawning methods
    public List<GameObject> SpawnProjectileBurst(string projectileName, Vector3 position, Vector3 direction, int count, float spread = 15f)
    {
        List<GameObject> projectiles = new List<GameObject>();

        for (int i = 0; i < count; i++)
        {
            // Calculate spread direction
            Vector3 spreadDirection = Quaternion.Euler(0, Random.Range(-spread, spread), 0) * direction;
            GameObject projectile = SpawnProjectile(projectileName, position, spreadDirection);

            if (projectile != null)
            {
                projectiles.Add(projectile);
            }
        }

        return projectiles;
    }

    public void ReturnAllProjectiles()
    {
        if (useObjectPool && ObjectPool.Instance != null)
        {
            foreach (var config in projectileConfigs)
            {
                if (config.projectilePrefab != null)
                {
                    ObjectPool.Instance.ReturnAllObjects(config.projectilePrefab.name);
                }
            }
        }
    }

    public void ReturnAllEffects()
    {
        if (useObjectPool && ObjectPool.Instance != null)
        {
            foreach (var pooledObject in effectPools)
            {
                if (pooledObject.prefab != null)
                {
                    ObjectPool.Instance.ReturnAllObjects(pooledObject.prefab.name);
                }
            }
        }
    }

    // Statistics methods
    public int GetProjectileSpawnCount(string projectileName)
    {
        return projectileSpawnCounts.ContainsKey(projectileName) ? projectileSpawnCounts[projectileName] : 0;
    }

    public int GetEffectSpawnCount(string effectName)
    {
        return effectSpawnCounts.ContainsKey(effectName) ? effectSpawnCounts[effectName] : 0;
    }

    public Dictionary<string, int> GetAllProjectileSpawnCounts()
    {
        return new Dictionary<string, int>(projectileSpawnCounts);
    }

    public Dictionary<string, int> GetAllEffectSpawnCounts()
    {
        return new Dictionary<string, int>(effectSpawnCounts);
    }

    // Debug methods
    [ContextMenu("Log Projectile Statistics")]
    public void LogProjectileStatistics()
    {
        Debug.Log("=== Projectile Pool Statistics ===");
        foreach (var kvp in projectileSpawnCounts)
        {
            Debug.Log($"Projectile {kvp.Key}: {kvp.Value} spawned");
        }
        foreach (var kvp in effectSpawnCounts)
        {
            Debug.Log($"Effect {kvp.Key}: {kvp.Value} spawned");
        }
    }

    [ContextMenu("Return All Projectiles")]
    public void ReturnAllProjectilesFromContext()
    {
        ReturnAllProjectiles();
    }

    [ContextMenu("Return All Effects")]
    public void ReturnAllEffectsFromContext()
    {
        ReturnAllEffects();
    }

    void OnDestroy()
    {
        // Clean up all projectiles and effects
        ReturnAllProjectiles();
        ReturnAllEffects();
    }
}