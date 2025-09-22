using UnityEngine;

[System.Serializable]
public class DifficultySettings
{
    [Header("Guard Scaling")]
    public int baseGuardCount = 3;
    public int guardsPerDay = 1;
    public float maxGuardCount = 15;

    [Header("Detection Ranges")]
    public float baseSpotDistance = 10f;
    public float spotDistanceIncreasePerDay = 1f;
    public float maxSpotDistance = 25f;

    public float baseHearingRange = 8f;
    public float hearingRangeIncreasePerDay = 0.5f;
    public float maxHearingRange = 20f;

    [Header("Citizen Behavior")]
    public float baseCitizenAlertness = 0.3f;
    public float alertnessIncreasePerDay = 0.1f;
    public float maxAlertness = 0.8f;

    public float baseCitizenSpeed = 3f;
    public float speedIncreasePerDay = 0.2f;
    public float maxCitizenSpeed = 6f;

    [Header("Vampire Hunter")]
    public int hunterSpawnDay = 5; // Hunter starts appearing on day 5
    public float hunterSpawnChance = 0.3f;
    public float hunterSpawnChanceIncreasePerDay = 0.1f;
    public float maxHunterSpawnChance = 0.8f;

    [Header("Environmental Hazards")]
    public int baseTrapCount = 2;
    public int trapsPerDay = 1;
    public float maxTrapCount = 8;

    [Header("Time Pressure")]
    public float baseNightDuration = 480f; // 8 minutes
    public float timeReductionPerDay = 30f; // 30 seconds less each day
    public float minNightDuration = 300f; // 5 minutes minimum

    [Header("Blood Requirements")]
    public float baseBloodGoal = 100f;
    public float bloodGoalIncreasePerDay = 25f;
    public float maxBloodGoal = 300f;
    
    [Header("Advanced Difficulty Features")]
    public bool enableDynamicDifficulty = true;
    public float difficultyBoostOnFailure = 0.2f;
    public float difficultyReductionOnSuccess = 0.1f;
    public int performanceTrackingDays = 3;
    
    [Header("Environmental Scaling")]
    public bool enableLightingProgression = true;
    public float baseLightIntensity = 1f;
    public float lightIncreasePerDay = 0.1f;
    public float maxLightIntensity = 2f;
    
    [Header("Citizen Social Behavior")]
    public bool enableSocialProgression = true;
    public float baseSocialRange = 5f;
    public float socialRangeIncreasePerDay = 0.5f;
    public float maxSocialRange = 15f;
}

public class DifficultyProgression : MonoBehaviour
{
    public static DifficultyProgression Instance { get; private set; }

    [Header("Difficulty Configuration")]
    public DifficultySettings settings = new DifficultySettings();

    [Header("Current Difficulty")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private float currentDifficultyMultiplier = 1f;

    [Header("Debug")]
    public bool debugMode = false;
    public bool logDifficultyChanges = true;

    // Events
    public System.Action<int> OnDayChanged;
    public System.Action<float> OnDifficultyChanged;

    // Cached difficulty values
    private int currentGuardCount;
    private float currentSpotDistance;
    private float currentHearingRange;
    private float currentCitizenAlertness;
    private float currentCitizenSpeed;
    private float currentHunterSpawnChance;
    private int currentTrapCount;
    private float currentNightDuration;
    private float currentBloodGoal;
    private float currentLightIntensity;
    private float currentSocialRange;
    
    public float GetCurrentBloodGoal()
    {
        return currentBloodGoal;
    }
    
    // Dynamic difficulty tracking
    private float[] recentPerformance;
    private int performanceIndex = 0;
    private float dynamicDifficultyModifier = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Initialize performance tracking array
        recentPerformance = new float[settings.performanceTrackingDays];
        for (int i = 0; i < recentPerformance.Length; i++)
        {
            recentPerformance[i] = 1f; // Neutral performance
        }

        // Get current day from GameManager
        if (GameManager.instance != null)
        {
            currentDay = GameManager.instance.currentDay;
        }

        CalculateDifficulty();
        ApplyDifficulty();

        if (debugMode)
        {
            Debug.Log($"DifficultyProgression initialized for Day {currentDay} with dynamic difficulty: {settings.enableDynamicDifficulty}");
        }
    }

    public void SetDay(int day)
    {
        if (day != currentDay)
        {
            currentDay = day;
            CalculateDifficulty();
            ApplyDifficulty();

            OnDayChanged?.Invoke(currentDay);

            if (logDifficultyChanges)
            {
                Debug.Log($"Difficulty updated for Day {currentDay}. Multiplier: {currentDifficultyMultiplier:F2}");
            }
        }
    }

    public void CalculateDifficulty()
    {
        // Use logarithmic scaling for more balanced progression
        float logScale = Mathf.Log10(1f + (currentDay - 1) * 0.5f);
        
        // Apply dynamic difficulty modifier
        float totalMultiplier = logScale * dynamicDifficultyModifier;
        currentDifficultyMultiplier = 1f + totalMultiplier;

        // Calculate specific values with logarithmic scaling
        currentGuardCount = Mathf.Min(
            settings.baseGuardCount + Mathf.RoundToInt(logScale * settings.guardsPerDay * 2f),
            (int)settings.maxGuardCount
        );

        currentSpotDistance = Mathf.Min(
            settings.baseSpotDistance + logScale * settings.spotDistanceIncreasePerDay * 3f,
            settings.maxSpotDistance
        );

        currentHearingRange = Mathf.Min(
            settings.baseHearingRange + logScale * settings.hearingRangeIncreasePerDay * 3f,
            settings.maxHearingRange
        );

        currentCitizenAlertness = Mathf.Min(
            settings.baseCitizenAlertness + logScale * settings.alertnessIncreasePerDay * 2f,
            settings.maxAlertness
        );

        currentCitizenSpeed = Mathf.Min(
            settings.baseCitizenSpeed + logScale * settings.speedIncreasePerDay * 2f,
            settings.maxCitizenSpeed
        );

        currentHunterSpawnChance = 0f;
        if (currentDay >= settings.hunterSpawnDay)
        {
            currentHunterSpawnChance = Mathf.Min(
                settings.hunterSpawnChance + (currentDay - settings.hunterSpawnDay) * settings.hunterSpawnChanceIncreasePerDay,
                settings.maxHunterSpawnChance
            );
        }

        currentTrapCount = Mathf.Min(
            settings.baseTrapCount + Mathf.RoundToInt(logScale * settings.trapsPerDay * 1.5f),
            (int)settings.maxTrapCount
        );

        // Night duration decreases more gradually
        float timePenalty = logScale * settings.timeReductionPerDay * 0.7f;
        currentNightDuration = Mathf.Max(
            settings.baseNightDuration - timePenalty,
            settings.minNightDuration
        );

        // Blood goal increases more gradually
        currentBloodGoal = Mathf.Min(
            settings.baseBloodGoal + logScale * settings.bloodGoalIncreasePerDay * 1.2f,
            settings.maxBloodGoal
        );
        
        // Calculate lighting progression
        if (settings.enableLightingProgression)
        {
            currentLightIntensity = Mathf.Min(
                settings.baseLightIntensity + (currentDay - 1) * settings.lightIncreasePerDay,
                settings.maxLightIntensity
            );
        }
        else
        {
            currentLightIntensity = settings.baseLightIntensity;
        }
        
        // Calculate social range progression
        if (settings.enableSocialProgression)
        {
            currentSocialRange = Mathf.Min(
                settings.baseSocialRange + (currentDay - 1) * settings.socialRangeIncreasePerDay,
                settings.maxSocialRange
            );
        }
        else
        {
            currentSocialRange = settings.baseSocialRange;
        }
        
        // Apply dynamic difficulty modifier
        if (settings.enableDynamicDifficulty)
        {
            UpdateDynamicDifficulty();
            
            // Apply modifier to key difficulty values
            currentDifficultyMultiplier *= dynamicDifficultyModifier;
            currentSpotDistance *= dynamicDifficultyModifier;
            currentHearingRange *= dynamicDifficultyModifier;
            currentCitizenAlertness = Mathf.Clamp01(currentCitizenAlertness * dynamicDifficultyModifier);
        }

        OnDifficultyChanged?.Invoke(currentDifficultyMultiplier);
    }

    public void ApplyDifficulty()
    {
        // Apply to GameManager
        if (GameManager.instance != null)
        {
            GameManager.instance.nightDuration = currentNightDuration;
            GameManager.instance.dayDuration = currentNightDuration;
            // Don't reset currentTime here as it might interrupt ongoing gameplay
            
            if (logDifficultyChanges)
            {
                Debug.Log($"[DifficultyProgression] Updated night duration to {currentNightDuration}s for day {currentDay}");
            }
        }

        // Apply to VampireStats
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            VampireStats vampireStats = playerObj.GetComponent<VampireStats>();
            if (vampireStats != null)
            {
                // GameManager now handles blood goal
                vampireStats.spotDistance = currentSpotDistance;
                
                if (logDifficultyChanges)
                {
                    Debug.Log($"[DifficultyProgression] Updated blood goal to {currentBloodGoal} and spot distance to {currentSpotDistance}");
                }
            }
        }

        // Apply to all guards
        ApplyToGuards();
        
        // Apply to all citizens  
        ApplyToCitizens();

        // Update spawner with new guard count
        ApplyToSpawners();

        // Update RandomEventManager with hunter spawn chance
        ApplyToEventManager();
        
        // Update trap spawners
        ApplyToTraps();
        
        // Update alertness manager
        ApplyToAlertnessManager();
        
        // Apply lighting changes
        ApplyToLighting();
        
        // Apply social behavior changes
        ApplyToSocialBehavior();

        if (debugMode)
        {
            LogCurrentDifficulty();
        }
    }
    
    private void ApplyToGuards()
    {
        GuardAI[] guards = FindObjectsOfType<GuardAI>();
        foreach (var guard in guards)
        {
            guard.viewDistance = currentSpotDistance;
            guard.soundDetectionRange = currentHearingRange;
            
            // Scale detection time by difficulty (harder = faster detection)
            float difficultyFactor = 1f / currentDifficultyMultiplier;
            guard.detectionTime = Mathf.Max(0.1f, guard.detectionTime * difficultyFactor);
            
            // Increase patrol speed slightly on higher days
            guard.patrolSpeed = Mathf.Min(guard.patrolSpeed * (1f + (currentDay - 1) * 0.05f), 6f);
        }
        
        if (guards.Length > 0 && logDifficultyChanges)
        {
            Debug.Log($"[DifficultyProgression] Updated {guards.Length} guards with view distance {currentSpotDistance} and hearing range {currentHearingRange}");
        }
    }
    
    private void ApplyToCitizens()
    {
        Citizen[] citizens = FindObjectsOfType<Citizen>();
        foreach (var citizen in citizens)
        {
            // Increase citizen view distance based on alertness
            float baseViewDistance = 35f; // Default citizen view distance
            citizen.viewDistance = baseViewDistance * (1f + currentCitizenAlertness);
            
            // Decrease detection time (faster detection) based on alertness
            float baseDetectionTime = 0.3f; // Default detection time
            citizen.detectionTime = baseDetectionTime * (1f - currentCitizenAlertness * 0.4f);
            
            // Increase movement speed
            UnityEngine.AI.NavMeshAgent agent = citizen.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = currentCitizenSpeed;
            }
            
            // Update social interaction range
            if (settings.enableSocialProgression)
            {
                citizen.socialInteractionRange = currentSocialRange;
            }
        }
        
        if (citizens.Length > 0 && logDifficultyChanges)
        {
            Debug.Log($"[DifficultyProgression] Updated {citizens.Length} citizens with alertness {currentCitizenAlertness:F2} and speed {currentCitizenSpeed:F1}");
        }
    }
    
    private void ApplyToSpawners()
    {
        // Find enhanced spawners and update guard counts
        EnhancedSpawner[] spawners = FindObjectsOfType<EnhancedSpawner>();
        foreach (var spawner in spawners)
        {
            spawner.AdjustGuardCount(currentGuardCount);
        }
        
        // Also check regular spawners
        Spawner[] regularSpawners = FindObjectsOfType<Spawner>();
        foreach (var spawner in regularSpawners)
        {
            spawner.SetTargetGuardCount(currentGuardCount);
        }
        
        if ((spawners.Length > 0 || regularSpawners.Length > 0) && logDifficultyChanges)
        {
            Debug.Log($"[DifficultyProgression] Updated spawners to target {currentGuardCount} guards");
        }
    }
    
    private void ApplyToEventManager()
    {
        RandomEventManager eventManager = FindObjectOfType<RandomEventManager>();
        if (eventManager != null)
        {
            // Update vampire hunter spawn chance
            eventManager.SetVampireHunterSpawnChance(currentHunterSpawnChance);
            
            // Increase general event frequency based on difficulty
            float eventFrequencyMultiplier = 1f + (currentDay - 1) * 0.1f;
            eventManager.SetEventFrequencyMultiplier(eventFrequencyMultiplier);
            
            if (logDifficultyChanges)
            {
                Debug.Log($"[DifficultyProgression] Updated event manager with hunter spawn chance {currentHunterSpawnChance:F2}");
            }
        }
    }
    
    private void ApplyToTraps()
    {
        // Find trap spawners and update trap counts
        GameObject[] trapSpawners = GameObject.FindGameObjectsWithTag("TrapSpawner");
        foreach (var spawner in trapSpawners)
        {
            // This would need to be implemented in trap spawning system
            // spawner.SetTargetTrapCount(currentTrapCount);
        }
        
        if (logDifficultyChanges && currentTrapCount > settings.baseTrapCount)
        {
            Debug.Log($"[DifficultyProgression] Should spawn {currentTrapCount} traps (not implemented yet)");
        }
    }
    
    private void ApplyToAlertnessManager()
    {
        GuardAlertnessManager alertnessManager = GuardAlertnessManager.instance;
        if (alertnessManager != null)
        {
            alertnessManager.SetDifficultyMultiplier(currentDifficultyMultiplier);
            
            if (logDifficultyChanges)
            {
                Debug.Log($"[DifficultyProgression] Updated alertness manager with difficulty multiplier {currentDifficultyMultiplier:F2}");
            }
        }
    }
    
    private void ApplyToLighting()
    {
        if (!settings.enableLightingProgression) return;
        
        // Update all scene lights
        Light[] sceneLights = FindObjectsOfType<Light>();
        foreach (var light in sceneLights)
        {
            // Only modify certain types of lights (exclude UI, flashlights, etc.)
            if (light.type == LightType.Directional || light.type == LightType.Point)
            {
                light.intensity = currentLightIntensity;
            }
        }
        
        // Update day/night lighting controller if it exists
        DayNightLightingController lightingController = FindObjectOfType<DayNightLightingController>();
        if (lightingController != null)
        {
            lightingController.SetDifficultyLightingMultiplier(currentLightIntensity);
        }
        
        if (logDifficultyChanges)
        {
            Debug.Log($"[DifficultyProgression] Updated lighting intensity to {currentLightIntensity:F2}");
        }
    }
    
    private void ApplyToSocialBehavior()
    {
        if (!settings.enableSocialProgression) return;
        
        // This is already handled in ApplyToCitizens(), but we could add more here
        // such as updating social interaction frequency, gossip speed, etc.
        
        if (logDifficultyChanges)
        {
            Debug.Log($"[DifficultyProgression] Updated social interaction range to {currentSocialRange:F1}");
        }
    }
    
    // Dynamic difficulty adjustment based on player performance
    private void UpdateDynamicDifficulty()
    {
        // Calculate average performance over recent days
        float averagePerformance = 0f;
        for (int i = 0; i < recentPerformance.Length; i++)
        {
            averagePerformance += recentPerformance[i];
        }
        averagePerformance /= recentPerformance.Length;
        
        // Adjust difficulty modifier based on performance
        if (averagePerformance < 0.5f) // Player struggling
        {
            dynamicDifficultyModifier = Mathf.Max(0.5f, dynamicDifficultyModifier - settings.difficultyReductionOnSuccess);
        }
        else if (averagePerformance > 1.2f) // Player doing too well
        {
            dynamicDifficultyModifier = Mathf.Min(2f, dynamicDifficultyModifier + settings.difficultyBoostOnFailure);
        }
        
        if (logDifficultyChanges && dynamicDifficultyModifier != 1f)
        {
            Debug.Log($"[DifficultyProgression] Dynamic difficulty modifier: {dynamicDifficultyModifier:F2} (avg performance: {averagePerformance:F2})");
        }
    }
    
    // Public methods for tracking player performance
    public void RecordDayPerformance(float bloodCollected, float bloodGoal, int timesSpotted, bool completedDay)
    {
        // Calculate performance score (0.0 = terrible, 1.0 = perfect, >1.0 = exceptional)
        float bloodPerformance = bloodCollected / bloodGoal;
        float stealthPerformance = Mathf.Max(0f, 1f - timesSpotted * 0.2f);
        float completionBonus = completedDay ? 0.2f : -0.5f;
        
        float totalPerformance = (bloodPerformance + stealthPerformance + completionBonus) / 2f;
        
        // Store in circular buffer
        recentPerformance[performanceIndex] = totalPerformance;
        performanceIndex = (performanceIndex + 1) % recentPerformance.Length;
        
        // Apply adaptive difficulty if enabled
        if (settings.enableDynamicDifficulty)
        {
            AdjustDynamicDifficulty();
        }
        
        if (logDifficultyChanges)
        {
            Debug.Log($"[DifficultyProgression] Day {currentDay} performance: {totalPerformance:F2} (blood: {bloodPerformance:F2}, stealth: {stealthPerformance:F2}, completed: {completedDay}). Dynamic modifier: {dynamicDifficultyModifier:F2}");
        }
    }
    
    void AdjustDynamicDifficulty()
    {
        // Calculate average performance over recent days
        float avgPerformance = 0f;
        int validDays = 0;
        
        for (int i = 0; i < recentPerformance.Length; i++)
        {
            if (recentPerformance[i] > 0f) // Valid performance recorded
            {
                avgPerformance += recentPerformance[i];
                validDays++;
            }
        }
        
        if (validDays == 0) return;
        avgPerformance /= validDays;
        
        // Adjust difficulty based on performance
        float oldModifier = dynamicDifficultyModifier;
        
        if (avgPerformance > 1.2f) // Player doing very well
        {
            dynamicDifficultyModifier = Mathf.Min(dynamicDifficultyModifier + settings.difficultyBoostOnFailure, 2f);
        }
        else if (avgPerformance < 0.6f) // Player struggling
        {
            dynamicDifficultyModifier = Mathf.Max(dynamicDifficultyModifier - settings.difficultyReductionOnSuccess, 0.5f);
        }
        else if (avgPerformance < 0.8f) // Player doing poorly
        {
            dynamicDifficultyModifier = Mathf.Max(dynamicDifficultyModifier - settings.difficultyReductionOnSuccess * 0.5f, 0.7f);
        }
        
        // Recalculate difficulty if modifier changed
        if (Mathf.Abs(oldModifier - dynamicDifficultyModifier) > 0.01f)
        {
            CalculateDifficulty();
            OnDifficultyChanged?.Invoke(dynamicDifficultyModifier);
            
            if (logDifficultyChanges)
            {
                Debug.Log($"[DifficultyProgression] Dynamic difficulty adjusted from {oldModifier:F2} to {dynamicDifficultyModifier:F2} based on performance: {avgPerformance:F2}");
            }
        }
    }

    void LogCurrentDifficulty()
    {
        Debug.Log($"=== Day {currentDay} Difficulty Settings ===");
        Debug.Log($"Guard Count: {currentGuardCount}");
        Debug.Log($"Spot Distance: {currentSpotDistance:F1}");
        Debug.Log($"Hearing Range: {currentHearingRange:F1}");
        Debug.Log($"Citizen Alertness: {currentCitizenAlertness:F2}");
        Debug.Log($"Citizen Speed: {currentCitizenSpeed:F1}");
        Debug.Log($"Hunter Spawn Chance: {currentHunterSpawnChance:F2}");
        Debug.Log($"Trap Count: {currentTrapCount}");
        Debug.Log($"Night Duration: {currentNightDuration:F0}s");
        Debug.Log($"Blood Goal: {currentBloodGoal:F0}");
        Debug.Log($"Light Intensity: {currentLightIntensity:F2}");
        Debug.Log($"Social Range: {currentSocialRange:F1}");
        Debug.Log($"Difficulty Multiplier: {currentDifficultyMultiplier:F2}");
        Debug.Log($"Dynamic Modifier: {dynamicDifficultyModifier:F2}");
    }

    // Public getters for other systems
    public int GetCurrentDay() => currentDay;
    public float GetDifficultyMultiplier() => currentDifficultyMultiplier;
    public int GetGuardCount() => currentGuardCount;
    public float GetSpotDistance() => currentSpotDistance;
    public float GetHearingRange() => currentHearingRange;
    public float GetCitizenAlertness() => currentCitizenAlertness;
    public float GetCitizenSpeed() => currentCitizenSpeed;
    public float GetHunterSpawnChance() => currentHunterSpawnChance;
    public int GetTrapCount() => currentTrapCount;
    public float GetNightDuration() => currentNightDuration;
    public float GetBloodGoal() => currentBloodGoal;
    public float GetLightIntensity() => currentLightIntensity;
    public float GetSocialRange() => currentSocialRange;
    public float GetDynamicDifficultyModifier() => dynamicDifficultyModifier;
    
    // Additional utility methods
    public bool ShouldSpawnVampireHunter() => UnityEngine.Random.value < currentHunterSpawnChance;
    public bool IsHardMode() => currentDifficultyMultiplier > 1.8f;
    public bool IsEasyMode() => currentDifficultyMultiplier < 0.8f;

    // Methods for dynamic difficulty adjustment
    public void IncreaseDifficulty(float amount = 0.1f)
    {
        currentDifficultyMultiplier += amount;
        CalculateDifficulty();
        ApplyDifficulty();
    }

    public void DecreaseDifficulty(float amount = 0.1f)
    {
        currentDifficultyMultiplier = Mathf.Max(1f, currentDifficultyMultiplier - amount);
        CalculateDifficulty();
        ApplyDifficulty();
    }

    // Context menu methods for testing
    [ContextMenu("Set Day 1")]
    public void SetDay1() => SetDay(1);

    [ContextMenu("Set Day 5")]
    public void SetDay5() => SetDay(5);

    [ContextMenu("Set Day 10")]
    public void SetDay10() => SetDay(10);

    [ContextMenu("Log Current Difficulty")]
    public void LogDifficulty() => LogCurrentDifficulty();

    [ContextMenu("Increase Difficulty")]
    public void IncreaseDifficultyFromContext() => IncreaseDifficulty();

    [ContextMenu("Decrease Difficulty")]
    public void DecreaseDifficultyFromContext() => DecreaseDifficulty();
    
    [ContextMenu("Record Good Performance")]
    public void RecordGoodPerformance() => RecordDayPerformance(120f, 100f, 0, true);
    
    [ContextMenu("Record Poor Performance")]
    public void RecordPoorPerformance() => RecordDayPerformance(50f, 100f, 5, false);
}

// All extension methods have been implemented in their respective target classes:
// - EnhancedSpawner.AdjustGuardCount(int targetCount)
// - Spawner.SetTargetGuardCount(int targetCount)
// - RandomEventManager.SetVampireHunterSpawnChance(float chance)
// - RandomEventManager.SetEventFrequencyMultiplier(float multiplier)
// - GuardAlertnessManager.SetDifficultyMultiplier(float multiplier)
// - DayNightLightingController.SetDifficultyLightingMultiplier(float multiplier)