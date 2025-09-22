using UnityEngine;
using System.Collections.Generic;

/*
ENHANCED SPAWNER SETUP GUIDE
============================

This guide will help you set up the EnhancedSpawner system to properly initialize all AI features
including personalities, visual/audio feedback, and proper integration with the vampire game systems.

SETUP STEPS:
============

1. REPLACE THE OLD SPAWNER:
   - Remove the old Spawner component from your scene
   - Add the EnhancedSpawner component instead
   - Assign all the required prefabs

2. PREFAB REQUIREMENTS:
   - All citizen prefabs must have the Citizen component
   - All guard prefabs must have the GuardAI component
   - Prefabs should have appropriate colliders and rigidbodies
   - Consider adding AudioSource components for audio feedback

3. PERSONALITY CONFIGURATION:
   - Adjust the personality distribution chances in the inspector
   - Fine-tune personality traits by rarity
   - Test different distributions for gameplay balance

4. VISUAL/AUDIO FEEDBACK:
   - Enable/disable visual feedback (lights) as needed
   - Enable/disable audio feedback (sounds) as needed
   - Configure guard communication and citizen social behavior

5. DEBUGGING:
   - Enable debug mode to see spawn summaries
   - Enable log spawn details for individual entity logging
   - Use the runtime spawn methods for testing

FEATURES:
=========

PERSONALITY SYSTEM:
- Random personality assignment with configurable chances
- Personality traits (bravery, curiosity, social) based on rarity
- Memory system for citizens
- Social behavior between citizens

VISUAL FEEDBACK:
- Dynamic lights for guards and citizens
- Color changes based on AI state
- Range and intensity configuration

AUDIO FEEDBACK:
- 3D spatial audio for guards and citizens
- Configurable volume and distance
- Audio source setup for state-based sounds

GUARD FEATURES:
- Patrol point assignment
- Guard communication system
- Visual state indicators
- Audio feedback for different states

CITIZEN FEATURES:
- Rarity-based blood value assignment
- Personality-driven behavior
- Social interaction system
- Memory of player encounters
- Schedule-based behavior

RUNTIME SPAWNING:
- SpawnGuard() method for dynamic guard spawning
- SpawnCitizen() method for dynamic citizen spawning
- Automatic waypoint group creation for spawned entities

INTEGRATION:
============

WITH GAME MANAGER:
- Works with the day/night cycle system
- Integrates with the alertness system
- Supports random events

WITH WAYPOINT SYSTEM:
- Uses existing waypoint groups
- Creates temporary groups for runtime spawning
- Supports all waypoint types

WITH AI SYSTEMS:
- Properly initializes GuardAI components
- Sets up Citizen components with all features
- Configures visual and audio feedback

EXAMPLE USAGE:
==============

// Spawn a guard at a specific position
EnhancedSpawner spawner = FindObjectOfType<EnhancedSpawner>();
GameObject guard = spawner.SpawnGuard(new Vector3(10, 0, 10), Quaternion.identity);

// Spawn a noble citizen
GameObject noble = spawner.SpawnCitizen(new Vector3(5, 0, 5), Quaternion.identity, CitizenRarity.Noble);

// Spawn a peasant with default settings
GameObject peasant = spawner.SpawnCitizen(new Vector3(0, 0, 0), Quaternion.identity);

TROUBLESHOOTING:
================

ISSUE: "Guard prefab missing GuardAI component!"
SOLUTION: Ensure your guard prefab has the GuardAI component attached.

ISSUE: "Citizen prefab missing Citizen component!"
SOLUTION: Ensure your citizen prefab has the Citizen component attached.

ISSUE: No visual feedback appearing
SOLUTION: Check that enableVisualFeedback is true and prefabs have Light components.

ISSUE: No audio feedback
SOLUTION: Check that enableAudioFeedback is true and prefabs have AudioSource components.

ISSUE: Personalities not being assigned
SOLUTION: Verify that personality distribution chances add up to 1.0 or less.

ISSUE: Entities not following patrol points
SOLUTION: Ensure waypoint groups have valid waypoints assigned.

PERFORMANCE CONSIDERATIONS:
==========================

- Limit the number of spawned entities for performance
- Disable visual/audio feedback if not needed
- Use object pooling for frequently spawned entities
- Consider LOD systems for distant entities

FUTURE ENHANCEMENTS:
====================

- Object pooling system for better performance
- More personality types and traits
- Advanced social networks between citizens
- Dynamic difficulty adjustment based on player performance
- Weather and time-based personality modifiers
- Faction system for different citizen groups
*/

public class EnhancedSpawnerSetupGuide : MonoBehaviour
{
    [Header("SP-014 Configuration")]
    public bool validateOnStart = false;
    public bool autoFixIssues = false;

    [Header("Results")]
    [SerializeField] private bool validationPassed = false;
    [SerializeField] private List<string> validationResults = new List<string>();

    void Start()
    {
        if (validateOnStart)
        {
            ValidateEnhancedSpawnerSetup();
        }
    }

    [ContextMenu("Validate Enhanced Spawner Setup")]
    public void ValidateEnhancedSpawnerSetup()
    {
        Debug.Log("=== SP-014: Enhanced Spawner Validation ===");

        validationResults.Clear();

        // Test 1: Check for EnhancedSpawner components
        bool hasSpawners = ValidateSpawnerComponents();

        // Test 2: Check prefab assignments
        bool prefabsValid = ValidatePrefabAssignments();

        // Test 3: Check waypoint group integration
        bool waypointsValid = ValidateWaypointIntegration();

        // Test 4: Check object pool configuration
        bool poolingValid = ValidateObjectPooling();

        // Test 5: Check district coverage
        bool districtsValid = ValidateDistrictCoverage();

        // Overall result
        validationPassed = hasSpawners && prefabsValid && waypointsValid && poolingValid && districtsValid;

        // Display results
        DisplayValidationResults();

        if (autoFixIssues && !validationPassed)
        {
            AutoFixCommonIssues();
        }
    }

    bool ValidateSpawnerComponents()
    {
        Debug.Log("--- Validating EnhancedSpawner components ---");

        EnhancedSpawner[] spawners = FindObjectsOfType<EnhancedSpawner>();

        if (spawners.Length == 0)
        {
            validationResults.Add("❌ No EnhancedSpawner components found in scene");
            return false;
        }

        validationResults.Add($"✅ Found {spawners.Length} EnhancedSpawner component(s)");

        // Check each spawner configuration
        bool allValid = true;
        for (int i = 0; i < spawners.Length; i++)
        {
            var spawner = spawners[i];
            string spawnerName = spawner.name;

            // Check basic configuration
            if (spawner.entityPools.Count == 0 &&
                (spawner.guardPrefab == null || spawner.peasantPrefab == null))
            {
                validationResults.Add($"❌ Spawner '{spawnerName}' has no prefabs assigned");
                allValid = false;
            }
            else
            {
                validationResults.Add($"✅ Spawner '{spawnerName}' has prefabs configured");
            }

            // Check pooling settings
            if (spawner.useObjectPool)
            {
                validationResults.Add($"✅ Spawner '{spawnerName}' configured for object pooling");
            }
            else
            {
                validationResults.Add($"⚠️ Spawner '{spawnerName}' not using object pooling - performance may suffer");
            }
        }

        return allValid;
    }

    bool ValidatePrefabAssignments()
    {
        Debug.Log("--- Validating prefab assignments ---");

        EnhancedSpawner[] spawners = FindObjectsOfType<EnhancedSpawner>();
        bool allValid = true;

        foreach (var spawner in spawners)
        {
            // Check guard prefab
            if (spawner.guardPrefab == null)
            {
                validationResults.Add($"❌ Spawner '{spawner.name}' missing guard prefab");
                allValid = false;
            }
            else if (spawner.guardPrefab.GetComponent<GuardAI>() == null)
            {
                validationResults.Add($"❌ Guard prefab '{spawner.guardPrefab.name}' missing GuardAI component");
                allValid = false;
            }
            else
            {
                validationResults.Add($"✅ Guard prefab '{spawner.guardPrefab.name}' valid");
            }

            // Check citizen prefabs
            GameObject[] citizenPrefabs = {
                spawner.peasantPrefab, spawner.merchantPrefab, spawner.priestPrefab,
                spawner.noblePrefab, spawner.royaltyPrefab
            };
            string[] citizenTypes = { "Peasant", "Merchant", "Priest", "Noble", "Royalty" };

            for (int i = 0; i < citizenPrefabs.Length; i++)
            {
                if (citizenPrefabs[i] == null)
                {
                    validationResults.Add($"⚠️ Spawner '{spawner.name}' missing {citizenTypes[i]} prefab");
                }
                else if (citizenPrefabs[i].GetComponent<Citizen>() == null)
                {
                    validationResults.Add($"❌ {citizenTypes[i]} prefab missing Citizen component");
                    allValid = false;
                }
                else
                {
                    validationResults.Add($"✅ {citizenTypes[i]} prefab '{citizenPrefabs[i].name}' valid");
                }
            }
        }

        return allValid;
    }

    bool ValidateWaypointIntegration()
    {
        Debug.Log("--- Validating waypoint integration ---");

        WaypointGroup[] waypointGroups = FindObjectsOfType<WaypointGroup>();

        if (waypointGroups.Length == 0)
        {
            validationResults.Add("❌ No WaypointGroup components found - spawning will fail");
            return false;
        }

        validationResults.Add($"✅ Found {waypointGroups.Length} waypoint groups");

        // Check for each entity type
        var groupTypes = System.Enum.GetValues(typeof(WaypointType));
        Dictionary<WaypointType, int> typeCount = new Dictionary<WaypointType, int>();

        foreach (WaypointGroup group in waypointGroups)
        {
            if (!typeCount.ContainsKey(group.groupType))
                typeCount[group.groupType] = 0;
            typeCount[group.groupType]++;

            // Validate group configuration
            if (group.waypoints == null || group.waypoints.Length == 0)
            {
                validationResults.Add($"❌ Waypoint group '{group.name}' has no waypoints assigned");
                return false;
            }

            if (group.maxEntities <= 0)
            {
                validationResults.Add($"⚠️ Waypoint group '{group.name}' has maxEntities = 0");
            }
        }

        // Report type distribution
        foreach (WaypointType type in groupTypes)
        {
            int count = typeCount.ContainsKey(type) ? typeCount[type] : 0;
            string status = count > 0 ? "✅" : "⚠️";
            validationResults.Add($"{status} {type} waypoint groups: {count}");
        }

        return true;
    }

    bool ValidateObjectPooling()
    {
        Debug.Log("--- Validating object pooling setup ---");

        if (ObjectPool.Instance == null)
        {
            validationResults.Add("❌ No ObjectPool instance found in scene");
            return false;
        }

        validationResults.Add("✅ ObjectPool instance found");

        // Check if spawners are configured for pooling
        EnhancedSpawner[] spawners = FindObjectsOfType<EnhancedSpawner>();
        int poolingEnabled = 0;

        foreach (var spawner in spawners)
        {
            if (spawner.useObjectPool)
                poolingEnabled++;
        }

        if (poolingEnabled == spawners.Length)
        {
            validationResults.Add($"✅ All {spawners.Length} spawners configured for object pooling");
        }
        else
        {
            validationResults.Add($"⚠️ Only {poolingEnabled}/{spawners.Length} spawners using object pooling");
        }

        return true;
    }

    bool ValidateDistrictCoverage()
    {
        Debug.Log("--- Validating district coverage ---");

        // Check if we have spawners covering key districts
        string[] requiredDistricts = { "Castle", "Market Square", "Residential Quarter", "Artisan Quarter" };
        List<string> missingDistricts = new List<string>();

        foreach (string district in requiredDistricts)
        {
            bool found = false;
            Transform districtTransform = FindDistrictByName(district);

            if (districtTransform != null)
            {
                // Check if there's a spawner or waypoint group in this district
                EnhancedSpawner spawnerInDistrict = districtTransform.GetComponentInChildren<EnhancedSpawner>();
                WaypointGroup waypointInDistrict = districtTransform.GetComponentInChildren<WaypointGroup>();

                if (spawnerInDistrict != null || waypointInDistrict != null)
                {
                    found = true;
                    validationResults.Add($"✅ District '{district}' has spawning coverage");
                }
            }

            if (!found)
            {
                missingDistricts.Add(district);
                validationResults.Add($"⚠️ District '{district}' missing spawning coverage");
            }
        }

        if (missingDistricts.Count == 0)
        {
            validationResults.Add("✅ All required districts have spawning coverage");
            return true;
        }
        else
        {
            validationResults.Add($"⚠️ {missingDistricts.Count} districts missing coverage");
            return false;
        }
    }

    Transform FindDistrictByName(string districtName)
    {
        // Search for district by name in the scene hierarchy
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains(districtName))
            {
                return obj.transform;
            }
        }
        return null;
    }

    void DisplayValidationResults()
    {
        Debug.Log("=== Enhanced Spawner Validation Results ===");

        foreach (string result in validationResults)
        {
            if (result.StartsWith("✅"))
                Debug.Log(result);
            else if (result.StartsWith("❌"))
                Debug.LogError(result);
            else if (result.StartsWith("⚠️"))
                Debug.LogWarning(result);
            else
                Debug.Log(result);
        }

        if (validationPassed)
        {
            Debug.Log("🎉 SP-014 Enhanced Spawner validation PASSED!");
        }
        else
        {
            Debug.LogWarning("⚠️ SP-014 Enhanced Spawner validation needs attention");
        }

        Debug.Log("=== Validation Complete ===");
    }

    [ContextMenu("Auto-Fix Common Issues")]
    public void AutoFixCommonIssues()
    {
        Debug.Log("--- Auto-fixing common spawner issues ---");

        int fixesApplied = 0;

        // Fix 1: Create ObjectPool if missing
        if (ObjectPool.Instance == null)
        {
            GameObject poolObj = new GameObject("ObjectPool");
            poolObj.AddComponent<ObjectPool>();
            fixesApplied++;
            Debug.Log("✅ Created missing ObjectPool instance");
        }

        // Fix 2: Enable object pooling on spawners
        EnhancedSpawner[] spawners = FindObjectsOfType<EnhancedSpawner>();
        foreach (var spawner in spawners)
        {
            if (!spawner.useObjectPool)
            {
                spawner.useObjectPool = true;
                spawner.autoInitializePools = true;
                fixesApplied++;
                Debug.Log($"✅ Enabled object pooling on spawner '{spawner.name}'");
            }
        }

        // Fix 3: Set reasonable maxEntities on waypoint groups with 0
        WaypointGroup[] groups = FindObjectsOfType<WaypointGroup>();
        foreach (var group in groups)
        {
            if (group.maxEntities <= 0)
            {
                group.maxEntities = group.groupType == WaypointType.Guard ? 3 : 5;
                fixesApplied++;
                Debug.Log($"✅ Set maxEntities to {group.maxEntities} on group '{group.name}'");
            }
        }

        Debug.Log($"✅ Auto-fix complete: {fixesApplied} issues resolved");

        // Re-run validation
        ValidateEnhancedSpawnerSetup();
    }

    [ContextMenu("Create District Spawner Setup")]
    public void CreateDistrictSpawnerSetup()
    {
        Debug.Log("--- Creating district spawner setup ---");

        string[] districts = { "Castle", "Market Square", "Residential Quarter", "Artisan Quarter" };

        foreach (string district in districts)
        {
            Transform districtTransform = FindDistrictByName(district);
            if (districtTransform != null)
            {
                // Check if spawner already exists
                if (districtTransform.GetComponentInChildren<EnhancedSpawner>() == null)
                {
                    GameObject spawnerObj = new GameObject($"{district} Spawner");
                    spawnerObj.transform.SetParent(districtTransform);
                    spawnerObj.transform.localPosition = Vector3.zero;

                    EnhancedSpawner spawner = spawnerObj.AddComponent<EnhancedSpawner>();
                    ConfigureSpawnerForDistrict(spawner, district);

                    Debug.Log($"✅ Created spawner for district '{district}'");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ District '{district}' not found in scene");
            }
        }
    }

    void ConfigureSpawnerForDistrict(EnhancedSpawner spawner, string district)
    {
        // Configure spawner settings based on district type
        spawner.useObjectPool = true;
        spawner.autoInitializePools = true;
        spawner.enableVisualFeedback = true;
        spawner.enableAudioFeedback = true;
        spawner.enableGuardCommunication = true;
        spawner.enableCitizenSocialBehavior = true;
        spawner.enableMemorySystem = true;
        spawner.debugMode = false;
        spawner.logSpawnDetails = false;

        // District-specific personality distributions
        switch (district)
        {
            case "Castle":
                spawner.cowardlyChance = 0.05f;  // Brave castle inhabitants
                spawner.normalChance = 0.3f;
                spawner.braveChance = 0.3f;
                spawner.curiousChance = 0.1f;
                spawner.socialChance = 0.2f;
                spawner.lonerChance = 0.05f;
                break;

            case "Market Square":
                spawner.cowardlyChance = 0.1f;   // Social marketplace
                spawner.normalChance = 0.3f;
                spawner.braveChance = 0.1f;
                spawner.curiousChance = 0.2f;
                spawner.socialChance = 0.25f;
                spawner.lonerChance = 0.05f;
                break;

            case "Residential Quarter":
                spawner.cowardlyChance = 0.2f;   // Normal citizens at home
                spawner.normalChance = 0.5f;
                spawner.braveChance = 0.1f;
                spawner.curiousChance = 0.1f;
                spawner.socialChance = 0.1f;
                spawner.lonerChance = 0.0f;
                break;

            case "Artisan Quarter":
                spawner.cowardlyChance = 0.1f;   // Curious craftspeople
                spawner.normalChance = 0.4f;
                spawner.braveChance = 0.1f;
                spawner.curiousChance = 0.3f;
                spawner.socialChance = 0.1f;
                spawner.lonerChance = 0.0f;
                break;
        }
    }

    [ContextMenu("Show Setup Instructions")]
    public void ShowSetupInstructions()
    {
        Debug.Log(@"
=== SP-014: Enhanced Spawner Setup Instructions ===

STEP 1: Ensure Required Components
☐ ObjectPool component in scene
☐ EnhancedSpawner components in each district
☐ WaypointGroup components with assigned waypoints

STEP 2: Configure Prefab References
☐ Assign Guard prefab (must have GuardAI component)
☐ Assign Citizen prefabs (Peasant, Merchant, Priest, Noble, Royalty)
☐ Verify all prefabs have required components

STEP 3: Configure Object Pooling
☐ Enable 'Use Object Pool' on all spawners
☐ Enable 'Auto Initialize Pools'
☐ Set appropriate pool sizes (Guards: 10-50, Citizens: 20-100)

STEP 4: Configure Waypoint Integration
☐ Create WaypointGroup for each entity type
☐ Assign waypoints to each group
☐ Set maxEntities > 0 on all groups
☐ Enable allowSharedWaypoints if needed

STEP 5: District-Specific Configuration
☐ Castle: Higher brave/noble personalities
☐ Market: Higher social/curious personalities
☐ Residential: Balanced normal personalities
☐ Artisan: Higher curious personalities

STEP 6: Performance Settings
☐ Enable Visual Feedback for debugging
☐ Enable Audio Feedback for immersion
☐ Enable Guard Communication for challenge
☐ Enable Citizen Social Behavior for realism
☐ Enable Memory System for persistence

VALIDATION: Run 'Validate Enhanced Spawner Setup' to verify configuration

TROUBLESHOOTING:
• No spawning → Check waypoint groups exist and have waypoints
• Performance issues → Verify object pooling enabled
• Missing entities → Check prefab assignments and maxEntities settings
• AI issues → Validate prefabs have required components (GuardAI, Citizen)
");
    }
} 