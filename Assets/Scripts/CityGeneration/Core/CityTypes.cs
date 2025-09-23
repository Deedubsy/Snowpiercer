using UnityEngine;

namespace CityGeneration.Core
{
    /// <summary>
    /// City and district-related enums and types
    /// </summary>

    public enum DistrictType
    {
        Castle,
        Market,
        Residential,
        Noble,
        Artisan,
        Religious,
        Military,
        Industrial,
        Commercial,
        Administrative
    }

    public enum ObjectType
    {
        Building,
        Wall,
        Street,
        Gate,
        Tower,
        Landmark,
        Character,
        Interactive,
        Decoration
    }

    /// <summary>
    /// Wrapper class for Vector3 to work with reference-type generic constraints
    /// </summary>
    public class Vector3Wrapper
    {
        public Vector3 position;
        public float radius;
        public string identifier;

        public Vector3Wrapper(Vector3 pos, float rad = 1f, string id = "")
        {
            position = pos;
            radius = rad;
            identifier = id;
        }

        public static implicit operator Vector3(Vector3Wrapper wrapper)
        {
            return wrapper.position;
        }

        public static implicit operator Vector3Wrapper(Vector3 vector)
        {
            return new Vector3Wrapper(vector);
        }
    }
}