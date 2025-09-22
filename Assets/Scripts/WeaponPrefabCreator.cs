using UnityEngine;

/// <summary>
/// Utility script to create weapon prefabs for the vampire hunter
/// </summary>
public class WeaponPrefabCreator : MonoBehaviour
{
    [Header("Weapon Settings")]
    public float crossbowDamage = 12f;
    public float crossbowSpeed = 20f;
    public float crossbowLifetime = 3f;
    
    public float holyWaterDamage = 8f;
    public float holyWaterSpeed = 12f;
    public float holyWaterLifetime = 2f;
    
    public float garlicBombDamage = 15f;
    public float garlicBombRadius = 5f;
    public float garlicBombDuration = 4f;
    
    [ContextMenu("Create Crossbow Bolt Prefab")]
    public void CreateCrossbowBolt()
    {
        // Create the bolt GameObject
        GameObject bolt = new GameObject("CrossbowBolt");
        
        // Add visual representation
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.transform.SetParent(bolt.transform);
        visual.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
        visual.transform.localPosition = Vector3.zero;
        
        // Remove the collider from visual
        DestroyImmediate(visual.GetComponent<Collider>());
        
        // Add components to main object
        Rigidbody rb = bolt.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 0f;
        
        CapsuleCollider collider = bolt.AddComponent<CapsuleCollider>();
        collider.isTrigger = true;
        collider.radius = 0.05f;
        collider.height = 0.3f;
        
        Projectile projectile = bolt.AddComponent<Projectile>();
        projectile.speed = crossbowSpeed;
        projectile.damage = crossbowDamage;
        projectile.lifetime = crossbowLifetime;
        projectile.targetLayer = LayerMask.GetMask("Player");
        
        // Add trail renderer for visual effect
        TrailRenderer trail = bolt.AddComponent<TrailRenderer>();
        trail.time = 0.5f;
        trail.startWidth = 0.05f;
        trail.endWidth = 0.01f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = Color.yellow;
        trail.endColor = Color.red;
        
        // Set layer
        bolt.layer = LayerMask.NameToLayer("Default");
        
        Debug.Log("Crossbow bolt prefab created! Save it as a prefab.");
    }
    
    [ContextMenu("Create Holy Water Prefab")]
    public void CreateHolyWater()
    {
        // Create the holy water GameObject
        GameObject holyWater = new GameObject("HolyWater");
        
        // Add visual representation
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.transform.SetParent(holyWater.transform);
        visual.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
        visual.transform.localPosition = Vector3.zero;
        
        // Remove the collider from visual
        DestroyImmediate(visual.GetComponent<Collider>());
        
        // Add components to main object
        Rigidbody rb = holyWater.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.linearDamping = 1f;
        
        CapsuleCollider collider = holyWater.AddComponent<CapsuleCollider>();
        collider.isTrigger = true;
        collider.radius = 0.1f;
        collider.height = 0.3f;
        
        Projectile projectile = holyWater.AddComponent<Projectile>();
        projectile.speed = holyWaterSpeed;
        projectile.damage = holyWaterDamage;
        projectile.lifetime = holyWaterLifetime;
        projectile.targetLayer = LayerMask.GetMask("Player");
        
        // Add particle system for water effect
        GameObject particles = new GameObject("WaterParticles");
        particles.transform.SetParent(holyWater.transform);
        particles.transform.localPosition = Vector3.zero;
        
        ParticleSystem ps = particles.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1f;
        main.startSpeed = 2f;
        main.startSize = 0.1f;
        main.startColor = Color.cyan;
        
        var emission = ps.emission;
        emission.rateOverTime = 20f;
        
        // Set layer
        holyWater.layer = LayerMask.NameToLayer("Default");
        
        Debug.Log("Holy water prefab created! Save it as a prefab.");
    }
    
    [ContextMenu("Create Garlic Bomb Prefab")]
    public void CreateGarlicBomb()
    {
        // Create the garlic bomb GameObject
        GameObject garlicBomb = new GameObject("GarlicBomb");
        
        // Add visual representation
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(garlicBomb.transform);
        visual.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        visual.transform.localPosition = Vector3.zero;
        
        // Remove the collider from visual
        DestroyImmediate(visual.GetComponent<Collider>());
        
        // Add components to main object
        Rigidbody rb = garlicBomb.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.linearDamping = 2f;
        
        SphereCollider collider = garlicBomb.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.15f;
        
        AreaEffect areaEffect = garlicBomb.AddComponent<AreaEffect>();
        areaEffect.radius = garlicBombRadius;
        areaEffect.damage = garlicBombDamage;
        areaEffect.duration = garlicBombDuration;
        areaEffect.targetLayer = LayerMask.GetMask("Player");
        
        // Add particle system for garlic effect
        GameObject particles = new GameObject("GarlicParticles");
        particles.transform.SetParent(garlicBomb.transform);
        particles.transform.localPosition = Vector3.zero;
        
        ParticleSystem ps = particles.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 2f;
        main.startSpeed = 1f;
        main.startSize = 0.05f;
        main.startColor = Color.green;
        
        var emission = ps.emission;
        emission.rateOverTime = 10f;
        
        // Set layer
        garlicBomb.layer = LayerMask.NameToLayer("Default");
        
        Debug.Log("Garlic bomb prefab created! Save it as a prefab.");
    }
    
    [ContextMenu("Create All Weapon Prefabs")]
    public void CreateAllWeapons()
    {
        CreateCrossbowBolt();
        CreateHolyWater();
        CreateGarlicBomb();
        Debug.Log("All weapon prefabs created! Save them as prefabs.");
    }
} 