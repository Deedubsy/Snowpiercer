using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text;

public enum LogLevel
{
    Info,
    Warning,
    Error,
    Critical
}

public enum LogCategory
{
    General,
    System,
    AI,
    Gameplay,
    Audio,
    Graphics,
    UI,
    Network,
    SaveLoad
}

public struct LogMessage
{
    public DateTime Timestamp { get; }
    public LogLevel Level { get; }
    public LogCategory Category { get; }
    public string Message { get; }
    public UnityEngine.Object Context { get; }

    public LogMessage(LogLevel level, LogCategory category, string message, UnityEngine.Object context)
    {
        Timestamp = DateTime.Now;
        Level = level;
        Category = category;
        Message = message;
        Context = context;
    }

    public override string ToString()
    {
        return $"[{Timestamp:HH:mm:ss}] [{Level}] [{Category}] {Message}";
    }
}

public class GameLogger : MonoBehaviour
{
    public static GameLogger Instance { get; private set; }

    [Header("Settings")]
    public LogLevel consoleLogLevel = LogLevel.Info;
    public LogLevel fileLogLevel = LogLevel.Warning;
    public bool enableFileLogging = true;
    public string logFileName = "gamelog.txt";
    public int logQueueLimit = 1000;

    [Header("In-Game Console")]
    public bool enableInGameConsole = true;
    public KeyCode toggleConsoleKey = KeyCode.BackQuote;

    private readonly Queue<LogMessage> logQueue = new Queue<LogMessage>();
    private StreamWriter logFileWriter;
    private StringBuilder stringBuilder = new StringBuilder();

    public static event Action<LogMessage> OnMessageLogged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLogger();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeLogger()
    {
        if (enableFileLogging)
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, logFileName);
                logFileWriter = new StreamWriter(path, true, Encoding.UTF8);
                logFileWriter.AutoFlush = true;
                Log(LogCategory.System, $"Log file initialized at: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to open log file: {e.Message}");
                enableFileLogging = false;
            }
        }
    }

    void Update()
    {
        ProcessLogQueue();

        if (enableInGameConsole && Input.GetKeyDown(toggleConsoleKey))
        {
            InGameDebugConsole.ToggleVisibility();
        }
    }

    private void ProcessLogQueue()
    {
        while (logQueue.Count > 0)
        {
            LogMessage logMessage = logQueue.Dequeue();

            // Log to Unity Console
            if (logMessage.Level >= consoleLogLevel)
            {
                LogToUnityConsole(logMessage);
            }

            // Log to File
            if (enableFileLogging && logMessage.Level >= fileLogLevel)
            {
                LogToFile(logMessage);
            }
            
            // Fire event for listeners (like the in-game console)
            OnMessageLogged?.Invoke(logMessage);
        }
    }

    private void QueueLog(LogLevel level, LogCategory category, string message, UnityEngine.Object context)
    {
        if (logQueue.Count >= logQueueLimit)
        {
            logQueue.Dequeue(); // Remove the oldest message to make space
        }
        logQueue.Enqueue(new LogMessage(level, category, message, context));
    }

    private void LogToUnityConsole(LogMessage logMessage)
    {
        switch (logMessage.Level)
        {
            case LogLevel.Info:
                Debug.Log(logMessage.ToString(), logMessage.Context);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(logMessage.ToString(), logMessage.Context);
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                Debug.LogError(logMessage.ToString(), logMessage.Context);
                break;
        }
    }

    private void LogToFile(LogMessage logMessage)
    {
        if (logFileWriter == null) return;
        
        stringBuilder.Clear();
        stringBuilder.AppendFormat("[{0:yyyy-MM-dd HH:mm:ss.fff}] ", logMessage.Timestamp);
        stringBuilder.AppendFormat("[{0,-8}] ", logMessage.Level.ToString().ToUpper());
        stringBuilder.AppendFormat("[{0,-10}] ", logMessage.Category);
        stringBuilder.Append(logMessage.Message);
        if (logMessage.Context != null)
        {
            stringBuilder.AppendFormat(" (Context: {0})", logMessage.Context.name);
        }

        logFileWriter.WriteLine(stringBuilder.ToString());
    }

    void OnDestroy()
    {
        ProcessLogQueue(); // Process any remaining logs
        if (logFileWriter != null)
        {
            logFileWriter.Close();
            logFileWriter = null;
        }
    }
    
    // --- Public Static Methods ---

    public static void Log(LogCategory category, string message, UnityEngine.Object context = null)
    {
        if (Instance != null)
        {
            Instance.QueueLog(LogLevel.Info, category, message, context);
        }
    }

    public static void LogWarning(LogCategory category, string message, UnityEngine.Object context = null)
    {
        if (Instance != null)
        {
            Instance.QueueLog(LogLevel.Warning, category, message, context);
        }
    }

    public static void LogError(LogCategory category, string message, UnityEngine.Object context = null)
    {
        if (Instance != null)
        {
            Instance.QueueLog(LogLevel.Error, category, message, context);
        }
    }
    
    public static void LogCritical(LogCategory category, string message, UnityEngine.Object context = null)
    {
        if (Instance != null)
        {
            Instance.QueueLog(LogLevel.Critical, category, message, context);
        }
    }
} 