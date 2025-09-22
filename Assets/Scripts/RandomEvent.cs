using UnityEngine;

[CreateAssetMenu(fileName = "RandomEvent", menuName = "Vampire/RandomEvent", order = 4)]
public class RandomEvent : ScriptableObject
{
    [Header("Event Info")]
    public string eventName;
    public string description;
    public float duration = 300f; // Duration in seconds
    
    [Header("Timing")]
    public float minTimeToTrigger = 60f; // Minimum time from night start
    public float maxTimeToTrigger = 600f; // Maximum time from night start
    public float triggerChance = 0.3f; // Chance to trigger when time is right
    
    [Header("Effects")]
    public bool affectsCitizens = true;
    public bool affectsGuards = true;
    public GuardAlertnessLevel guardAlertnessChange = GuardAlertnessLevel.Normal;
    public float citizenSpeedMultiplier = 1f;
    public float guardSpeedMultiplier = 1f;
    public bool citizensGoInside = false;
    public bool increaseGuardPatrols = false;
    
    [Header("Special Effects")]
    public bool spawnVampireHunter = false;
    public bool createStorm = false;
    public bool triggerFestival = false;
    
    [Header("Audio/Visual")]
    public AudioClip eventAudio;
    public GameObject visualEffect;
} 