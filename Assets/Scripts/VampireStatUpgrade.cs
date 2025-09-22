using UnityEngine;

public enum VampireStatType
{
    SpotDistance,
    WalkSpeed,
    CrouchSpeed,
    KillDrainRange,
    BloodDrainSpeed,
    SprintDuration,
    ShadowCloakTime
}

[CreateAssetMenu(fileName = "VampireStatUpgrade", menuName = "Vampire/StatUpgrade", order = 1)]
public class VampireStatUpgrade : ScriptableObject
{
    public VampireStatType statType;
    public string displayName;
    public float baseValue;
    public float currentValue;
    public float maxValue = 100f;
    public float upgradeIncrement = 1f;
    public int baseCost = 10;
    public float costMultiplier = 1.5f;
    public int maxLevel = 10;
    public int currentLevel = 1;

    public void ResetToBase()
    {
        currentValue = baseValue;
        currentLevel = 1;
    }

    public bool CanUpgrade()
    {
        return currentValue + upgradeIncrement <= maxValue && currentLevel < maxLevel;
    }

    public int GetCurrentCost()
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, currentLevel - 1));
    }

    public void Upgrade()
    {
        if (CanUpgrade())
        {
            currentValue += upgradeIncrement;
            currentLevel++;
        }
    }
    public void SetLevel(int level)
    {
        if (CanUpgrade())
        {
            currentValue = level * upgradeIncrement;
            currentLevel = level;
        }
    }

}