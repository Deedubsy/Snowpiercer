using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// SP-010: Save/Load Comprehensive Testing
/// Tests save/load functionality across all game states and edge cases
/// </summary>
public class SaveLoadComprehensiveTest : MonoBehaviour
{
    [Header("Test Configuration")]
    public bool runOnStart = false;
    public bool enableDetailedLogging = true;

    [Header("Test Scenarios")]
    public bool testBasicSaveLoad = true;
    public bool testEdgeCases = true;
    public bool testDataIntegrity = true;
    public bool testPerformance = true;

    [Header("Test Parameters")]
    public float testWaitTime = 1f;

    private List<string> testResults = new List<string>();
    private bool testInProgress = false;

    // Test data storage
    private GameStateSnapshot originalState;
    private GameStateSnapshot loadedState;

    [System.Serializable]
    public class GameStateSnapshot
    {
        public int currentDay;
        public float currentBlood;
        public float bloodCarryOver;
        public bool returnedToCastle;
        public int upgradePoints;
        public float currentTime;
        public int timesSpotted;

        public GameStateSnapshot()
        {
            // Initialize with current game state
            if (GameManager.instance != null)
            {
                currentDay = GameManager.instance.currentDay;
                currentBlood = GameManager.instance.currentBlood;
                bloodCarryOver = GameManager.instance.bloodCarryOver;
                returnedToCastle = GameManager.instance.returnedToCastle;
                currentTime = GameManager.instance.currentTime;
                timesSpotted = GameManager.instance.timesSpotted;
            }

            if (PermanentUpgradeSystem.Instance != null)
            {
                upgradePoints = PermanentUpgradeSystem.Instance.availableBloodPoints;
            }
        }

        public bool Equals(GameStateSnapshot other)
        {
            if (other == null) return false;

            return currentDay == other.currentDay &&
                   Mathf.Approximately(currentBlood, other.currentBlood) &&
                   Mathf.Approximately(bloodCarryOver, other.bloodCarryOver) &&
                   returnedToCastle == other.returnedToCastle &&
                   upgradePoints == other.upgradePoints &&
                   Mathf.Approximately(currentTime, other.currentTime) &&
                   timesSpotted == other.timesSpotted;
        }

        public override string ToString()
        {
            return $"Day:{currentDay}, Blood:{currentBlood:F1}, CarryOver:{bloodCarryOver:F1}, " +
                   $"Castle:{returnedToCastle}, Upgrades:{upgradePoints}, Time:{currentTime:F1}, Spotted:{timesSpotted}";
        }
    }

    void Start()
    {
        if (runOnStart)
        {
            StartCoroutine(RunSaveLoadTests());
        }
    }

    [ContextMenu("Run Save/Load Tests")]
    public void RunTests()
    {
        if (!testInProgress)
        {
            StartCoroutine(RunSaveLoadTests());
        }
        else
        {
            Debug.LogWarning("Save/Load test already in progress");
        }
    }

    IEnumerator RunSaveLoadTests()
    {
        testInProgress = true;
        testResults.Clear();

        LogTest("=== SP-010: Save/Load Comprehensive Tests Starting ===");
        yield return new WaitForSeconds(testWaitTime);

        // Test 1: Basic Save/Load
        if (testBasicSaveLoad)
        {
            yield return StartCoroutine(TestBasicSaveLoad());
        }

        // Test 2: Edge Cases
        if (testEdgeCases)
        {
            yield return StartCoroutine(TestEdgeCases());
        }

        // Test 3: Data Integrity
        if (testDataIntegrity)
        {
            yield return StartCoroutine(TestDataIntegrity());
        }

        // Test 4: Performance
        if (testPerformance)
        {
            yield return StartCoroutine(TestPerformance());
        }

        // Final Results
        LogTest("=== SP-010: Save/Load Tests Complete ===");
        LogTestResults();

        testInProgress = false;
    }

    IEnumerator TestBasicSaveLoad()
    {
        LogTest("--- Test 1: Basic Save/Load ---");

        if (!ValidateSaveSystem())
        {
            testResults.Add("❌ Save system not available for testing");
            yield break;
        }

        SaveSystem saveSystem = FindObjectOfType<SaveSystem>();

        // Set up known game state
        SetupTestGameState();
        yield return new WaitForSeconds(testWaitTime);

        // Capture original state
        originalState = new GameStateSnapshot();
        LogTest($"Original state: {originalState}");

        // Perform save
        LogTest("Performing save operation...");
        bool saveSuccess = false;
        try
        {
            saveSystem.SaveGame();
            saveSuccess = true;
            LogTest("✅ Save operation completed");
        }
        catch (System.Exception e)
        {
            LogTest($"❌ Save operation failed: {e.Message}");
        }

        yield return new WaitForSeconds(testWaitTime);

        if (!saveSuccess)
        {
            testResults.Add("❌ Basic save operation failed");
            yield break;
        }

        // Modify game state
        ModifyGameState();
        yield return new WaitForSeconds(testWaitTime);

        // Perform load
        LogTest("Performing load operation...");
        bool loadSuccess = false;
        try
        {
            saveSystem.LoadGame();
            loadSuccess = true;
            LogTest("✅ Load operation completed");
        }
        catch (System.Exception e)
        {
            LogTest($"❌ Load operation failed: {e.Message}");
        }

        yield return new WaitForSeconds(testWaitTime);

        if (!loadSuccess)
        {
            testResults.Add("❌ Basic load operation failed");
            yield break;
        }

        // Verify state restoration
        loadedState = new GameStateSnapshot();
        LogTest($"Loaded state: {loadedState}");

        if (originalState.Equals(loadedState))
        {
            testResults.Add("✅ Basic save/load working correctly");
            LogTest("✅ Game state perfectly restored");
        }
        else
        {
            testResults.Add("❌ Basic save/load state mismatch");
            LogTest("❌ Game state not properly restored");
            LogTest($"Expected: {originalState}");
            LogTest($"Got: {loadedState}");
        }

        yield return new WaitForSeconds(testWaitTime);
    }

    IEnumerator TestEdgeCases()
    {
        LogTest("--- Test 2: Edge Cases ---");

        SaveSystem saveSystem = FindObjectOfType<SaveSystem>();
        if (saveSystem == null)
        {
            testResults.Add("❌ Edge cases test failed - no SaveSystem");
            yield break;
        }

        // Test Case 1: Save with extreme values
        LogTest("Testing edge case: extreme values...");
        SetupExtremeGameState();
        yield return new WaitForSeconds(testWaitTime);

        GameStateSnapshot extremeState = new GameStateSnapshot();
        saveSystem.SaveGame();
        yield return new WaitForSeconds(testWaitTime);

        // Reset and load
        ResetGameState();
        yield return new WaitForSeconds(testWaitTime);

        saveSystem.LoadGame();
        yield return new WaitForSeconds(testWaitTime);

        GameStateSnapshot restoredExtremeState = new GameStateSnapshot();

        if (extremeState.Equals(restoredExtremeState))
        {
            LogTest("✅ Extreme values saved/loaded correctly");
        }
        else
        {
            LogTest("❌ Extreme values not properly handled");
        }

        // Test Case 2: Rapid save/load cycles
        LogTest("Testing edge case: rapid save/load cycles...");
        bool rapidTestPassed = true;

        for (int i = 0; i < 5; i++)
        {
            try
            {
                saveSystem.SaveGame();
                saveSystem.LoadGame();
            }
            catch (System.Exception e)
            {
                LogTest($"❌ Rapid cycle {i} failed: {e.Message}");
                rapidTestPassed = false;
                break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        if (rapidTestPassed)
        {
            LogTest("✅ Rapid save/load cycles handled correctly");
        }

        // Test Case 3: Save during different game states
        LogTest("Testing edge case: save during different game states...");
        bool gameStateTestPassed = TestSaveInDifferentStates();

        if (rapidTestPassed && gameStateTestPassed)
        {
            testResults.Add("✅ Edge cases handled correctly");
        }
        else
        {
            testResults.Add("❌ Edge cases failed");
        }

        yield return new WaitForSeconds(testWaitTime);
    }

    IEnumerator TestDataIntegrity()
    {
        LogTest("--- Test 3: Data Integrity ---");

        SaveSystem saveSystem = FindObjectOfType<SaveSystem>();
        if (saveSystem == null)
        {
            testResults.Add("❌ Data integrity test failed - no SaveSystem");
            yield break;
        }

        // Set up comprehensive game state
        SetupComprehensiveGameState();
        yield return new WaitForSeconds(testWaitTime);

        // Save state
        originalState = new GameStateSnapshot();
        saveSystem.SaveGame();
        LogTest("Comprehensive state saved");

        yield return new WaitForSeconds(testWaitTime);

        // Significantly modify state
        RandomlyModifyGameState();
        yield return new WaitForSeconds(testWaitTime);

        // Load and verify
        saveSystem.LoadGame();
        yield return new WaitForSeconds(testWaitTime);

        loadedState = new GameStateSnapshot();

        // Check individual components
        bool integrityPassed = ValidateDataIntegrity(originalState, loadedState);

        if (integrityPassed)
        {
            testResults.Add("✅ Data integrity maintained");
            LogTest("✅ All data components properly preserved");
        }
        else
        {
            testResults.Add("❌ Data integrity compromised");
            LogTest("❌ Some data components lost or corrupted");
        }

        yield return new WaitForSeconds(testWaitTime);
    }

    IEnumerator TestPerformance()
    {
        LogTest("--- Test 4: Performance ---");

        SaveSystem saveSystem = FindObjectOfType<SaveSystem>();
        if (saveSystem == null)
        {
            testResults.Add("❌ Performance test failed - no SaveSystem");
            yield break;
        }

        // Test save performance
        float saveStartTime = Time.realtimeSinceStartup;
        saveSystem.SaveGame();
        float saveTime = Time.realtimeSinceStartup - saveStartTime;

        LogTest($"Save operation took: {saveTime * 1000f:F1}ms");

        yield return new WaitForSeconds(testWaitTime);

        // Test load performance
        float loadStartTime = Time.realtimeSinceStartup;
        saveSystem.LoadGame();
        float loadTime = Time.realtimeSinceStartup - loadStartTime;

        LogTest($"Load operation took: {loadTime * 1000f:F1}ms");

        // Performance evaluation
        bool savePerformanceGood = saveTime < 0.1f; // Less than 100ms
        bool loadPerformanceGood = loadTime < 0.1f; // Less than 100ms

        if (savePerformanceGood && loadPerformanceGood)
        {
            testResults.Add("✅ Save/Load performance excellent");
            LogTest($"✅ Performance targets met - Save: {saveTime * 1000f:F1}ms, Load: {loadTime * 1000f:F1}ms");
        }
        else
        {
            testResults.Add("⚠️ Save/Load performance needs optimization");
            LogTest($"⚠️ Performance targets missed - Save: {saveTime * 1000f:F1}ms, Load: {loadTime * 1000f:F1}ms");
        }

        yield return new WaitForSeconds(testWaitTime);
    }

    bool ValidateSaveSystem()
    {
        SaveSystem saveSystem = FindObjectOfType<SaveSystem>();
        if (saveSystem == null)
        {
            LogTest("❌ SaveSystem component not found");
            return false;
        }

        if (GameManager.instance == null)
        {
            LogTest("❌ GameManager.instance is null");
            return false;
        }

        LogTest("✅ Save system components available");
        return true;
    }

    void SetupTestGameState()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.currentDay = 3;
            GameManager.instance.currentBlood = 75.5f;
            GameManager.instance.bloodCarryOver = 12.3f;
            GameManager.instance.returnedToCastle = true;
            GameManager.instance.currentTime = 240f;
            GameManager.instance.timesSpotted = 2;
        }

        if (PermanentUpgradeSystem.Instance != null)
        {
            PermanentUpgradeSystem.Instance.availableBloodPoints = 8;
        }

        LogTest("Test game state set up");
    }

    void SetupExtremeGameState()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.currentDay = 10; // Max day
            GameManager.instance.currentBlood = 999.9f; // Very high blood
            GameManager.instance.bloodCarryOver = 0.1f; // Tiny carry over
            GameManager.instance.returnedToCastle = false;
            GameManager.instance.currentTime = 0.1f; // Almost sunrise
            GameManager.instance.timesSpotted = 100; // Many detections
        }

        if (PermanentUpgradeSystem.Instance != null)
        {
            PermanentUpgradeSystem.Instance.availableBloodPoints = 999;
        }

        LogTest("Extreme game state set up");
    }

    void SetupComprehensiveGameState()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.currentDay = 7;
            GameManager.instance.currentBlood = 150.75f;
            GameManager.instance.bloodCarryOver = 25.25f;
            GameManager.instance.returnedToCastle = true;
            GameManager.instance.currentTime = 180.5f;
            GameManager.instance.timesSpotted = 5;
        }

        if (PermanentUpgradeSystem.Instance != null)
        {
            PermanentUpgradeSystem.Instance.availableBloodPoints = 42;
        }

        LogTest("Comprehensive game state set up");
    }

    void ModifyGameState()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.currentDay = 5;
            GameManager.instance.currentBlood = 50f;
            GameManager.instance.bloodCarryOver = 0f;
            GameManager.instance.returnedToCastle = false;
        }

        LogTest("Game state modified");
    }

    void ResetGameState()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.currentDay = 1;
            GameManager.instance.currentBlood = 0f;
            GameManager.instance.bloodCarryOver = 0f;
            GameManager.instance.returnedToCastle = false;
            GameManager.instance.currentTime = 480f;
            GameManager.instance.timesSpotted = 0;
        }

        if (PermanentUpgradeSystem.Instance != null)
        {
            PermanentUpgradeSystem.Instance.availableBloodPoints = 0;
        }

        LogTest("Game state reset");
    }

    void RandomlyModifyGameState()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.currentDay = Random.Range(1, 11);
            GameManager.instance.currentBlood = Random.Range(0f, 200f);
            GameManager.instance.bloodCarryOver = Random.Range(0f, 50f);
            GameManager.instance.returnedToCastle = Random.value > 0.5f;
            GameManager.instance.currentTime = Random.Range(60f, 480f);
            GameManager.instance.timesSpotted = Random.Range(0, 20);
        }

        LogTest("Game state randomly modified");
    }

    bool TestSaveInDifferentStates()
    {
        // This would test saving during different game states like:
        // - During day vs night
        // - During combat
        // - During scene transitions
        // For now, we'll simulate different states

        LogTest("Testing save in different game states...");

        try
        {
            SaveSystem saveSystem = FindObjectOfType<SaveSystem>();

            // Test 1: Save during "night"
            GameManager.instance.currentTime = 300f; // Night time
            saveSystem.SaveGame();

            // Test 2: Save during "day"
            GameManager.instance.currentTime = 0f; // Sunrise
            saveSystem.SaveGame();

            // Test 3: Save with different castle states
            GameManager.instance.returnedToCastle = true;
            saveSystem.SaveGame();

            GameManager.instance.returnedToCastle = false;
            saveSystem.SaveGame();

            return true;
        }
        catch (System.Exception e)
        {
            LogTest($"❌ Save in different states failed: {e.Message}");
            return false;
        }
    }

    bool ValidateDataIntegrity(GameStateSnapshot original, GameStateSnapshot loaded)
    {
        LogTest($"Validating data integrity...");
        LogTest($"Original: {original}");
        LogTest($"Loaded:   {loaded}");

        bool dayIntact = original.currentDay == loaded.currentDay;
        bool bloodIntact = Mathf.Approximately(original.currentBlood, loaded.currentBlood);
        bool carryOverIntact = Mathf.Approximately(original.bloodCarryOver, loaded.bloodCarryOver);
        bool castleIntact = original.returnedToCastle == loaded.returnedToCastle;
        bool upgradesIntact = original.upgradePoints == loaded.upgradePoints;
        bool timeIntact = Mathf.Approximately(original.currentTime, loaded.currentTime);
        bool spottedIntact = original.timesSpotted == loaded.timesSpotted;

        LogTest($"Integrity check - Day:{dayIntact}, Blood:{bloodIntact}, Carry:{carryOverIntact}, " +
                $"Castle:{castleIntact}, Upgrades:{upgradesIntact}, Time:{timeIntact}, Spotted:{spottedIntact}");

        return dayIntact && bloodIntact && carryOverIntact && castleIntact &&
               upgradesIntact && timeIntact && spottedIntact;
    }

    void LogTest(string message)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[Save/Load Test] {message}");
        }
    }

    void LogTestResults()
    {
        Debug.Log("=== SP-010 Test Results Summary ===");
        foreach (string result in testResults)
        {
            Debug.Log(result);
        }

        int passed = 0;
        int failed = 0;
        int warnings = 0;

        foreach (string result in testResults)
        {
            if (result.StartsWith("✅"))
                passed++;
            else if (result.StartsWith("❌"))
                failed++;
            else if (result.StartsWith("⚠️"))
                warnings++;
        }

        Debug.Log($"Tests Passed: {passed}, Failed: {failed}, Warnings: {warnings}");

        if (failed == 0)
        {
            Debug.Log("🎉 All SP-010 save/load tests PASSED!");
        }
        else
        {
            Debug.LogWarning($"⚠️ {failed} SP-010 tests FAILED - save system needs attention");
        }
    }
}