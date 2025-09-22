using UnityEngine;
using System.Collections;

/// <summary>
/// Sprint 1 test script to verify manager singleton initialization order
/// This should be added to a test GameObject in the scene temporarily
/// </summary>
public class ManagerInitializationTest : MonoBehaviour
{
    [Header("Test Configuration")]
    public bool runTestOnStart = true;
    public float testDelay = 1f; // Wait for initialization

    void Start()
    {
        if (runTestOnStart)
        {
            StartCoroutine(TestManagerInitialization());
        }
    }

    IEnumerator TestManagerInitialization()
    {
        Debug.Log("=== Manager Initialization Test Starting ===");
        yield return new WaitForSeconds(testDelay);

        // Test critical managers that were integrated in SP-001, SP-002, SP-003
        TestManagerInstance("GameManager", GameManager.instance != null);
        TestManagerInstance("PermanentUpgradeSystem", PermanentUpgradeSystem.Instance != null);
        TestManagerInstance("DynamicObjectiveSystem", DynamicObjectiveSystem.Instance != null);
        TestManagerInstance("GuardAlertnessManager", GuardAlertnessManager.Instance != null);
        TestManagerInstance("NoiseManager", NoiseManager.Instance != null);
        TestManagerInstance("CitizenManager", CitizenManager.Instance != null);
        TestManagerInstance("AudioManager", AudioManager.Instance != null);
        TestManagerInstance("DebugUIManager", DebugUIManager.Instance != null);

        // Test cross-dependencies
        Debug.Log("=== Testing Cross-Dependencies ===");
        TestCrossDependencies();

        Debug.Log("=== Manager Initialization Test Complete ===");
    }

    void TestManagerInstance(string managerName, bool isInitialized)
    {
        if (isInitialized)
        {
            Debug.Log($"✅ {managerName} - INITIALIZED");
        }
        else
        {
            Debug.LogError($"❌ {managerName} - NOT INITIALIZED");
        }
    }

    void TestCrossDependencies()
    {
        // Test the integration points we just fixed
        bool canAddUpgradePoints = GameManager.instance != null && PermanentUpgradeSystem.Instance != null;
        Debug.Log($"Can add upgrade points: {(canAddUpgradePoints ? "✅" : "❌")}");

        bool canTrackObjectives = DynamicObjectiveSystem.Instance != null;
        Debug.Log($"Can track objectives: {(canTrackObjectives ? "✅" : "❌")}");

        // Test if systems can communicate
        if (canTrackObjectives && PermanentUpgradeSystem.Instance != null)
        {
            Debug.Log("✅ Objective ↔ Upgrade system communication possible");
        }
        else
        {
            Debug.LogError("❌ Objective ↔ Upgrade system communication BLOCKED");
        }
    }

    [ContextMenu("Run Test Now")]
    public void RunTestNow()
    {
        StartCoroutine(TestManagerInitialization());
    }
}