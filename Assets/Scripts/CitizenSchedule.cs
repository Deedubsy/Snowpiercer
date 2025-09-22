using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ScheduleEntry
{
    public WaypointGroup destination;
    public float switchTime; // Time in seconds from night start
    public float duration; // How long to stay at this location (-1 for indefinite)
}

[CreateAssetMenu(fileName = "CitizenSchedule", menuName = "Vampire/CitizenSchedule", order = 2)]
public class CitizenSchedule : ScriptableObject
{
    public string scheduleName;
    public List<ScheduleEntry> scheduleEntries;
    public bool isSleeper = false; // If true, this citizen stays in houses and sleeps
    public float wakeUpChance = 0.3f; // Chance to wake up when door is opened
} 