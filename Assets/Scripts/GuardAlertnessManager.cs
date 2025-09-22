using UnityEngine;
using System.Collections.Generic;

public class GuardAlertnessManager : MonoBehaviour
{
    public GuardAlertness alertnessConfig;
    public GuardAlertnessLevel currentAlertness = GuardAlertnessLevel.Normal;
    public List<GuardAI> allGuards = new List<GuardAI>();
    
    [Header("Alertness Triggers")]
    public int missingCitizensThreshold = 3; // Alert after X citizens missing
    public int trapTriggersThreshold = 2; // Alert after X traps triggered
    public float alertnessDecayTime = 60f; // Time for alertness to decay
    
    private int missingCitizens = 0;
    private int trapTriggers = 0;
    private float lastAlertTime = 0f;
    
    public static GuardAlertnessManager instance;
    public static GuardAlertnessManager Instance => instance;
    
    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    
    void Update()
    {
        // Decay alertness over time
        if (currentAlertness > GuardAlertnessLevel.Normal && Time.time - lastAlertTime > alertnessDecayTime)
        {
            DecreaseAlertness();
        }
    }
    
    public void RegisterGuard(GuardAI guard)
    {
        if (!allGuards.Contains(guard))
            allGuards.Add(guard);
    }
    
    public void UnregisterGuard(GuardAI guard)
    {
        allGuards.Remove(guard);
    }
    
    public List<GuardAI> GetAllGuards()
    {
        // Clean up any null references
        allGuards.RemoveAll(g => g == null);
        return allGuards;
    }
    
    public void SetAlertnessLevel(GuardAlertnessLevel level)
    {
        currentAlertness = level;
        lastAlertTime = Time.time;
        
        // Apply alertness to all guards
        foreach (var guard in allGuards)
        {
            if (guard != null)
            {
                guard.currentAlertness = level;
            }
        }
    }
    
    public void OnCitizenMissing()
    {
        missingCitizens++;
        Debug.Log($"Citizen missing! Total: {missingCitizens}");
        
        if (missingCitizens >= missingCitizensThreshold)
        {
            IncreaseAlertness(GuardAlertnessLevel.Suspicious);
        }
    }
    
    public void OnTrapTriggered()
    {
        trapTriggers++;
        Debug.Log($"Trap triggered! Total: {trapTriggers}");
        
        if (trapTriggers >= trapTriggersThreshold)
        {
            IncreaseAlertness(GuardAlertnessLevel.Alert);
        }
    }
    
    public void OnPlayerSpotted(Vector3 playerPosition)
    {
        IncreaseAlertness(GuardAlertnessLevel.Panic);
        AlertAllGuards(playerPosition);
    }
    
    public void OnLoudNoise(Vector3 noisePosition)
    {
        IncreaseAlertness(GuardAlertnessLevel.Suspicious);
        AlertNearbyGuards(noisePosition, 20f);
    }
    
    public void IncreaseAlertness(GuardAlertnessLevel newLevel)
    {
        if (newLevel > currentAlertness)
        {
            currentAlertness = newLevel;
            lastAlertTime = Time.time;
            UpdateAllGuards();
            Debug.Log($"Guard alertness increased to: {newLevel}");
        }
    }
    
    public void DecreaseAlertness()
    {
        if (currentAlertness > GuardAlertnessLevel.Normal)
        {
            currentAlertness--;
            UpdateAllGuards();
            Debug.Log($"Guard alertness decreased to: {currentAlertness}");
        }
    }
    
    void UpdateAllGuards()
    {
        foreach (var guard in allGuards)
        {
            guard.UpdateAlertness(currentAlertness);
        }
    }
    
    void AlertAllGuards(Vector3 playerPosition)
    {
        foreach (var guard in allGuards)
        {
            guard.Alert(playerPosition);
        }
    }
    
    void AlertNearbyGuards(Vector3 position, float radius)
    {
        foreach (var guard in allGuards)
        {
            float distance = Vector3.Distance(guard.transform.position, position);
            if (distance <= radius)
            {
                guard.Alert(position);
            }
        }
    }
    
    public GuardAlertness.AlertnessLevel GetCurrentAlertnessLevel()
    {
        return alertnessConfig != null ? alertnessConfig.GetLevel(currentAlertness) : null;
    }
    
    // Method for DifficultyProgression integration
    public void SetDifficultyMultiplier(float multiplier)
    {
        // Apply difficulty multiplier to alertness system
        // Higher multiplier = faster alertness increase, slower decay
        alertnessDecayTime = 60f / multiplier; // Base decay time adjusted by difficulty
        
        // Adjust thresholds based on difficulty
        int baseMissingThreshold = 3;
        int baseTrapThreshold = 2;
        
        missingCitizensThreshold = Mathf.Max(1, Mathf.RoundToInt(baseMissingThreshold / multiplier));
        trapTriggersThreshold = Mathf.Max(1, Mathf.RoundToInt(baseTrapThreshold / multiplier));
        
        Debug.Log($"[GuardAlertnessManager] Updated difficulty multiplier to {multiplier:F2}. Decay time: {alertnessDecayTime:F1}s, Missing threshold: {missingCitizensThreshold}, Trap threshold: {trapTriggersThreshold}");
    }
} 