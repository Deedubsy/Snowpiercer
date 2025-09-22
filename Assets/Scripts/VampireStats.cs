using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VampireStats : MonoBehaviour
{
    public static VampireStats instance;

    [Header("Blood Settings")]
    public float bloodPerCitizen = 25f;   // Blood gained per citizen drained.

    [Header("Vampire Core Stats")]
    [Tooltip("How far enemies can see you. Higher = more visible.")]
    public float spotDistance = 10f;
    [Tooltip("Standard movement speed when not crouching or sprinting.")]
    public float walkSpeed = 5f;
    [Tooltip("Movement speed when crouching (stealth).")]
    public float crouchSpeed = 2f;
    [Tooltip("How far away you can be to drink blood or use powers.")]
    public float killDrainRange = 2f;
    [Tooltip("How long it takes to drain a citizen (seconds). Lower = faster.")]
    public float bloodDrainSpeed = 2f;
    [Tooltip("How long you can sprint before stamina runs out (seconds).")]
    public float sprintDuration = 5f;
    [Tooltip("Time you can stay hidden in shadows or blend in (seconds).")]
    public float shadowCloakTime = 10f;

    [Header("UI (Optional)")]
    public Slider bloodSlider;            // Slider to display daily blood progress.
    public Text dayText;                  // UI text to display current day info.
    public GameObject winScreenUI;        // Win screen to display when the game is won.

    [Header("Cumulative Stats")]
    public float totalBlood = 0f;

    [Header("Disguise System")]
    [Tooltip("Whether the player is currently disguised")]
    public bool isDisguised = false;
    [Tooltip("Reduces detection range when disguised")]
    public float disguiseDetectionModifier = 0.5f;
    [Tooltip("Reduces suspicion build rate when disguised")]
    public float disguiseSuspicionModifier = 0.7f;
    [Tooltip("How long the disguise lasts (seconds)")]
    public float disguiseDuration = 300f;
    private float currentDisguiseTime = 0f;

    [Header("Sabotage Skills")]
    [Tooltip("Player's sabotage skill level (1-5)")]
    public int sabotageSkillLevel = 1;
    [Tooltip("Speed multiplier for sabotage actions")]
    public float sabotageSpeed = 1f;
    [Tooltip("Has wire cutters for bell sabotage")]
    public bool hasWireCutters = false;
    [Tooltip("Has silent nail gun for remote sabotage")]
    public bool hasSilentNailGun = false;
    [Tooltip("Number of sabotage tool uses remaining")]
    public int sabotageToolUses = 3;

    [Header("Detection Modifiers")]
    [Tooltip("Detection modifier when crouching")]
    public float crouchDetectionModifier = 0.6f;
    [Tooltip("Noise radius modifier when sprinting")]
    public float sprintNoiseModifier = 2f;
    [Tooltip("Detection modifier when in shadows")]
    public float shadowDetectionModifier = 0.4f;

    [Header("References")]
    public VampireUpgradeManager upgradeManager;

    public System.Action<bool> OnDisguiseStateChanged;
    
    [Header("Permanent Abilities")]
    public bool hasNightVision = false;
    public bool hasShadowStep = false;
    public bool hasHypnoticGaze = false;
    public bool hasBloodFrenzy = false;
    private float shadowStepCooldown = 30f;
    private float lastShadowStepTime = -30f;
    private float bloodFrenzyDuration = 0f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Setup the blood progress slider from GameManager
        if (bloodSlider != null && GameManager.instance != null)
        {
            bloodSlider.maxValue = GameManager.instance.dailyBloodGoal;
            bloodSlider.value = GameManager.instance.GetCurrentBlood();
        }
        // Update the day text UI from GameManager
        if (dayText != null && GameManager.instance != null)
        {
            dayText.text = "Day " + GameManager.instance.currentDay + " / " + GameManager.instance.maxDays;
        }
        // Ensure the win screen is hidden at start.
        if (winScreenUI != null)
        {
            winScreenUI.SetActive(false);
        }

        if (upgradeManager == null)
        {
            upgradeManager = FindObjectOfType<VampireUpgradeManager>();
        }
    }

    void Update()
    {
        // Update disguise timer
        if (isDisguised && currentDisguiseTime > 0)
        {
            currentDisguiseTime -= Time.deltaTime;
            if (currentDisguiseTime <= 0)
            {
                RemoveDisguise();
            }
        }
    }

    // Call this method to add blood (for example, after draining a citizen).
    public void AddBlood(float amount)
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.AddBlood(amount);
        }
        totalBlood += amount;
        
        // Update UI if we have a reference
        if (bloodSlider != null && GameManager.instance != null)
        {
            bloodSlider.value = GameManager.instance.GetCurrentBlood();
        }
    }

    public void ResetDailyProgress()
    {
        // Let GameManager handle the reset
        if (bloodSlider != null && GameManager.instance != null)
            bloodSlider.value = 0f;
        GameLogger.Log(LogCategory.Gameplay, "Daily blood progress reset.", this);
    }

    public List<string> GetUnlockedUpgrades()
    {
        if (upgradeManager != null)
        {
            return upgradeManager.GetUnlockedUpgradeIDs();
        }
        return new List<string>();
    }

    public void SetUnlockedUpgrades(List<string> unlockedIds)
    {
        if (upgradeManager != null)
        {
            upgradeManager.ApplyUpgradesFromSave(unlockedIds);
        }
    }

    // Disguise System Methods
    public void ApplyDisguise(float duration = -1)
    {
        isDisguised = true;
        currentDisguiseTime = duration > 0 ? duration : disguiseDuration;
        OnDisguiseStateChanged?.Invoke(true);
        GameLogger.Log(LogCategory.Gameplay, "Player applied disguise", this);
    }

    public void RemoveDisguise()
    {
        isDisguised = false;
        currentDisguiseTime = 0;
        OnDisguiseStateChanged?.Invoke(false);
        GameLogger.Log(LogCategory.Gameplay, "Player disguise removed", this);
    }

    public float GetEffectiveDetectionRange()
    {
        float range = spotDistance;

        if (isDisguised)
            range *= disguiseDetectionModifier;

        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null && playerController.IsCrouching())
            range *= crouchDetectionModifier;

        if (IsInShadow())
            range *= shadowDetectionModifier;

        return range;
    }

    public float GetEffectiveNoiseRadius(float baseRadius)
    {
        float radius = baseRadius;

        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null && playerController.IsSprinting())
            radius *= sprintNoiseModifier;

        return radius;
    }

    private bool IsInShadow()
    {
        // Check if player is in a shadow trigger area
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.5f);
        foreach (var col in colliders)
        {
            if (col.CompareTag("Shadow") || col.CompareTag("IndoorArea"))
                return true;
        }
        return false;
    }

    // Sabotage System Methods
    public bool CanSabotage()
    {
        return sabotageSkillLevel > 0 && (hasWireCutters || hasSilentNailGun) && sabotageToolUses > 0;
    }

    public void UseSabotageTool()
    {
        if (sabotageToolUses > 0)
        {
            sabotageToolUses--;
            GameLogger.Log(LogCategory.Gameplay, $"Sabotage tool used. Remaining: {sabotageToolUses}", this);
        }
    }

    public void UpgradeSabotageSkill()
    {
        sabotageSkillLevel = Mathf.Min(sabotageSkillLevel + 1, 5);
        sabotageSpeed *= 1.2f;
        GameLogger.Log(LogCategory.Gameplay, $"Sabotage skill upgraded to level {sabotageSkillLevel}", this);
    }

    public void AddSabotageTools(bool wireCutters, bool nailGun, int uses)
    {
        if (wireCutters) hasWireCutters = true;
        if (nailGun) hasSilentNailGun = true;
        sabotageToolUses += uses;
        GameLogger.Log(LogCategory.Gameplay, $"Added sabotage tools. Total uses: {sabotageToolUses}", this);
    }

    // Properties
    public bool IsDisguised => isDisguised;
    public float DisguiseTimeRemaining => currentDisguiseTime;
    public int SabotageLevel => sabotageSkillLevel;
    public float SabotageSpeed => sabotageSpeed;
    public bool HasSabotageTools => hasWireCutters || hasSilentNailGun;
    public int SabotageToolsRemaining => sabotageToolUses;
}
