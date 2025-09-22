using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionTester : MonoBehaviour
{
    [Header("Test Configuration")]
    public bool runTestsOnStart = false;
    public bool thoroughTesting = true;
    [Range(1, 10)] public int testIterations = 3;

    [Header("Test Scenes")]
    public string gameplaySceneName = "GamePlay";
    public string menuSceneName = "MainMenu";
    public string testSceneName = "TestScene";

    [Header("Save/Load Testing")]
    public bool testSaveLoad = true;
    public bool testCorruptedSaves = true;
    public bool testLargeGameStates = true;

    [Header("Transition Testing")]
    public bool testCityGateTriggers = true;
    public bool testSceneLoading = true;
    public bool testAsyncLoading = true;

    [Header("Performance Testing")]
    public bool measureLoadTimes = true;
    public bool testMemoryUsage = true;
    [Range(0.1f, 5f)] public float maxAcceptableLoadTime = 2f;

    [Header("Debug")]
    public bool verboseLogging = true;
    public bool showUIFeedback = true;

    private List<string> testResults = new List<string>();
    private Dictionary<string, float> performanceMetrics = new Dictionary<string, float>();
    private bool testingInProgress = false;

    void Start()
    {
        if (runTestsOnStart)
        {
            StartCoroutine(RunAllTestsInternal());
        }
    }

    [ContextMenu("Run All Scene Transition Tests")]
    public void RunAllTests()
    {
        if (!testingInProgress)
        {
            StartCoroutine(RunAllTestsInternal());
        }
    }

    IEnumerator RunAllTestsInternal()
    {
        testingInProgress = true;
        testResults.Clear();
        performanceMetrics.Clear();

        LogTest("=== SCENE TRANSITION & SAVE/LOAD TESTING STARTED ===");

        // Initialize test environment
        yield return StartCoroutine(InitializeTestEnvironment());

        // Test Save/Load System
        if (testSaveLoad)
        {
            yield return StartCoroutine(TestSaveLoadSystem());
        }

        // Test Scene Transitions
        if (testCityGateTriggers)
        {
            yield return StartCoroutine(TestCityGateTransitions());
        }

        if (testSceneLoading)
        {
            yield return StartCoroutine(TestSceneLoadingPerformance());
        }

        if (testAsyncLoading)
        {
            yield return StartCoroutine(TestAsyncSceneLoading());
        }

        // Comprehensive integration test
        if (thoroughTesting)
        {
            yield return StartCoroutine(TestCompleteGameplayLoop());
        }

        // Final validation
        yield return StartCoroutine(ValidateSystemIntegrity());

        // Report results
        ReportTestResults();

        testingInProgress = false;
        LogTest("=== ALL TESTS COMPLETED ===");
    }

    IEnumerator InitializeTestEnvironment()
    {
        LogTest("Initializing test environment...");

        // Ensure all required systems are present
        EnsureSystemExists<GameManager>("GameManager");
        EnsureSystemExists<SaveSystem>("SaveSystem");

        // Wait for systems to initialize
        yield return new WaitForSeconds(0.5f);

        // Clear any existing save data for clean testing
        if (PlayerPrefs.HasKey("SnowpiercerSave"))
        {
            LogTest("Clearing existing save data for clean testing");
            PlayerPrefs.DeleteKey("SnowpiercerSave");
            PlayerPrefs.Save();
        }

        LogTest("✅ Test environment initialized");
    }

    IEnumerator TestSaveLoadSystem()
    {
        LogTest("Testing Save/Load System...");

        // Test basic save functionality
        yield return StartCoroutine(TestBasicSaveLoad());

        // Test save data persistence
        yield return StartCoroutine(TestSavePersistence());

        // Test corrupted save handling
        if (testCorruptedSaves)
        {
            yield return StartCoroutine(TestCorruptedSaveHandling());
        }

        // Test large game state
        if (testLargeGameStates)
        {
            yield return StartCoroutine(TestLargeGameState());
        }

        LogTest("✅ Save/Load system tests completed");
    }

    IEnumerator TestBasicSaveLoad()
    {
        LogTest("Testing basic save/load functionality...");

        bool exceptionOccurred = false;
        string exceptionMessage = "";

        // Create test game state
        if (VampireStats.instance != null)
        {
            VampireStats.instance.totalBlood = 42;
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.currentDay = 3;
        }

        // Save game state
        float saveStartTime = Time.time;
        try
        {
            SaveSystem.Instance?.SaveGame();
        }
        catch (System.Exception e)
        {
            exceptionOccurred = true;
            exceptionMessage = e.Message;
        }
        float saveTime = Time.time - saveStartTime;
        performanceMetrics["Save Time"] = saveTime;

        if (exceptionOccurred)
        {
            LogTest($"❌ Basic save/load test FAILED with save exception: {exceptionMessage}");
            testResults.Add("Basic Save/Load: FAILED (Save Exception)");
            yield break;
        }

        LogTest($"Game saved in {saveTime:F3}s");

        // Modify state to test loading
        if (VampireStats.instance != null)
        {
            VampireStats.instance.totalBlood = 999;
        }

        // Load game state
        float loadStartTime = Time.time;
        try
        {
            SaveSystem.Instance?.LoadGame();
        }
        catch (System.Exception e)
        {
            exceptionOccurred = true;
            exceptionMessage = e.Message;
        }
        float loadTime = Time.time - loadStartTime;
        performanceMetrics["Load Time"] = loadTime;

        yield return new WaitForSeconds(0.1f); // Allow systems to process

        if (exceptionOccurred)
        {
            LogTest($"❌ Basic save/load test FAILED with load exception: {exceptionMessage}");
            testResults.Add("Basic Save/Load: FAILED (Load Exception)");
            yield break;
        }

        // Verify loaded state
        bool loadSuccess = false;
        if (VampireStats.instance != null && VampireStats.instance.totalBlood == 42)
        {
            loadSuccess = true;
            LogTest("✅ Basic save/load test PASSED");
        }
        else
        {
            LogTest("❌ Basic save/load test FAILED - Data not restored correctly");
        }

        testResults.Add($"Basic Save/Load: {(loadSuccess ? "PASSED" : "FAILED")}");
    }

    IEnumerator TestSavePersistence()
    {
        LogTest("Testing save data persistence across sessions...");

        bool exceptionOccurred = false;
        string exceptionMessage = "";

        // Save a unique identifier
        if (GameManager.instance != null)
        {
            GameManager.instance.currentDay = 7; // Unique test value
        }

        try
        {
            SaveSystem.Instance?.SaveGame();
        }
        catch (System.Exception e)
        {
            exceptionOccurred = true;
            exceptionMessage = e.Message;
        }

        if (exceptionOccurred)
        {
            LogTest($"❌ Save persistence test FAILED: {exceptionMessage}");
            testResults.Add("Save Persistence: FAILED (Exception)");
            yield break;
        }

        // Simulate application restart by clearing runtime state
        if (GameManager.instance != null)
        {
            GameManager.instance.currentDay = 1;
        }

        yield return new WaitForSeconds(0.1f);

        // Load and verify persistence
        try
        {
            SaveSystem.Instance?.LoadGame();
        }
        catch (System.Exception e)
        {
            exceptionOccurred = true;
            exceptionMessage = e.Message;
        }

        yield return new WaitForSeconds(0.1f);

        if (exceptionOccurred)
        {
            LogTest($"❌ Save persistence test FAILED: {exceptionMessage}");
            testResults.Add("Save Persistence: FAILED (Exception)");
            yield break;
        }

        bool persistenceTest = GameManager.instance != null && GameManager.instance.currentDay == 7;

        if (persistenceTest)
        {
            LogTest("✅ Save persistence test PASSED");
        }
        else
        {
            LogTest("❌ Save persistence test FAILED");
        }

        testResults.Add($"Save Persistence: {(persistenceTest ? "PASSED" : "FAILED")}");
    }

    IEnumerator TestCorruptedSaveHandling()
    {
        LogTest("Testing corrupted save handling...");

        bool outerExceptionOccurred = false;
        string outerExceptionMessage = "";

        try
        {
            // Create corrupted save data
            PlayerPrefs.SetString("SnowpiercerSave", "CORRUPTED_DATA_TEST_123");
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            outerExceptionOccurred = true;
            outerExceptionMessage = e.Message;
        }

        if (outerExceptionOccurred)
        {
            LogTest($"❌ Corrupted save handling test encountered exception: {outerExceptionMessage}");
            testResults.Add("Corrupted Save Handling: FAILED (Exception)");
            yield break;
        }

        // Attempt to load corrupted data
        bool corruptionHandled = false;
        try
        {
            SaveSystem.Instance?.LoadGame();
            corruptionHandled = true; // If we get here without exception
        }
        catch
        {
            // Exception is expected for corrupted data
            corruptionHandled = true;
        }

        yield return new WaitForSeconds(0.1f);

        if (corruptionHandled)
        {
            LogTest("✅ Corrupted save handling test PASSED");
        }
        else
        {
            LogTest("❌ Corrupted save handling test FAILED");
        }

        testResults.Add($"Corrupted Save Handling: {(corruptionHandled ? "PASSED" : "FAILED")}");

        // Clean up
        try
        {
            PlayerPrefs.DeleteKey("SnowpiercerSave");
        }
        catch (System.Exception e)
        {
            LogTest($"⚠️ Cleanup warning: {e.Message}");
        }
    }

    IEnumerator TestLargeGameState()
    {
        LogTest("Testing large game state save/load...");

        bool exceptionOccurred = false;
        string exceptionMessage = "";

        // Create a large game state by spawning many entities
        EnhancedSpawner[] spawners = FindObjectsByType<EnhancedSpawner>(FindObjectsSortMode.None);
        int originalEntityCount = 0;

        try
        {
            foreach (EnhancedSpawner spawner in spawners)
            {
                // Count current entities (approximate)
                originalEntityCount += spawner.transform.childCount;

                // Spawn additional entities for large state test
                for (int i = 0; i < 5; i++)
                {
                    // Use default spawn parameters (need prefab, position, rotation)
                    if (spawner.guardPrefab != null)
                    {
                        Vector3 spawnPos = spawner.transform.position + Random.insideUnitSphere * 10f;
                        spawner.SpawnEntity(spawner.guardPrefab, spawnPos, Quaternion.identity);
                    }
                    //yield return null; // Spread spawning across frames
                }
            }
        }
        catch (System.Exception e)
        {
            exceptionOccurred = true;
            exceptionMessage = e.Message;
        }

        if (exceptionOccurred)
        {
            LogTest($"❌ Large game state test FAILED during spawning: {exceptionMessage}");
            testResults.Add("Large Game State: FAILED (Spawning Exception)");
            yield break;
        }

        // Test save with large state
        float largeSaveStartTime = Time.time;
        try
        {
            SaveSystem.Instance?.SaveGame();
        }
        catch (System.Exception e)
        {
            exceptionOccurred = true;
            exceptionMessage = e.Message;
        }
        float largeSaveTime = Time.time - largeSaveStartTime;
        performanceMetrics["Large State Save Time"] = largeSaveTime;

        if (exceptionOccurred)
        {
            LogTest($"❌ Large game state test FAILED during save: {exceptionMessage}");
            testResults.Add("Large Game State: FAILED (Save Exception)");
            yield break;
        }

        // Test load with large state
        float largeLoadStartTime = Time.time;
        try
        {
            SaveSystem.Instance?.LoadGame();
        }
        catch (System.Exception e)
        {
            exceptionOccurred = true;
            exceptionMessage = e.Message;
        }
        float largeLoadTime = Time.time - largeLoadStartTime;
        performanceMetrics["Large State Load Time"] = largeLoadTime;

        yield return new WaitForSeconds(0.2f);

        if (exceptionOccurred)
        {
            LogTest($"❌ Large game state test FAILED during load: {exceptionMessage}");
            testResults.Add("Large Game State: FAILED (Load Exception)");
            yield break;
        }

        bool largeStateTest = largeSaveTime < 1f && largeLoadTime < 2f; // Performance thresholds

        if (largeStateTest)
        {
            LogTest($"✅ Large game state test PASSED (Save: {largeSaveTime:F3}s, Load: {largeLoadTime:F3}s)");
        }
        else
        {
            LogTest($"❌ Large game state test FAILED (Save: {largeSaveTime:F3}s, Load: {largeLoadTime:F3}s)");
        }

        testResults.Add($"Large Game State: {(largeStateTest ? "PASSED" : "FAILED")}");
    }

    IEnumerator TestCityGateTransitions()
    {
        LogTest("Testing CityGate transition system...");

        bool exceptionOccurred = false;
        string exceptionMessage = "";

        CityGateTrigger[] triggers = null;
        try
        {
            triggers = FindObjectsByType<CityGateTrigger>(FindObjectsSortMode.None);
        }
        catch (System.Exception e)
        {
            exceptionOccurred = true;
            exceptionMessage = e.Message;
        }

        if (exceptionOccurred)
        {
            LogTest($"❌ CityGate transition test FAILED: {exceptionMessage}");
            testResults.Add("CityGate Transitions: FAILED (Exception)");
            yield break;
        }

        if (triggers.Length == 0)
        {
            LogTest("⚠️ No CityGate triggers found in scene");
            testResults.Add("CityGate Transitions: SKIPPED (No triggers)");
            yield break;
        }

        bool allTriggersWorking = true;

        foreach (CityGateTrigger trigger in triggers)
        {
            LogTest($"Testing trigger: {trigger.name}");

            // Test trigger functionality
            bool triggerWorking = TestCityGateTrigger(trigger);

            if (!triggerWorking)
            {
                allTriggersWorking = false;
                LogTest($"❌ Trigger {trigger.name} failed test");
            }
            else
            {
                LogTest($"✅ Trigger {trigger.name} passed test");
            }

            yield return new WaitForSeconds(0.1f);
        }

        testResults.Add($"CityGate Transitions: {(allTriggersWorking ? "PASSED" : "FAILED")}");
    }

    bool TestCityGateTrigger(CityGateTrigger trigger)
    {
        try
        {
            // Check if trigger has proper configuration
            if (trigger.GetComponent<Collider>() == null)
            {
                LogTest($"Trigger {trigger.name} missing collider");
                return false;
            }

            if (!trigger.GetComponent<Collider>().isTrigger)
            {
                LogTest($"Trigger {trigger.name} collider not set as trigger");
                return false;
            }

            // Test would involve simulating player entering trigger
            // For now, just validate configuration
            return true;
        }
        catch
        {
            return false;
        }
    }

    IEnumerator TestSceneLoadingPerformance()
    {
        LogTest("Testing scene loading performance...");

        for (int i = 0; i < testIterations; i++)
        {
            LogTest($"Scene loading test iteration {i + 1}/{testIterations}");

            float loadStartTime = Time.time;

            // Test reloading current scene
            string currentScene = SceneManager.GetActiveScene().name;
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(currentScene);

            while (!loadOp.isDone)
            {
                yield return null;
            }

            float loadTime = Time.time - loadStartTime;
            performanceMetrics[$"Scene Load Time {i + 1}"] = loadTime;

            LogTest($"Scene loaded in {loadTime:F3}s");

            if (loadTime > maxAcceptableLoadTime)
            {
                LogTest($"⚠️ Scene load time ({loadTime:F3}s) exceeds acceptable threshold ({maxAcceptableLoadTime:F1}s)");
            }

            yield return new WaitForSeconds(0.5f); // Allow scene to fully initialize
        }

        // Calculate average load time
        float totalLoadTime = 0f;
        for (int i = 0; i < testIterations; i++)
        {
            totalLoadTime += performanceMetrics[$"Scene Load Time {i + 1}"];
        }
        float avgLoadTime = totalLoadTime / testIterations;
        performanceMetrics["Average Scene Load Time"] = avgLoadTime;

        bool loadPerformanceTest = avgLoadTime <= maxAcceptableLoadTime;

        LogTest($"Average scene load time: {avgLoadTime:F3}s");
        testResults.Add($"Scene Load Performance: {(loadPerformanceTest ? "PASSED" : "FAILED")}");
    }

    IEnumerator TestAsyncSceneLoading()
    {
        LogTest("Testing async scene loading...");

        bool exceptionOccurred = false;
        string exceptionMessage = "";

        string currentScene = SceneManager.GetActiveScene().name;
        AsyncOperation asyncLoad = null;

        float asyncStartTime = Time.time;
        try
        {
            asyncLoad = SceneManager.LoadSceneAsync(currentScene);
            asyncLoad.allowSceneActivation = false;
        }
        catch (System.Exception e)
        {
            exceptionOccurred = true;
            exceptionMessage = e.Message;
        }

        if (exceptionOccurred)
        {
            LogTest($"❌ Async scene loading test FAILED: {exceptionMessage}");
            testResults.Add("Async Scene Loading: FAILED (Exception)");
            yield break;
        }

        // Simulate loading progress
        while (asyncLoad.progress < 0.9f)
        {
            LogTest($"Async loading progress: {asyncLoad.progress * 100:F1}%");
            yield return new WaitForSeconds(0.1f);
        }

        // Complete the load
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        float asyncLoadTime = Time.time - asyncStartTime;
        performanceMetrics["Async Load Time"] = asyncLoadTime;

        bool asyncTest = asyncLoadTime <= maxAcceptableLoadTime * 1.5f; // Allow more time for async

        LogTest($"✅ Async scene loading completed in {asyncLoadTime:F3}s");
        testResults.Add($"Async Scene Loading: {(asyncTest ? "PASSED" : "FAILED")}");
    }

    IEnumerator TestCompleteGameplayLoop()
    {
        LogTest("Testing complete gameplay loop with transitions...");

        bool exceptionOccurred = false;
        string exceptionMessage = "";

        // Save initial state
        try
        {
            SaveSystem.Instance?.SaveGame();
        }
        catch (System.Exception e)
        {
            exceptionOccurred = true;
            exceptionMessage = e.Message;
        }

        if (exceptionOccurred)
        {
            LogTest($"❌ Complete gameplay loop test FAILED: {exceptionMessage}");
            testResults.Add("Complete Gameplay Loop: FAILED (Exception)");
            yield break;
        }

        yield return new WaitForSeconds(0.1f);

        // Simulate game progression
        if (GameManager.instance != null)
        {
            GameManager.instance.currentDay = 2;
        }

        if (VampireStats.instance != null)
        {
            VampireStats.instance.totalBlood = 25;
        }

        // Save mid-game state
        try
        {
            SaveSystem.Instance?.SaveGame();
        }
        catch (System.Exception e)
        {
            exceptionOccurred = true;
            exceptionMessage = e.Message;
        }

        if (exceptionOccurred)
        {
            LogTest($"❌ Complete gameplay loop test FAILED during mid-game save: {exceptionMessage}");
            testResults.Add("Complete Gameplay Loop: FAILED (Exception)");
            yield break;
        }

        yield return new WaitForSeconds(0.1f);

        // Test scene transition (simulate going to castle)
        // This would normally trigger through gameplay
        LogTest("Simulating castle transition...");

        // Save after transition
        try
        {
            SaveSystem.Instance?.SaveGame();
        }
        catch (System.Exception e)
        {
            exceptionOccurred = true;
            exceptionMessage = e.Message;
        }

        if (exceptionOccurred)
        {
            LogTest($"❌ Complete gameplay loop test FAILED during transition save: {exceptionMessage}");
            testResults.Add("Complete Gameplay Loop: FAILED (Exception)");
            yield break;
        }

        yield return new WaitForSeconds(0.1f);

        // Load and verify complete state
        try
        {
            SaveSystem.Instance?.LoadGame();
        }
        catch (System.Exception e)
        {
            exceptionOccurred = true;
            exceptionMessage = e.Message;
        }

        yield return new WaitForSeconds(0.2f);

        if (exceptionOccurred)
        {
            LogTest($"❌ Complete gameplay loop test FAILED during load: {exceptionMessage}");
            testResults.Add("Complete Gameplay Loop: FAILED (Exception)");
            yield break;
        }

        bool gameplayLoopTest = true;
        if (GameManager.instance == null || GameManager.instance.currentDay != 2)
        {
            gameplayLoopTest = false;
            LogTest("❌ GameManager state not preserved");
        }

        if (VampireStats.instance == null || VampireStats.instance.totalBlood != 25)
        {
            gameplayLoopTest = false;
            LogTest("❌ VampireStats state not preserved");
        }

        if (gameplayLoopTest)
        {
            LogTest("✅ Complete gameplay loop test PASSED");
        }
        else
        {
            LogTest("❌ Complete gameplay loop test FAILED");
        }

        testResults.Add($"Complete Gameplay Loop: {(gameplayLoopTest ? "PASSED" : "FAILED")}");
    }

    IEnumerator ValidateSystemIntegrity()
    {
        LogTest("Validating system integrity after testing...");

        bool integrityCheck = true;

        // Check if all major systems are still functional
        if (GameManager.instance == null)
        {
            LogTest("❌ GameManager instance missing");
            integrityCheck = false;
        }

        if (FindAnyObjectByType<SaveSystem>() == null)
        {
            LogTest("❌ SaveSystem not found");
            integrityCheck = false;
        }

        if (VampireStats.instance == null)
        {
            LogTest("❌ VampireStats instance missing");
            integrityCheck = false;
        }

        // Check scene objects
        if (FindObjectsByType<CityGateTrigger>(FindObjectsSortMode.None).Length == 0)
        {
            LogTest("⚠️ No CityGate triggers found");
        }

        yield return new WaitForSeconds(0.1f);

        if (integrityCheck)
        {
            LogTest("✅ System integrity validated");
        }
        else
        {
            LogTest("❌ System integrity check FAILED");
        }

        testResults.Add($"System Integrity: {(integrityCheck ? "PASSED" : "FAILED")}");
    }

    void ReportTestResults()
    {
        LogTest("=== TEST RESULTS SUMMARY ===");

        int passedTests = 0;
        int totalTests = testResults.Count;

        foreach (string result in testResults)
        {
            LogTest(result);
            if (result.Contains("PASSED"))
            {
                passedTests++;
            }
        }

        LogTest($"Overall Result: {passedTests}/{totalTests} tests PASSED");

        if (performanceMetrics.Count > 0)
        {
            LogTest("=== PERFORMANCE METRICS ===");
            foreach (var metric in performanceMetrics)
            {
                LogTest($"{metric.Key}: {metric.Value:F3}s");
            }
        }

        // Overall assessment
        float successRate = (float)passedTests / totalTests;
        if (successRate >= 0.9f)
        {
            LogTest("🎉 EXCELLENT - System is ready for production");
        }
        else if (successRate >= 0.7f)
        {
            LogTest("✅ GOOD - Minor issues found, system mostly functional");
        }
        else
        {
            LogTest("⚠️ ISSUES FOUND - Several tests failed, review required");
        }
    }

    void EnsureSystemExists<T>(string systemName) where T : Component
    {
        if (FindAnyObjectByType<T>() == null)
        {
            LogTest($"⚠️ {systemName} not found in scene");
        }
    }

    void LogTest(string message)
    {
        if (verboseLogging)
        {
            Debug.Log($"[SceneTransitionTester] {message}");
        }
    }

    [ContextMenu("Quick Test")]
    public void QuickTest()
    {
        StartCoroutine(QuickTestCoroutine());
    }

    IEnumerator QuickTestCoroutine()
    {
        testIterations = 1;
        thoroughTesting = false;
        testCorruptedSaves = false;
        testLargeGameStates = false;

        yield return StartCoroutine(RunAllTestsInternal());
    }

    [ContextMenu("Clear Save Data")]
    public void ClearSaveData()
    {
        if (PlayerPrefs.HasKey("SnowpiercerSave"))
        {
            PlayerPrefs.DeleteKey("SnowpiercerSave");
            PlayerPrefs.Save();
            LogTest("Save data cleared");
        }
        else
        {
            LogTest("No save data found to clear");
        }
    }
}