using UnityEngine;
using System.Collections.Generic;

namespace CityGeneration.Core
{
    /// <summary>
    /// Context object that carries all shared data between generation modules
    /// </summary>
    [System.Serializable]
    public class CityGenerationContext
    {
        [Header("Configuration")]
        public CityConfiguration config;

        [Header("Shared Systems")]
        public CityCollisionManager collisionManager;
        public Transform cityParent;

        [Header("Generation State")]
        public CityLayout cityLayout;
        public Dictionary<string, object> sharedData;

        [Header("Progress Tracking")]
        public float overallProgress;
        public string currentPhase;

        public CityGenerationContext(CityConfiguration config)
        {
            this.config = config;
            this.sharedData = new Dictionary<string, object>();
            this.cityLayout = new CityLayout();
            this.overallProgress = 0f;
            this.currentPhase = "Initializing";

            // Initialize collision manager
            this.collisionManager = new CityCollisionManager();
            this.collisionManager.Initialize(config.GetCitySize());

            // Create city parent object
            CreateCityParent();
        }

        private void CreateCityParent()
        {
            if (cityParent == null)
            {
                var parentObject = new GameObject("Generated_Medieval_City");
                cityParent = parentObject.transform;
                cityParent.position = Vector3.zero;
            }
        }

        /// <summary>
        /// Store shared data that can be accessed by other generators
        /// </summary>
        public void SetSharedData(string key, object value)
        {
            sharedData[key] = value;
        }

        /// <summary>
        /// Retrieve shared data
        /// </summary>
        public T GetSharedData<T>(string key, T defaultValue = default(T))
        {
            if (sharedData.ContainsKey(key) && sharedData[key] is T)
            {
                return (T)sharedData[key];
            }
            return defaultValue;
        }

        /// <summary>
        /// Update the overall generation progress
        /// </summary>
        public void UpdateProgress(float progress, string phase)
        {
            overallProgress = Mathf.Clamp01(progress);
            currentPhase = phase;
        }
    }

    /// <summary>
    /// Configuration object for city generation
    /// Temporary implementation - will be expanded into ScriptableObject
    /// </summary>
    [System.Serializable]
    public class CityConfiguration
    {
        [Header("City Layout")]
        public WallShape wallShape = WallShape.Square;
        public float cityRadius = 50f;
        public Vector2 squareWallSize = new Vector2(100f, 80f);

        [Header("Wall Properties")]
        public float wallThickness = 2f;
        public float wallHeight = 8f;
        public float gateWidth = 6f;

        [Header("Building Settings")]
        [Range(0.3f, 1.0f)] public float buildingDensity = 0.7f;
        [Range(1, 5)] public int maxBuildingsPerDistrict = 3;
        [Range(2f, 8f)] public float minBuildingHeight = 3f;
        [Range(4f, 15f)] public float maxBuildingHeight = 8f;

        [Header("Street Layout")]
        public float streetWidth = 4f;
        public float mainRoadWidth = 6f;

        [Header("Districts")]
        public bool includeCastle = true;
        public bool includeCathedral = true;
        public bool includeMarketSquare = true;
        public bool includeNobleQuarter = true;
        public bool includeResidential = true;

        [Header("Performance")]
        public int maxTotalObjects = 200;
        public bool combineWallMeshes = true;
        public bool combineBuildingMeshes = false;

        public float GetCitySize()
        {
            return wallShape == WallShape.Circular ? cityRadius * 2f : Mathf.Max(squareWallSize.x, squareWallSize.y);
        }
    }

    public enum WallShape
    {
        Circular,
        Square
    }
}