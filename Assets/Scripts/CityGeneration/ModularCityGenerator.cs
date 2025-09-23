using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using CityGeneration.Core;
using CityGeneration.Generators;
using CityGeneration.Navigation;

namespace CityGeneration
{
    /// <summary>
    /// Master orchestrator that coordinates all city generation modules
    /// Replaces the monolithic MedievalCityBuilder with a modular, progressive system
    /// </summary>
    public class ModularCityGenerator : MonoBehaviour
    {
        [Header("Generation Modules")]
        [SerializeField] private TerrainGenerator terrainGenerator;
        [SerializeField] private WallGenerator wallGenerator;
        [SerializeField] private StreetGenerator streetGenerator;
        [SerializeField] private BuildingGenerator buildingGenerator;
        [SerializeField] private AutoNavMeshGenerator navMeshGenerator;

        [Header("City Configuration")]
        [SerializeField] private CityConfiguration cityConfig;

        [Header("Progress Reporting")]
        public bool showProgressInConsole = true;
        public bool enableDetailedLogging = false;

        [Header("Performance")]
        public bool generateProgressively = true;
        public float maxGenerationTimePerFrame = 0.016f; // 16ms for 60fps

        // Events for UI integration
        public event Action<float> OnProgressUpdated;
        public event Action<string> OnStatusUpdated;
        public event Action<CityLayout> OnCityGenerated;
        public event Action<string> OnGenerationError;

        private CityGenerationContext currentContext;
        private bool isGenerating = false;
        private CityLayout lastGeneratedCity;

        // Generator execution order (dependency-based)
        private readonly Type[] generatorExecutionOrder = new Type[]
        {
            typeof(TerrainGenerator),
            typeof(WallGenerator),
            typeof(StreetGenerator),
            typeof(BuildingGenerator),
            typeof(AutoNavMeshGenerator)  // NavMesh generation last after all objects are placed
        };

        private void Awake()
        {
            // Auto-find generators if not assigned
            if (terrainGenerator == null) terrainGenerator = GetComponent<TerrainGenerator>();
            if (wallGenerator == null) wallGenerator = GetComponent<WallGenerator>();
            if (streetGenerator == null) streetGenerator = GetComponent<StreetGenerator>();
            if (buildingGenerator == null) buildingGenerator = GetComponent<BuildingGenerator>();
            if (navMeshGenerator == null) navMeshGenerator = GetComponent<AutoNavMeshGenerator>();

            // Create default configuration if none provided
            if (cityConfig == null)
            {
                cityConfig = new CityConfiguration();
            }

            // Subscribe to generator progress events
            SubscribeToGeneratorEvents();
        }

        /// <summary>
        /// Generate a complete city using the current configuration
        /// </summary>
        public async Task<CityLayout> GenerateCity()
        {
            if (isGenerating)
            {
                Debug.LogWarning("City generation already in progress");
                return null;
            }

            try
            {
                isGenerating = true;
                UpdateStatus("Starting city generation...");

                // Clear any existing city
                ClearCity();

                // Initialize generation context
                currentContext = new CityGenerationContext(cityConfig);

                // Generate city progressively
                CityLayout cityLayout;
                if (generateProgressively)
                {
                    cityLayout = await GenerateCityProgressive();
                }
                else
                {
                    cityLayout = await GenerateCityImmediate();
                }

                lastGeneratedCity = cityLayout;
                OnCityGenerated?.Invoke(cityLayout);
                UpdateStatus("City generation completed");
                UpdateProgress(1f);

                return cityLayout;
            }
            catch (Exception ex)
            {
                string errorMessage = $"City generation failed: {ex.Message}";
                Debug.LogError(errorMessage);
                OnGenerationError?.Invoke(errorMessage);
                throw;
            }
            finally
            {
                isGenerating = false;
            }
        }

        /// <summary>
        /// Generate city with progressive updates (non-blocking)
        /// </summary>
        private async Task<CityLayout> GenerateCityProgressive()
        {
            var cityLayout = new CityLayout();
            int totalPhases = generatorExecutionOrder.Length;

            for (int phaseIndex = 0; phaseIndex < totalPhases; phaseIndex++)
            {
                Type generatorType = generatorExecutionOrder[phaseIndex];
                BaseGenerator generator = GetGeneratorOfType(generatorType);

                if (generator == null)
                {
                    Debug.LogWarning($"Generator not found: {generatorType.Name}");
                    continue;
                }

                float phaseStartProgress = (float)phaseIndex / totalPhases;
                float phaseEndProgress = (float)(phaseIndex + 1) / totalPhases;

                UpdateStatus($"Generating {generatorType.Name.Replace("Generator", "")}...");

                // Subscribe to generator progress
                var progressReporter = new ProgressReporter(generatorType.Name, true);
                progressReporter.OnProgress += (progress) =>
                {
                    float overallProgress = phaseStartProgress + (progress.progress * (phaseEndProgress - phaseStartProgress));
                    UpdateProgress(overallProgress);
                };

                try
                {
                    var result = await generator.GenerateAsync(currentContext);
                    ApplyResultToLayout(result, cityLayout, generatorType);

                    if (enableDetailedLogging)
                    {
                        Debug.Log($"Completed {generatorType.Name}: {result.objectsGenerated} objects generated");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to generate {generatorType.Name}: {ex.Message}");
                    throw;
                }
                finally
                {
                    progressReporter.Dispose();
                }

                // Yield control between phases
                await Task.Yield();
            }

            return cityLayout;
        }

        /// <summary>
        /// Generate city immediately (blocking)
        /// </summary>
        private async Task<CityLayout> GenerateCityImmediate()
        {
            var cityLayout = new CityLayout();

            // Generate terrain
            if (terrainGenerator != null)
            {
                UpdateStatus("Generating terrain...");
                var terrainResult = await terrainGenerator.GenerateAsync(currentContext);
                cityLayout.terrain = terrainResult as TerrainGenerationResult;
                UpdateProgress(0.25f);
            }

            // Generate walls
            if (wallGenerator != null)
            {
                UpdateStatus("Generating walls...");
                var wallResult = await wallGenerator.GenerateAsync(currentContext);
                cityLayout.walls = wallResult as WallGenerationResult;
                UpdateProgress(0.5f);
            }

            // Generate streets
            if (streetGenerator != null)
            {
                UpdateStatus("Generating streets...");
                var streetResult = await streetGenerator.GenerateAsync(currentContext);
                cityLayout.streets = streetResult as StreetGenerationResult;
                UpdateProgress(0.75f);
            }

            // Generate buildings
            if (buildingGenerator != null)
            {
                UpdateStatus("Generating buildings...");
                var buildingResult = await buildingGenerator.GenerateAsync(currentContext);
                cityLayout.buildings = buildingResult as BuildingGenerationResult;
                UpdateProgress(0.8f);
            }

            // Generate NavMesh
            if (navMeshGenerator != null)
            {
                UpdateStatus("Generating navigation mesh...");
                var navMeshResult = await navMeshGenerator.GenerateAsync(currentContext);
                cityLayout.navMesh = navMeshResult as NavMeshGenerationResult;
                UpdateProgress(1f);
            }

            return cityLayout;
        }

        /// <summary>
        /// Clear the currently generated city
        /// </summary>
        public void ClearCity()
        {
            if (currentContext?.cityParent != null)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.DestroyObjectImmediate(currentContext.cityParent.gameObject);
#else
                DestroyImmediate(currentContext.cityParent.gameObject);
#endif
            }

            // Clear terrain separately if it exists
            if (terrainGenerator != null)
            {
                terrainGenerator.ClearTerrain();
            }

            lastGeneratedCity = null;
            currentContext = null;

            UpdateStatus("City cleared");
            UpdateProgress(0f);
        }

        /// <summary>
        /// Get city generation statistics
        /// </summary>
        public CityGenerationStats GetGenerationStats()
        {
            if (lastGeneratedCity == null)
                return new CityGenerationStats();

            return new CityGenerationStats
            {
                totalObjects = lastGeneratedCity.GetTotalObjectCount(),
                terrainGenerated = lastGeneratedCity.terrain?.IsValid() ?? false,
                wallSegments = lastGeneratedCity.walls?.wallSegments.Count ?? 0,
                gates = lastGeneratedCity.walls?.gates.Count ?? 0,
                towers = lastGeneratedCity.walls?.towers.Count ?? 0,
                streets = (lastGeneratedCity.streets?.mainRoads.Count ?? 0) + (lastGeneratedCity.streets?.secondaryStreets.Count ?? 0),
                buildings = lastGeneratedCity.buildings?.buildings.Count ?? 0,
                districts = lastGeneratedCity.buildings?.buildingsByDistrict.Count ?? 0,
                navMeshGenerated = lastGeneratedCity.navMesh?.IsValid() ?? false,
                navigationAreas = lastGeneratedCity.navMesh?.navigationAreas ?? 0,
                navigableArea = lastGeneratedCity.navMesh?.totalNavigableArea ?? 0f,
                offMeshLinks = lastGeneratedCity.navMesh?.offMeshLinks.Count ?? 0,
                collisionStats = currentContext?.collisionManager.GetPerformanceStats() ?? "No stats available"
            };
        }

        /// <summary>
        /// Update city configuration and regenerate if requested
        /// </summary>
        public async Task UpdateConfiguration(CityConfiguration newConfig, bool regenerate = false)
        {
            cityConfig = newConfig;

            if (regenerate && !isGenerating)
            {
                await GenerateCity();
            }
        }

        private BaseGenerator GetGeneratorOfType(Type generatorType)
        {
            if (generatorType == typeof(TerrainGenerator)) return terrainGenerator;
            if (generatorType == typeof(WallGenerator)) return wallGenerator;
            if (generatorType == typeof(StreetGenerator)) return streetGenerator;
            if (generatorType == typeof(BuildingGenerator)) return buildingGenerator;
            if (generatorType == typeof(AutoNavMeshGenerator)) return navMeshGenerator;

            return null;
        }

        private void ApplyResultToLayout(GenerationResult result, CityLayout cityLayout, Type generatorType)
        {
            switch (result)
            {
                case TerrainGenerationResult terrainResult:
                    cityLayout.terrain = terrainResult;
                    break;
                case WallGenerationResult wallResult:
                    cityLayout.walls = wallResult;
                    break;
                case StreetGenerationResult streetResult:
                    cityLayout.streets = streetResult;
                    break;
                case BuildingGenerationResult buildingResult:
                    cityLayout.buildings = buildingResult;
                    break;
                case NavMeshGenerationResult navMeshResult:
                    cityLayout.navMesh = navMeshResult;
                    break;
            }
        }

        private void SubscribeToGeneratorEvents()
        {
            // Subscribe to individual generator progress if needed
            // This could be expanded for more granular progress reporting
        }

        private void UpdateProgress(float progress)
        {
            if (showProgressInConsole)
            {
                Debug.Log($"City Generation Progress: {progress * 100f:F1}%");
            }

            OnProgressUpdated?.Invoke(progress);
        }

        private void UpdateStatus(string status)
        {
            if (showProgressInConsole)
            {
                Debug.Log($"City Generation: {status}");
            }

            OnStatusUpdated?.Invoke(status);
        }

        // Inspector methods for testing
        [ContextMenu("Generate City")]
        public void GenerateCityInspector()
        {
            if (Application.isPlaying)
            {
                _ = GenerateCity();
            }
            else
            {
                Debug.LogWarning("City generation only works in Play mode");
            }
        }

        [ContextMenu("Clear City")]
        public void ClearCityInspector()
        {
            ClearCity();
        }

        [ContextMenu("Show Generation Stats")]
        public void ShowGenerationStatsInspector()
        {
            var stats = GetGenerationStats();
            Debug.Log($"=== City Generation Statistics ===\n{stats}");
        }

        private void OnDestroy()
        {
            ClearCity();
        }

        private void OnValidate()
        {
            // Ensure configuration is valid
            if (cityConfig != null)
            {
                cityConfig.wallThickness = Mathf.Max(0.1f, cityConfig.wallThickness);
                cityConfig.wallHeight = Mathf.Max(1f, cityConfig.wallHeight);
                cityConfig.buildingDensity = Mathf.Clamp01(cityConfig.buildingDensity);
            }
        }
    }

    /// <summary>
    /// Statistics about the generated city
    /// </summary>
    [System.Serializable]
    public class CityGenerationStats
    {
        public int totalObjects;
        public bool terrainGenerated;
        public int wallSegments;
        public int gates;
        public int towers;
        public int streets;
        public int buildings;
        public int districts;
        public bool navMeshGenerated;
        public int navigationAreas;
        public float navigableArea;
        public int offMeshLinks;
        public string collisionStats;

        public override string ToString()
        {
            return $"Total Objects: {totalObjects}\n" +
                   $"Terrain: {(terrainGenerated ? "Generated" : "Not Generated")}\n" +
                   $"Walls: {wallSegments} segments, {gates} gates, {towers} towers\n" +
                   $"Streets: {streets} segments\n" +
                   $"Buildings: {buildings} in {districts} districts\n" +
                   $"NavMesh: {(navMeshGenerated ? "Generated" : "Not Generated")}\n" +
                   $"Navigation: {navigationAreas} areas, {navigableArea:F1}m² navigable, {offMeshLinks} links\n" +
                   $"Collision System: {collisionStats}";
        }
    }
}