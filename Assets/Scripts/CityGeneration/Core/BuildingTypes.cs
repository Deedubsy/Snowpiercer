using UnityEngine;

namespace CityGeneration.Core
{
    /// <summary>
    /// Building-related enums and types
    /// </summary>

    public enum BuildingType
    {
        Castle,
        Cathedral,
        House,
        Shop,
        Tavern,
        Barracks,
        Workshop,
        Temple,
        Market,
        Forge,
        Library,
        Stable,
        Gatehouse,
        Tower
    }

    public enum ArchitecturalStyle
    {
        Medieval,
        Gothic,
        Romanesque,
        Norman,
        Tudor,
        Fortress,
        Ecclesiastical
    }

    public enum WeatheringLevel
    {
        Pristine,
        New,
        Light,
        Medium,
        Heavy,
        Ruined
    }

    public enum WealthLevel
    {
        Poor,
        Common,
        Comfortable,
        Wealthy,
        Merchant,
        Rich,
        Noble,
        Royal
    }

    public enum FeatureCategory
    {
        Decorative,
        Functional,
        Defensive,
        Commercial,
        Religious,
        Gameplay
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