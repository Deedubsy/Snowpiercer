using UnityEngine;
using System.Threading.Tasks;
using System.Collections;
using CityGeneration.Core;
using CityGeneration.Generators;

namespace CityGeneration.Testing
{
    /// <summary>
    /// Test harness for validating the modular city generation system
    /// Provides automated testing and performance benchmarking
    /// </summary>
    public class ModularCityTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        public bool autoRunTestsOnStart = false;
        public bool enablePerformanceTests = true;
        public bool enableValidationTests = true;
        public bool enableStressTests = false;

        [Header("Test Results")]
        [SerializeField] private TestResults lastTestResults;

        private ModularCityGenerator cityGenerator;

        private void Start()
        {
            // Find or create city generator
            cityGenerator = FindObjectOfType<ModularCityGenerator>();
            if (cityGenerator == null)
            {
                Debug.LogError("ModularCityGenerator not found in scene. Please add one to test.");
                return;
            }

            if (autoRunTestsOnStart)
            {
                StartCoroutine(RunAllTests());
            }
        }

        /// <summary>
        /// Run all available tests
        /// </summary>
        [ContextMenu("Run All Tests")]
        public void RunAllTestsMenu()
        {
            StartCoroutine(RunAllTests());
        }

        private IEnumerator RunAllTests()
        {
            Debug.Log("=== Starting Modular City Generation Tests ===");

            lastTestResults = new TestResults();
            var startTime = Time.realtimeSinceStartup;

            // Basic functionality tests
            yield return StartCoroutine(TestBasicGeneration());
            yield return StartCoroutine(TestModuleIndependence());
            yield return StartCoroutine(TestCollisionSystem());

            if (enableValidationTests)
            {
                yield return StartCoroutine(TestValidation());
            }

            if (enablePerformanceTests)
            {
                yield return StartCoroutine(TestPerformance());
            }

            if (enableStressTests)
            {
                yield return StartCoroutine(TestStressConditions());
            }

            lastTestResults.totalTestTime = Time.realtimeSinceStartup - startTime;

            Debug.Log("=== Test Results ===");
            Debug.Log(lastTestResults.ToString());
        }

        private IEnumerator TestBasicGeneration()
        {
            Debug.Log("Testing basic city generation...");

            var task = cityGenerator.GenerateCity();
            yield return new WaitUntil(() => task.IsCompleted);

            try
            {
                if (task.Exception != null)
                {
                    throw task.Exception;
                }

                var cityLayout = task.Result;
                lastTestResults.basicGenerationPassed = ValidateCityLayout(cityLayout);

                if (lastTestResults.basicGenerationPassed)
                {
                    Debug.Log("✅ Basic generation test PASSED");
                }
                else
                {
                    Debug.LogError("❌ Basic generation test FAILED");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Basic generation test FAILED with exception: {ex.Message}");
                lastTestResults.basicGenerationPassed = false;
            }
        }

        private IEnumerator TestModuleIndependence()
        {
            Debug.Log("Testing module independence...");
            bool allModulesPassed = true;

            // Test terrain generation alone
            var terrainGenerator = cityGenerator.GetComponent<TerrainGenerator>();
            if (terrainGenerator != null)
            {
                var context = new CityGenerationContext(new CityConfiguration());
                var task = terrainGenerator.GenerateAsync(context);
                yield return new WaitUntil(() => task.IsCompleted);

                try
                {
                    if (task.Exception != null || !task.Result.IsValid())
                    {
                        Debug.LogError("❌ Terrain module test failed");
                        allModulesPassed = false;
                    }

                    // Clean up
                    terrainGenerator.ClearTerrain();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ Terrain module test FAILED with exception: {ex.Message}");
                    allModulesPassed = false;
                }
            }

            // Test wall generation alone
            var wallGenerator = cityGenerator.GetComponent<WallGenerator>();
            if (wallGenerator != null)
            {
                var context = new CityGenerationContext(new CityConfiguration());
                var task = wallGenerator.GenerateAsync(context);
                yield return new WaitUntil(() => task.IsCompleted);

                try
                {
                    if (task.Exception != null || !task.Result.IsValid())
                    {
                        Debug.LogError("❌ Wall module test failed");
                        allModulesPassed = false;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ Wall module test FAILED with exception: {ex.Message}");
                    allModulesPassed = false;
                }
            }

            lastTestResults.moduleIndependencePassed = allModulesPassed;

            if (allModulesPassed)
            {
                Debug.Log("✅ Module independence test PASSED");
            }
            else
            {
                Debug.LogError("❌ Module independence test FAILED");
            }
        }

        private IEnumerator TestCollisionSystem()
        {
            Debug.Log("Testing collision system...");

            try
            {
                var collisionManager = new CityCollisionManager();
                collisionManager.Initialize(100f);

                // Test basic collision detection
                var testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                testObject.transform.position = Vector3.zero;

                collisionManager.RegisterStaticObject(testObject, ObjectType.Building, 5f);

                // Test position validation
                bool validPosition = collisionManager.IsPositionValid(Vector3.zero, 1f, ObjectType.Building);
                bool invalidPosition = !collisionManager.IsPositionValid(Vector3.zero, 1f, ObjectType.Street);

                // Test nearest position finding
                Vector3 nearestValid = collisionManager.FindNearestValidPosition(Vector3.zero, 1f, ObjectType.Street);

                lastTestResults.collisionSystemPassed = !validPosition && invalidPosition && nearestValid != Vector3.zero;

                // Cleanup
                DestroyImmediate(testObject);

                if (lastTestResults.collisionSystemPassed)
                {
                    Debug.Log("✅ Collision system test PASSED");
                }
                else
                {
                    Debug.LogError("❌ Collision system test FAILED");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Collision system test FAILED with exception: {ex.Message}");
                lastTestResults.collisionSystemPassed = false;
            }

            yield return null;
        }

        private IEnumerator TestValidation()
        {
            Debug.Log("Testing validation systems...");

            // Test with invalid configuration
            var invalidConfig = new CityConfiguration();
            invalidConfig.wallThickness = -1f; // Invalid
            invalidConfig.wallHeight = 0f; // Invalid

            var context = new CityGenerationContext(invalidConfig);
            var wallGenerator = cityGenerator.GetComponent<WallGenerator>();

            if (wallGenerator != null)
            {
                // This should either fix the config or fail gracefully
                var task = wallGenerator.GenerateAsync(context);
                yield return new WaitUntil(() => task.IsCompleted);

                try
                {
                    // Check if it handled invalid config appropriately
                    lastTestResults.validationPassed = task.Result != null;

                    if (lastTestResults.validationPassed)
                    {
                        Debug.Log("✅ Validation test PASSED");
                    }
                    else
                    {
                        Debug.LogError("❌ Validation test FAILED");
                    }
                }
                catch (System.Exception ex)
                {
                    // Exceptions are expected for invalid configurations
                    lastTestResults.validationPassed = true;
                    Debug.Log("✅ Validation test PASSED (graceful failure)");
                }
            }
            else
            {
                lastTestResults.validationPassed = false;
                Debug.LogError("❌ Validation test FAILED - no wall generator");
            }
        }

        private IEnumerator TestPerformance()
        {
            Debug.Log("Testing performance...");

            var startTime = Time.realtimeSinceStartup;
            var startMemory = System.GC.GetTotalMemory(false);

            var task = cityGenerator.GenerateCity();
            yield return new WaitUntil(() => task.IsCompleted);

            try
            {

                var endTime = Time.realtimeSinceStartup;
                var endMemory = System.GC.GetTotalMemory(false);

                lastTestResults.generationTime = endTime - startTime;
                lastTestResults.memoryUsed = (endMemory - startMemory) / (1024 * 1024); // MB

                // Performance criteria (adjust as needed)
                bool performanceAcceptable = lastTestResults.generationTime < 10f && lastTestResults.memoryUsed < 100; // 10 seconds, 100MB

                lastTestResults.performancePassed = performanceAcceptable;

                if (performanceAcceptable)
                {
                    Debug.Log($"✅ Performance test PASSED (Time: {lastTestResults.generationTime:F2}s, Memory: {lastTestResults.memoryUsed:F1}MB)");
                }
                else
                {
                    Debug.LogError($"❌ Performance test FAILED (Time: {lastTestResults.generationTime:F2}s, Memory: {lastTestResults.memoryUsed:F1}MB)");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Performance test FAILED with exception: {ex.Message}");
                lastTestResults.performancePassed = false;
            }
        }

        private IEnumerator TestStressConditions()
        {
            Debug.Log("Testing stress conditions...");

            // Test with very large city
            var stressConfig = new CityConfiguration();
            stressConfig.squareWallSize = new Vector2(500f, 500f);
            stressConfig.maxBuildingsPerDistrict = 10;
            stressConfig.buildingDensity = 1f;

            var task = cityGenerator.UpdateConfiguration(stressConfig, true);
            yield return new WaitUntil(() => task.IsCompleted);

            try
            {

                var stats = cityGenerator.GetGenerationStats();
                lastTestResults.stressPassed = stats.totalObjects > 100; // Expect many objects

                if (lastTestResults.stressPassed)
                {
                    Debug.Log($"✅ Stress test PASSED ({stats.totalObjects} objects generated)");
                }
                else
                {
                    Debug.LogError($"❌ Stress test FAILED ({stats.totalObjects} objects generated)");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Stress test FAILED with exception: {ex.Message}");
                lastTestResults.stressPassed = false;
            }

            // Reset to normal configuration
            yield return new WaitForSeconds(1f);
            var normalConfig = new CityConfiguration();
            var resetTask = cityGenerator.UpdateConfiguration(normalConfig, false);
            yield return new WaitUntil(() => resetTask.IsCompleted);
        }

        private bool ValidateCityLayout(CityLayout cityLayout)
        {
            if (cityLayout == null) return false;

            // Check that basic components were generated
            bool hasWalls = cityLayout.walls?.IsValid() ?? false;
            bool hasStreets = cityLayout.streets?.IsValid() ?? false;
            bool hasBuildings = cityLayout.buildings?.IsValid() ?? false;

            return hasWalls && hasStreets && hasBuildings;
        }

        [ContextMenu("Test Basic Generation Only")]
        public void TestBasicGenerationOnly()
        {
            StartCoroutine(TestBasicGeneration());
        }

        [ContextMenu("Test Performance Only")]
        public void TestPerformanceOnly()
        {
            StartCoroutine(TestPerformance());
        }

        [ContextMenu("Show Last Test Results")]
        public void ShowLastTestResults()
        {
            if (lastTestResults != null)
            {
                Debug.Log("=== Last Test Results ===");
                Debug.Log(lastTestResults.ToString());
            }
            else
            {
                Debug.Log("No test results available. Run tests first.");
            }
        }
    }

    [System.Serializable]
    public class TestResults
    {
        public bool basicGenerationPassed;
        public bool moduleIndependencePassed;
        public bool collisionSystemPassed;
        public bool validationPassed;
        public bool performancePassed;
        public bool stressPassed;

        public float generationTime;
        public float memoryUsed; // MB
        public float totalTestTime;

        public bool AllTestsPassed => basicGenerationPassed && moduleIndependencePassed &&
                                      collisionSystemPassed && validationPassed &&
                                      performancePassed && stressPassed;

        public override string ToString()
        {
            return $"Test Results Summary:\n" +
                   $"✓ Basic Generation: {(basicGenerationPassed ? "PASS" : "FAIL")}\n" +
                   $"✓ Module Independence: {(moduleIndependencePassed ? "PASS" : "FAIL")}\n" +
                   $"✓ Collision System: {(collisionSystemPassed ? "PASS" : "FAIL")}\n" +
                   $"✓ Validation: {(validationPassed ? "PASS" : "FAIL")}\n" +
                   $"✓ Performance: {(performancePassed ? "PASS" : "FAIL")} ({generationTime:F2}s, {memoryUsed:F1}MB)\n" +
                   $"✓ Stress Test: {(stressPassed ? "PASS" : "FAIL")}\n" +
                   $"Total Test Time: {totalTestTime:F2}s\n" +
                   $"Overall Result: {(AllTestsPassed ? "✅ ALL TESTS PASSED" : "❌ SOME TESTS FAILED")}";
        }
    }
}