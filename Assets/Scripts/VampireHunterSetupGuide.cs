using UnityEngine;

/// <summary>
/// Vampire Hunter Setup Guide
/// 
/// This guide explains how to set up the vampire hunter system with all its components.
/// 
/// SETUP STEPS:
/// 
/// 1. Create Vampire Hunter Prefab:
///    - Create an empty GameObject named "VampireHunter"
///    - Add NavMeshAgent component
///    - Add Animator component (if you have animations)
///    - Add AudioSource component
///    - Add CapsuleCollider for physics
///    - Add the VampireHunter script
///    - Configure the script settings (health, damage, detection range, etc.)
///    - Set the layer to "Guard" or create a new "VampireHunter" layer
///    - Create a prefab from this GameObject
/// 
/// 2. Create Weapon Prefabs:
///    
///    Crossbow Bolt:
///    - Create a small GameObject (e.g., capsule or arrow model)
///    - Add Rigidbody component
///    - Add Collider component (set to trigger)
///    - Add the Projectile script
///    - Configure speed, damage, and lifetime
///    - Add visual effects (trail renderer, particle system)
///    - Create prefab
///    
///    Holy Water:
///    - Create a bottle/flask GameObject
///    - Add Rigidbody component
///    - Add Collider component (set to trigger)
///    - Add the Projectile script
///    - Configure for shorter range but area damage
///    - Add water splash effects
///    - Create prefab
///    
///    Garlic Bomb:
///    - Create a bomb/pouch GameObject
///    - Add Rigidbody component
///    - Add Collider component (set to trigger)
///    - Add the AreaEffect script
///    - Configure radius, damage, and duration
///    - Add explosion and area effect prefabs
///    - Create prefab
/// 
/// 3. Configure RandomEventManager:
///    - Assign the VampireHunter prefab to the "Vampire Hunter Prefab" field
///    - Ensure the "Vampire Hunter" event has spawnVampireHunter = true
/// 
/// 4. Configure VampireHunter Script:
///    - Assign weapon prefabs to crossbowPrefab, holyWaterPrefab, garlicBombPrefab
///    - Set playerLayer to include the Player layer
///    - Set obstacleLayer to include walls/obstacles
///    - Configure detection and attack ranges
///    - Set movement speeds and AI behavior
/// 
/// 5. Add Visual Effects (Optional):
///    - Create detection effect (particle system with yellow/red colors)
///    - Create attack effect (impact particles)
///    - Create death effect (explosion or fade out)
///    - Assign audio clips for detection, attack, death sounds
/// 
/// 6. Test the System:
///    - Trigger the "Vampire Hunter" random event
///    - Verify the hunter spawns and hunts the player
///    - Test all weapon types (crossbow, holy water, garlic bomb)
///    - Verify damage system works correctly
/// 
/// TROUBLESHOOTING:
/// - Ensure all prefabs have the correct components
/// - Check that layers are set up correctly
/// - Verify NavMesh is baked for the hunter to navigate
/// - Make sure the player has the PlayerHealth component
/// - Check console for any error messages
/// 
/// BALANCING:
/// - Adjust hunter health based on game progression
/// - Tune weapon damage and cooldowns
/// - Modify detection ranges for difficulty
/// - Adjust movement speeds for gameplay feel
/// </summary>
public class VampireHunterSetupGuide : MonoBehaviour
{
    [Header("Setup Status")]
    [SerializeField] private bool vampireHunterPrefabAssigned = false;
    [SerializeField] private bool weaponPrefabsAssigned = false;
    [SerializeField] private bool randomEventManagerFound = false;
    
    void Start()
    {
        CheckSetupStatus();
    }
    
    void CheckSetupStatus()
    {
        // Check RandomEventManager
        RandomEventManager eventManager = FindObjectOfType<RandomEventManager>();
        randomEventManagerFound = eventManager != null;
        
        if (randomEventManagerFound)
        {
            vampireHunterPrefabAssigned = eventManager.vampireHunterPrefab != null;
        }
        
        // Log setup status
        if (!randomEventManagerFound)
        {
            Debug.LogWarning("RandomEventManager not found! Please create one.");
        }
        else if (!vampireHunterPrefabAssigned)
        {
            Debug.LogWarning("Vampire Hunter prefab not assigned in RandomEventManager!");
        }
        else
        {
            Debug.Log("Vampire Hunter system ready!");
        }
    }
    
    [ContextMenu("Check Setup Status")]
    public void CheckSetup()
    {
        CheckSetupStatus();
    }
    
    [ContextMenu("Test Vampire Hunter Spawn")]
    public void TestSpawn()
    {
        RandomEventManager eventManager = FindObjectOfType<RandomEventManager>();
        if (eventManager != null)
        {
            eventManager.TriggerEventByName("Vampire Hunter");
        }
        else
        {
            Debug.LogError("RandomEventManager not found!");
        }
    }
} 