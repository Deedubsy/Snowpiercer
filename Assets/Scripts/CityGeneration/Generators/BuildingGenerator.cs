using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using CityGeneration.Core;

namespace CityGeneration.Generators
{
    /// <summary>
    /// Generates buildings for different districts in the medieval city
    /// Extracted and modernized from MedievalCityBuilder
    /// </summary>
    public class BuildingGenerator : BaseGenerator
    {
        [Header("Building Configuration")]
        [Range(0.3f, 1.0f)] public float buildingDensity = 0.7f;
        [Range(1, 5)] public int maxBuildingsPerDistrict = 3;
        [Range(2f, 8f)] public float minBuildingHeight = 3f;
        [Range(4f, 15f)] public float maxBuildingHeight = 8f;
        [Range(3f, 12f)] public float minBuildingSize = 5f;
        [Range(6f, 20f)] public float maxBuildingSize = 10f;

        [Header("District Configuration")]
        public bool includeCastle = true;
        public bool includeCathedral = true;
        public bool includeMarketSquare = true;
        public bool includeNobleQuarter = true;
        public bool includeArtisanQuarter = true;
        public bool includeResidential = true;
        public bool includeTavernDistrict = true;
        public bool includeBarracks = true;

        [Header("Building Colors")]
        public Color[] buildingColors = new Color[]
        {
            new Color(0.9f, 0.8f, 0.7f), // Cream
            new Color(0.7f, 0.6f, 0.5f), // Brown
            new Color(0.8f, 0.7f, 0.6f), // Tan
            new Color(0.6f, 0.5f, 0.4f)  // Dark Brown
        };

        [Header("Performance")]
        public bool combineBuildingMeshes = false;
        public int buildingBatchSize = 5; // Buildings per batch for progressive generation

        private List<GameObject> generatedBuildings = new List<GameObject>();
        private Dictionary<string, List<GameObject>> buildingsByDistrict = new Dictionary<string, List<GameObject>>();

        // Building templates (simplified from original)
        private readonly BuildingTemplate[] buildingTemplates = new BuildingTemplate[]
        {
            new BuildingTemplate { type = BuildingType.Castle, size = new Vector3(15, 12, 15), height = 12f, color = new Color(0.6f, 0.6f, 0.7f), hasCourtyard = true },
            new BuildingTemplate { type = BuildingType.Cathedral, size = new Vector3(12, 15, 20), height = 15f, color = new Color(0.8f, 0.8f, 0.9f), hasCourtyard = false },
            new BuildingTemplate { type = BuildingType.House, size = new Vector3(6, 5, 8), height = 5f, color = new Color(0.9f, 0.8f, 0.7f), hasCourtyard = false },
            new BuildingTemplate { type = BuildingType.Shop, size = new Vector3(8, 4, 6), height = 4f, color = new Color(0.8f, 0.7f, 0.6f), hasCourtyard = false },
            new BuildingTemplate { type = BuildingType.Tavern, size = new Vector3(10, 6, 10), height = 6f, color = new Color(0.7f, 0.5f, 0.3f), hasCourtyard = false },
            new BuildingTemplate { type = BuildingType.Barracks, size = new Vector3(12, 4, 8), height = 4f, color = new Color(0.6f, 0.5f, 0.5f), hasCourtyard = true },
            new BuildingTemplate { type = BuildingType.Workshop, size = new Vector3(7, 4, 9), height = 4f, color = new Color(0.7f, 0.6f, 0.5f), hasCourtyard = false }
        };

        protected override async Task<GenerationResult> GenerateInternal(CityGenerationContext context)
        {
            var result = new BuildingGenerationResult();
            generatedBuildings.Clear();
            buildingsByDistrict.Clear();

            // Copy configuration from context if available
            ApplyContextConfiguration(context);

            Transform buildingParent = CreateCategoryParent("Buildings");

            try
            {
                // Generate district definitions
                UpdateProgress(0f, "Planning districts...");
                var districts = await PlanDistricts(context);

                // Generate buildings for each district
                float districtProgress = 0f;
                int totalDistricts = districts.Count;

                foreach (var district in districts)
                {
                    UpdateProgress(districtProgress / totalDistricts, $"Building {district.Key} district...");

                    var buildingsInDistrict = await GenerateBuildingsForDistrict(district.Key, district.Value, buildingParent, context);

                    if (buildingsInDistrict.Count > 0)
                    {
                        buildingsByDistrict[district.Key] = buildingsInDistrict;
                        result.buildings.AddRange(buildingsInDistrict);
                    }

                    districtProgress++;
                    await Task.Yield();
                }

                result.buildingsByDistrict = new Dictionary<string, List<GameObject>>(buildingsByDistrict);

                // Register with collision system
                UpdateProgress(0.9f, "Registering building collisions...");
                RegisterBuildingCollisions(result);

                // Optimize if requested
                if (combineBuildingMeshes)
                {
                    UpdateProgress(0.95f, "Optimizing building meshes...");
                    await OptimizeBuildingMeshes(result);
                }

                result.objectsGenerated = result.buildings.Count;
                LogDebug($"Generated {result.objectsGenerated} buildings across {districts.Count} districts");

                return result;
            }
            catch (System.Exception ex)
            {
                result.MarkAsError($"Building generation failed: {ex.Message}");
                throw;
            }
        }

        private async Task<Dictionary<string, Vector3>> PlanDistricts(CityGenerationContext context)
        {
            var districts = new Dictionary<string, Vector3>();

            // Get city bounds from walls if available
            float citySize = context.config?.GetCitySize() ?? 100f;
            float districtRadius = citySize * 0.3f;

            // Plan district locations (simplified grid layout)
            if (includeCastle)
                districts["Castle"] = Vector3.zero; // Center

            if (includeMarketSquare)
                districts["Market"] = new Vector3(0, 0, -districtRadius * 0.5f);

            if (includeResidential)
                districts["Residential"] = new Vector3(-districtRadius * 0.7f, 0, 0);

            if (includeNobleQuarter)
                districts["Noble"] = new Vector3(districtRadius * 0.7f, 0, 0);

            if (includeArtisanQuarter)
                districts["Artisan"] = new Vector3(-districtRadius * 0.5f, 0, districtRadius * 0.5f);

            if (includeTavernDistrict)
                districts["Tavern"] = new Vector3(districtRadius * 0.5f, 0, districtRadius * 0.5f);

            if (includeBarracks)
                districts["Barracks"] = new Vector3(0, 0, districtRadius * 0.8f);

            if (includeCathedral)
                districts["Cathedral"] = new Vector3(districtRadius * 0.3f, 0, -districtRadius * 0.8f);

            await Task.Yield();
            return districts;
        }

        private async Task<List<GameObject>> GenerateBuildingsForDistrict(string districtName, Vector3 center, Transform parent, CityGenerationContext context)
        {
            var buildings = new List<GameObject>();

            Transform districtParent = new GameObject($"District_{districtName}").transform;
            districtParent.SetParent(parent);
            districtParent.position = center;

            // Determine building types for this district
            var buildingTypes = GetBuildingTypesForDistrict(districtName);
            if (buildingTypes.Count == 0)
            {
                LogDebug($"No building types defined for district: {districtName}");
                return buildings;
            }

            // Generate buildings in a radial pattern around district center
            float districtSize = GetDistrictSize(districtName);
            int buildingCount = Mathf.RoundToInt(maxBuildingsPerDistrict * buildingDensity);

            for (int i = 0; i < buildingCount; i++)
            {
                // Find valid building position
                Vector3 buildingPos = await FindValidBuildingPosition(center, districtSize, context);

                if (buildingPos == Vector3.zero)
                {
                    LogDebug($"Could not find valid position for building {i} in {districtName}");
                    continue;
                }

                // Select building type
                BuildingType buildingType = SelectBuildingType(buildingTypes, districtName);

                // Create building
                GameObject building = await CreateBuildingOfType(buildingType, buildingPos, districtParent, i);

                if (building != null)
                {
                    buildings.Add(building);
                    generatedBuildings.Add(building);

                    // Register with collision system
                    float buildingRadius = CalculateBuildingRadius(building);
                    collisionManager.RegisterStaticObject(building, ObjectType.Building, buildingRadius);
                }

                // Progressive generation
                if (i % buildingBatchSize == 0)
                {
                    float progress = (float)i / buildingCount;
                    UpdateProgress(progress, $"Generated {i}/{buildingCount} buildings in {districtName}");
                    await Task.Yield();
                }
            }

            LogDebug($"Generated {buildings.Count} buildings for {districtName} district");
            return buildings;
        }

        private async Task<Vector3> FindValidBuildingPosition(Vector3 center, float districtSize, CityGenerationContext context)
        {
            int maxAttempts = 20;
            float minDistanceFromCenter = districtSize * 0.1f;
            float maxDistanceFromCenter = districtSize * 0.8f;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Generate random position within district bounds
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(minDistanceFromCenter, maxDistanceFromCenter);

                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance
                );

                Vector3 candidate = center + offset;

                // Check if position is valid using collision manager
                float buildingRadius = (minBuildingSize + maxBuildingSize) * 0.25f; // Average building radius

                if (collisionManager.IsPositionValid(candidate, buildingRadius, ObjectType.Building))
                {
                    // Additional check: not too close to walls or other constraints
                    if (IsPositionSuitableForBuilding(candidate, context))
                    {
                        return candidate;
                    }
                }

                await Task.Yield();
            }

            return Vector3.zero; // No valid position found
        }

        private bool IsPositionSuitableForBuilding(Vector3 position, CityGenerationContext context)
        {
            // Check minimum distance from walls if available
            var nearbyWalls = collisionManager.GetObjectsInRadius(position, 10f, ObjectType.Wall);
            if (nearbyWalls.Count > 0)
            {
                foreach (var wall in nearbyWalls)
                {
                    if (Vector3.Distance(position, wall.transform.position) < 8f)
                    {
                        return false;
                    }
                }
            }

            // Check if too close to roads (want some distance but not too far)
            if (collisionManager.IsPositionOnRoad(position, 3f))
            {
                return false; // Too close to road
            }

            float distanceToNearestRoad = Vector3.Distance(position, collisionManager.GetNearestRoadPoint(position));
            if (distanceToNearestRoad > 20f)
            {
                return false; // Too far from road access
            }

            return true;
        }

        private async Task<GameObject> CreateBuildingOfType(BuildingType type, Vector3 position, Transform parent, int index)
        {
            BuildingTemplate template = GetTemplateForType(type);
            if (template == null)
            {
                LogDebug($"No template found for building type: {type}");
                return null;
            }

            string buildingName = $"{type}_{index}";
            GameObject building = CreateCube(buildingName, position, parent);

            // Apply template properties with some variation
            Vector3 size = template.size;
            size.x += Random.Range(-1f, 1f);
            size.z += Random.Range(-1f, 1f);
            size.x = Mathf.Clamp(size.x, minBuildingSize, maxBuildingSize);
            size.z = Mathf.Clamp(size.z, minBuildingSize, maxBuildingSize);
            size.y = Random.Range(minBuildingHeight, maxBuildingHeight);

            building.transform.localScale = size;
            building.transform.position = position + Vector3.up * (size.y * 0.5f);

            // Apply color with variation
            Color buildingColor = template.color;
            buildingColor.r += Random.Range(-0.1f, 0.1f);
            buildingColor.g += Random.Range(-0.1f, 0.1f);
            buildingColor.b += Random.Range(-0.1f, 0.1f);
            buildingColor = Color.Lerp(buildingColor, buildingColors[Random.Range(0, buildingColors.Length)], 0.3f);

            ApplyMaterial(building, buildingColor, false);

            // Add building type component for identification
            var buildingInfo = building.AddComponent<BuildingInfo>();
            buildingInfo.buildingType = type;
            buildingInfo.hasCourtyard = template.hasCourtyard;

            await Task.Yield();
            return building;
        }

        private List<BuildingType> GetBuildingTypesForDistrict(string districtName)
        {
            var types = new List<BuildingType>();

            switch (districtName.ToLower())
            {
                case "castle":
                    types.Add(BuildingType.Castle);
                    types.Add(BuildingType.Barracks);
                    break;
                case "market":
                    types.Add(BuildingType.Shop);
                    types.Add(BuildingType.Tavern);
                    types.Add(BuildingType.House);
                    break;
                case "residential":
                    types.Add(BuildingType.House);
                    break;
                case "noble":
                    types.Add(BuildingType.House);
                    break;
                case "artisan":
                    types.Add(BuildingType.Workshop);
                    types.Add(BuildingType.House);
                    break;
                case "tavern":
                    types.Add(BuildingType.Tavern);
                    types.Add(BuildingType.House);
                    break;
                case "barracks":
                    types.Add(BuildingType.Barracks);
                    break;
                case "cathedral":
                    types.Add(BuildingType.Cathedral);
                    break;
                default:
                    types.Add(BuildingType.House); // Default
                    break;
            }

            return types;
        }

        private BuildingType SelectBuildingType(List<BuildingType> availableTypes, string districtName)
        {
            if (availableTypes.Count == 0)
                return BuildingType.House;

            // Primary building type for district center
            if (districtName.ToLower() == "castle" && availableTypes.Contains(BuildingType.Castle))
                return BuildingType.Castle;
            if (districtName.ToLower() == "cathedral" && availableTypes.Contains(BuildingType.Cathedral))
                return BuildingType.Cathedral;

            // Random selection with some weighting
            return availableTypes[Random.Range(0, availableTypes.Count)];
        }

        private float GetDistrictSize(string districtName)
        {
            switch (districtName.ToLower())
            {
                case "castle": return 30f;
                case "cathedral": return 25f;
                case "market": return 35f;
                default: return 20f;
            }
        }

        private BuildingTemplate GetTemplateForType(BuildingType type)
        {
            foreach (var template in buildingTemplates)
            {
                if (template.type == type) return template;
            }
            return null;
        }

        private float CalculateBuildingRadius(GameObject building)
        {
            var renderer = building.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds.size.magnitude * 0.5f;
            }
            return building.transform.localScale.magnitude * 0.5f;
        }

        private void RegisterBuildingCollisions(BuildingGenerationResult result)
        {
            foreach (var building in result.buildings)
            {
                float radius = CalculateBuildingRadius(building);
                collisionManager.RegisterStaticObject(building, ObjectType.Building, radius);
            }
        }

        private async Task OptimizeBuildingMeshes(BuildingGenerationResult result)
        {
            // TODO: Implement mesh combining for better performance
            await Task.Yield();
            LogDebug("Building mesh optimization completed");
        }

        private void ApplyContextConfiguration(CityGenerationContext context)
        {
            if (context?.config != null)
            {
                var config = context.config;
                buildingDensity = config.buildingDensity;
                maxBuildingsPerDistrict = config.maxBuildingsPerDistrict;
                minBuildingHeight = config.minBuildingHeight;
                maxBuildingHeight = config.maxBuildingHeight;
            }
        }

        protected override Task<bool> ValidatePreConditions()
        {
            // Ensure we have building templates
            bool hasTemplates = buildingTemplates != null && buildingTemplates.Length > 0;

            if (!hasTemplates)
            {
                LogDebug("No building templates available");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        protected override Task ValidateResult(GenerationResult result)
        {
            var buildingResult = result as BuildingGenerationResult;

            if (buildingResult?.buildings.Count == 0)
            {
                LogDebug("Warning: No buildings were generated");
            }

            return Task.CompletedTask;
        }
    }

    // Supporting classes
    [System.Serializable]
    public class BuildingTemplate
    {
        public BuildingType type;
        public Vector3 size;
        public float height;
        public Color color;
        public bool hasCourtyard;
    }

    public enum BuildingType
    {
        Castle,
        Cathedral,
        House,
        Shop,
        Tavern,
        Barracks,
        Workshop
    }

    /// <summary>
    /// Component attached to generated buildings for identification
    /// </summary>
    public class BuildingInfo : MonoBehaviour
    {
        public BuildingType buildingType;
        public bool hasCourtyard;
        public bool hasInterior;

        public Vector3[] GetEntrancePoints()
        {
            // Simple entrance at the front of the building
            Vector3 frontCenter = transform.position + transform.forward * (transform.localScale.z * 0.5f);
            return new Vector3[] { frontCenter };
        }

        public Vector3 GetInteriorSpawnPoint()
        {
            return transform.position + Vector3.up * 2f;
        }
    }
}