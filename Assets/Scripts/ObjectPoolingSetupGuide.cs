using UnityEngine;

/*
 * OBJECT POOLING SYSTEM SETUP GUIDE
 * =================================
 * 
 * This system provides efficient object spawning and management for the vampire game.
 * It includes three main components:
 * 
 * 1. ObjectPool - Generic object pooling system
 * 2. PooledSpawner - Specialized spawner for entities (guards, citizens, etc.)
 * 3. ProjectilePool - Specialized pool for projectiles and effects
 * 
 * SETUP INSTRUCTIONS:
 * ===================
 * 
 * STEP 1: Create ObjectPool GameObject
 * ------------------------------------
 * 1. Create an empty GameObject in your scene
 * 2. Name it "ObjectPool"
 * 3. Add the ObjectPool component to it
 * 4. Configure the pooled objects list with your prefabs
 * 5. Set initial pool sizes and max pool sizes
 * 
 * STEP 2: Set up PooledSpawner
 * -----------------------------
 * 1. Create an empty GameObject in your scene
 * 2. Name it "PooledSpawner"
 * 3. Add the PooledSpawner component to it
 * 4. Configure entity pools with your prefabs
 * 5. Set spawn settings and debug options
 * 
 * STEP 3: Set up ProjectilePool
 * ------------------------------
 * 1. Create an empty GameObject in your scene
 * 2. Name it "ProjectilePool"
 * 3. Add the ProjectilePool component to it
 * 4. Configure projectile and effect pools
 * 5. Set auto-return delays and trail settings
 * 
 * STEP 4: Configure Prefabs
 * --------------------------
 * 1. Ensure all prefabs have unique names (used as pool identifiers)
 * 2. Add PooledObjectComponent to prefabs that need auto-return functionality
 * 3. Configure any specific initialization requirements
 * 
 * STEP 5: Integration with Existing Systems
 * -----------------------------------------
 * 1. Update EnhancedSpawner to use object pooling (already done)
 * 2. Update VampireHunter to use ProjectilePool for weapons
 * 3. Update any other spawners to use the pool system
 * 
 * USAGE EXAMPLES:
 * ===============
 * 
 * Spawning Entities:
 * ------------------
 * // Using PooledSpawner
 * PooledSpawner spawner = FindObjectOfType<PooledSpawner>();
 * GameObject guard = spawner.SpawnEntity("Guard", position, rotation);
 * GameObject citizen = spawner.SpawnAtWaypoint("Citizen", waypoint);
 * 
 * // Using ObjectPool directly
 * GameObject obj = ObjectPool.Instance.GetObject("Guard", position, rotation);
 * ObjectPool.Instance.ReturnObject(obj);
 * 
 * Spawning Projectiles:
 * ---------------------
 * ProjectilePool projectilePool = FindObjectOfType<ProjectilePool>();
 * GameObject bolt = projectilePool.SpawnCrossbowBolt(position, direction, speed);
 * GameObject effect = projectilePool.SpawnBloodEffect(position);
 * 
 * Batch Spawning:
 * ---------------
 * List<GameObject> guards = spawner.SpawnMultiple("Guard", 5, center, radius);
 * List<GameObject> bolts = projectilePool.SpawnProjectileBurst("CrossbowBolt", position, direction, 3, 15f);
 * 
 * PERFORMANCE BENEFITS:
 * ====================
 * - Reduces garbage collection
 * - Faster object creation/destruction
 * - Better memory management
 * - Improved frame rate stability
 * 
 * DEBUGGING:
 * ==========
 * - Enable debugMode on any pool component
 * - Use context menu options to log statistics
 * - Monitor pool sizes and active object counts
 * - Check for pool exhaustion warnings
 * 
 * TROUBLESHOOTING:
 * ================
 * - "Pool not found" errors: Check prefab names match pool names
 * - "Pool empty" warnings: Increase initial pool size or max pool size
 * - Objects not returning: Check PooledObjectComponent configuration
 * - Performance issues: Monitor pool sizes and adjust as needed
 */

public class ObjectPoolingSetupGuide : MonoBehaviour
{
    [Header("Setup Verification")]
    public bool verifyObjectPool = true;
    public bool verifyPooledSpawner = true;
    public bool verifyProjectilePool = true;
    
    [Header("Auto Setup")]
    public bool autoCreateMissingComponents = true;
    public bool autoConfigurePools = true;

    void Start()
    {
        if (verifyObjectPool)
        {
            VerifyObjectPoolSetup();
        }
        
        if (verifyPooledSpawner)
        {
            VerifyPooledSpawnerSetup();
        }
        
        if (verifyProjectilePool)
        {
            VerifyProjectilePoolSetup();
        }
        
        if (autoCreateMissingComponents)
        {
            CreateMissingComponents();
        }
        
        if (autoConfigurePools)
        {
            AutoConfigurePools();
        }
    }

    void VerifyObjectPoolSetup()
    {
        ObjectPool objectPool = FindObjectOfType<ObjectPool>();
        if (objectPool == null)
        {
            Debug.LogWarning("ObjectPool not found in scene! Creating one...");
            if (autoCreateMissingComponents)
            {
                CreateObjectPool();
            }
        }
        else
        {
            Debug.Log("ObjectPool found and ready.");
        }
    }

    void VerifyPooledSpawnerSetup()
    {
        PooledSpawner pooledSpawner = FindObjectOfType<PooledSpawner>();
        if (pooledSpawner == null)
        {
            Debug.LogWarning("PooledSpawner not found in scene! Creating one...");
            if (autoCreateMissingComponents)
            {
                CreatePooledSpawner();
            }
        }
        else
        {
            Debug.Log("PooledSpawner found and ready.");
        }
    }

    void VerifyProjectilePoolSetup()
    {
        ProjectilePool projectilePool = FindObjectOfType<ProjectilePool>();
        if (projectilePool == null)
        {
            Debug.LogWarning("ProjectilePool not found in scene! Creating one...");
            if (autoCreateMissingComponents)
            {
                CreateProjectilePool();
            }
        }
        else
        {
            Debug.Log("ProjectilePool found and ready.");
        }
    }

    void CreateMissingComponents()
    {
        if (FindObjectOfType<ObjectPool>() == null)
        {
            CreateObjectPool();
        }
        
        if (FindObjectOfType<PooledSpawner>() == null)
        {
            CreatePooledSpawner();
        }
        
        if (FindObjectOfType<ProjectilePool>() == null)
        {
            CreateProjectilePool();
        }
    }

    void CreateObjectPool()
    {
        GameObject objectPoolGO = new GameObject("ObjectPool");
        ObjectPool objectPool = objectPoolGO.AddComponent<ObjectPool>();
        
        // Set default configuration
        objectPool.debugMode = false;
        objectPool.logPoolOperations = false;
        
        Debug.Log("Created ObjectPool GameObject");
    }

    void CreatePooledSpawner()
    {
        GameObject spawnerGO = new GameObject("PooledSpawner");
        PooledSpawner spawner = spawnerGO.AddComponent<PooledSpawner>();
        
        // Set default configuration
        spawner.useObjectPool = true;
        spawner.autoInitializePools = true;
        spawner.debugMode = false;
        
        Debug.Log("Created PooledSpawner GameObject");
    }

    void CreateProjectilePool()
    {
        GameObject projectilePoolGO = new GameObject("ProjectilePool");
        ProjectilePool projectilePool = projectilePoolGO.AddComponent<ProjectilePool>();
        
        // Set default configuration
        projectilePool.autoInitialize = true;
        projectilePool.useObjectPool = true;
        projectilePool.debugMode = false;
        
        Debug.Log("Created ProjectilePool GameObject");
    }

    void AutoConfigurePools()
    {
        // This would auto-configure pools based on prefabs found in the project
        // For now, just log that manual configuration is needed
        Debug.Log("Auto-configure pools: Manual configuration required. See setup guide above.");
    }

    [ContextMenu("Verify All Pool Systems")]
    public void VerifyAllSystems()
    {
        VerifyObjectPoolSetup();
        VerifyPooledSpawnerSetup();
        VerifyProjectilePoolSetup();
        
        Debug.Log("=== Pool System Verification Complete ===");
    }

    [ContextMenu("Create All Missing Components")]
    public void CreateAllMissingComponents()
    {
        CreateMissingComponents();
        Debug.Log("=== Created all missing pool components ===");
    }

    [ContextMenu("Log Pool Statistics")]
    public void LogAllPoolStatistics()
    {
        ObjectPool objectPool = FindObjectOfType<ObjectPool>();
        if (objectPool != null)
        {
            objectPool.LogPoolStatistics();
        }
        
        PooledSpawner spawner = FindObjectOfType<PooledSpawner>();
        if (spawner != null)
        {
            spawner.LogSpawnStatistics();
        }
        
        ProjectilePool projectilePool = FindObjectOfType<ProjectilePool>();
        if (projectilePool != null)
        {
            projectilePool.LogProjectileStatistics();
        }
    }

    [ContextMenu("Return All Objects to Pools")]
    public void ReturnAllObjectsToPools()
    {
        ObjectPool objectPool = FindObjectOfType<ObjectPool>();
        if (objectPool != null)
        {
            objectPool.ReturnAllObjects();
        }
        
        PooledSpawner spawner = FindObjectOfType<PooledSpawner>();
        if (spawner != null)
        {
            spawner.ReturnAllSpawned();
        }
        
        ProjectilePool projectilePool = FindObjectOfType<ProjectilePool>();
        if (projectilePool != null)
        {
            projectilePool.ReturnAllProjectiles();
            projectilePool.ReturnAllEffects();
        }
        
        Debug.Log("=== Returned all objects to pools ===");
    }
} 