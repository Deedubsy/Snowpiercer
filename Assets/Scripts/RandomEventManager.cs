using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RandomEventManager : MonoBehaviour
{
    [Header("Event Configuration")]
    public List<RandomEvent> availableEvents = new List<RandomEvent>();
    public int maxActiveEvents = 2;

    [Header("Special Event Prefabs")]
    public GameObject vampireHunterPrefab;

    [Header("Debug")]
    public bool enableRandomEvents = true;
    public bool debugMode = false;

    private List<ActiveEvent> activeEvents = new List<ActiveEvent>();
    private GameManager gameManager;
    private GuardAlertnessManager alertnessManager;
    private CitizenScheduleManager scheduleManager;
    private AudioSource audioSource;

    private float nightStartTime;
    private float lastEventCheck;
    private float eventCheckInterval = 30f; // Check for new events every 30 seconds

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        alertnessManager = FindObjectOfType<GuardAlertnessManager>();
        scheduleManager = FindObjectOfType<CitizenScheduleManager>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        nightStartTime = Time.time;
        lastEventCheck = Time.time;
    }

    void Update()
    {
        if (!enableRandomEvents || gameManager == null) return;

        // Update active events
        UpdateActiveEvents();

        // Check for new events
        if (Time.time - lastEventCheck >= eventCheckInterval)
        {
            CheckForNewEvents();
            lastEventCheck = Time.time;
        }
    }

    void UpdateActiveEvents()
    {
        for (int i = activeEvents.Count - 1; i >= 0; i--)
        {
            activeEvents[i].UpdateDuration(Time.deltaTime);

            if (!activeEvents[i].isActive)
            {
                EndEvent(activeEvents[i]);
                activeEvents.RemoveAt(i);
            }
        }
    }

    void CheckForNewEvents()
    {
        if (activeEvents.Count >= maxActiveEvents) return;

        float timeSinceNightStart = Time.time - nightStartTime;

        foreach (RandomEvent randomEvent in availableEvents)
        {
            if (ShouldTriggerEvent(randomEvent, timeSinceNightStart))
            {
                TriggerEvent(randomEvent);
                break; // Only trigger one event per check
            }
        }
    }

    bool ShouldTriggerEvent(RandomEvent randomEvent, float timeSinceNightStart)
    {
        // Check if event is already active
        if (activeEvents.Any(e => e.eventData == randomEvent && e.isActive))
            return false;

        // Check timing constraints
        if (timeSinceNightStart < randomEvent.minTimeToTrigger ||
            timeSinceNightStart > randomEvent.maxTimeToTrigger)
            return false;

        // Check random chance
        return Random.Range(0f, 1f) < randomEvent.triggerChance;
    }

    void TriggerEvent(RandomEvent randomEvent)
    {
        ActiveEvent activeEvent = new ActiveEvent(randomEvent, Time.time);
        activeEvents.Add(activeEvent);

        ApplyEventEffects(activeEvent);

        // Play audio if available
        if (randomEvent.eventAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(randomEvent.eventAudio);
        }

        // Spawn visual effect if available
        if (randomEvent.visualEffect != null)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            Instantiate(randomEvent.visualEffect, spawnPosition, Quaternion.identity);
        }

        if (debugMode)
        {
            Debug.Log($"Random Event Triggered: {randomEvent.eventName} - {randomEvent.description}");
        }

        // Notify other systems
        NotifyEventStart(randomEvent);
    }

    void ApplyEventEffects(ActiveEvent activeEvent)
    {
        RandomEvent eventData = activeEvent.eventData;

        // Apply guard alertness change
        if (eventData.affectsGuards && alertnessManager != null)
        {
            //alertnessManager.SetAlertnessLevel(eventData.guardAlertnessChange);
        }

        // Apply citizen effects
        if (eventData.affectsCitizens)
        {
            ApplyCitizenEffects(eventData);
        }

        // Apply guard effects
        if (eventData.affectsGuards)
        {
            ApplyGuardEffects(eventData);
        }

        // Handle special effects
        HandleSpecialEffects(eventData);
    }

    void ApplyCitizenEffects(RandomEvent eventData)
    {
        Citizen[] citizens = FindObjectsOfType<Citizen>();

        foreach (Citizen citizen in citizens)
        {
            if (eventData.citizensGoInside)
            {
                citizen.GoInside();
            }

            // Apply speed multiplier
            if (eventData.citizenSpeedMultiplier != 1f)
            {
                citizen.ApplySpeedMultiplier(eventData.citizenSpeedMultiplier);
            }
        }
    }

    void ApplyGuardEffects(RandomEvent eventData)
    {
        GuardAI[] guards = FindObjectsOfType<GuardAI>();

        foreach (GuardAI guard in guards)
        {
            // Apply speed multiplier
            if (eventData.guardSpeedMultiplier != 1f)
            {
                guard.ApplySpeedMultiplier(eventData.guardSpeedMultiplier);
            }

            // Increase patrol frequency
            if (eventData.increaseGuardPatrols)
            {
                guard.IncreasePatrolFrequency();
            }
        }
    }

    void HandleSpecialEffects(RandomEvent eventData)
    {
        if (eventData.spawnVampireHunter)
        {
            SpawnVampireHunter();
        }

        if (eventData.createStorm)
        {
            CreateStorm();
        }

        if (eventData.triggerFestival)
        {
            TriggerFestival();
        }
    }

    void EndEvent(ActiveEvent activeEvent)
    {
        RandomEvent eventData = activeEvent.eventData;

        // Revert effects
        if (eventData.affectsCitizens)
        {
            RevertCitizenEffects(eventData);
        }

        if (eventData.affectsGuards)
        {
            RevertGuardEffects(eventData);
        }

        if (debugMode)
        {
            Debug.Log($"Random Event Ended: {eventData.eventName}");
        }

        // Notify other systems
        NotifyEventEnd(eventData);
    }

    void RevertCitizenEffects(RandomEvent eventData)
    {
        Citizen[] citizens = FindObjectsOfType<Citizen>();

        foreach (Citizen citizen in citizens)
        {
            if (eventData.citizensGoInside)
            {
                citizen.LeaveHouse();
            }

            if (eventData.citizenSpeedMultiplier != 1f)
            {
                citizen.ResetSpeedMultiplier();
            }
        }
    }

    void RevertGuardEffects(RandomEvent eventData)
    {
        GuardAI[] guards = FindObjectsOfType<GuardAI>();

        foreach (GuardAI guard in guards)
        {
            if (eventData.guardSpeedMultiplier != 1f)
            {
                guard.ResetSpeedMultiplier();
            }

            if (eventData.increaseGuardPatrols)
            {
                guard.ResetPatrolFrequency();
            }
        }
    }

    Vector3 GetRandomSpawnPosition()
    {
        // Get a random position in the game area
        float x = Random.Range(-50f, 50f);
        float z = Random.Range(-50f, 50f);
        float y = 0f;

        // Raycast to find ground level
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(x, 100f, z), Vector3.down, out hit, 200f))
        {
            y = hit.point.y;
        }

        return new Vector3(x, y, z);
    }

    void SpawnVampireHunter()
    {
        if (vampireHunterPrefab == null)
        {
            Debug.LogWarning("Vampire Hunter prefab not assigned!");
            return;
        }

        Vector3 spawnPosition = GetRandomSpawnPosition();

        // Find a position away from the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 playerPos = player.transform.position;
            Vector3 direction = (spawnPosition - playerPos).normalized;
            spawnPosition = playerPos + direction * 20f; // Spawn 20 units away from player

            // Ensure spawn position is on ground
            RaycastHit hit;
            if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out hit, 20f))
            {
                spawnPosition = hit.point;
            }
        }

        GameObject hunter = Instantiate(vampireHunterPrefab, spawnPosition, Quaternion.identity);
        Debug.Log($"Vampire Hunter spawned at {spawnPosition}!");

        // Set up the hunter
        VampireHunter vampireHunter = hunter.GetComponent<VampireHunter>();
        if (vampireHunter != null)
        {
            // Configure hunter based on current game state
            vampireHunter.health = 100f + (GameManager.instance != null ? GameManager.instance.currentDay * 10 : 0);
            vampireHunter.maxHealth = vampireHunter.health;
        }
    }

    void CreateStorm()
    {
        // TODO: Implement storm effects
        Debug.Log("Storm created!");
    }

    void TriggerFestival()
    {
        // TODO: Implement festival effects
        Debug.Log("Festival triggered!");
    }

    void NotifyEventStart(RandomEvent eventData)
    {
        // Notify other systems about event start
        if (scheduleManager != null)
        {
            scheduleManager.OnEventStart(eventData);
        }
    }

    void NotifyEventEnd(RandomEvent eventData)
    {
        // Notify other systems about event end
        if (scheduleManager != null)
        {
            scheduleManager.OnEventEnd(eventData);
        }
    }

    // Public methods for external triggering
    public void TriggerEventByName(string eventName)
    {
        RandomEvent eventToTrigger = availableEvents.Find(e => e.eventName == eventName);
        if (eventToTrigger != null)
        {
            TriggerEvent(eventToTrigger);
        }
    }
    
    // Methods for DifficultyProgression integration
    public void SetVampireHunterSpawnChance(float chance)
    {
        // Find vampire hunter events and update their trigger chance
        foreach (var eventData in availableEvents)
        {
            if (eventData.spawnVampireHunter)
            {
                eventData.triggerChance = chance;
            }
        }
        
        Debug.Log($"[RandomEventManager] Updated vampire hunter spawn chance to {chance:F2}");
    }
    
    public void SetEventFrequencyMultiplier(float multiplier)
    {
        // Adjust event check interval based on multiplier
        float baseEventCheckInterval = 30f;
        eventCheckInterval = baseEventCheckInterval / multiplier;
        
        // Ensure minimum check interval
        eventCheckInterval = Mathf.Max(5f, eventCheckInterval);
        
        Debug.Log($"[RandomEventManager] Updated event frequency multiplier to {multiplier:F2} (check interval: {eventCheckInterval:F1}s)");
    }

    public List<ActiveEvent> GetActiveEvents()
    {
        return activeEvents.Where(e => e.isActive).ToList();
    }

    public bool IsEventActive(string eventName)
    {
        return activeEvents.Any(e => e.eventData.eventName == eventName && e.isActive);
    }
}