using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class Objective
{
    public string id;
    public string title;
    public string description;
    public ObjectiveType type;
    public int bloodReward;
    public int upgradePointReward;
    public float timeLimit; // 0 = no time limit
    public bool isOptional;
    public bool isCompleted;
    public bool isFailed;
    public float progress;
    public float targetValue;
    
    // Conditions
    public CitizenRarity targetRarity;
    public int targetCount;
    public string targetLocation;
    public bool requireStealth;
    public int maxDetections;
}

public enum ObjectiveType
{
    DrainSpecificRarity,    // Drain X citizens of specific rarity
    StealthDrain,          // Drain without being detected
    SpeedRun,              // Complete objectives within time limit
    AreaClear,             // Clear all citizens in specific area
    NoDamage,              // Complete night without taking damage
    ChainDrain,            // Drain X citizens within Y seconds
    AvoidGuards,           // Complete without alerting guards
    CollectExcessBlood,    // Collect X blood over the daily goal
    HuntVampireHunter,     // Defeat vampire hunter
    SabotageObjective      // Sabotage specific objects
}

public class DynamicObjectiveSystem : MonoBehaviour
{
    public static DynamicObjectiveSystem Instance { get; private set; }
    
    [Header("Objective Configuration")]
    public List<Objective> activeObjectives = new List<Objective>();
    public List<Objective> completedObjectives = new List<Objective>();
    public int maxActiveObjectives = 3;
    
    [Header("UI References")]
    public GameObject objectiveUI;
    public Transform objectiveListContainer;
    public GameObject objectivePrefab;
    
    [Header("Objective Templates")]
    private List<Objective> objectiveTemplates = new List<Objective>();
    
    // Events
    public Action<Objective> OnObjectiveCompleted;
    public Action<Objective> OnObjectiveFailed;
    public Action<Objective> OnObjectiveAdded;
    
    // Tracking
    private Dictionary<string, float> objectiveTimers = new Dictionary<string, float>();
    private int currentNightDetections = 0;
    private float lastDrainTime = 0f;
    private int chainDrainCount = 0;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeObjectiveTemplates();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeObjectiveTemplates()
    {
        // Stealth objectives
        objectiveTemplates.Add(new Objective
        {
            id = "stealth_master",
            title = "Shadow Predator",
            description = "Drain 3 citizens without being detected",
            type = ObjectiveType.StealthDrain,
            bloodReward = 50,
            upgradePointReward = 10,
            isOptional = true,
            targetCount = 3,
            requireStealth = true,
            maxDetections = 0
        });
        
        // Rarity hunt objectives
        objectiveTemplates.Add(new Objective
        {
            id = "noble_hunt",
            title = "Noble Blood",
            description = "Drain 2 Noble citizens",
            type = ObjectiveType.DrainSpecificRarity,
            bloodReward = 75,
            upgradePointReward = 15,
            isOptional = true,
            targetRarity = CitizenRarity.Noble,
            targetCount = 2
        });
        
        // Speed objectives
        objectiveTemplates.Add(new Objective
        {
            id = "speed_demon",
            title = "Swift Hunter",
            description = "Collect 150 blood in under 3 minutes",
            type = ObjectiveType.SpeedRun,
            bloodReward = 100,
            upgradePointReward = 20,
            timeLimit = 180f,
            isOptional = true,
            targetValue = 150f
        });
        
        // Chain objectives
        objectiveTemplates.Add(new Objective
        {
            id = "feeding_frenzy",
            title = "Feeding Frenzy",
            description = "Drain 5 citizens within 60 seconds",
            type = ObjectiveType.ChainDrain,
            bloodReward = 80,
            upgradePointReward = 15,
            isOptional = true,
            targetCount = 5,
            timeLimit = 60f
        });
        
        // Perfect run objectives
        objectiveTemplates.Add(new Objective
        {
            id = "perfect_night",
            title = "Perfect Night",
            description = "Complete the night without taking damage",
            type = ObjectiveType.NoDamage,
            bloodReward = 100,
            upgradePointReward = 25,
            isOptional = true
        });
        
        // Excess blood objectives
        objectiveTemplates.Add(new Objective
        {
            id = "blood_hoarder",
            title = "Blood Hoarder",
            description = "Collect 50 blood over the daily requirement",
            type = ObjectiveType.CollectExcessBlood,
            bloodReward = 0, // No blood reward since you're already collecting excess
            upgradePointReward = 30,
            isOptional = true,
            targetValue = 50f
        });
    }
    
    void Start()
    {
        // Subscribe to game events
        if (GameManager.instance != null)
        {
            GameManager.instance.OnSundown += OnNightStart;
            GameManager.instance.OnSunrise += OnNightEnd;
        }
    }
    
    void Update()
    {
        // Update objective timers
        List<string> expiredTimers = new List<string>();
        
        foreach (var kvp in objectiveTimers)
        {
            objectiveTimers[kvp.Key] -= Time.deltaTime;
            if (objectiveTimers[kvp.Key] <= 0)
            {
                expiredTimers.Add(kvp.Key);
                
                var objective = activeObjectives.Find(o => o.id == kvp.Key);
                if (objective != null && !objective.isCompleted)
                {
                    FailObjective(objective);
                }
            }
        }
        
        // Clean up expired timers
        foreach (string id in expiredTimers)
        {
            objectiveTimers.Remove(id);
        }
        
        // Update chain drain timer
        if (chainDrainCount > 0 && Time.time - lastDrainTime > 60f)
        {
            chainDrainCount = 0;
        }
    }
    
    void OnNightStart()
    {
        // Clear previous night's objectives
        activeObjectives.Clear();
        objectiveTimers.Clear();
        currentNightDetections = 0;
        chainDrainCount = 0;
        
        // Generate new objectives for the night
        GenerateNightObjectives();
    }
    
    void OnNightEnd()
    {
        // Check for any incomplete objectives
        foreach (var objective in activeObjectives)
        {
            if (!objective.isCompleted && !objective.isFailed)
            {
                FailObjective(objective);
            }
        }
    }
    
    void GenerateNightObjectives()
    {
        // Shuffle available templates
        List<Objective> availableTemplates = new List<Objective>(objectiveTemplates);
        
        // Pick random objectives based on current day and difficulty
        int objectiveCount = Mathf.Min(maxActiveObjectives, 1 + (GameManager.instance.currentDay / 3));
        
        for (int i = 0; i < objectiveCount && availableTemplates.Count > 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableTemplates.Count);
            Objective template = availableTemplates[randomIndex];
            availableTemplates.RemoveAt(randomIndex);
            
            // Create a copy of the template
            Objective newObjective = new Objective
            {
                id = template.id + "_" + System.Guid.NewGuid().ToString().Substring(0, 8),
                title = template.title,
                description = template.description,
                type = template.type,
                bloodReward = template.bloodReward,
                upgradePointReward = template.upgradePointReward,
                timeLimit = template.timeLimit,
                isOptional = template.isOptional,
                targetRarity = template.targetRarity,
                targetCount = template.targetCount,
                targetLocation = template.targetLocation,
                requireStealth = template.requireStealth,
                maxDetections = template.maxDetections,
                targetValue = template.targetValue
            };
            
            // Add timer if needed
            if (newObjective.timeLimit > 0)
            {
                objectiveTimers[newObjective.id] = newObjective.timeLimit;
            }
            
            activeObjectives.Add(newObjective);
            OnObjectiveAdded?.Invoke(newObjective);
        }
        
        Debug.Log($"Generated {activeObjectives.Count} objectives for Night {GameManager.instance.currentDay}");
    }
    
    public void OnCitizenDrained(Citizen citizen)
    {
        // Update chain drain tracking
        if (Time.time - lastDrainTime <= 60f)
        {
            chainDrainCount++;
        }
        else
        {
            chainDrainCount = 1;
        }
        lastDrainTime = Time.time;
        
        // Check objectives
        foreach (var objective in activeObjectives)
        {
            if (objective.isCompleted || objective.isFailed) continue;
            
            switch (objective.type)
            {
                case ObjectiveType.DrainSpecificRarity:
                    if (citizen.rarity == objective.targetRarity)
                    {
                        objective.progress++;
                        if (objective.progress >= objective.targetCount)
                        {
                            CompleteObjective(objective);
                        }
                    }
                    break;
                    
                case ObjectiveType.StealthDrain:
                    if (currentNightDetections == 0)
                    {
                        objective.progress++;
                        if (objective.progress >= objective.targetCount)
                        {
                            CompleteObjective(objective);
                        }
                    }
                    break;
                    
                case ObjectiveType.ChainDrain:
                    if (chainDrainCount >= objective.targetCount)
                    {
                        CompleteObjective(objective);
                    }
                    break;
            }
        }
    }
    
    public void OnPlayerDetected()
    {
        currentNightDetections++;
        
        // Check stealth objectives
        foreach (var objective in activeObjectives)
        {
            if (objective.requireStealth && currentNightDetections > objective.maxDetections)
            {
                FailObjective(objective);
            }
        }
    }
    
    public void OnBloodCollected(float amount)
    {
        foreach (var objective in activeObjectives)
        {
            if (objective.isCompleted || objective.isFailed) continue;
            
            switch (objective.type)
            {
                case ObjectiveType.SpeedRun:
                    objective.progress += amount;
                    if (objective.progress >= objective.targetValue)
                    {
                        CompleteObjective(objective);
                    }
                    break;
                    
                case ObjectiveType.CollectExcessBlood:
                    float excess = (GameManager.instance.currentBlood + GameManager.instance.bloodCarryOver) - GameManager.instance.dailyBloodGoal;
                    if (excess >= objective.targetValue)
                    {
                        CompleteObjective(objective);
                    }
                    break;
            }
        }
    }
    
    public void OnPlayerDamaged()
    {
        // Fail no damage objectives
        foreach (var objective in activeObjectives)
        {
            if (objective.type == ObjectiveType.NoDamage)
            {
                FailObjective(objective);
            }
        }
    }
    
    void CompleteObjective(Objective objective)
    {
        if (objective.isCompleted) return;
        
        objective.isCompleted = true;
        objective.progress = objective.targetValue > 0 ? objective.targetValue : objective.targetCount;
        
        // Grant rewards
        if (GameManager.instance != null)
        {
            GameManager.instance.AddBlood(objective.bloodReward);
        }
        
        if (PermanentUpgradeSystem.Instance != null && objective.upgradePointReward > 0)
        {
            PermanentUpgradeSystem.Instance.AddBloodPoints(objective.upgradePointReward);
        }
        
        completedObjectives.Add(objective);
        OnObjectiveCompleted?.Invoke(objective);
        
        Debug.Log($"Objective completed: {objective.title}! Rewards: {objective.bloodReward} blood, {objective.upgradePointReward} upgrade points");
    }
    
    void FailObjective(Objective objective)
    {
        if (objective.isFailed || objective.isCompleted) return;
        
        objective.isFailed = true;
        OnObjectiveFailed?.Invoke(objective);
        
        Debug.Log($"Objective failed: {objective.title}");
    }
    
    public List<Objective> GetActiveObjectives()
    {
        return activeObjectives.FindAll(o => !o.isCompleted && !o.isFailed);
    }
}