using UnityEngine;
using CityGeneration.Core;

namespace CityGeneration.Rules
{
    /// <summary>
    /// Base class for all placement rules in the procedural rule system
    /// Rules determine where districts and buildings can be placed based on realistic constraints
    /// </summary>
    public abstract class PlacementRule : ScriptableObject
    {
        [Header("Rule Configuration")]
        public string ruleName;
        [Range(0f, 10f)] public float priority = 1f;
        public bool isRequired = false;
        public bool enableDebugLogging = false;

        [Header("Rule Description")]
        [TextArea(2, 4)]
        public string ruleDescription = "Describe what this rule does...";

        /// <summary>
        /// Check if a position can be used for placement
        /// </summary>
        /// <param name="position">World position to test</param>
        /// <param name="context">Placement context with city data</param>
        /// <returns>True if placement is allowed</returns>
        public abstract bool CanPlace(Vector3 position, PlacementContext context);

        /// <summary>
        /// Calculate how desirable this position is (0-1, higher is better)
        /// </summary>
        /// <param name="position">World position to evaluate</param>
        /// <param name="context">Placement context with city data</param>
        /// <returns>Desirability score 0-1</returns>
        public abstract float GetDesirability(Vector3 position, PlacementContext context);

        /// <summary>
        /// Get the influence radius of this rule
        /// </summary>
        public virtual float GetInfluenceRadius(PlacementContext context)
        {
            return 20f; // Default influence radius
        }

        /// <summary>
        /// Optional: Modify the placement position to better fit the rule
        /// </summary>
        public virtual Vector3 ModifyPosition(Vector3 originalPosition, PlacementContext context)
        {
            return originalPosition;
        }

        /// <summary>
        /// Helper method for debug logging
        /// </summary>
        protected void LogDebug(string message)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[{ruleName}] {message}");
            }
        }

        /// <summary>
        /// Helper method to calculate distance to nearest object of type
        /// </summary>
        protected float GetDistanceToNearest(Vector3 position, System.Type objectType, PlacementContext context)
        {
            // Implementation depends on context's object tracking
            // This is a simplified version
            return Vector3.Distance(position, Vector3.zero);
        }

        /// <summary>
        /// Helper method to check if position is within city bounds
        /// </summary>
        protected bool IsWithinCityBounds(Vector3 position, PlacementContext context)
        {
            if (context.cityBounds.HasValue)
            {
                return context.cityBounds.Value.Contains(position);
            }
            return true; // Assume valid if no bounds specified
        }

        /// <summary>
        /// Helper method to get terrain height at position
        /// </summary>
        protected float GetTerrainHeight(Vector3 position, PlacementContext context)
        {
            if (context.terrain != null)
            {
                return context.terrain.SampleHeight(position);
            }
            return 0f;
        }

        /// <summary>
        /// Helper method to calculate slope at position
        /// </summary>
        protected float GetTerrainSlope(Vector3 position, PlacementContext context)
        {
            if (context.terrain == null) return 0f;

            float sampleDistance = 1f;
            float heightCenter = GetTerrainHeight(position, context);
            float heightRight = GetTerrainHeight(position + Vector3.right * sampleDistance, context);
            float heightForward = GetTerrainHeight(position + Vector3.forward * sampleDistance, context);

            Vector3 slopeVector = new Vector3(
                heightRight - heightCenter,
                0f,
                heightForward - heightCenter
            );

            return slopeVector.magnitude / sampleDistance;
        }

        /// <summary>
        /// Validate rule configuration
        /// </summary>
        public virtual bool ValidateRule()
        {
            if (string.IsNullOrEmpty(ruleName))
            {
                Debug.LogError($"Rule {GetType().Name} has no name specified");
                return false;
            }

            if (priority < 0f)
            {
                Debug.LogError($"Rule {ruleName} has negative priority");
                return false;
            }

            return true;
        }

        protected virtual void OnValidate()
        {
            ValidateRule();
        }
    }

    /// <summary>
    /// Context object containing all data needed for placement rule evaluation
    /// </summary>
    [System.Serializable]
    public class PlacementContext
    {
        [Header("Target Object")]
        public PlacementType targetType;
        public DistrictType targetDistrict;
        public BuildingType targetBuilding;

        [Header("City Data")]
        public CityGenerationContext cityContext;
        public CityCollisionManager collisionManager;
        public Bounds? cityBounds;
        public Terrain terrain;

        [Header("Existing Objects")]
        public GameObject[] existingDistricts;
        public GameObject[] existingBuildings;
        public GameObject[] walls;
        public GameObject[] roads;

        [Header("Custom Data")]
        public System.Collections.Generic.Dictionary<string, object> customData;

        public PlacementContext(CityGenerationContext cityContext)
        {
            this.cityContext = cityContext;
            this.collisionManager = cityContext.collisionManager;
            this.customData = new System.Collections.Generic.Dictionary<string, object>();
        }

        /// <summary>
        /// Get all objects of a specific type within radius
        /// </summary>
        public GameObject[] GetObjectsInRadius(Vector3 position, float radius, ObjectType objectType)
        {
            if (collisionManager != null)
            {
                return collisionManager.GetObjectsInRadius(position, radius, objectType).ToArray();
            }
            return new GameObject[0];
        }

        /// <summary>
        /// Check if position has clear line of sight to another position
        /// </summary>
        public bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            if (collisionManager != null)
            {
                return collisionManager.HasClearPath(from, to, 1f);
            }
            return true;
        }

        /// <summary>
        /// Get distance to nearest road
        /// </summary>
        public float GetDistanceToRoad(Vector3 position)
        {
            if (collisionManager != null)
            {
                Vector3 nearestRoad = collisionManager.GetNearestRoadPoint(position);
                return Vector3.Distance(position, nearestRoad);
            }
            return float.MaxValue;
        }

        /// <summary>
        /// Check if position is accessible by road
        /// </summary>
        public bool IsAccessibleByRoad(Vector3 position, float maxDistance = 50f)
        {
            return GetDistanceToRoad(position) <= maxDistance;
        }

        /// <summary>
        /// Get or set custom data
        /// </summary>
        public T GetCustomData<T>(string key, T defaultValue = default(T))
        {
            if (customData.ContainsKey(key) && customData[key] is T)
            {
                return (T)customData[key];
            }
            return defaultValue;
        }

        public void SetCustomData(string key, object value)
        {
            customData[key] = value;
        }
    }

    /// <summary>
    /// Types of objects that can be placed
    /// </summary>
    public enum PlacementType
    {
        District,
        Building,
        Decoration,
        Landmark
    }

    // DistrictType moved to CityGenerationTypes.cs
}