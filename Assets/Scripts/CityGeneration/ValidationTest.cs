using CityGeneration.Core;
using UnityEngine;

namespace CityGeneration.Testing
{
    /// <summary>
    /// Simple validation script to test if all types compile correctly
    /// </summary>
    public class ValidationTest : MonoBehaviour
    {
        void Start()
        {
            // Test building types
            BuildingType building = BuildingType.Castle;
            ArchitecturalStyle style = ArchitecturalStyle.Medieval;
            WeatheringLevel weathering = WeatheringLevel.Medium;
            WealthLevel wealth = WealthLevel.Noble;
            FeatureCategory category = FeatureCategory.Decorative;
            ClimateType climate = ClimateType.Temperate;
            WindDirection wind = WindDirection.North;

            // Test city types
            DistrictType district = DistrictType.Market;
            ObjectType objType = ObjectType.Building;

            // Test context classes
            var buildingContext = new BuildingContext();
            //var placementContext = new PlacementContext();
            var wrapper = new Vector3Wrapper(Vector3.zero, 1f, "test");

            // Test component
            var buildingInfo = gameObject.AddComponent<EnhancedBuildingInfo>();

            Debug.Log($"All CityGeneration types compiled successfully! Building: {building}, District: {district}");
        }
    }
}