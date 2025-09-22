using UnityEngine;

/// <summary>
/// This guide explains how to set up and use the comprehensive error logging system.
///
/// === OVERVIEW ===
/// The system provides a robust logging solution with features beyond Unity's default Debug.Log.
/// - GameLogger: The core singleton that manages all logging operations.
/// - InGameDebugConsole: A UI component for viewing logs directly within the game.
///
/// === FEATURES ===
/// - Log Levels: Differentiate logs by severity (Info, Warning, Error, Critical).
/// - Categorization: Assign categories (AI, Audio, Gameplay, etc.) for easier filtering and analysis.
/// - File Logging: Automatically writes logs to a persistent file (gamelog.txt) for debugging builds.
/// - In-Game Console: A toggleable UI to view logs on any device, with filtering options.
/// - Centralized Control: Manage log levels for the console and file from one place.
/// - Asynchronous: Logging is queued to minimize performance impact on the main thread.
///
/// === SETUP INSTRUCTIONS ===
///
/// 1. CREATE THE GAMELOGGER OBJECT:
///    - Create a new, empty GameObject in your main scene (e.g., the one with GameManager).
///    - Name it "GameLogger".
///    - Add the `GameLogger.cs` script to this object.
///
/// 2. CONFIGURE THE GAMELOGGER:
///    - In the Inspector for the "GameLogger" object, you can set:
///      - Console Log Level: The minimum level to show in the Unity Editor console.
///      - File Log Level: The minimum level to write to the log file.
///      - Enable File Logging: Toggle file logging on or off.
///      - Enable In-Game Console: Toggle the availability of the in-game console.
///      - Toggle Console Key: The key used to show/hide the in-game console (default is Backquote `).
///
/// 3. (OPTIONAL) ADD IN-GAME CONSOLE:
///    - Add the `InGameDebugConsole.cs` script to the "GameLogger" object.
///    - The UI will be created automatically at runtime when you press the toggle key for the first time.
///
/// === USAGE ===
///
/// To write a log message from any script, use the static methods of the GameLogger class.
/// This replaces any calls you might have to `Debug.Log`, `Debug.LogWarning`, etc.
///
/// --- EXAMPLES ---
///
/// // Logging an informational message
/// GameLogger.Log(LogCategory.Gameplay, "Player has picked up the key.", this);
///
/// // Logging a warning
/// GameLogger.LogWarning(LogCategory.Audio, "Audio clip 'Explosion' not found, playing default.", this);
///
/// // Logging an error
/// GameLogger.LogError(LogCategory.AI, "Guard AI state machine entered a null state.", guardObject);
///
/// // Logging a critical, game-breaking error
/// GameLogger.LogCritical(LogCategory.System, "Failed to initialize save system. Game cannot continue.", this);
///
///
/// === VIEWING LOGS ===
///
/// - UNITY CONSOLE: Logs will appear here as usual, based on the 'Console Log Level' setting.
///
/// - LOG FILE:
///   - Find the `gamelog.txt` file in the persistent data path for your platform.
///   - You can find the exact path by running the game and checking the first message from the logger.
///   - Editor (Windows): C:\Users\<YourUser>\AppData\LocalLow\<CompanyName>\<ProjectName>
///
/// - IN-GAME CONSOLE:
///   - Press the toggle key (default: `) to open and close it.
///   - Click the filter buttons at the bottom to change which log levels are displayed.
///
/// </summary>
public class ErrorLoggingSetupGuide : MonoBehaviour
{
    [Header("Setup Checklist")]
    [SerializeField] private bool gameLoggerObjectExists = false;
    [SerializeField] private bool inGameConsoleComponentAdded = false;

    [ContextMenu("Verify Setup")]
    private void VerifySetup()
    {
        GameLogger logger = FindObjectOfType<GameLogger>();
        gameLoggerObjectExists = logger != null;
        
        if (logger != null)
        {
            inGameConsoleComponentAdded = logger.GetComponent<InGameDebugConsole>() != null;
            if (!inGameConsoleComponentAdded)
            {
                Debug.LogWarning("The InGameDebugConsole component is not attached to the GameLogger. Add it to enable the in-game UI.", logger);
            }
        }
        else
        {
            Debug.LogError("Setup verification failed: No GameLogger object found in the scene. Please follow the setup instructions.");
        }

        if (gameLoggerObjectExists && inGameConsoleComponentAdded)
        {
            Debug.Log("Error logging setup appears to be correct!");
        }
    }
} 