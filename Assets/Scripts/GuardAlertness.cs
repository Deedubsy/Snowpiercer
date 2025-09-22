using UnityEngine;

[CreateAssetMenu(fileName = "GuardAlertness", menuName = "Vampire/GuardAlertness", order = 3)]
public class GuardAlertness : ScriptableObject
{
    [System.Serializable]
    public class AlertnessLevel
    {
        public string levelName;
        public float spotDistanceMultiplier = 1f;
        public float patrolSpeedMultiplier = 1f;
        public float detectionTimeMultiplier = 1f;
        public float alertRadiusMultiplier = 1f;
        public Color gizmoColor = Color.yellow;
    }

    public AlertnessLevel normal;
    public AlertnessLevel suspicious;
    public AlertnessLevel alert;
    public AlertnessLevel panic;

    public AlertnessLevel GetLevel(GuardAlertnessLevel level)
    {
        switch (level)
        {
            case GuardAlertnessLevel.Normal: return normal;
            case GuardAlertnessLevel.Suspicious: return suspicious;
            case GuardAlertnessLevel.Alert: return alert;
            case GuardAlertnessLevel.Panic: return panic;
            default: return normal;
        }
    }
}

public enum GuardAlertnessLevel
{
    Normal,
    Suspicious,
    Alert,
    Panic
} 