using UnityEngine;
using System.Collections.Generic;

public class CitizenScheduleManager : MonoBehaviour
{
    public List<Citizen> citizens = new List<Citizen>();
    public float nightStartTime = 0f;
    private GameManager gameManager;

    void Start()
    {
        gameManager = GameManager.instance;
        if (gameManager != null)
        {
            nightStartTime = Time.time;
        }
    }

    void Update()
    {
        if (gameManager == null) return;
        float nightTime = Time.time - nightStartTime;
        
        foreach (var citizen in citizens)
        {
            if (citizen.schedule != null)
            {
                UpdateCitizenSchedule(citizen, nightTime);
            }
        }
    }

    void UpdateCitizenSchedule(Citizen citizen, float nightTime)
    {
        var schedule = citizen.schedule;
        if (schedule.scheduleEntries == null || schedule.scheduleEntries.Count == 0) return;

        // Find the current schedule entry based on time
        ScheduleEntry currentEntry = null;
        foreach (var entry in schedule.scheduleEntries)
        {
            if (nightTime >= entry.switchTime)
            {
                currentEntry = entry;
            }
        }

        if (currentEntry != null && currentEntry.destination != citizen.patrolGroup)
        {
            // Switch to new location
            citizen.SwitchToWaypointGroup(currentEntry.destination);
        }
    }

    public void OnHouseDoorOpened(WaypointGroup houseGroup, bool isStealthy)
    {
        // Find citizens in this house
        foreach (var citizen in citizens)
        {
            if (citizen.patrolGroup == houseGroup && citizen.schedule != null && citizen.schedule.isSleeper)
            {
                float wakeChance = citizen.schedule.wakeUpChance;
                if (!isStealthy) wakeChance *= 2f; // Double chance if not stealthy
                
                if (Random.value < wakeChance)
                {
                    citizen.WakeUp();
                }
            }
        }
    }

    public void RegisterCitizen(Citizen citizen)
    {
        if (!citizens.Contains(citizen))
            citizens.Add(citizen);
    }

    public void UnregisterCitizen(Citizen citizen)
    {
        citizens.Remove(citizen);
    }

    // Event notification methods
    public void OnEventStart(RandomEvent eventData)
    {
        // Handle event start effects on citizen schedules
        if (eventData.affectsCitizens)
        {
            foreach (var citizen in citizens)
            {
                // Citizens might change their behavior based on events
                if (eventData.citizensGoInside && citizen.schedule != null)
                {
                    // Force citizens to go to their house waypoint groups
                    var houseEntry = citizen.schedule.scheduleEntries.Find(e => e.destination.groupType == WaypointType.House);
                    if (houseEntry != null)
                    {
                        citizen.SwitchToWaypointGroup(houseEntry.destination);
                    }
                }
            }
        }
    }

    public void OnEventEnd(RandomEvent eventData)
    {
        // Handle event end effects on citizen schedules
        if (eventData.affectsCitizens)
        {
            foreach (var citizen in citizens)
            {
                // Citizens might return to normal behavior
                if (eventData.citizensGoInside)
                {
                    // Return to normal schedule
                    float nightTime = Time.time - nightStartTime;
                    UpdateCitizenSchedule(citizen, nightTime);
                }
            }
        }
    }
} 