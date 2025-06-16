using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Time Settings")]
    public float dayDuration = 300f; // e.g., 5 minutes per night.
    private float currentTime;

    [Header("Day Settings")]
    public int maxDays = 10;
    private int currentDay = 1;

    [Header("Level Stats")]
    public int timesSpotted = 0;           // How many times the vampire was spotted.
    private float levelTimer = 0f;         // Total elapsed time in the level.
    private float levelCompletionTime = 0f; // Captured when level completes.

    [Header("UI Elements")]
    public TMPro.TMP_Text timeText;           // Displays remaining night time.
    public TMPro.TMP_Text dayText;            // Displays current day count.
    public TMPro.TMP_Text bloodCollectedText; // Displays the current day's blood progress.

    [Header("References")]
    public VampireStats vampireStats;   // Reference to the VampireStats script.
    public GameObject gameOverPanel;      // Reference to the Game Over panel.

    // Used to mark if the player returned to castle during the night.
    public bool returnedToCastle = false;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        currentTime = dayDuration;
        UpdateTimeUI();
        UpdateDayUI();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return; // Game paused.

        // Increment the overall level timer.
        levelTimer += Time.deltaTime;

        // Countdown the night.
        currentTime -= Time.deltaTime;
        if (currentTime <= 0f)
        {
            if (!returnedToCastle)
            {
                Debug.Log("Night ended without returning to castle. Daily progress lost.");
                if (vampireStats != null)
                    vampireStats.ResetDailyProgress();
            }
            EndDay();
        }

        UpdateTimeUI();
        UpdateStatsUI();
    }

    // Called at the end of each night/day.
    void EndDay()
    {
        currentDay++;
        if (currentDay > maxDays)
        {
            LevelComplete();
        }
        else
        {
            // Reset for next day.
            currentTime = dayDuration;
            returnedToCastle = false;
            UpdateDayUI();
        }
    }

    void UpdateTimeUI()
    {
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    void UpdateDayUI()
    {
        if (dayText != null)
            dayText.text = "Day " + currentDay + " / " + maxDays;
    }

    void UpdateStatsUI()
    {
        if (vampireStats != null && bloodCollectedText != null)
            bloodCollectedText.text = "Blood: " + vampireStats.currentBlood.ToString("0") + " / " + vampireStats.dailyBloodGoal;
    }

    // Called when the player returns to the castle.
    public void ReturnToCastle()
    {
        returnedToCastle = true;
        Debug.Log("Returned to castle. Night complete.");
        EndDay();
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
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
