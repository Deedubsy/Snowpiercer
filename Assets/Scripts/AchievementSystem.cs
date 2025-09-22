using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class Achievement
{
    public string id;
    public string title;
    public string description;
    public string iconName;
    public bool isUnlocked = false;
    public DateTime unlockDate;
    public int progress = 0;
    public int targetProgress = 1;
    public AchievementType type;
    public AchievementRarity rarity;
    public int bloodReward = 0;
    public bool isSecret = false;
}

public enum AchievementType
{
    BloodCollection,
    Stealth,
    Survival,
    Exploration,
    Combat,
    Social,
    TimeBased,
    Special
}

public enum AchievementRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public class AchievementSystem : MonoBehaviour
{
    public static AchievementSystem Instance { get; private set; }

    [Header("Achievement Settings")]
    public bool enableAchievements = true;
    public bool showNotifications = true;
    public float notificationDuration = 3f;
    public bool autoSave = true;

    [Header("UI References")]
    public GameObject achievementNotification;
    public TMPro.TMP_Text notificationTitle;
    public TMPro.TMP_Text notificationDescription;
    public UnityEngine.UI.Image notificationIcon;

    [Header("Audio")]
    public AudioClip achievementUnlockSound;
    public AudioClip achievementProgressSound;

    [Header("Debug")]
    public bool debugMode = false;
    public bool logAchievements = true;

    private List<Achievement> achievements = new List<Achievement>();
    private AudioSource audioSource;
    private Dictionary<string, Achievement> achievementLookup = new Dictionary<string, Achievement>();

    public event Action<Achievement> OnAchievementUnlocked;
    public event Action<Achievement> OnAchievementProgress;
    public event Action OnAchievementsLoaded;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAchievementSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeAchievementSystem()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        CreateDefaultAchievements();
        LoadAchievements();

        if (debugMode)
        {
            Debug.Log("AchievementSystem initialized");
        }
    }

    void CreateDefaultAchievements()
    {
        achievements.Clear();

        // Blood Collection Achievements
        AddAchievement(new Achievement
        {
            id = "first_blood",
            title = "First Blood",
            description = "Drain your first citizen",
            type = AchievementType.BloodCollection,
            rarity = AchievementRarity.Common,
            bloodReward = 10
        });

        AddAchievement(new Achievement
        {
            id = "blood_hunter",
            title = "Blood Hunter",
            description = "Collect 100 blood in a single night",
            type = AchievementType.BloodCollection,
            rarity = AchievementRarity.Uncommon,
            bloodReward = 50,
            targetProgress = 100
        });

        AddAchievement(new Achievement
        {
            id = "vampire_lord",
            title = "Vampire Lord",
            description = "Collect 1000 total blood",
            type = AchievementType.BloodCollection,
            rarity = AchievementRarity.Rare,
            bloodReward = 200,
            targetProgress = 1000
        });

        // Stealth Achievements
        AddAchievement(new Achievement
        {
            id = "shadow_walker",
            title = "Shadow Walker",
            description = "Complete a night without being spotted",
            type = AchievementType.Stealth,
            rarity = AchievementRarity.Uncommon,
            bloodReward = 75
        });

        AddAchievement(new Achievement
        {
            id = "ghost",
            title = "Ghost",
            description = "Complete 5 nights without being spotted",
            type = AchievementType.Stealth,
            rarity = AchievementRarity.Rare,
            bloodReward = 150,
            targetProgress = 5
        });

        AddAchievement(new Achievement
        {
            id = "invisible",
            title = "Invisible",
            description = "Complete 10 nights without being spotted",
            type = AchievementType.Stealth,
            rarity = AchievementRarity.Epic,
            bloodReward = 300,
            targetProgress = 10
        });

        // Survival Achievements
        AddAchievement(new Achievement
        {
            id = "survivor",
            title = "Survivor",
            description = "Complete your first night",
            type = AchievementType.Survival,
            rarity = AchievementRarity.Common,
            bloodReward = 25
        });

        AddAchievement(new Achievement
        {
            id = "night_walker",
            title = "Night Walker",
            description = "Complete 10 nights",
            type = AchievementType.Survival,
            rarity = AchievementRarity.Uncommon,
            bloodReward = 100,
            targetProgress = 10
        });

        AddAchievement(new Achievement
        {
            id = "immortal",
            title = "Immortal",
            description = "Complete 50 nights",
            type = AchievementType.Survival,
            rarity = AchievementRarity.Epic,
            bloodReward = 500,
            targetProgress = 50
        });

        // Exploration Achievements
        AddAchievement(new Achievement
        {
            id = "explorer",
            title = "Explorer",
            description = "Visit all areas of the city",
            type = AchievementType.Exploration,
            rarity = AchievementRarity.Uncommon,
            bloodReward = 75
        });

        AddAchievement(new Achievement
        {
            id = "speed_demon",
            title = "Speed Demon",
            description = "Return to castle with 5+ minutes remaining",
            type = AchievementType.TimeBased,
            rarity = AchievementRarity.Rare,
            bloodReward = 100
        });

        // Special Achievements
        AddAchievement(new Achievement
        {
            id = "noble_feast",
            title = "Noble Feast",
            description = "Drain 10 noble citizens",
            type = AchievementType.BloodCollection,
            rarity = AchievementRarity.Rare,
            bloodReward = 200,
            targetProgress = 10
        });

        AddAchievement(new Achievement
        {
            id = "priest_hunter",
            title = "Priest Hunter",
            description = "Drain 5 priest citizens",
            type = AchievementType.BloodCollection,
            rarity = AchievementRarity.Epic,
            bloodReward = 300,
            targetProgress = 5
        });

        AddAchievement(new Achievement
        {
            id = "royal_blood",
            title = "Royal Blood",
            description = "Drain a royal citizen",
            type = AchievementType.BloodCollection,
            rarity = AchievementRarity.Legendary,
            bloodReward = 500
        });

        // Secret Achievements
        AddAchievement(new Achievement
        {
            id = "vampire_hunter_survivor",
            title = "???",
            description = "Survive an encounter with a vampire hunter",
            type = AchievementType.Special,
            rarity = AchievementRarity.Epic,
            bloodReward = 250,
            isSecret = true
        });

        AddAchievement(new Achievement
        {
            id = "garlic_survivor",
            title = "???",
            description = "Survive a garlic trap",
            type = AchievementType.Special,
            rarity = AchievementRarity.Rare,
            bloodReward = 150,
            isSecret = true
        });

        AddAchievement(new Achievement
        {
            id = "holy_survivor",
            title = "???",
            description = "Survive a holy symbol trap",
            type = AchievementType.Special,
            rarity = AchievementRarity.Epic,
            bloodReward = 250,
            isSecret = true
        });
    }

    void AddAchievement(Achievement achievement)
    {
        achievements.Add(achievement);
        achievementLookup[achievement.id] = achievement;
    }

    public void UnlockAchievement(string achievementId)
    {
        if (!enableAchievements || !achievementLookup.ContainsKey(achievementId)) return;

        Achievement achievement = achievementLookup[achievementId];
        if (achievement.isUnlocked) return;

        achievement.isUnlocked = true;
        achievement.unlockDate = DateTime.Now;
        achievement.progress = achievement.targetProgress;

        // Award blood reward
        if (achievement.bloodReward > 0)
        {
            if (VampireStats.instance != null)
            {
                VampireStats.instance.AddBlood(achievement.bloodReward);
            }
        }

        // Show notification
        if (showNotifications)
        {
            ShowAchievementNotification(achievement);
        }

        // Play sound
        if (achievementUnlockSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(achievementUnlockSound);
        }

        // Save achievements
        if (autoSave)
        {
            SaveAchievements();
        }

        OnAchievementUnlocked?.Invoke(achievement);

        if (logAchievements)
        {
            Debug.Log($"Achievement Unlocked: {achievement.title} - {achievement.description}");
        }
    }

    public void UpdateAchievementProgress(string achievementId, int progress)
    {
        if (!enableAchievements || !achievementLookup.ContainsKey(achievementId)) return;

        Achievement achievement = achievementLookup[achievementId];
        if (achievement.isUnlocked) return;

        achievement.progress = Mathf.Min(progress, achievement.targetProgress);

        // Check if achievement should be unlocked
        if (achievement.progress >= achievement.targetProgress)
        {
            UnlockAchievement(achievementId);
        }
        else
        {
            // Play progress sound
            if (achievementProgressSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(achievementProgressSound);
            }

            OnAchievementProgress?.Invoke(achievement);

            if (logAchievements)
            {
                Debug.Log($"Achievement Progress: {achievement.title} - {achievement.progress}/{achievement.targetProgress}");
            }
        }
    }

    public void IncrementAchievement(string achievementId, int increment = 1)
    {
        if (!enableAchievements || !achievementLookup.ContainsKey(achievementId)) return;

        Achievement achievement = achievementLookup[achievementId];
        if (achievement.isUnlocked) return;

        UpdateAchievementProgress(achievementId, achievement.progress + increment);
    }

    void ShowAchievementNotification(Achievement achievement)
    {
        if (achievementNotification == null) return;

        // Update notification UI
        if (notificationTitle != null)
        {
            string title = achievement.isSecret ? "Secret Achievement Unlocked!" : "Achievement Unlocked!";
            notificationTitle.text = title;
        }

        if (notificationDescription != null)
        {
            string description = achievement.isSecret ? achievement.description : $"{achievement.title}\n{achievement.description}";
            notificationDescription.text = description;
        }

        // Show notification
        achievementNotification.SetActive(true);

        // Hide after duration
        StartCoroutine(HideNotificationAfterDelay(notificationDuration));
    }

    System.Collections.IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (achievementNotification != null)
        {
            achievementNotification.SetActive(false);
        }
    }

    public void SaveAchievements()
    {
        try
        {
            List<AchievementData> achievementData = new List<AchievementData>();
            
            foreach (var achievement in achievements)
            {
                achievementData.Add(new AchievementData
                {
                    id = achievement.id,
                    isUnlocked = achievement.isUnlocked,
                    unlockDate = achievement.unlockDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    progress = achievement.progress
                });
            }

            string json = JsonUtility.ToJson(new AchievementSaveData { achievements = achievementData }, true);
            PlayerPrefs.SetString("Achievements", json);
            PlayerPrefs.Save();

            if (debugMode)
            {
                Debug.Log("Achievements saved successfully");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save achievements: {e.Message}");
        }
    }

    public void LoadAchievements()
    {
        try
        {
            if (PlayerPrefs.HasKey("Achievements"))
            {
                string json = PlayerPrefs.GetString("Achievements");
                AchievementSaveData saveData = JsonUtility.FromJson<AchievementSaveData>(json);

                foreach (var achievementData in saveData.achievements)
                {
                    if (achievementLookup.ContainsKey(achievementData.id))
                    {
                        Achievement achievement = achievementLookup[achievementData.id];
                        achievement.isUnlocked = achievementData.isUnlocked;
                        achievement.progress = achievementData.progress;

                        if (achievementData.isUnlocked && !string.IsNullOrEmpty(achievementData.unlockDate))
                        {
                            if (DateTime.TryParse(achievementData.unlockDate, out DateTime unlockDate))
                            {
                                achievement.unlockDate = unlockDate;
                            }
                        }
                    }
                }

                if (debugMode)
                {
                    Debug.Log("Achievements loaded successfully");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load achievements: {e.Message}");
        }

        OnAchievementsLoaded?.Invoke();
    }

    public void ResetAchievements()
    {
        foreach (var achievement in achievements)
        {
            achievement.isUnlocked = false;
            achievement.progress = 0;
            achievement.unlockDate = DateTime.MinValue;
        }

        PlayerPrefs.DeleteKey("Achievements");
        PlayerPrefs.Save();

        if (debugMode)
        {
            Debug.Log("Achievements reset");
        }
    }

    // Public getter methods
    public List<Achievement> GetAllAchievements()
    {
        return achievements.ToList();
    }

    public List<Achievement> GetUnlockedAchievements()
    {
        return achievements.Where(a => a.isUnlocked).ToList();
    }

    public List<Achievement> GetLockedAchievements()
    {
        return achievements.Where(a => !a.isUnlocked).ToList();
    }

    public List<Achievement> GetAchievementsByType(AchievementType type)
    {
        return achievements.Where(a => a.type == type).ToList();
    }

    public List<Achievement> GetAchievementsByRarity(AchievementRarity rarity)
    {
        return achievements.Where(a => a.rarity == rarity).ToList();
    }

    public Achievement GetAchievement(string achievementId)
    {
        return achievementLookup.ContainsKey(achievementId) ? achievementLookup[achievementId] : null;
    }

    public int GetTotalAchievements()
    {
        return achievements.Count;
    }

    public int GetUnlockedCount()
    {
        return achievements.Count(a => a.isUnlocked);
    }

    public float GetCompletionPercentage()
    {
        if (achievements.Count == 0) return 0f;
        return (float)GetUnlockedCount() / achievements.Count * 100f;
    }

    public int GetTotalBloodRewards()
    {
        return achievements.Where(a => a.isUnlocked).Sum(a => a.bloodReward);
    }

    // Context menu methods for testing
    [ContextMenu("Unlock All Achievements")]
    public void UnlockAllAchievements()
    {
        foreach (var achievement in achievements)
        {
            if (!achievement.isUnlocked)
            {
                UnlockAchievement(achievement.id);
            }
        }
    }

    [ContextMenu("Reset All Achievements")]
    public void ResetAllAchievements()
    {
        ResetAchievements();
    }

    [ContextMenu("Save Achievements")]
    public void SaveAchievementsFromContext()
    {
        SaveAchievements();
    }

    [ContextMenu("Load Achievements")]
    public void LoadAchievementsFromContext()
    {
        LoadAchievements();
    }
}

// Serialization classes
[Serializable]
public class AchievementData
{
    public string id;
    public bool isUnlocked;
    public string unlockDate;
    public int progress;
}

[Serializable]
public class AchievementSaveData
{
    public List<AchievementData> achievements = new List<AchievementData>();
} 