using UnityEngine;
using System.Collections.Generic;
using CityGeneration.Core;

namespace CityGeneration.Buildings
{
    /// <summary>
    /// Advanced building template system for creating diverse, contextual buildings
    /// Supports procedural features, architectural styles, and environmental adaptation
    /// </summary>
    [CreateAssetMenu(fileName = "New Building Template", menuName = "City Generation/Building Template")]
    public class BuildingTemplate : ScriptableObject
    {
        [Header("Basic Properties")]
        public string templateName;
        public BuildingType buildingType;
        public ArchitecturalStyle architecturalStyle;

        [Header("Geometry Variations")]
        public BuildingGeometry[] geometryVariants;
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
        public Vector2 heightRange = new Vector2(0.9f, 1.1f);
        public bool allowRotation = true;

        [Header("Materials & Colors")]
        public MaterialSet[] materialSets;
        public ColorPalette[] colorPalettes;
        public WeatheringLevel defaultWeathering = WeatheringLevel.Medium;

        [Header("Procedural Features")]
        public BuildingFeature[] availableFeatures;
        [Range(0f, 1f)] public float featureDensity = 0.5f;
        public bool adaptToContext = true;

        [Header("Gameplay Properties")]
        public bool hasInterior = false;
        public bool allowsHiding = true;
        public bool isLandmark = false;
        public PatrolRoute[] suggestedPatrolRoutes;

        [Header("Generation Constraints")]
        public float minDistanceFromOthers = 5f;
        public DistrictType[] preferredDistricts;
        public DistrictType[] forbiddenDistricts;

        /// <summary>
        /// Generate a building instance from this template
        /// </summary>
        public GameObject GenerateBuilding(Vector3 position, BuildingContext context, Transform parent = null)
        {
            // Select geometry variant
            var geometry = SelectGeometryVariant(context);
            if (geometry == null)
            {
                Debug.LogError($"No valid geometry found for template {templateName}");
                return null;
            }

            // Create base building
            GameObject building = CreateBaseBuilding(geometry, position, parent);

            // Apply scaling and rotation
            ApplyTransformVariations(building, geometry, context);

            // Apply materials and colors
            ApplyVisualVariations(building, context);

            // Add procedural features
            AddProceduralFeatures(building, context);

            // Setup gameplay components
            SetupGameplayFeatures(building, context);

            // Add building info component
            var buildingInfo = building.GetComponent<EnhancedBuildingInfo>();
            if (buildingInfo == null)
            {
                buildingInfo = building.AddComponent<EnhancedBuildingInfo>();
            }

            PopulateBuildingInfo(buildingInfo, geometry, context);

            return building;
        }

        private BuildingGeometry SelectGeometryVariant(BuildingContext context)
        {
            if (geometryVariants == null || geometryVariants.Length == 0)
            {
                Debug.LogError($"No geometry variants defined for template {templateName}");
                return null;
            }

            // Weight selection based on context
            var weightedVariants = new List<WeightedGeometry>();
            foreach (var variant in geometryVariants)
            {
                float weight = CalculateGeometryWeight(variant, context);
                if (weight > 0f)
                {
                    weightedVariants.Add(new WeightedGeometry { geometry = variant, weight = weight });
                }
            }

            if (weightedVariants.Count == 0)
            {
                Debug.LogWarning($"No suitable geometry variants for context, using first available");
                return geometryVariants[0];
            }

            // Select based on weighted probability
            float totalWeight = 0f;
            foreach (var weighted in weightedVariants)
            {
                totalWeight += weighted.weight;
            }

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var weighted in weightedVariants)
            {
                currentWeight += weighted.weight;
                if (randomValue <= currentWeight)
                {
                    return weighted.geometry;
                }
            }

            return weightedVariants[0].geometry; // Fallback
        }

        private float CalculateGeometryWeight(BuildingGeometry geometry, BuildingContext context)
        {
            float baseWeight = geometry.rarityWeight;

            // Adjust weight based on district wealth
            if (context.districtWealth < 0.3f && geometry.isLuxury)
            {
                baseWeight *= 0.2f; // Much less likely in poor districts
            }
            else if (context.districtWealth > 0.7f && !geometry.isLuxury)
            {
                baseWeight *= 0.5f; // Less likely simple buildings in wealthy areas
            }

            // Adjust based on available space
            Vector3 requiredSize = geometry.baseSize;
            if (context.availableSpace.x < requiredSize.x || context.availableSpace.z < requiredSize.z)
            {
                baseWeight *= 0.1f; // Much less likely if doesn't fit
            }

            // Adjust based on architectural coherence
            if (context.neighboringStyles != null)
            {
                bool matchesNeighbors = false;
                foreach (var style in context.neighboringStyles)
                {
                    if (geometry.architecturalStyle == style)
                    {
                        matchesNeighbors = true;
                        break;
                    }
                }

                if (matchesNeighbors)
                {
                    baseWeight *= 1.5f; // Prefer matching styles
                }
                else if (context.neighboringStyles.Length > 2)
                {
                    baseWeight *= 0.7f; // Slightly discourage style mixing in dense areas
                }
            }

            return Mathf.Max(0f, baseWeight);
        }

        private GameObject CreateBaseBuilding(BuildingGeometry geometry, Vector3 position, Transform parent)
        {
            GameObject building;

            if (geometry.prefab != null)
            {
                // Use prefab if available
                building = Instantiate(geometry.prefab, position, Quaternion.identity, parent);
                building.name = $"{templateName}_{geometry.variantName}";
            }
            else
            {
                // Create procedural geometry
                building = CreateProceduralBuilding(geometry, position, parent);
            }

            return building;
        }

        private GameObject CreateProceduralBuilding(BuildingGeometry geometry, Vector3 position, Transform parent)
        {
            GameObject building = new GameObject($"{templateName}_{geometry.variantName}");
            building.transform.SetParent(parent);
            building.transform.position = position;

            // Create base structure
            switch (geometry.geometryType)
            {
                case GeometryType.Simple:
                    CreateSimpleBuilding(building, geometry);
                    break;
                case GeometryType.Compound:
                    CreateCompoundBuilding(building, geometry);
                    break;
                case GeometryType.Tower:
                    CreateTowerBuilding(building, geometry);
                    break;
                case GeometryType.Complex:
                    CreateComplexBuilding(building, geometry);
                    break;
            }

            return building;
        }

        private void CreateSimpleBuilding(GameObject building, BuildingGeometry geometry)
        {
            // Create main building cube
            GameObject main = GameObject.CreatePrimitive(PrimitiveType.Cube);
            main.name = "MainStructure";
            main.transform.SetParent(building.transform);
            main.transform.localPosition = Vector3.zero;
            main.transform.localScale = geometry.baseSize;
        }

        private void CreateCompoundBuilding(GameObject building, BuildingGeometry geometry)
        {
            // Create main structure
            GameObject main = GameObject.CreatePrimitive(PrimitiveType.Cube);
            main.name = "MainStructure";
            main.transform.SetParent(building.transform);
            main.transform.localPosition = Vector3.zero;
            main.transform.localScale = geometry.baseSize;

            // Add smaller attached structures
            int attachmentCount = Random.Range(1, 3);
            for (int i = 0; i < attachmentCount; i++)
            {
                GameObject attachment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                attachment.name = $"Attachment_{i}";
                attachment.transform.SetParent(building.transform);

                // Random attachment position
                Vector3 attachmentPos = new Vector3(
                    Random.Range(-geometry.baseSize.x * 0.4f, geometry.baseSize.x * 0.4f),
                    0f,
                    Random.Range(-geometry.baseSize.z * 0.6f, geometry.baseSize.z * 0.6f)
                );
                attachment.transform.localPosition = attachmentPos;

                // Smaller scale
                Vector3 attachmentScale = geometry.baseSize * Random.Range(0.3f, 0.6f);
                attachmentScale.y *= Random.Range(0.5f, 0.8f);
                attachment.transform.localScale = attachmentScale;
            }
        }

        private void CreateTowerBuilding(GameObject building, BuildingGeometry geometry)
        {
            // Create base
            GameObject baseStructure = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseStructure.name = "Base";
            baseStructure.transform.SetParent(building.transform);
            baseStructure.transform.localPosition = Vector3.zero;

            Vector3 baseSize = geometry.baseSize;
            baseSize.y *= 0.3f; // Lower base
            baseStructure.transform.localScale = baseSize;

            // Create tower
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = "Tower";
            tower.transform.SetParent(building.transform);

            Vector3 towerSize = geometry.baseSize;
            towerSize.x *= 0.6f;
            towerSize.z *= 0.6f;
            towerSize.y *= 1.5f; // Taller
            tower.transform.localScale = towerSize;

            Vector3 towerPos = Vector3.up * (baseSize.y * 0.5f + towerSize.y * 0.5f);
            tower.transform.localPosition = towerPos;
        }

        private void CreateComplexBuilding(GameObject building, BuildingGeometry geometry)
        {
            // Create central courtyard design
            Vector3 courtyardSize = geometry.baseSize * 0.4f;

            // Create four wings around courtyard
            Vector3 wingSize = new Vector3(
                geometry.baseSize.x * 0.3f,
                geometry.baseSize.y,
                geometry.baseSize.z * 0.3f
            );

            Vector3[] wingPositions = {
                new Vector3(-courtyardSize.x, 0, -courtyardSize.z), // SW
                new Vector3(courtyardSize.x, 0, -courtyardSize.z),  // SE
                new Vector3(courtyardSize.x, 0, courtyardSize.z),   // NE
                new Vector3(-courtyardSize.x, 0, courtyardSize.z)   // NW
            };

            for (int i = 0; i < wingPositions.Length; i++)
            {
                GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wing.name = $"Wing_{i}";
                wing.transform.SetParent(building.transform);
                wing.transform.localPosition = wingPositions[i];
                wing.transform.localScale = wingSize;
            }
        }

        private void ApplyTransformVariations(GameObject building, BuildingGeometry geometry, BuildingContext context)
        {
            // Apply scale variation
            if (geometry.allowScaling)
            {
                float scaleMultiplier = Random.Range(scaleRange.x, scaleRange.y);
                float heightMultiplier = Random.Range(heightRange.x, heightRange.y);

                Vector3 currentScale = building.transform.localScale;
                building.transform.localScale = new Vector3(
                    currentScale.x * scaleMultiplier,
                    currentScale.y * heightMultiplier,
                    currentScale.z * scaleMultiplier
                );
            }

            // Apply rotation
            if (allowRotation && geometry.allowRotation)
            {
                float randomRotation = Random.Range(0f, 360f);
                building.transform.Rotate(0f, randomRotation, 0f);
            }

            // Adjust position to ground level
            AdjustToGroundLevel(building, context);
        }

        private void AdjustToGroundLevel(GameObject building, BuildingContext context)
        {
            // Get building bounds
            Bounds bounds = GetBuildingBounds(building);
            float groundLevel = GetGroundLevel(building.transform.position, context);

            // Adjust position so building sits on ground
            Vector3 currentPos = building.transform.position;
            currentPos.y = groundLevel + bounds.size.y * 0.5f;
            building.transform.position = currentPos;
        }

        private Bounds GetBuildingBounds(GameObject building)
        {
            var renderers = building.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(building.transform.position, Vector3.one * 5f);
            }

            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }

            return combinedBounds;
        }

        private float GetGroundLevel(Vector3 position, BuildingContext context)
        {
            if (context.terrain != null)
            {
                return context.terrain.SampleHeight(position);
            }

            // Fallback: raycast down
            if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f))
            {
                return hit.point.y;
            }

            return 0f;
        }

        private void ApplyVisualVariations(GameObject building, BuildingContext context)
        {
            // Select material set
            var materialSet = SelectMaterialSet(context);
            var colorPalette = SelectColorPalette(context);

            // Apply materials to all renderers
            var renderers = building.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                ApplyMaterialToRenderer(renderer, materialSet, colorPalette, context);
            }
        }

        private MaterialSet SelectMaterialSet(BuildingContext context)
        {
            if (materialSets == null || materialSets.Length == 0)
            {
                return CreateDefaultMaterialSet();
            }

            // Select based on district wealth and style
            foreach (var materialSet in materialSets)
            {
                if (IsAppropriateMaterialSet(materialSet, context))
                {
                    return materialSet;
                }
            }

            return materialSets[0]; // Fallback
        }

        private ColorPalette SelectColorPalette(BuildingContext context)
        {
            if (colorPalettes == null || colorPalettes.Length == 0)
            {
                return CreateDefaultColorPalette();
            }

            // Select based on context
            return colorPalettes[Random.Range(0, colorPalettes.Length)];
        }

        private void ApplyMaterialToRenderer(Renderer renderer, MaterialSet materialSet, ColorPalette colorPalette, BuildingContext context)
        {
            Material material = new Material(Shader.Find("Standard"));

            // Base color from palette
            material.color = colorPalette.GetRandomColor();

            // Apply material properties
            material.SetFloat("_Metallic", materialSet.metallicness);
            material.SetFloat("_Smoothness", 1f - materialSet.roughness);

            // Apply weathering
            ApplyWeathering(material, context.weathering);

            renderer.material = material;
        }

        private void ApplyWeathering(Material material, WeatheringLevel weathering)
        {
            switch (weathering)
            {
                case WeatheringLevel.Pristine:
                    // No weathering
                    break;
                case WeatheringLevel.Light:
                    material.color = Color.Lerp(material.color, Color.gray, 0.1f);
                    material.SetFloat("_Smoothness", material.GetFloat("_Smoothness") * 0.9f);
                    break;
                case WeatheringLevel.Medium:
                    material.color = Color.Lerp(material.color, Color.gray, 0.2f);
                    material.SetFloat("_Smoothness", material.GetFloat("_Smoothness") * 0.7f);
                    break;
                case WeatheringLevel.Heavy:
                    material.color = Color.Lerp(material.color, Color.gray, 0.4f);
                    material.SetFloat("_Smoothness", material.GetFloat("_Smoothness") * 0.5f);
                    break;
                case WeatheringLevel.Ruined:
                    material.color = Color.Lerp(material.color, new Color(0.3f, 0.3f, 0.2f), 0.6f);
                    material.SetFloat("_Smoothness", 0.1f);
                    break;
            }
        }

        private void AddProceduralFeatures(GameObject building, BuildingContext context)
        {
            if (availableFeatures == null || availableFeatures.Length == 0) return;

            foreach (var feature in availableFeatures)
            {
                if (Random.value < feature.placementChance * featureDensity)
                {
                    feature.ApplyFeature(building, context);
                }
            }

            // Add context-specific features
            if (adaptToContext)
            {
                AddContextualFeatures(building, context);
            }
        }

        private void AddContextualFeatures(GameObject building, BuildingContext context)
        {
            // Add features based on district type and context
            switch (context.districtType)
            {
                case DistrictType.Market:
                    if (buildingType == BuildingType.Shop)
                    {
                        AddShopSign(building, context);
                    }
                    break;
                case DistrictType.Military:
                    AddDefensiveFeatures(building, context);
                    break;
                case DistrictType.Religious:
                    AddReligiousFeatures(building, context);
                    break;
            }

            // Add wealth-based features
            if (context.districtWealth > 0.7f)
            {
                AddLuxuryFeatures(building, context);
            }
        }

        private void AddShopSign(GameObject building, BuildingContext context)
        {
            // Create a simple shop sign
            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "ShopSign";
            sign.transform.SetParent(building.transform);

            // Position in front of building
            Bounds bounds = GetBuildingBounds(building);
            Vector3 signPos = building.transform.position + building.transform.forward * (bounds.size.z * 0.6f);
            signPos.y = bounds.center.y + bounds.size.y * 0.2f;

            sign.transform.position = signPos;
            sign.transform.localScale = new Vector3(2f, 0.5f, 0.1f);

            // Color it distinctively
            var renderer = sign.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.8f, 0.2f, 0.2f); // Red sign
                renderer.material = material;
            }
        }

        private void AddDefensiveFeatures(GameObject building, BuildingContext context)
        {
            // Add defensive elements for military buildings
            // This is a simplified example - could add battlements, arrow slits, etc.
        }

        private void AddReligiousFeatures(GameObject building, BuildingContext context)
        {
            // Add religious symbols, spires, etc.
        }

        private void AddLuxuryFeatures(GameObject building, BuildingContext context)
        {
            // Add luxury elements like decorative elements, better materials, etc.
        }

        private void SetupGameplayFeatures(GameObject building, BuildingContext context)
        {
            // Add colliders
            var buildingCollider = building.GetComponent<Collider>();
            if (buildingCollider == null)
            {
                buildingCollider = building.AddComponent<BoxCollider>();
            }

            // Setup hiding spots if applicable
            if (allowsHiding)
            {
                CreateHidingSpots(building, context);
            }

            // Setup patrol routes if suggested
            if (suggestedPatrolRoutes != null && suggestedPatrolRoutes.Length > 0)
            {
                SetupPatrolRoutes(building, context);
            }
        }

        private void CreateHidingSpots(GameObject building, BuildingContext context)
        {
            // Create hiding spots around the building
            // This would integrate with the existing stealth system
        }

        private void SetupPatrolRoutes(GameObject building, BuildingContext context)
        {
            // Setup suggested patrol routes for AI
            // This would integrate with the AI system
        }

        private void PopulateBuildingInfo(EnhancedBuildingInfo buildingInfo, BuildingGeometry geometry, BuildingContext context)
        {
            buildingInfo.buildingTemplate = this;
            buildingInfo.buildingType = buildingType;
            buildingInfo.architecturalStyle = architecturalStyle;
            buildingInfo.hasInterior = hasInterior;
            buildingInfo.allowsHiding = allowsHiding;
            buildingInfo.isLandmark = isLandmark;
            buildingInfo.districtType = context.districtType;
            buildingInfo.wealthLevel = context.districtWealth;
            buildingInfo.weatheringLevel = context.weathering;
        }

        // Helper methods for default creation
        private MaterialSet CreateDefaultMaterialSet()
        {
            return new MaterialSet
            {
                setName = "Default",
                metallicness = 0f,
                roughness = 0.8f
            };
        }

        private ColorPalette CreateDefaultColorPalette()
        {
            return new ColorPalette
            {
                paletteName = "Default",
                colors = new Color[] { Color.white, Color.gray, new Color(0.9f, 0.8f, 0.7f) }
            };
        }

        private bool IsAppropriateMaterialSet(MaterialSet materialSet, BuildingContext context)
        {
            // Simple logic - could be enhanced with more sophisticated matching
            return true;
        }
    }

    // Supporting classes and enums would be defined in separate files
    // This is a simplified version for the implementation

    [System.Serializable]
    public class BuildingGeometry
    {
        public string variantName;
        public GeometryType geometryType = GeometryType.Simple;
        public GameObject prefab;
        public Vector3 baseSize = new Vector3(8f, 6f, 8f);
        public bool allowScaling = true;
        public bool allowRotation = true;
        public int rarityWeight = 1;
        public bool isLuxury = false;
        public ArchitecturalStyle architecturalStyle;
    }

    [System.Serializable]
    public class MaterialSet
    {
        public string setName;
        public float metallicness = 0f;
        public float roughness = 0.8f;
    }

    [System.Serializable]
    public class ColorPalette
    {
        public string paletteName;
        public Color[] colors;

        public Color GetRandomColor()
        {
            if (colors == null || colors.Length == 0)
                return Color.white;

            return colors[Random.Range(0, colors.Length)];
        }
    }

    public enum GeometryType
    {
        Simple,
        Compound,
        Tower,
        Complex
    }

    public enum ArchitecturalStyle
    {
        Peasant,
        Merchant,
        Noble,
        Religious,
        Military,
        Royal
    }

    public enum WeatheringLevel
    {
        Pristine,
        Light,
        Medium,
        Heavy,
        Ruined
    }

    [System.Serializable]
    public class PatrolRoute
    {
        public Vector3[] waypoints;
        public float patrolSpeed = 2f;
        public bool isLooping = true;
    }

    [System.Serializable]
    public class WeightedGeometry
    {
        public BuildingGeometry geometry;
        public float weight;
    }
}