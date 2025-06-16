using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    [Header("Stat UI Elements")]
    public Text spottedText;       // Displays how many times the vampire was spotted.
    public Text bloodText;         // Displays the total blood gathered.
    public Text timeText;          // Displays how quickly the level was completed.

    [Header("Buttons")]
    public Button nextLevelButton; // Button to proceed to the next level.
    public Button mainMenuButton;  // Button to quit to the main menu.

    // This method should be called when the level is complete or the game is over.
    // It fills in the stats on the screen.
    public void SetStats(int timesSpotted, float totalBlood, float completionTime)
    {
        if (spottedText != null)
            spottedText.text = "Times Spotted: " + timesSpotted.ToString();

        if (bloodText != null)
            bloodText.text = "Blood Gathered: " + totalBlood.ToString("0") + " units";

        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(completionTime / 60f);
            int seconds = Mathf.FloorToInt(completionTime % 60f);
            timeText.text = string.Format("Completion Time: {0:00}:{1:00}", minutes, seconds);
        }
    }

    void Start()
    {
        // Set up button listeners.
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(LoadNextLevel);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(QuitToMainMenu);
    }

    // Loads the next level. Here we assume a build index system.
    void LoadNextLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex + 1);
    }

    // Returns to the main menu scene.
    void QuitToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
