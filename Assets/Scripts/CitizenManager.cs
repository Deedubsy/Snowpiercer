using System.Collections.Generic;
using UnityEngine;

public class CitizenManager : MonoBehaviour
{
    public static CitizenManager Instance { get; private set; }
    
    private List<Citizen> allCitizens = new List<Citizen>();
    private Dictionary<Citizen, Vector3> citizenPositions = new Dictionary<Citizen, Vector3>();
    
    [Header("Performance Settings")]
    public int maxCitizensToCheck = 20;
    public float maxInteractionDistance = 15f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
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
    
    public void RegisterCitizen(Citizen citizen)
    {
        if (!allCitizens.Contains(citizen))
        {
            allCitizens.Add(citizen);
            citizenPositions[citizen] = citizen.transform.position;
            
            if (showDebugInfo)
            {
                Debug.Log($"[CitizenManager] Registered {citizen.name}. Total citizens: {allCitizens.Count}");
            }
        }
    }
    
    public void UnregisterCitizen(Citizen citizen)
    {
        if (allCitizens.Remove(citizen))
        {
            citizenPositions.Remove(citizen);
            
            if (showDebugInfo)
            {
                Debug.Log($"[CitizenManager] Unregistered {citizen.name}. Total citizens: {allCitizens.Count}");
            }
        }
    }
    
    public List<Citizen> GetAllCitizens()
    {
        return allCitizens;
    }
    
    public List<Citizen> GetCitizensInRange(Vector3 position, float range)
    {
        List<Citizen> nearbyBitizens = new List<Citizen>();
        float rangeSquared = range * range;
        
        foreach (var citizen in allCitizens)
        {
            if (citizen == null) continue;
            
            float distanceSquared = (citizen.transform.position - position).sqrMagnitude;
            if (distanceSquared <= rangeSquared)
            {
                nearbyBitizens.Add(citizen);
            }
        }
        
        return nearbyBitizens;
    }
    
    public Citizen GetNearestCitizen(Vector3 position, float maxRange = float.MaxValue)
    {
        Citizen nearestCitizen = null;
        float nearestDistanceSquared = maxRange * maxRange;
        
        foreach (var citizen in allCitizens)
        {
            if (citizen == null) continue;
            
            float distanceSquared = (citizen.transform.position - position).sqrMagnitude;
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestCitizen = citizen;
                nearestDistanceSquared = distanceSquared;
            }
        }
        
        return nearestCitizen;
    }
    
    public List<Citizen> GetCitizensInRangeExcluding(Vector3 position, float range, Citizen excludeCitizen)
    {
        List<Citizen> nearbyBitizens = new List<Citizen>();
        float rangeSquared = range * range;
        
        foreach (var citizen in allCitizens)
        {
            if (citizen == null || citizen == excludeCitizen) continue;
            
            float distanceSquared = (citizen.transform.position - position).sqrMagnitude;
            if (distanceSquared <= rangeSquared)
            {
                nearbyBitizens.Add(citizen);
            }
        }
        
        return nearbyBitizens;
    }
    
    public void UpdateCitizenPosition(Citizen citizen)
    {
        if (citizenPositions.ContainsKey(citizen))
        {
            citizenPositions[citizen] = citizen.transform.position;
        }
    }
    
    void Update()
    {
        // Update positions periodically to maintain spatial accuracy
        // This is still much more efficient than FindObjectsOfType every frame
        foreach (var citizen in allCitizens)
        {
            if (citizen != null)
            {
                citizenPositions[citizen] = citizen.transform.position;
            }
        }
        
        // Clean up null references
        allCitizens.RemoveAll(c => c == null);
        
        // Remove null entries from position dictionary
        var keysToRemove = new List<Citizen>();
        foreach (var kvp in citizenPositions)
        {
            if (kvp.Key == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            citizenPositions.Remove(key);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        Gizmos.color = Color.blue;
        foreach (var citizen in allCitizens)
        {
            if (citizen != null)
            {
                Gizmos.DrawWireSphere(citizen.transform.position, 1f);
            }
        }
    }
    
    // Performance monitoring methods
    public int GetRegisteredCitizenCount()
    {
        return allCitizens.Count;
    }
    
    public void LogPerformanceStats()
    {
        Debug.Log($"[CitizenManager] Performance Stats:");
        Debug.Log($"  - Total Citizens: {allCitizens.Count}");
        Debug.Log($"  - Position Cache Size: {citizenPositions.Count}");
        Debug.Log($"  - Max Interaction Distance: {maxInteractionDistance}");
    }
}