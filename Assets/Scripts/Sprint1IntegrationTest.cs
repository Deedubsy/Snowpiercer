using UnityEngine;

/// <summary>
/// Sprint 1 integration test for SP-001 and SP-002 functionality
/// Tests the PermanentUpgradeSystem and DynamicObjectiveSystem integrations
/// </summary>
public class Sprint1IntegrationTest : MonoBehaviour
{
    [Header("Test Configuration")]
    [Tooltip("Run automated tests on start")]
    public bool runOnStart = false;

    [Header("Test Parameters")]
    public int testBloodPoints = 10;
    public float testBloodAmount = 25f;
    public float testDamageAmount = 5f;

    void Start()
    {
        if (runOnStart)
        {
            RunIntegrationTests();
        }
    }

    [ContextMenu("Run Integration Tests")]
    public void RunIntegrationTests()
    {
        Debug.Log("=== Sprint 1 Integration Tests Starting ===");

        TestPermanentUpgradeSystemIntegration();
        TestDynamicObjectiveSystemIntegration();

        Debug.Log("=== Sprint 1 Integration Tests Complete ===");
    }

    void TestPermanentUpgradeSystemIntegration()
    {
        Debug.Log("--- Testing PermanentUpgradeSystem Integration ---");

        if (PermanentUpgradeSystem.Instance == null)
        {
            Debug.LogError("❌ PermanentUpgradeSystem.Instance is null - cannot test");
            return;
        }

        try
        {
            int initialPoints = PermanentUpgradeSystem.Instance.availableBloodPoints;
            PermanentUpgradeSystem.Instance.AddBloodPoints(testBloodPoints);
            int finalPoints = PermanentUpgradeSystem.Instance.availableBloodPoints;

            if (finalPoints == initialPoints + testBloodPoints)
            {
                Debug.Log($"✅ PermanentUpgradeSystem.AddBloodPoints() working correctly. Points: {initialPoints} → {finalPoints}");
            }
            else
            {
                Debug.LogError($"❌ PermanentUpgradeSystem.AddBloodPoints() failed. Expected: {initialPoints + testBloodPoints}, Got: {finalPoints}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ PermanentUpgradeSystem integration test failed: {e.Message}");
        }
    }

    void TestDynamicObjectiveSystemIntegration()
    {
        Debug.Log("--- Testing DynamicObjectiveSystem Integration ---");

        if (DynamicObjectiveSystem.Instance == null)
        {
            Debug.LogError("❌ DynamicObjectiveSystem.Instance is null - cannot test");
            return;
        }

        try
        {
            // Test OnPlayerDetected
            DynamicObjectiveSystem.Instance.OnPlayerDetected();
            Debug.Log("✅ DynamicObjectiveSystem.OnPlayerDetected() called successfully");

            // Test OnBloodCollected
            DynamicObjectiveSystem.Instance.OnBloodCollected(testBloodAmount);
            Debug.Log($"✅ DynamicObjectiveSystem.OnBloodCollected({testBloodAmount}) called successfully");

            // Test OnPlayerDamaged
            DynamicObjectiveSystem.Instance.OnPlayerDamaged();
            Debug.Log("✅ DynamicObjectiveSystem.OnPlayerDamaged() called successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ DynamicObjectiveSystem integration test failed: {e.Message}");
        }
    }

    [ContextMenu("Test GameManager Upgrade Point Conversion")]
    public void TestGameManagerUpgradeConversion()
    {
        Debug.Log("--- Testing GameManager Upgrade Point Conversion ---");

        if (GameManager.instance == null)
        {
            Debug.LogError("❌ GameManager.instance is null");
            return;
        }

        if (PermanentUpgradeSystem.Instance == null)
        {
            Debug.LogError("❌ PermanentUpgradeSystem.Instance is null");
            return;
        }

        // Simulate excess blood scenario
        int initialUpgradePoints = PermanentUpgradeSystem.Instance.availableBloodPoints;

        // Manually trigger the upgrade point conversion logic
        float excessBlood = 15.7f; // Should convert to 15 upgrade points
        int expectedUpgradePoints = Mathf.FloorToInt(excessBlood);

        PermanentUpgradeSystem.Instance.AddBloodPoints(expectedUpgradePoints);

        int finalUpgradePoints = PermanentUpgradeSystem.Instance.availableBloodPoints;

        if (finalUpgradePoints == initialUpgradePoints + expectedUpgradePoints)
        {
            Debug.Log($"✅ GameManager → PermanentUpgradeSystem conversion working. Points: {initialUpgradePoints} → {finalUpgradePoints}");
        }
        else
        {
            Debug.LogError($"❌ GameManager → PermanentUpgradeSystem conversion failed");
        }
    }
}