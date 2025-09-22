using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;

/// <summary>
/// SP-004: Performance testing with multiple AI entities
/// Tests game performance with 20+ Guards, 30+ Citizens, and monitors key metrics
/// </summary>
public class PerformanceStressTest : MonoBehaviour
{
    [Header("Test Configuration")]
    public bool runOnStart = false;
    public float testDuration = 60f; // Test for 1 minute
    public float sampleInterval = 1f; // Sample every second

    [Header("Entity Targets")]
    public int targetGuards = 20;
    public int targetCitizens = 30;

    [Header("Performance Targets")]
    [Tooltip("Minimum acceptable FPS")]
    public float targetFPS = 30f;
    [Tooltip("Maximum acceptable memory usage in MB")]
    public float maxMemoryMB = 2048f;
    [Tooltip("Maximum acceptable GC spike in ms")]
    public float maxGCSpike = 5f;

    [Header("Prefab References")]
    public GameObject guardPrefab;
    public GameObject citizenPrefab;
    public Transform spawnArea;
    public float spawnRadius = 50f;

    [Header("Test Results")]
    [SerializeField] private List<float> fpsHistory = new List<float>();
    [SerializeField] private List<float> memoryHistory = new List<float>();
    [SerializeField] private float averageFPS;
    [SerializeField] private float minFPS;
    [SerializeField] private float maxMemoryUsed;
    [SerializeField] private int totalFrames;

    private List<GameObject> spawnedEntities = new List<GameObject>();
    private bool testInProgress = false;
    private float testStartTime;

    void Start()
    {
        if (runOnStart)
        {
            StartPerformanceTest();
        }
    }

    [ContextMenu("Start Performance Test")]
    public void StartPerformanceTest()
    {
        if (!testInProgress)
        {
            StartCoroutine(RunPerformanceStressTest());
        }
        else
        {
            Debug.LogWarning("Performance test already in progress");
        }
    }

    IEnumerator RunPerformanceStressTest()
    {
        testInProgress = true;
        Debug.Log("=== SP-004: Performance Stress Test Starting ===");

        // Initialize test data
        fpsHistory.Clear();
        memoryHistory.Clear();
        totalFrames = 0;
        testStartTime = Time.time;

        // Step 1: Setup test environment
        yield return StartCoroutine(SetupTestEnvironment());

        // Step 2: Run performance monitoring
        yield return StartCoroutine(MonitorPerformance());

        // Step 3: Analyze results
        AnalyzeResults();

        // Step 4: Cleanup
        CleanupTestEnvironment();

        Debug.Log("=== SP-004: Performance Stress Test Complete ===");
        testInProgress = false;
    }

    IEnumerator SetupTestEnvironment()
    {
        Debug.Log("--- Setting up test environment ---");

        // Find or create spawn area
        if (spawnArea == null)
        {
            GameObject spawnAreaObj = new GameObject("Performance Test Spawn Area");
            spawnArea = spawnAreaObj.transform;
            spawnArea.position = Vector3.zero;
        }

        // Spawn Guards
        for (int i = 0; i < targetGuards; i++)
        {
            if (guardPrefab != null)
            {
                Vector3 spawnPos = GetRandomSpawnPosition();
                GameObject guard = Instantiate(guardPrefab, spawnPos, Quaternion.identity);
                guard.name = $"TestGuard_{i}";
                spawnedEntities.Add(guard);
            }
            else
            {
                Debug.LogWarning("Guard prefab not assigned - creating empty GameObjects for testing");
                GameObject guard = new GameObject($"TestGuard_{i}");
                guard.transform.position = GetRandomSpawnPosition();
                // Add basic components for testing
                guard.AddComponent<Rigidbody>();
                guard.AddComponent<BoxCollider>();
                spawnedEntities.Add(guard);
            }

            // Yield every few spawns to prevent frame hitches
            if (i % 5 == 0)
                yield return null;
        }

        Debug.Log($"Spawned {targetGuards} guards");

        // Spawn Citizens
        for (int i = 0; i < targetCitizens; i++)
        {
            if (citizenPrefab != null)
            {
                Vector3 spawnPos = GetRandomSpawnPosition();
                GameObject citizen = Instantiate(citizenPrefab, spawnPos, Quaternion.identity);
                citizen.name = $"TestCitizen_{i}";
                spawnedEntities.Add(citizen);
            }
            else
            {
                Debug.LogWarning("Citizen prefab not assigned - creating empty GameObjects for testing");
                GameObject citizen = new GameObject($"TestCitizen_{i}");
                citizen.transform.position = GetRandomSpawnPosition();
                // Add basic components for testing
                citizen.AddComponent<Rigidbody>();
                citizen.AddComponent<SphereCollider>();
                spawnedEntities.Add(citizen);
            }

            // Yield every few spawns to prevent frame hitches
            if (i % 5 == 0)
                yield return null;
        }

        Debug.Log($"Spawned {targetCitizens} citizens");
        Debug.Log($"Total entities spawned: {spawnedEntities.Count}");

        // Wait for everything to initialize
        yield return new WaitForSeconds(2f);
    }

    IEnumerator MonitorPerformance()
    {
        Debug.Log($"--- Monitoring performance for {testDuration} seconds ---");

        float nextSampleTime = Time.time + sampleInterval;
        int framesSinceLastSample = 0;
        float timeSinceLastSample = 0f;

        while (Time.time - testStartTime < testDuration)
        {
            framesSinceLastSample++;
            timeSinceLastSample += Time.unscaledDeltaTime;
            totalFrames++;

            if (Time.time >= nextSampleTime)
            {
                // Calculate FPS for this sample period
                float currentFPS = framesSinceLastSample / timeSinceLastSample;
                fpsHistory.Add(currentFPS);

                // Get memory usage
                float memoryMB = Profiler.GetTotalAllocatedMemory() / (1024f * 1024f);
                memoryHistory.Add(memoryMB);

                // Log periodic updates
                if (fpsHistory.Count % 10 == 0)
                {
                    Debug.Log($"Performance sample {fpsHistory.Count}: FPS={currentFPS:F1}, Memory={memoryMB:F1}MB");
                }

                // Reset for next sample
                framesSinceLastSample = 0;
                timeSinceLastSample = 0f;
                nextSampleTime = Time.time + sampleInterval;
            }

            yield return null;
        }
    }

    void AnalyzeResults()
    {
        Debug.Log("--- Analyzing performance results ---");

        if (fpsHistory.Count == 0)
        {
            Debug.LogError("No performance data collected!");
            return;
        }

        // Calculate FPS statistics
        float totalFPS = 0f;
        minFPS = float.MaxValue;
        float maxFPS = 0f;

        foreach (float fps in fpsHistory)
        {
            totalFPS += fps;
            if (fps < minFPS) minFPS = fps;
            if (fps > maxFPS) maxFPS = fps;
        }

        averageFPS = totalFPS / fpsHistory.Count;

        // Calculate memory statistics
        maxMemoryUsed = 0f;
        float totalMemory = 0f;

        foreach (float memory in memoryHistory)
        {
            totalMemory += memory;
            if (memory > maxMemoryUsed) maxMemoryUsed = memory;
        }

        float averageMemory = totalMemory / memoryHistory.Count;

        // Performance evaluation
        bool fpsTargetMet = averageFPS >= targetFPS && minFPS >= (targetFPS * 0.8f); // Allow 20% variance
        bool memoryTargetMet = maxMemoryUsed <= maxMemoryMB;

        // Log results
        Debug.Log("=== Performance Test Results ===");
        Debug.Log($"Test Duration: {testDuration}s");
        Debug.Log($"Total Entities: {spawnedEntities.Count} (Guards: {targetGuards}, Citizens: {targetCitizens})");
        Debug.Log($"Total Frames: {totalFrames}");
        Debug.Log($"FPS - Average: {averageFPS:F1}, Min: {minFPS:F1}, Max: {maxFPS:F1}");
        Debug.Log($"Memory - Average: {averageMemory:F1}MB, Peak: {maxMemoryUsed:F1}MB");
        Debug.Log($"FPS Target ({targetFPS}) Met: {(fpsTargetMet ? "✅" : "❌")}");
        Debug.Log($"Memory Target ({maxMemoryMB}MB) Met: {(memoryTargetMet ? "✅" : "❌")}");

        // Overall assessment
        if (fpsTargetMet && memoryTargetMet)
        {
            Debug.Log("🎉 SP-004 Performance targets ACHIEVED!");
        }
        else
        {
            Debug.LogWarning("⚠️ SP-004 Performance targets NOT MET - optimization needed");

            if (!fpsTargetMet)
            {
                Debug.LogWarning($"FPS below target: {averageFPS:F1} < {targetFPS} (min: {minFPS:F1})");
            }

            if (!memoryTargetMet)
            {
                Debug.LogWarning($"Memory above target: {maxMemoryUsed:F1}MB > {maxMemoryMB}MB");
            }
        }

        // Additional diagnostics
        LogPerformanceDiagnostics();
    }

    void LogPerformanceDiagnostics()
    {
        Debug.Log("--- Performance Diagnostics ---");

        // Check for common performance issues
        int activeGuards = 0;
        int activeCitizens = 0;
        int totalComponents = 0;

        foreach (GameObject entity in spawnedEntities)
        {
            if (entity != null)
            {
                Component[] components = entity.GetComponents<Component>();
                totalComponents += components.Length;

                if (entity.name.Contains("Guard"))
                    activeGuards++;
                else if (entity.name.Contains("Citizen"))
                    activeCitizens++;
            }
        }

        Debug.Log($"Active Entities - Guards: {activeGuards}, Citizens: {activeCitizens}");
        Debug.Log($"Total Components: {totalComponents}");
        Debug.Log($"Components per Entity: {(float)totalComponents / spawnedEntities.Count:F1}");

        // Check manager status
        CheckManagerPerformance();
    }

    void CheckManagerPerformance()
    {
        Debug.Log("--- Manager System Performance ---");

        // Check if performance-critical managers are present
        bool hasGuardManager = GuardAlertnessManager.Instance != null;
        bool hasCitizenManager = CitizenManager.Instance != null;
        bool hasNoiseManager = NoiseManager.Instance != null;

        Debug.Log($"GuardAlertnessManager: {(hasGuardManager ? "✅" : "❌")}");
        Debug.Log($"CitizenManager: {(hasCitizenManager ? "✅" : "❌")}");
        Debug.Log($"NoiseManager: {(hasNoiseManager ? "✅" : "❌")}");

        if (!hasGuardManager || !hasCitizenManager)
        {
            Debug.LogWarning("Key managers missing - this may impact performance test accuracy");
        }
    }

    Vector3 GetRandomSpawnPosition()
    {
        Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = spawnArea.position + new Vector3(randomPoint.x, 0, randomPoint.y);
        return spawnPos;
    }

    void CleanupTestEnvironment()
    {
        Debug.Log("--- Cleaning up test environment ---");

        foreach (GameObject entity in spawnedEntities)
        {
            if (entity != null)
            {
                DestroyImmediate(entity);
            }
        }

        spawnedEntities.Clear();
        Debug.Log("Test environment cleaned up");
    }

    [ContextMenu("Stop Test")]
    public void StopTest()
    {
        if (testInProgress)
        {
            StopAllCoroutines();
            CleanupTestEnvironment();
            testInProgress = false;
            Debug.Log("Performance test stopped manually");
        }
    }

    void OnGUI()
    {
        if (testInProgress)
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(10, 10, 300, 20), "Performance Test Running...");
            GUI.Label(new Rect(10, 30, 300, 20), $"Entities: {spawnedEntities.Count}");
            GUI.Label(new Rect(10, 50, 300, 20), $"Test Time: {Time.time - testStartTime:F1}s / {testDuration}s");

            if (fpsHistory.Count > 0)
            {
                GUI.Label(new Rect(10, 70, 300, 20), $"Current FPS: {fpsHistory[fpsHistory.Count - 1]:F1}");
            }

            GUI.color = Color.white;
        }
    }
}