using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace CityGeneration.Core
{
    /// <summary>
    /// Base class for all generation results
    /// </summary>
    [System.Serializable]
    public abstract class GenerationResult
    {
        public bool isSuccessful = true;
        public string errorMessage = "";
        public float generationTime = 0f;
        public int objectsGenerated = 0;

        public virtual void MarkAsError(string error)
        {
            isSuccessful = false;
            errorMessage = error;
        }

        public virtual bool IsValid()
        {
            return isSuccessful && objectsGenerated > 0;
        }
    }

    /// <summary>
    /// Result from wall generation
    /// </summary>
    [System.Serializable]
    public class WallGenerationResult : GenerationResult
    {
        public List<GameObject> wallSegments = new List<GameObject>();
        public List<GameObject> gates = new List<GameObject>();
        public List<GameObject> towers = new List<GameObject>();
        public List<GameObject> fortifications = new List<GameObject>();

        public override bool IsValid()
        {
            return base.IsValid() && wallSegments.Count > 0;
        }
    }

    /// <summary>
    /// Result from building generation
    /// </summary>
    [System.Serializable]
    public class BuildingGenerationResult : GenerationResult
    {
        public List<GameObject> buildings = new List<GameObject>();
        public Dictionary<string, List<GameObject>> buildingsByDistrict = new Dictionary<string, List<GameObject>>();

        public override bool IsValid()
        {
            return base.IsValid() && buildings.Count > 0;
        }
    }

    /// <summary>
    /// Result from street generation
    /// </summary>
    [System.Serializable]
    public class StreetGenerationResult : GenerationResult
    {
        public List<GameObject> mainRoads = new List<GameObject>();
        public List<GameObject> secondaryStreets = new List<GameObject>();
        public List<GameObject> intersections = new List<GameObject>();

        public override bool IsValid()
        {
            return base.IsValid() && (mainRoads.Count > 0 || secondaryStreets.Count > 0);
        }
    }

    /// <summary>
    /// Result from terrain generation
    /// </summary>
    [System.Serializable]
    public class TerrainGenerationResult : GenerationResult
    {
        public GameObject terrain;
        public Bounds terrainBounds;

        public override bool IsValid()
        {
            return base.IsValid() && terrain != null;
        }
    }

    /// <summary>
    /// Result from NavMesh generation
    /// </summary>
    [System.Serializable]
    public class NavMeshGenerationResult : GenerationResult
    {
        public NavMeshData navMeshData;
        public List<NavMeshAgent> configuredAgents = new List<NavMeshAgent>();
        public List<OffMeshLink> offMeshLinks = new List<OffMeshLink>();
        public int navigationAreas = 0;
        public float totalNavigableArea = 0f;
        public bool connectivityTestPassed = false;

        public override bool IsValid()
        {
            return base.IsValid() && navMeshData != null && totalNavigableArea > 0f;
        }
    }

    /// <summary>
    /// Combined city layout containing all generation results
    /// </summary>
    [System.Serializable]
    public class CityLayout
    {
        public TerrainGenerationResult terrain;
        public WallGenerationResult walls;
        public StreetGenerationResult streets;
        public BuildingGenerationResult buildings;
        public NavMeshGenerationResult navMesh;

        public bool IsComplete()
        {
            return terrain?.IsValid() == true &&
                   walls?.IsValid() == true &&
                   streets?.IsValid() == true &&
                   buildings?.IsValid() == true &&
                   navMesh?.IsValid() == true;
        }

        public int GetTotalObjectCount()
        {
            int count = 0;
            count += terrain?.objectsGenerated ?? 0;
            count += walls?.objectsGenerated ?? 0;
            count += streets?.objectsGenerated ?? 0;
            count += buildings?.objectsGenerated ?? 0;
            count += navMesh?.objectsGenerated ?? 0;
            return count;
        }
    }
}