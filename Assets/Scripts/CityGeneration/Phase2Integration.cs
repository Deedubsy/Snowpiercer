using UnityEngine;
using System.Threading.Tasks;
using CityGeneration.Core;
using CityGeneration.Generators;
using CityGeneration.Rules;
using CityGeneration.Navigation;

namespace CityGeneration
{
    /// <summary>
    /// Integration controller that sets up and manages all Phase 2 enhancements
    /// Provides a simple interface for testing and using the enhanced city generation system
    /// </summary>
    public class Phase2Integration : MonoBehaviour
    {
        [Header("Phase 2 Systems")]
        [SerializeField] private ModularCityGenerator cityGenerator;
        [SerializeField] private IntelligentDistrictGenerator districtGenerator;
        [SerializeField] private AutoNavMeshGenerator navMeshGenerator;

        [Header("Configuration")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool generateCityOnStart = false;
        [SerializeField] private bool enableDetailedLogging = true;

        [Header("Integration Status")]
        [SerializeField] private bool phase2SystemsReady = false;
        [SerializeField] private string lastIntegrationStatus = "Not initialized";

        private CityLayout lastGeneratedCity;

        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupPhase2Systems();
            }

            if (generateCityOnStart && phase2SystemsReady)
            {
                _ = GenerateEnhancedCity();
            }
        }

        /// <summary>
        /// Setup all Phase 2 systems and ensure they're properly configured
        /// </summary>
        [ContextMenu("Setup Phase 2 Systems")]
        public void SetupPhase2Systems()
        {
            try
            {
                LogMessage("Setting up Phase 2 systems...");

                // Find or create main city generator
                if (cityGenerator == null)
                {
                    cityGenerator = GetComponent<ModularCityGenerator>();
                    if (cityGenerator == null)
                    {
                        cityGenerator = gameObject.AddComponent<ModularCityGenerator>();
                    }
                }

                // Setup district generator
                if (districtGenerator == null)
                {
                    districtGenerator = GetComponent<IntelligentDistrictGenerator>();
                    if (districtGenerator == null)
                    {
                        districtGenerator = gameObject.AddComponent<IntelligentDistrictGenerator>();
                    }
                }

                // Setup NavMesh generator
                if (navMeshGenerator == null)
                {
                    navMeshGenerator = GetComponent<AutoNavMeshGenerator>();
                    if (navMeshGenerator == null)
                    {
                        navMeshGenerator = gameObject.AddComponent<AutoNavMeshGenerator>();
                    }
                }

                // Ensure all generators are configured
                SetupGeneratorDependencies();

                phase2SystemsReady = true;
                lastIntegrationStatus = "Phase 2 systems ready";
                LogMessage("Phase 2 systems setup completed successfully");
            }
            catch (System.Exception ex)
            {
                phase2SystemsReady = false;
                lastIntegrationStatus = $"Setup failed: {ex.Message}";
                LogError($"Failed to setup Phase 2 systems: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate a city using all Phase 2 enhancements
        /// </summary>
        [ContextMenu("Generate Enhanced City")]
        public async Task<CityLayout> GenerateEnhancedCity()
        {
            if (!phase2SystemsReady)
            {
                LogWarning("Phase 2 systems not ready. Running setup first...");
                SetupPhase2Systems();
            }

            if (!phase2SystemsReady)
            {
                LogError("Cannot generate city - Phase 2 systems setup failed");
                return null;
            }

            try
            {
                LogMessage("Starting enhanced city generation with Phase 2 systems...");

                // Generate the city using the modular system
                lastGeneratedCity = await cityGenerator.GenerateCity();

                if (lastGeneratedCity != null && lastGeneratedCity.IsComplete())
                {
                    LogMessage("Enhanced city generation completed successfully");
                    LogCityStatistics();
                    return lastGeneratedCity;
                }
                else
                {
                    LogWarning("City generation completed but city layout is incomplete");
                    return lastGeneratedCity;
                }
            }
            catch (System.Exception ex)
            {
                LogError($"Enhanced city generation failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Test individual Phase 2 systems
        /// </summary>
        [ContextMenu("Test Phase 2 Systems")]
        public async Task TestPhase2Systems()
        {
            if (!phase2SystemsReady)
            {
                LogWarning("Setting up Phase 2 systems for testing...");
                SetupPhase2Systems();
            }

            LogMessage("=== Testing Phase 2 Systems ===");

            // Test rule system
            await TestRuleSystem();

            // Test district generation
            await TestDistrictGeneration();

            // Test NavMesh integration
            await TestNavMeshIntegration();

            LogMessage("=== Phase 2 System Tests Completed ===");
        }

        /// <summary>
        /// Clear the current city and reset systems
        /// </summary>
        [ContextMenu("Clear City")]
        public void ClearCity()
        {
            if (cityGenerator != null)
            {
                cityGenerator.ClearCity();
            }

            lastGeneratedCity = null;
            LogMessage("City cleared");
        }

        /// <summary>
        /// Get comprehensive statistics about the last generated city
        /// </summary>
        [ContextMenu("Show Enhanced Statistics")]
        public void ShowEnhancedStatistics()
        {
            if (lastGeneratedCity == null)
            {
                LogMessage("No city has been generated yet");
                return;
            }

            var stats = cityGenerator.GetGenerationStats();
            LogMessage($"=== Enhanced City Statistics ===\n{stats}");

            // Show Phase 2 specific statistics
            ShowPhase2Statistics();
        }

        private void SetupGeneratorDependencies()
        {
            // Ensure the modular city generator has all required components
            var generators = GetComponents<BaseGenerator>();

            // Check if we have all required generators
            bool hasTerrainGen = System.Array.Exists(generators, g => g is TerrainGenerator);
            bool hasWallGen = System.Array.Exists(generators, g => g is WallGenerator);
            bool hasStreetGen = System.Array.Exists(generators, g => g is StreetGenerator);
            bool hasBuildingGen = System.Array.Exists(generators, g => g is BuildingGenerator);

            // Add missing generators
            if (!hasTerrainGen) gameObject.AddComponent<TerrainGenerator>();
            if (!hasWallGen) gameObject.AddComponent<WallGenerator>();
            if (!hasStreetGen) gameObject.AddComponent<StreetGenerator>();
            if (!hasBuildingGen) gameObject.AddComponent<BuildingGenerator>();

            LogMessage("Generator dependencies configured");
        }

        private async Task TestRuleSystem()
        {
            LogMessage("Testing Procedural Rule System...");

            if (districtGenerator != null && districtGenerator.ruleEngine != null)
            {
                // Test rule engine with a simple context
                var context = new CityGenerationContext(new CityConfiguration());
                var placementContext = new PlacementContext(context);

                var result = await districtGenerator.ruleEngine.FindBestDistrictPosition(
                    DistrictType.Market, placementContext);

                if (result.success)
                {
                    LogMessage($"Rule system test PASSED - Found position at {result.position} with score {result.score:F2}");
                }
                else
                {
                    LogWarning($"Rule system test FAILED - {result.errorMessage}");
                }
            }
            else
            {
                LogWarning("Rule system not available for testing");
            }
        }

        private async Task TestDistrictGeneration()
        {
            LogMessage("Testing Intelligent District Generation...");

            if (districtGenerator != null)
            {
                try
                {
                    var context = new CityGenerationContext(new CityConfiguration());
                    var result = await districtGenerator.GenerateAsync(context);

                    if (result.isSuccessful)
                    {
                        LogMessage($"District generation test PASSED - Generated {result.objectsGenerated} districts");
                    }
                    else
                    {
                        LogWarning($"District generation test FAILED - {result.errorMessage}");
                    }
                }
                catch (System.Exception ex)
                {
                    LogError($"District generation test ERROR - {ex.Message}");
                }
            }
            else
            {
                LogWarning("District generator not available for testing");
            }
        }

        private async Task TestNavMeshIntegration()
        {
            LogMessage("Testing NavMesh Integration...");

            if (navMeshGenerator != null)
            {
                try
                {
                    var context = new CityGenerationContext(new CityConfiguration());
                    var result = await navMeshGenerator.GenerateAsync(context);

                    if (result.isSuccessful)
                    {
                        LogMessage($"NavMesh integration test PASSED - Generated navigation mesh");
                    }
                    else
                    {
                        LogWarning($"NavMesh integration test FAILED - {result.errorMessage}");
                    }
                }
                catch (System.Exception ex)
                {
                    LogError($"NavMesh integration test ERROR - {ex.Message}");
                }
            }
            else
            {
                LogWarning("NavMesh generator not available for testing");
            }
        }

        private void ShowPhase2Statistics()
        {
            LogMessage("=== Phase 2 Enhancement Statistics ===");

            // Rule system statistics
            if (districtGenerator?.ruleEngine != null)
            {
                var vizData = districtGenerator.ruleEngine.GetVisualizationData();
                LogMessage($"Rule System: {vizData.evaluatedCandidates?.Length ?? 0} candidates evaluated");
            }

            // Building template statistics
            if (lastGeneratedCity?.buildings != null)
            {
                LogMessage($"Building Variations: {lastGeneratedCity.buildings.buildingsByDistrict.Count} district types with buildings");
            }

            // NavMesh statistics
            if (lastGeneratedCity?.navMesh != null)
            {
                var navMesh = lastGeneratedCity.navMesh;
                LogMessage($"NavMesh: {navMesh.navigationAreas} areas, {navMesh.totalNavigableArea:F1}m² navigable");
                LogMessage($"Connectivity: {(navMesh.connectivityTestPassed ? "PASSED" : "FAILED")}");
            }
        }

        private void LogCityStatistics()
        {
            if (cityGenerator != null)
            {
                var stats = cityGenerator.GetGenerationStats();
                LogMessage($"City Generation Complete:\n{stats}");
            }
        }

        private void LogMessage(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[Phase2Integration] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[Phase2Integration] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[Phase2Integration] {message}");
        }

        private void OnValidate()
        {
            // Update status when values change in inspector
            if (Application.isPlaying && autoSetupOnStart && !phase2SystemsReady)
            {
                SetupPhase2Systems();
            }
        }
    }
}