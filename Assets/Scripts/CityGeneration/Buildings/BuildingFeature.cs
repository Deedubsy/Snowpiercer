using UnityEngine;
using CityGeneration.Core;

namespace CityGeneration.Buildings
{
    /// <summary>
    /// Base class for procedural building features
    /// Features add architectural details, gameplay elements, and visual variety
    /// </summary>
    public abstract class BuildingFeature : ScriptableObject
    {
        [Header("Feature Configuration")]
        public string featureName;
        [Range(0f, 1f)] public float placementChance = 0.5f;
        public bool canStack = false;
        public int maxInstances = 1;

        [Header("Placement Constraints")]
        public ArchitecturalStyle[] compatibleStyles;
        public BuildingType[] compatibleBuildingTypes;
        public DistrictType[] preferredDistricts;
        public WealthLevel minimumWealth = WealthLevel.Poor;

        [Header("Feature Properties")]
        public FeatureCategory category = FeatureCategory.Decorative;
        public bool affectsGameplay = false;
        public bool providesHiding = false;
        public bool blocksMovement = false;

        /// <summary>
        /// Apply this feature to a building
        /// </summary>
        public abstract void ApplyFeature(GameObject building, BuildingContext context);

        /// <summary>
        /// Check if this feature can be applied to the building
        /// </summary>
        public virtual bool CanApplyTo(GameObject building, BuildingContext context)
        {
            // Check architectural style compatibility
            if (compatibleStyles != null && compatibleStyles.Length > 0)
            {
                bool styleCompatible = false;
                foreach (var style in compatibleStyles)
                {
                    if (style == context.architecturalStyle)
                    {
                        styleCompatible = true;
                        break;
                    }
                }
                if (!styleCompatible) return false;
            }

            // Check building type compatibility
            if (compatibleBuildingTypes != null && compatibleBuildingTypes.Length > 0)
            {
                bool typeCompatible = false;
                var buildingInfo = building.GetComponent<EnhancedBuildingInfo>();
                if (buildingInfo != null)
                {
                    foreach (var type in compatibleBuildingTypes)
                    {
                        if (type == buildingInfo.buildingType)
                        {
                            typeCompatible = true;
                            break;
                        }
                    }
                }
                if (!typeCompatible) return false;
            }

            // Check district preference
            if (preferredDistricts != null && preferredDistricts.Length > 0)
            {
                bool districtPreferred = false;
                foreach (var district in preferredDistricts)
                {
                    if (district == context.districtType)
                    {
                        districtPreferred = true;
                        break;
                    }
                }
                if (!districtPreferred) return false;
            }

            // Check wealth requirement
            if (GetWealthLevel(context.districtWealth) < minimumWealth)
            {
                return false;
            }

            // Check if already at maximum instances
            if (!canStack && GetExistingFeatureCount(building) >= maxInstances)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get attachment points on the building for this feature
        /// </summary>
        protected virtual Vector3[] GetAttachmentPoints(GameObject building, AttachmentType attachmentType)
        {
            Bounds bounds = GetBuildingBounds(building);
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;

            switch (attachmentType)
            {
                case AttachmentType.Roof:
                    return new Vector3[]
                    {
                        center + Vector3.up * (size.y * 0.5f),
                        center + Vector3.up * (size.y * 0.5f) + Vector3.right * (size.x * 0.3f),
                        center + Vector3.up * (size.y * 0.5f) + Vector3.left * (size.x * 0.3f),
                        center + Vector3.up * (size.y * 0.5f) + Vector3.forward * (size.z * 0.3f),
                        center + Vector3.up * (size.y * 0.5f) + Vector3.back * (size.z * 0.3f)
                    };

                case AttachmentType.Wall:
                    return new Vector3[]
                    {
                        center + Vector3.right * (size.x * 0.5f),   // East wall
                        center + Vector3.left * (size.x * 0.5f),    // West wall
                        center + Vector3.forward * (size.z * 0.5f), // North wall
                        center + Vector3.back * (size.z * 0.5f)     // South wall
                    };

                case AttachmentType.Ground:
                    return new Vector3[]
                    {
                        center + Vector3.right * (size.x * 0.7f) + Vector3.down * (size.y * 0.5f),
                        center + Vector3.left * (size.x * 0.7f) + Vector3.down * (size.y * 0.5f),
                        center + Vector3.forward * (size.z * 0.7f) + Vector3.down * (size.y * 0.5f),
                        center + Vector3.back * (size.z * 0.7f) + Vector3.down * (size.y * 0.5f)
                    };

                case AttachmentType.Corner:
                    return new Vector3[]
                    {
                        center + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f),   // NE corner
                        center + new Vector3(-size.x * 0.5f, 0f, size.z * 0.5f),  // NW corner
                        center + new Vector3(size.x * 0.5f, 0f, -size.z * 0.5f),  // SE corner
                        center + new Vector3(-size.x * 0.5f, 0f, -size.z * 0.5f)  // SW corner
                    };

                default:
                    return new Vector3[] { center };
            }
        }

        /// <summary>
        /// Create a feature GameObject with proper naming and parenting
        /// </summary>
        protected GameObject CreateFeatureObject(string name, Vector3 position, Transform parent, PrimitiveType primitiveType = PrimitiveType.Cube)
        {
            GameObject feature = GameObject.CreatePrimitive(primitiveType);
            feature.name = name;
            feature.transform.SetParent(parent);
            feature.transform.position = position;

            // Add feature identifier component
            var featureComponent = feature.AddComponent<BuildingFeatureComponent>();
            featureComponent.featureType = this;
            featureComponent.category = category;
            featureComponent.affectsGameplay = affectsGameplay;
            featureComponent.providesHiding = providesHiding;
            featureComponent.blocksMovement = blocksMovement;

            return feature;
        }

        /// <summary>
        /// Apply material based on building's material set
        /// </summary>
        protected void ApplyFeatureMaterial(GameObject featureObject, BuildingContext context, Color? colorOverride = null)
        {
            var renderer = featureObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));

                if (colorOverride.HasValue)
                {
                    material.color = colorOverride.Value;
                }
                else
                {
                    // Use building's color palette
                    material.color = GetBuildingColor(context);
                }

                // Apply weathering
                ApplyWeathering(material, context.weathering);

                renderer.material = material;
            }
        }

        /// <summary>
        /// Align feature to building's facing direction
        /// </summary>
        protected void AlignToBuilding(GameObject feature, GameObject building, bool faceOutward = true)
        {
            Vector3 buildingForward = building.transform.forward;

            if (faceOutward)
            {
                feature.transform.LookAt(feature.transform.position + buildingForward);
            }
            else
            {
                feature.transform.LookAt(feature.transform.position - buildingForward);
            }
        }

        // Helper methods
        protected Bounds GetBuildingBounds(GameObject building)
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

        protected int GetExistingFeatureCount(GameObject building)
        {
            var existingFeatures = building.GetComponentsInChildren<BuildingFeatureComponent>();
            int count = 0;
            foreach (var feature in existingFeatures)
            {
                if (feature.featureType.GetType() == this.GetType())
                {
                    count++;
                }
            }
            return count;
        }

        protected WealthLevel GetWealthLevel(float wealthValue)
        {
            if (wealthValue < 0.2f) return WealthLevel.Poor;
            if (wealthValue < 0.4f) return WealthLevel.Common;
            if (wealthValue < 0.6f) return WealthLevel.Comfortable;
            if (wealthValue < 0.8f) return WealthLevel.Wealthy;
            return WealthLevel.Rich;
        }

        protected Color GetBuildingColor(BuildingContext context)
        {
            // Default color based on district type
            switch (context.districtType)
            {
                case DistrictType.Castle: return new Color(0.6f, 0.6f, 0.7f);
                case DistrictType.Religious: return new Color(0.8f, 0.8f, 0.9f);
                case DistrictType.Market: return new Color(0.8f, 0.7f, 0.4f);
                case DistrictType.Residential: return new Color(0.7f, 0.8f, 0.6f);
                case DistrictType.Military: return new Color(0.6f, 0.5f, 0.5f);
                default: return Color.white;
            }
        }

        protected void ApplyWeathering(Material material, WeatheringLevel weathering)
        {
            switch (weathering)
            {
                case WeatheringLevel.Light:
                    material.color = Color.Lerp(material.color, Color.gray, 0.1f);
                    break;
                case WeatheringLevel.Medium:
                    material.color = Color.Lerp(material.color, Color.gray, 0.2f);
                    break;
                case WeatheringLevel.Heavy:
                    material.color = Color.Lerp(material.color, Color.gray, 0.4f);
                    break;
                case WeatheringLevel.Ruined:
                    material.color = Color.Lerp(material.color, new Color(0.3f, 0.3f, 0.2f), 0.6f);
                    break;
            }
        }

        protected bool IsValidAttachmentPoint(Vector3 point, GameObject building, BuildingContext context)
        {
            // Check if point is within reasonable bounds
            Bounds bounds = GetBuildingBounds(building);
            if (!bounds.Contains(point) && Vector3.Distance(point, bounds.center) > bounds.size.magnitude)
            {
                return false;
            }

            // Check collision with existing features
            var existingFeatures = building.GetComponentsInChildren<BuildingFeatureComponent>();
            foreach (var feature in existingFeatures)
            {
                if (Vector3.Distance(point, feature.transform.position) < 2f)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Component attached to feature objects for identification and gameplay integration
    /// </summary>
    public class BuildingFeatureComponent : MonoBehaviour
    {
        public BuildingFeature featureType;
        public FeatureCategory category;
        public bool affectsGameplay;
        public bool providesHiding;
        public bool blocksMovement;

        private void Start()
        {
            // Setup gameplay integration
            if (providesHiding)
            {
                SetupHidingSpot();
            }

            if (blocksMovement)
            {
                SetupMovementBlocker();
            }
        }

        private void SetupHidingSpot()
        {
            // Add components for stealth system integration
            var hidingSpot = gameObject.AddComponent<ShadowTrigger>();
            // Configure hiding spot properties
        }

        private void SetupMovementBlocker()
        {
            // Ensure collider blocks movement
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = false;
            }
        }
    }

    /// <summary>
    /// Context information for building feature generation
    /// </summary>
    [System.Serializable]
    public class BuildingContext
    {
        [Header("Building Properties")]
        public BuildingType buildingType;
        public ArchitecturalStyle architecturalStyle;
        public Vector3 availableSpace;

        [Header("District Context")]
        public DistrictType districtType;
        public float districtWealth; // 0-1
        public WealthLevel wealthLevel;

        [Header("Environmental Context")]
        public WeatheringLevel weathering = WeatheringLevel.Medium;
        public Terrain terrain;
        public float distanceToWalls;
        public float distanceToCenter;

        [Header("Neighbors")]
        public ArchitecturalStyle[] neighboringStyles;
        public BuildingType[] nearbyBuildings;

        [Header("Climate")]
        public ClimateType climate = ClimateType.Temperate;
        public WindDirection primaryWindDirection = WindDirection.North;

        public BuildingContext()
        {
            availableSpace = new Vector3(20f, 15f, 20f);
            districtWealth = 0.5f;
            wealthLevel = WealthLevel.Common;
        }

        public BuildingContext(CityGenerationContext cityContext)
        {
            availableSpace = new Vector3(20f, 15f, 20f);
            districtWealth = 0.5f;
            wealthLevel = WealthLevel.Common;

            // Extract relevant information from city context
            if (cityContext != null)
            {
                // Could populate from city context data
            }
        }
    }

    // Enums and supporting types
    public enum AttachmentType
    {
        Roof,
        Wall,
        Ground,
        Corner,
        Interior
    }

    public enum FeatureCategory
    {
        Structural,
        Decorative,
        Functional,
        Defensive,
        Religious,
        Commercial
    }

    public enum WealthLevel
    {
        Poor,
        Common,
        Comfortable,
        Wealthy,
        Rich
    }

    public enum ClimateType
    {
        Temperate,
        Cold,
        Hot,
        Wet,
        Dry
    }

    public enum WindDirection
    {
        North,
        South,
        East,
        West,
        Northeast,
        Northwest,
        Southeast,
        Southwest
    }
}