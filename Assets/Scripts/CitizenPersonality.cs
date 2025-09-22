using UnityEngine;

public enum CitizenPersonality
{
    Cowardly,    // Runs away from danger, easily scared
    Normal,      // Standard behavior
    Brave,       // Investigates threats, less likely to run
    Curious,     // Investigates unusual things, longer detection time
    Social,      // Interacts with other citizens frequently
    Loner        // Avoids other citizens, prefers solitude
}

[System.Serializable]
public class MemoryEntry
{
    public enum MemoryType
    {
        PlayerSighting,
        Noise,
        Light,
        SocialInteraction,
        Threat,
        UnusualEvent,
        DangerousEvent
    }

    public MemoryType type;
    public Vector3 location;
    public float timestamp;
    public float importance; // 0-1, how important this memory is
    public string description;

    public MemoryEntry(MemoryType type, Vector3 location, float importance, string description)
    {
        this.type = type;
        this.location = location;
        this.timestamp = Time.time;
        this.importance = importance;
        this.description = description;
    }

    public bool IsExpired(float decayTime)
    {
        return Time.time - timestamp > decayTime;
    }

    public float GetAge()
    {
        return Time.time - timestamp;
    }
}