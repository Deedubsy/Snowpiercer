using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Time Settings")]
    public float dayDuration = 300f; // e.g., 5 minutes per night.
    public float currentTime;

    [Header("Day Settings")]
    public int maxDays = 10;
    public int currentDay = 1;

    [Header("Level Stats")]
    public int timesSpotted = 0;           // How many times the vampire was spotted.
    private float levelTimer = 0f;         // Total elapsed time in the level.
    private float levelCompletionTime = 0f; // Captured when level completes.
    
    [Header("Blood Progress")]
    public float currentBlood = 0f;        // Blood collected in current night
    public float dailyBloodGoal = 100f;    // Required blood per night
    public float bloodCarryOver = 0f;      // Excess blood from previous nights
    public float bloodRetentionOnDeath = 0.5f; // Percentage of blood kept on sunrise death

    [Header("UI Elements")]
    public TMPro.TMP_Text timeText;           // Displays remaining night time.
    public TMPro.TMP_Text dayText;            // Displays current day count.
    public TMPro.TMP_Text bloodCollectedText; // Displays the current day's blood progress.
    public GameObject sunriseWarningUI;       // Warning UI for imminent sunrise
    public TMPro.TMP_Text sunriseWarningText; // Text for sunrise warning
    
    [Header("Sunrise Warning")]
    public float warningTime = 60f;           // Show warning when X seconds remain
    public float criticalWarningTime = 30f;   // Critical warning threshold
    private bool warningShown = false;
    private bool criticalWarningShown = false;

    [Header("References")]
    public VampireStats vampireStats;   // Reference to the VampireStats script.
    public GameObject gameOverPanel;      // Reference to the Game Over panel.
    public WaypointGenerator waypointGenerator; // Reference to the WaypointGenerator
    public RandomEventManager randomEventManager; // Reference to the RandomEventManager
    public SaveSystem saveSystem;        // Reference to the SaveSystem
    public DifficultyProgression difficultyProgression; // Reference to the DifficultyProgression

    // Used to mark if the player returned to castle during the night.
    public bool returnedToCastle = false;

    [Header("Day/Night Cycle Settings")]
    [Tooltip("Duration of the night in seconds (8 in-game hours = 480s by default)")]
    public float nightDuration = 480f;
    public System.Action OnSundown;
    public System.Action OnSunrise;
    private bool isNight = false;

    // Flag to prevent saving when player quits mid-game
    private bool shouldSaveOnQuit = true;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // Preload common UI strings for performance
        StringCache.PreloadCommonStrings();
        
        // Load saved game data if available
        if (saveSystem == null)
        {
            saveSystem = FindObjectOfType<SaveSystem>();
        }

        if (saveSystem != null)
        {
            saveSystem.LoadGame();
        }

        // Initialize difficulty progression
        if (difficultyProgression == null)
        {
            difficultyProgression = FindObjectOfType<DifficultyProgression>();
        }

        if (difficultyProgression != null)
        {
            // Set the current day and apply difficulty
            difficultyProgression.SetDay(currentDay);
        }

        currentTime = nightDuration;
        dayDuration = nightDuration;
        isNight = true;
        OnSundown?.Invoke();
        UpdateTimeUI();
        UpdateDayUI();
        if (waypointGenerator != null)
        {
            waypointGenerator.GenerateWaypointsInAreas();
        }

        // Initialize RandomEventManager if not assigned
        if (randomEventManager == null)
        {
            randomEventManager = FindObjectOfType<RandomEventManager>();
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f) return; // Game paused.

        levelTimer += Time.deltaTime;
        if (isNight)
        {
            currentTime -= Time.deltaTime;
            
            // Check for sunrise warnings
            if (currentTime <= warningTime && !warningShown)
            {
                ShowSunriseWarning(false);
                warningShown = true;
            }
            else if (currentTime <= criticalWarningTime && !criticalWarningShown)
            {
                ShowSunriseWarning(true);
                criticalWarningShown = true;
            }
            
            if (currentTime <= 0f)
            {
                isNight = false;
                OnSunrise?.Invoke();
                if (!returnedToCastle)
                {
                    Debug.Log("Sunrise! Player did not reach the city gate in time.");
                    
                    // Apply blood retention penalty instead of instant death
                    float retainedBlood = currentBlood * bloodRetentionOnDeath;
                    bloodCarryOver = retainedBlood;
                    
                    Debug.Log($"Blood penalty applied. Retained {retainedBlood:F0} blood for next night.");
                    
                    // Only game over if this was the last day or no blood collected
                    if (currentDay >= maxDays || (currentBlood <= 0f && bloodCarryOver <= 0f))
                    {
                        GameOver();
                        return;
                    }
                }
                EndDay();
            }
            UpdateTimeUI();
            UpdateStatsUI();
        }
    }

    // Called at the end of each night/day.
    void EndDay()
    {
        // Calculate excess blood if any
        float totalBlood = currentBlood + bloodCarryOver;
        if (totalBlood > dailyBloodGoal && returnedToCastle)
        {
            // Convert excess blood to upgrade points
            float excess = totalBlood - dailyBloodGoal;
            int upgradePoints = Mathf.FloorToInt(excess);
            
            if (upgradePoints > 0)
            {
                if (PermanentUpgradeSystem.Instance != null)
                {
                    PermanentUpgradeSystem.Instance.AddBloodPoints(upgradePoints);
                    Debug.Log($"Added {upgradePoints} upgrade points from excess blood");
                }
                else
                {
                    Debug.LogWarning("PermanentUpgradeSystem not found - upgrade points not added");
                }
            }
            
            // Keep fractional blood for next night
            bloodCarryOver = excess - upgradePoints;
            Debug.Log($"Fractional blood carried over: {bloodCarryOver:F1}");
        }
        else if (!returnedToCastle)
        {
            // Blood carry over already set in sunrise penalty
        }
        else
        {
            bloodCarryOver = 0f;
        }
        
        // Record performance for the day that just ended
        if (difficultyProgression != null)
        {
            float bloodCollected = currentBlood;
            bool completedDay = returnedToCastle;
            
            difficultyProgression.RecordDayPerformance(bloodCollected, dailyBloodGoal, timesSpotted, completedDay);
        }
        
        // Reset daily stats
        currentBlood = 0f;
        if (vampireStats != null)
        {
            vampireStats.ResetDailyProgress();
        }
        timesSpotted = 0;

        currentDay++;

        // Update difficulty for the new day
        if (difficultyProgression != null)
        {
            difficultyProgression.SetDay(currentDay);
            // Update blood goal from difficulty progression
            dailyBloodGoal = difficultyProgression.GetCurrentBloodGoal();
        }

        if (currentDay > maxDays)
        {
            LevelComplete();
        }
        else
        {
            currentTime = nightDuration;
            dayDuration = nightDuration;
            isNight = true;
            returnedToCastle = false;
            warningShown = false;
            criticalWarningShown = false;
            OnSundown?.Invoke();
            UpdateDayUI();
            UpdateStatsUI();
            
            // Hide sunrise warning if it was showing
            if (sunriseWarningUI != null)
                sunriseWarningUI.SetActive(false);
        }
    }
    
    void ShowSunriseWarning(bool critical)
    {
        if (sunriseWarningUI != null)
        {
            sunriseWarningUI.SetActive(true);
            
            if (sunriseWarningText != null)
            {
                if (critical)
                {
                    sunriseWarningText.text = "CRITICAL: Sunrise imminent! Return to castle NOW!";
                    sunriseWarningText.color = Color.red;
                }
                else
                {
                    sunriseWarningText.text = "Warning: Sunrise approaching. Return to castle soon.";
                    sunriseWarningText.color = Color.yellow;
                }
            }
            
            // Auto-hide after a few seconds
            StartCoroutine(HideSunriseWarning(critical ? 5f : 3f));
        }
    }
    
    IEnumerator HideSunriseWarning(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (sunriseWarningUI != null)
            sunriseWarningUI.SetActive(false);
    }

    void UpdateTimeUI()
    {
        if (timeText != null)
        {
            timeText.text = StringCache.GetTimeString(currentTime);
        }
    }

    void UpdateDayUI()
    {
        if (dayText != null)
            dayText.text = StringCache.GetDayString(currentDay, maxDays);
    }

    void UpdateStatsUI()
    {
        if (bloodCollectedText != null)
        {
            float effectiveGoal = Mathf.Max(0f, dailyBloodGoal - bloodCarryOver);
            bloodCollectedText.text = StringCache.GetBloodString(currentBlood + bloodCarryOver, dailyBloodGoal);
        }
    }
    
    // Called when player collects blood
    public void AddBlood(float amount)
    {
        currentBlood += amount;
        Debug.Log($"Blood collected: {amount}. Total: {currentBlood + bloodCarryOver}/{dailyBloodGoal}");
        
        UpdateStatsUI();
        
        // Check if daily goal reached
        if (currentBlood + bloodCarryOver >= dailyBloodGoal)
        {
            Debug.Log("Daily blood goal reached! Return to castle to complete the night.");
        }
    }
    
    public float GetCurrentBlood()
    {
        return currentBlood + bloodCarryOver;
    }

    // Called when the player returns to the castle.
    public void ReturnToCastle()
    {
        returnedToCastle = true;
        Debug.Log("Returned to castle. Night complete.");
        isNight = false;

        // Save game when night is successfully completed
        if (saveSystem != null)
        {
            saveSystem.SaveGame();
            Debug.Log("Game saved after successful night completion.");
        }

        EndDay();
    }

    // Called when the player enters the town.
    public void EnterTown()
    {
        Debug.Log("Player entered town area.");
        isNight = true; // Make sure we're in night mode when entering town

        // Save current state
        if (saveSystem != null)
        {
            saveSystem.SaveGame();
            Debug.Log("Game saved when entering town.");
        }

        // Reset some town-specific states if needed
        returnedToCastle = false;
    }

    // Display a message to the player (for UI feedback)
    public void ShowMessage(string message)
    {
        Debug.Log($"Message: {message}");

        // TODO: Implement proper UI message display
        // For now, just log to console
        // Could integrate with a UI message system later
    }

    // Called by guards (or any system) when the vampire is spotted.
    public void IncrementSpotted()
    {
        timesSpotted++;
        Debug.Log("Vampire spotted! Total sightings: " + timesSpotted);
    }

    // Called when the final day is completed.
    void LevelComplete()
    {
        levelCompletionTime = levelTimer;
        Debug.Log("Level Complete! The vampire has fully regained his strength.");

        // Save game when level is completed
        if (saveSystem != null)
        {
            saveSystem.SaveGame();
            Debug.Log("Game saved after level completion.");
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            GameOverScreen gameOver = gameOverPanel.GetComponent<GameOverScreen>();
            if (gameOver != null)
            {
                // Pass in times spotted, cumulative blood, and completion time.
                gameOver.SetStats(timesSpotted, vampireStats.totalBlood, levelCompletionTime);
            }
        }
        Time.timeScale = 0f;
    }

    // Optional: Restart the level.
    public void RestartLevel()
    {
        // Don't save when restarting - player wants to start fresh
        shouldSaveOnQuit = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GameOver()
    {
        // Record failed day performance
        if (difficultyProgression != null)
        {
            float bloodCollected = currentBlood;
            float bloodGoal = dailyBloodGoal;
            difficultyProgression.RecordDayPerformance(bloodCollected, bloodGoal, timesSpotted, false);
        }

        // Don't save on game over - player failed
        shouldSaveOnQuit = false;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            GameOverScreen gameOver = gameOverPanel.GetComponent<GameOverScreen>();
            if (gameOver != null)
            {
                gameOver.SetStats(timesSpotted, vampireStats != null ? vampireStats.totalBlood : 0, levelTimer);
            }
        }
        Time.timeScale = 0f;
    }

    // Prevent saving when player quits mid-game
    void OnApplicationQuit()
    {
        shouldSaveOnQuit = false;
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Don't save when game is paused (mobile)
            shouldSaveOnQuit = false;
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            // Don't save when game loses focus
            shouldSaveOnQuit = false;
        }
    }
}
