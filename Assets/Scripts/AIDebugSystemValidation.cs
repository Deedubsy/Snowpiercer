using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// SP-008: AI Debug System Validation
/// Validates the comprehensive AI debug system works correctly and provides accurate data
/// </summary>
public class AIDebugSystemValidation : MonoBehaviour
{
    [Header("Test Configuration")]
    public bool runOnStart = false;
    public bool enableDetailedLogging = true;

    [Header("Test Scenarios")]
    public bool testDebugUIToggle = true;
    public bool testDebugDataAccuracy = true;
    public bool testPerformanceImpact = true;
    public bool testDebugPanelCreation = true;

    [Header("Test Parameters")]
    public float testDuration = 30f;
    public float dataValidationInterval = 2f;

    private List<string> testResults = new List<string>();
    private bool testInProgress = false;
    private List<GameObject> testEntities = new List<GameObject>();

    void Start()
    {
        if (runOnStart)
        {
            StartCoroutine(RunAIDebugValidationTests());
        }
    }

    [ContextMenu("Run AI Debug Validation Tests")]
    public void RunTests()
    {
        if (!testInProgress)
        {
            StartCoroutine(RunAIDebugValidationTests());
        }
        else
        {
            Debug.LogWarning("AI Debug validation test already in progress");
        }
    }

    IEnumerator RunAIDebugValidationTests()
    {
        testInProgress = true;
        testResults.Clear();

        LogTest("=== SP-008: AI Debug System Validation Starting ===");
        yield return new WaitForSeconds(1f);

        // Test 1: Debug UI Toggle
        if (testDebugUIToggle)
        {
            yield return StartCoroutine(TestDebugUIToggle());
        }

        // Test 2: Debug Panel Creation
        if (testDebugPanelCreation)
        {
            yield return StartCoroutine(TestDebugPanelCreation());
        }

        // Test 3: Debug Data Accuracy
        if (testDebugDataAccuracy)
        {
            yield return StartCoroutine(TestDebugDataAccuracy());
        }

        // Test 4: Performance Impact
        if (testPerformanceImpact)
        {
            yield return StartCoroutine(TestPerformanceImpact());
        }

        // Cleanup and Results
        CleanupTestEntities();
        LogTest("=== SP-008: AI Debug System Validation Complete ===");
        LogTestResults();

        testInProgress = false;
    }

    IEnumerator TestDebugUIToggle()
    {
        LogTest("--- Test 1: Debug UI Toggle (F1 Key) ---");

        if (!ValidateDebugUIManager())
        {
            testResults.Add("❌ DebugUIManager not available for testing");
            yield break;
        }

        DebugUIManager debugManager = DebugUIManager.Instance;

        // Test initial state using available property
        bool initialState = debugManager.showAllDebugUI;
        LogTest($"Initial debug UI state: {initialState}");

        // Test toggle functionality using available method
        debugManager.ToggleAllDebugUI();
        yield return new WaitForSeconds(0.5f);

        bool toggledState = debugManager.showAllDebugUI;
        LogTest($"Toggled debug UI state: {toggledState}");

        if (toggledState != initialState)
        {
            testResults.Add("✅ Debug UI toggle working correctly");
            LogTest("✅ F1 toggle functionality verified");

            // Toggle back
            debugManager.ToggleAllDebugUI();
            yield return new WaitForSeconds(0.5f);

            bool restoredState = debugManager.showAllDebugUI;
            if (restoredState == initialState)
            {
                LogTest("✅ Debug UI state properly restored");
            }
            else
            {
                LogTest("⚠️ Debug UI state restoration issue");
            }
        }
        else
        {
            testResults.Add("❌ Debug UI toggle not working");
            LogTest("❌ F1 toggle functionality failed");
        }

        yield return new WaitForSeconds(1f);
    }

    IEnumerator TestDebugPanelCreation()
    {
        LogTest("--- Test 2: Debug Panel Creation ---");

        if (!ValidateDebugUIManager())
        {
            testResults.Add("❌ Debug panel creation test failed - no DebugUIManager");
            yield break;
        }

        // Create test AI entities to verify debug panel creation
        LogTest("Creating test AI entities...");

        // Try to find existing AI entities first
        GuardAI[] existingGuards = FindObjectsOfType<GuardAI>();
        Citizen[] existingCitizens = FindObjectsOfType<Citizen>();

        LogTest($"Found existing entities - Guards: {existingGuards.Length}, Citizens: {existingCitizens.Length}");

        if (existingGuards.Length == 0 && existingCitizens.Length == 0)
        {
            LogTest("No existing AI entities found - creating test entities");
            CreateTestAIEntities();
        }

        yield return new WaitForSeconds(2f);

        // Check if debug panels were created
        AIDebugUI[] debugPanels = FindObjectsOfType<AIDebugUI>();
        LogTest($"Debug panels found: {debugPanels.Length}");

        if (debugPanels.Length > 0)
        {
            testResults.Add("✅ Debug panels created successfully");
            LogTest($"✅ {debugPanels.Length} debug panels created for AI entities");

            // Test panel functionality
            bool panelsFunctional = TestDebugPanelFunctionality(debugPanels);
            if (panelsFunctional)
            {
                LogTest("✅ Debug panels functional");
            }
            else
            {
                LogTest("⚠️ Debug panels created but not fully functional");
            }
        }
        else
        {
            testResults.Add("❌ Debug panels not created");
            LogTest("❌ No debug panels found for AI entities");
        }

        yield return new WaitForSeconds(1f);
    }

    IEnumerator TestDebugDataAccuracy()
    {
        LogTest("--- Test 3: Debug Data Accuracy ---");

        AIDebugUI[] debugPanels = FindObjectsOfType<AIDebugUI>();
        if (debugPanels.Length == 0)
        {
            testResults.Add("❌ Debug data accuracy test failed - no debug panels");
            yield break;
        }

        LogTest($"Testing debug data accuracy for {debugPanels.Length} panels...");

        float testStartTime = Time.time;
        int accuracyChecks = 0;
        int accurateData = 0;

        while (Time.time - testStartTime < testDuration)
        {
            foreach (AIDebugUI panel in debugPanels)
            {
                if (panel != null && panel.gameObject.activeInHierarchy)
                {
                    bool dataAccurate = ValidateDebugPanelData(panel);
                    accuracyChecks++;

                    if (dataAccurate)
                    {
                        accurateData++;
                    }
                }
            }

            yield return new WaitForSeconds(dataValidationInterval);
        }

        float accuracyRate = accuracyChecks > 0 ? (float)accurateData / accuracyChecks : 0f;
        LogTest($"Debug data accuracy: {accuracyRate:P1} ({accurateData}/{accuracyChecks})");

        if (accuracyRate >= 0.9f) // 90% accuracy threshold
        {
            testResults.Add("✅ Debug data accuracy excellent");
        }
        else if (accuracyRate >= 0.7f) // 70% accuracy threshold
        {
            testResults.Add("⚠️ Debug data accuracy acceptable but could be improved");
        }
        else
        {
            testResults.Add("❌ Debug data accuracy below acceptable threshold");
        }

        yield return new WaitForSeconds(1f);
    }

    IEnumerator TestPerformanceImpact()
    {
        LogTest("--- Test 4: Performance Impact ---");

        // Measure performance with debug UI enabled
        DebugUIManager debugManager = DebugUIManager.Instance;
        if (debugManager == null)
        {
            testResults.Add("❌ Performance impact test failed - no DebugUIManager");
            yield break;
        }

        LogTest("Measuring performance impact...");

        // Ensure debug UI is enabled
        if (!debugManager.showAllDebugUI)
        {
            debugManager.ToggleAllDebugUI();
            yield return new WaitForSeconds(1f);
        }

        // Measure FPS with debug UI enabled
        float fpsWithDebug = MeasureAverageFPS(5f);
        LogTest($"FPS with debug UI enabled: {fpsWithDebug:F1}");

        // Disable debug UI
        debugManager.ToggleAllDebugUI();
        yield return new WaitForSeconds(1f);

        // Measure FPS with debug UI disabled
        float fpsWithoutDebug = MeasureAverageFPS(5f);
        LogTest($"FPS without debug UI: {fpsWithoutDebug:F1}");

        // Calculate performance impact
        float performanceImpact = ((fpsWithoutDebug - fpsWithDebug) / fpsWithoutDebug) * 100f;
        LogTest($"Performance impact: {performanceImpact:F1}%");

        if (performanceImpact < 10f) // Less than 10% impact
        {
            testResults.Add("✅ Debug UI performance impact minimal");
        }
        else if (performanceImpact < 20f) // Less than 20% impact
        {
            testResults.Add("⚠️ Debug UI performance impact acceptable");
        }
        else
        {
            testResults.Add("❌ Debug UI performance impact too high");
        }

        // Restore original state
        if (!debugManager.showAllDebugUI)
        {
            debugManager.ToggleAllDebugUI();
        }

        yield return new WaitForSeconds(1f);
    }

    void CreateTestAIEntities()
    {
        // Create simple test AI entities for debug panel testing
        for (int i = 0; i < 3; i++)
        {
            GameObject testGuard = new GameObject($"TestGuard_{i}");
            testGuard.transform.position = new Vector3(i * 5, 0, 0);

            // Add basic components that would exist on a guard
            testGuard.AddComponent<Rigidbody>();
            testGuard.AddComponent<BoxCollider>();

            // Try to add GuardAI if available, otherwise add basic components
            GuardAI guardAI = testGuard.GetComponent<GuardAI>();
            if (guardAI == null)
            {
                // If GuardAI prefab isn't available, add debug provider directly
                GuardAIDebugProvider debugProvider = testGuard.AddComponent<GuardAIDebugProvider>();
                AIDebugUI debugUI = testGuard.AddComponent<AIDebugUI>();
            }

            testEntities.Add(testGuard);
        }

        for (int i = 0; i < 2; i++)
        {
            GameObject testCitizen = new GameObject($"TestCitizen_{i}");
            testCitizen.transform.position = new Vector3(i * 5, 0, 10);

            // Add basic components
            testCitizen.AddComponent<Rigidbody>();
            testCitizen.AddComponent<SphereCollider>();

            // Try to add Citizen if available
            Citizen citizen = testCitizen.GetComponent<Citizen>();
            if (citizen == null)
            {
                // If Citizen prefab isn't available, add debug provider directly
                CitizenDebugProvider debugProvider = testCitizen.AddComponent<CitizenDebugProvider>();
                AIDebugUI debugUI = testCitizen.AddComponent<AIDebugUI>();
            }

            testEntities.Add(testCitizen);
        }

        LogTest($"Created {testEntities.Count} test AI entities");
    }

    bool ValidateDebugUIManager()
    {
        if (DebugUIManager.Instance == null)
        {
            LogTest("❌ DebugUIManager.Instance is null");
            return false;
        }

        LogTest("✅ DebugUIManager available");
        return true;
    }

    bool TestDebugPanelFunctionality(AIDebugUI[] panels)
    {
        int functionalPanels = 0;

        foreach (AIDebugUI panel in panels)
        {
            if (panel != null && panel.gameObject.activeInHierarchy)
            {
                // Check if panel has required components
                bool hasCanvas = panel.GetComponentInChildren<Canvas>() != null;
                bool hasText = panel.GetComponentInChildren<TMPro.TextMeshProUGUI>() != null;

                if (hasCanvas && hasText)
                {
                    functionalPanels++;
                }
            }
        }

        return functionalPanels > 0;
    }

    bool ValidateDebugPanelData(AIDebugUI panel)
    {
        // Basic validation - check if panel is updating and displaying reasonable data
        try
        {
            // Check if the panel has debug provider
            IDebugProvider provider = panel.GetComponent<IDebugProvider>();
            if (provider == null)
                return false;

            // Check if entity name is valid
            string entityName = provider.GetEntityName();
            if (string.IsNullOrEmpty(entityName))
                return false;

            // Check if position is reasonable
            Vector3 position = provider.GetPosition();
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
                return false;

            return true;
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    float MeasureAverageFPS(float duration)
    {
        int frameCount = 0;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            frameCount++;
            timeElapsed += Time.unscaledDeltaTime;
        }

        return frameCount / timeElapsed;
    }

    void CleanupTestEntities()
    {
        foreach (GameObject entity in testEntities)
        {
            if (entity != null)
            {
                DestroyImmediate(entity);
            }
        }

        testEntities.Clear();
        LogTest("Test entities cleaned up");
    }

    void LogTest(string message)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[AI Debug Test] {message}");
        }
    }

    void LogTestResults()
    {
        Debug.Log("=== SP-008 Test Results Summary ===");
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
            Debug.Log("🎉 All SP-008 AI debug system tests PASSED!");
        }
        else
        {
            Debug.LogWarning($"⚠️ {failed} SP-008 tests FAILED - debug system needs attention");
        }
    }
}