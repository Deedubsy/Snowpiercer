using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class PermanentUpgrade
{
    public string id;
    public string name;
    public string description;
    public int bloodCost;
    public int tier; // 0 = base, 1 = advanced, 2 = master
    public bool isUnlocked;
    public List<string> prerequisites; // IDs of required upgrades
    public UpgradeEffect effect;
}

[Serializable]
public class UpgradeEffect
{
    public float spotDistanceModifier = 1f;
    public float walkSpeedModifier = 1f;
    public float crouchSpeedModifier = 1f;
    public float bloodDrainSpeedModifier = 1f;
    public float sprintDurationModifier = 1f;
    public float bloodGainModifier = 1f;
    public float noiseReductionModifier = 1f;
    public bool enableNightVision = false;
    public bool enableWallSense = false;
    public bool enableShadowStep = false;
    public bool enableHypnoticGaze = false;
    public bool enableBloodFrenzy = false;
}

public class PermanentUpgradeSystem : MonoBehaviour
{
    public static PermanentUpgradeSystem Instance { get; private set; }
    
    [Header("Upgrade Configuration")]
    public List<PermanentUpgrade> allUpgrades = new List<PermanentUpgrade>();
    public int availableBloodPoints = 0; // Blood points to spend on upgrades
    
    [Header("UI References")]
    public GameObject upgradeTreeUI;
    public Transform upgradeButtonContainer;
    public GameObject upgradeButtonPrefab;
    
    // Events
    public Action<PermanentUpgrade> OnUpgradePurchased;
    public Action<int> OnBloodPointsChanged;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUpgrades();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeUpgrades()
    {
        // Tier 0 - Basic upgrades
        allUpgrades.Add(new PermanentUpgrade
        {
            id = "stealth_1",
            name = "Shadow Walker I",
            description = "Reduce detection range by 15%",
            bloodCost = 50,
            tier = 0,
            effect = new UpgradeEffect { spotDistanceModifier = 0.85f }
        });
        
        allUpgrades.Add(new PermanentUpgrade
        {
            id = "speed_1",
            name = "Swift Hunter I",
            description = "Increase movement speed by 10%",
            bloodCost = 50,
            tier = 0,
            effect = new UpgradeEffect { walkSpeedModifier = 1.1f, crouchSpeedModifier = 1.1f }
        });
        
        allUpgrades.Add(new PermanentUpgrade
        {
            id = "drain_1",
            name = "Efficient Feeder I",
            description = "Drain blood 20% faster",
            bloodCost = 50,
            tier = 0,
            effect = new UpgradeEffect { bloodDrainSpeedModifier = 0.8f }
        });
        
        // Tier 1 - Advanced upgrades
        allUpgrades.Add(new PermanentUpgrade
        {
            id = "stealth_2",
            name = "Shadow Walker II",
            description = "Reduce detection range by 30% and noise by 20%",
            bloodCost = 150,
            tier = 1,
            prerequisites = new List<string> { "stealth_1" },
            effect = new UpgradeEffect { spotDistanceModifier = 0.7f, noiseReductionModifier = 0.8f }
        });
        
        allUpgrades.Add(new PermanentUpgrade
        {
            id = "nightvision",
            name = "Vampiric Sight",
            description = "See enemies through walls for 2 seconds when crouching still",
            bloodCost = 200,
            tier = 1,
            prerequisites = new List<string> { "stealth_1" },
            effect = new UpgradeEffect { enableNightVision = true }
        });
        
        allUpgrades.Add(new PermanentUpgrade
        {
            id = "bloodbonus",
            name = "Blood Connoisseur",
            description = "Gain 25% more blood from all sources",
            bloodCost = 200,
            tier = 1,
            prerequisites = new List<string> { "drain_1" },
            effect = new UpgradeEffect { bloodGainModifier = 1.25f }
        });
        
        // Tier 2 - Master upgrades
        allUpgrades.Add(new PermanentUpgrade
        {
            id = "shadowstep",
            name = "Shadow Step",
            description = "Teleport short distances through shadows (cooldown: 30s)",
            bloodCost = 500,
            tier = 2,
            prerequisites = new List<string> { "stealth_2", "speed_1" },
            effect = new UpgradeEffect { enableShadowStep = true }
        });
        
        allUpgrades.Add(new PermanentUpgrade
        {
            id = "hypnotic",
            name = "Hypnotic Gaze",
            description = "Temporarily turn guards into allies",
            bloodCost = 500,
            tier = 2,
            prerequisites = new List<string> { "nightvision" },
            effect = new UpgradeEffect { enableHypnoticGaze = true }
        });
        
        allUpgrades.Add(new PermanentUpgrade
        {
            id = "bloodfrenzy",
            name = "Blood Frenzy",
            description = "Each kill grants 30s of increased speed and power",
            bloodCost = 500,
            tier = 2,
            prerequisites = new List<string> { "bloodbonus", "speed_1" },
            effect = new UpgradeEffect { enableBloodFrenzy = true }
        });
    }
    
    public void AddBloodPoints(int amount)
    {
        availableBloodPoints += amount;
        OnBloodPointsChanged?.Invoke(availableBloodPoints);
        Debug.Log($"Blood points added: {amount}. Total: {availableBloodPoints}");
    }
    
    public bool CanPurchaseUpgrade(PermanentUpgrade upgrade)
    {
        if (upgrade.isUnlocked) return false;
        if (availableBloodPoints < upgrade.bloodCost) return false;
        
        // Check prerequisites
        foreach (string prereqId in upgrade.prerequisites)
        {
            var prereq = GetUpgradeById(prereqId);
            if (prereq == null || !prereq.isUnlocked) return false;
        }
        
        return true;
    }
    
    public void PurchaseUpgrade(string upgradeId)
    {
        var upgrade = GetUpgradeById(upgradeId);
        if (upgrade == null || !CanPurchaseUpgrade(upgrade)) return;
        
        availableBloodPoints -= upgrade.bloodCost;
        upgrade.isUnlocked = true;
        
        // Apply upgrade effects
        ApplyUpgradeEffects(upgrade);
        
        OnUpgradePurchased?.Invoke(upgrade);
        OnBloodPointsChanged?.Invoke(availableBloodPoints);
        
        Debug.Log($"Upgrade purchased: {upgrade.name}");
    }
    
    void ApplyUpgradeEffects(PermanentUpgrade upgrade)
    {
        if (VampireStats.instance == null) return;
        
        var effect = upgrade.effect;
        var stats = VampireStats.instance;
        
        // Apply stat modifiers
        stats.spotDistance *= effect.spotDistanceModifier;
        stats.walkSpeed *= effect.walkSpeedModifier;
        stats.crouchSpeed *= effect.crouchSpeedModifier;
        stats.bloodDrainSpeed *= effect.bloodDrainSpeedModifier;
        stats.sprintDuration *= effect.sprintDurationModifier;
        
        // Store ability unlocks in VampireStats for access
        if (effect.enableNightVision) stats.hasNightVision = true;
        if (effect.enableShadowStep) stats.hasShadowStep = true;
        if (effect.enableHypnoticGaze) stats.hasHypnoticGaze = true;
        if (effect.enableBloodFrenzy) stats.hasBloodFrenzy = true;
    }
    
    public PermanentUpgrade GetUpgradeById(string id)
    {
        return allUpgrades.Find(u => u.id == id);
    }
    
    public List<PermanentUpgrade> GetUnlockedUpgrades()
    {
        return allUpgrades.FindAll(u => u.isUnlocked);
    }
    
    public void SaveUpgrades()
    {
        // Save unlocked upgrades and blood points
        var saveData = new UpgradeSaveData
        {
            unlockedIds = GetUnlockedUpgrades().ConvertAll(u => u.id),
            bloodPoints = availableBloodPoints
        };
        
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString("PermanentUpgrades", json);
        PlayerPrefs.Save();
    }
    
    public void LoadUpgrades()
    {
        if (!PlayerPrefs.HasKey("PermanentUpgrades")) return;
        
        string json = PlayerPrefs.GetString("PermanentUpgrades");
        var saveData = JsonUtility.FromJson<UpgradeSaveData>(json);
        
        availableBloodPoints = saveData.bloodPoints;
        
        // Unlock and apply saved upgrades
        foreach (string id in saveData.unlockedIds)
        {
            var upgrade = GetUpgradeById(id);
            if (upgrade != null)
            {
                upgrade.isUnlocked = true;
                ApplyUpgradeEffects(upgrade);
            }
        }
    }
}

[Serializable]
public class UpgradeSaveData
{
    public List<string> unlockedIds;
    public int bloodPoints;
}