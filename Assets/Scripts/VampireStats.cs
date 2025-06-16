using UnityEngine;
using UnityEngine.UI;

public class VampireStats : MonoBehaviour
{
    [Header("Day Progression Settings")]
    public int maxDays = 10;              // Total number of days required to win.
    public int currentDay = 1;            // The current day.
    public float dailyBloodGoal = 100f;   // Blood required to complete one day.

    [Header("Blood Settings")]
    public float currentBlood = 0f;       // Blood accumulated in the current day.
    public float bloodPerCitizen = 25f;   // Blood gained per citizen drained.

    [Header("UI (Optional)")]
    public Slider bloodSlider;            // Slider to display daily blood progress.
    public Text dayText;                  // UI text to display current day info.
    public GameObject winScreenUI;        // Win screen to display when the game is won.

    [Header("Cumulative Stats")]
    public float totalBlood = 0f;

    void Start()
    {
        // Setup the blood progress slider.
        if (bloodSlider != null)
        {
            bloodSlider.maxValue = dailyBloodGoal;
            bloodSlider.value = currentBlood;
        }
        // Update the day text UI.
        if (dayText != null)
        {
            dayText.text = "Day " + currentDay + " / " + maxDays;
        }
        // Ensure the win screen is hidden at start.
        if (winScreenUI != null)
        {
            winScreenUI.SetActive(false);
        }
    }

    // Call this method to add blood (for example, after draining a citizen).
    public void AddBlood(float amount)
    {
        currentBlood += amount;
        totalBlood += amount;
        if (bloodSlider != null)
        {
            bloodSlider.value = currentBlood;
        }
        Debug.Log("Day " + currentDay + " - Blood collected: " + currentBlood);
        CheckDayProgress();
    }

    // Checks whether the current day's goal has been reached.
    void CheckDayProgress()
    {
        if (currentBlood >= dailyBloodGoal)
        {
            Debug.Log("Day " + currentDay + " completed!");
            currentDay++;

            // Check if the final day has been completed.
            if (currentDay > maxDays)
            {
                WinGame();
            }
            else
            {
                // Reset blood for the next day.
                currentBlood = 0f;
                if (bloodSlider != null)
                {
                    bloodSlider.value = currentBlood;
                }
                if (dayText != null)
                {
                    dayText.text = "Day " + currentDay + " / " + maxDays;
                }
                // Optionally, show a day transition UI or message here.
            }
        }
    }

    // Handles the win condition.
    void WinGame()
    {
        Debug.Log("The vampire has regained full strength. You win!");
        if (winScreenUI != null)
        {
            winScreenUI.SetActive(true);
        }
        // Pause the game.
        Time.timeScale = 0f;
        // Optionally, load a win scene:
        // SceneManager.LoadScene("WinScene");
    }

    public void ResetDailyProgress()
    {
        currentBlood = 0f;
        if (bloodSlider != null)
            bloodSlider.value = currentBlood;
        Debug.Log("Daily blood progress reset.");
    }
}
