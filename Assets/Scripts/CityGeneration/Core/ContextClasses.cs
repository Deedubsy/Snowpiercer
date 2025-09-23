using UnityEngine;
using System.Collections.Generic;

namespace CityGeneration.Core
{
    /// <summary>
    /// Context classes for city generation
    /// </summary>

    /// <summary>
    /// Comprehensive context information for building generation
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

        [Header("City Context")]
        public CityGenerationContext cityContext;
        public Vector3 districtCenter;
        public float districtRadius;

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
            this.cityContext = cityContext;
            if (cityContext != null)
            {
                // Could populate from city context data
            }
        }

        public BuildingContext(DistrictType district = DistrictType.Residential,
                              WealthLevel wealth = WealthLevel.Common,
                              ArchitecturalStyle style = ArchitecturalStyle.Medieval)
        {
            districtType = district;
            wealthLevel = wealth;
            architecturalStyle = style;
            availableSpace = new Vector3(20f, 15f, 20f);
            districtWealth = 0.5f;
        }
    }

    // PlacementContext moved to PlacementRule.cs (more comprehensive version)

    /// <summary>
    /// Enhanced building information component for generated buildings
    /// </summary>
    public class EnhancedBuildingInfo : MonoBehaviour
    {
        [Header("Building Properties")]
        public CityGeneration.Buildings.BuildingTemplate buildingTemplate;
        public BuildingType buildingType;
        public ArchitecturalStyle architecturalStyle;

        [Header("Navigation")]
        public bool hasInterior = false;
        public bool allowsHiding = true;
        public bool isLandmark = false;

        [Header("Context")]
        public DistrictType districtType;
        public float wealthLevel;
        public WeatheringLevel weatheringLevel = WeatheringLevel.Medium;

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