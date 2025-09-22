using UnityEngine;
using System.Collections.Generic;

public interface IDebugProvider
{
    string GetEntityName();
    string GetCurrentState();
    float GetDetectionProgress();
    Vector3 GetPosition();
    Dictionary<string, object> GetDebugData();
}

[System.Serializable]
public class DebugData
{
    public string key;
    public string value;
    public Color color = Color.white;
    
    public DebugData(string key, string value, Color color = default)
    {
        this.key = key;
        this.value = value;
        this.color = color == default ? Color.white : color;
    }
}

[System.Serializable]
public class AIDebugInfo
{
    public string entityName;
    public string currentState;
    public float detectionProgress;
    public Vector3 position;
    public List<DebugData> debugEntries = new List<DebugData>();
    
    public void AddEntry(string key, object value, Color color = default)
    {
        if (color == default) color = Color.white;
        
        string valueString = value?.ToString() ?? "null";
        debugEntries.Add(new DebugData(key, valueString, color));
    }
    
    public void Clear()
    {
        debugEntries.Clear();
    }
}