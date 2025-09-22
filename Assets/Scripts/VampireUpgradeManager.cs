using System.Collections.Generic;
using UnityEngine;

public class VampireUpgradeManager : MonoBehaviour
{
    [Header("Stat Upgrades")]
    public List<VampireStatUpgrade> upgrades;
    public VampireStats vampireStats;

    public bool TryUpgradeStat(VampireStatType type)
    {
        var upgrade = upgrades.Find(u => u.statType == type);
        if (upgrade != null && upgrade.CanUpgrade() && GameManager.instance != null && GameManager.instance.GetCurrentBlood() >= upgrade.GetCurrentCost())
        {
            GameManager.instance.AddBlood(-upgrade.GetCurrentCost());
            upgrade.Upgrade();
            ApplyUpgradesToStats();
            return true;
        }
        return false;
    }

    public void ApplyUpgradesToStats()
    {
        if (vampireStats == null) return;
        foreach (var upgrade in upgrades)
        {
            switch (upgrade.statType)
            {
                case VampireStatType.SpotDistance:
                    vampireStats.spotDistance = upgrade.currentValue;
                    break;
                case VampireStatType.WalkSpeed:
                    vampireStats.walkSpeed = upgrade.currentValue;
                    break;
                case VampireStatType.CrouchSpeed:
                    vampireStats.crouchSpeed = upgrade.currentValue;
                    break;
                case VampireStatType.KillDrainRange:
                    vampireStats.killDrainRange = upgrade.currentValue;
                    break;
                case VampireStatType.BloodDrainSpeed:
                    vampireStats.bloodDrainSpeed = upgrade.currentValue;
                    break;
                case VampireStatType.SprintDuration:
                    vampireStats.sprintDuration = upgrade.currentValue;
                    break;
                case VampireStatType.ShadowCloakTime:
                    vampireStats.shadowCloakTime = upgrade.currentValue;
                    break;
            }
        }
    }

    public void ResetAllUpgrades()
    {
        foreach (var upgrade in upgrades)
            upgrade.ResetToBase();
        ApplyUpgradesToStats();
    }

    public List<string> GetUnlockedUpgradeIDs()
    {
        List<string> unlockedIds = new List<string>();
        foreach (var upgrade in upgrades)
        {
            if (upgrade.currentLevel > 0)
            {
                // We can save the stat type and its level
                unlockedIds.Add($"{upgrade.statType}:{upgrade.currentLevel}");
            }
        }
        return unlockedIds;
    }

    public void ApplyUpgradesFromSave(List<string> unlockedIds)
    {
        foreach (string id in unlockedIds)
        {
            string[] parts = id.Split(':');
            if (parts.Length == 2)
            {
                if (System.Enum.TryParse<VampireStatType>(parts[0], out var type) && int.TryParse(parts[1], out var level))
                {
                    var upgrade = upgrades.Find(u => u.statType == type);
                    if (upgrade != null)
                    {
                        upgrade.SetLevel(level);
                    }
                }
            }
        }
        ApplyUpgradesToStats();
    }
}