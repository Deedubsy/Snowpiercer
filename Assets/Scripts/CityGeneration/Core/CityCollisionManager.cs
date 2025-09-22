using UnityEngine;
using System.Collections.Generic;

namespace CityGeneration.Core
{
    /// <summary>
    /// High-performance collision manager for city generation
    /// Uses spatial grids to replace expensive collision detection
    /// </summary>
    public class CityCollisionManager
    {
        private SpatialGrid<GameObject> streetGrid;
        private SpatialGrid<GameObject> buildingGrid;
        private SpatialGrid<GameObject> wallGrid;
        private SpatialGrid<Vector3> roadNetworkGrid;

        private Dictionary<ObjectType, SpatialGrid<GameObject>> gridsByType;

        public CityCollisionManager()
        {
            gridsByType = new Dictionary<ObjectType, SpatialGrid<GameObject>>();
        }

        /// <summary>
        /// Initialize the collision manager with city bounds
        /// </summary>
        public void Initialize(float citySize)
        {
            float cellSize = Mathf.Max(5f, citySize / 20f); // Adaptive cell size
            var bounds = new Bounds(Vector3.zero, Vector3.one * citySize);

            streetGrid = new SpatialGrid<GameObject>(cellSize, bounds);
            buildingGrid = new SpatialGrid<GameObject>(cellSize, bounds);
            wallGrid = new SpatialGrid<GameObject>(cellSize, bounds);
            roadNetworkGrid = new SpatialGrid<Vector3>(cellSize, bounds);

            gridsByType[ObjectType.Street] = streetGrid;
            gridsByType[ObjectType.Building] = buildingGrid;
            gridsByType[ObjectType.Wall] = wallGrid;

            Debug.Log($"CityCollisionManager initialized with cell size: {cellSize}, bounds: {bounds}");
        }

        /// <summary>
        /// Register a static object (buildings, walls)
        /// </summary>
        public void RegisterStaticObject(GameObject obj, ObjectType type, float radius = 0f)
        {
            if (obj == null) return;

            float objectRadius = radius > 0f ? radius : CalculateObjectRadius(obj);
            var grid = GetGridForType(type);
            grid?.AddObject(obj, obj.transform.position, objectRadius);
        }

        /// <summary>
        /// Register a dynamic object that may move
        /// </summary>
        public void RegisterDynamicObject(GameObject obj, ObjectType type, float radius = 0f)
        {
            RegisterStaticObject(obj, type, radius);
            // TODO: Add dynamic object tracking for moving objects
        }

        /// <summary>
        /// Register a road network point
        /// </summary>
        public void RegisterRoadPoint(Vector3 position, float width = 4f)
        {
            roadNetworkGrid.AddObject(position, position, width * 0.5f);
        }

        /// <summary>
        /// Check if a position is valid for placement
        /// </summary>
        public bool IsPositionValid(Vector3 position, float radius, ObjectType layer, ObjectType[] excludeTypes = null)
        {
            var excludeSet = excludeTypes != null ? new HashSet<ObjectType>(excludeTypes) : new HashSet<ObjectType>();

            foreach (var kvp in gridsByType)
            {
                if (kvp.Key == layer || excludeSet.Contains(kvp.Key))
                    continue;

                var grid = kvp.Value;
                if (!grid.IsPositionValid(position, radius))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Find the nearest valid position around a desired location
        /// </summary>
        public Vector3 FindNearestValidPosition(Vector3 desired, float radius, ObjectType layer, int maxAttempts = 20, ObjectType[] excludeTypes = null)
        {
            if (IsPositionValid(desired, radius, layer, excludeTypes))
            {
                return desired;
            }

            var excludeSet = excludeTypes != null ? new HashSet<ObjectType>(excludeTypes) : new HashSet<ObjectType>();

            // Use the most restrictive grid for position finding
            var primaryGrid = GetGridForType(GetMostRestrictiveType(excludeSet));
            if (primaryGrid != null)
            {
                Vector3 candidate = primaryGrid.FindNearestValidPosition(desired, radius, maxAttempts);

                // Validate against all other grids
                if (IsPositionValid(candidate, radius, layer, excludeTypes))
                {
                    return candidate;
                }
            }

            return desired; // Fallback to original position
        }

        /// <summary>
        /// Check if there's a clear path between two points
        /// </summary>
        public bool HasClearPath(Vector3 start, Vector3 end, float pathWidth = 2f)
        {
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);
            int samples = Mathf.Max(3, Mathf.CeilToInt(distance / pathWidth));

            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector3 samplePoint = Vector3.Lerp(start, end, t);

                // Check against buildings and walls
                if (!IsPositionValid(samplePoint, pathWidth * 0.5f, ObjectType.Street, new[] { ObjectType.Street }))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get all objects of a type within radius
        /// </summary>
        public List<GameObject> GetObjectsInRadius(Vector3 position, float radius, ObjectType type)
        {
            var grid = GetGridForType(type);
            if (grid == null) return new List<GameObject>();

            var spatialObjects = grid.GetObjectsInRadius(position, radius);
            var result = new List<GameObject>();

            foreach (var spatialObj in spatialObjects)
            {
                result.Add(spatialObj.obj);
            }

            return result;
        }

        /// <summary>
        /// Get the nearest object of a specific type
        /// </summary>
        public GameObject GetNearestObject(Vector3 position, ObjectType type, System.Func<GameObject, bool> filter = null)
        {
            var grid = GetGridForType(type);
            if (grid == null) return null;

            var spatialObj = grid.GetNearestObject(position, filter);
            return spatialObj?.obj;
        }

        /// <summary>
        /// Get the nearest road point
        /// </summary>
        public Vector3 GetNearestRoadPoint(Vector3 position)
        {
            var spatialObj = roadNetworkGrid.GetNearestObject(position);
            return spatialObj?.obj ?? position;
        }

        /// <summary>
        /// Check if position is on or near a road
        /// </summary>
        public bool IsPositionOnRoad(Vector3 position, float tolerance = 2f)
        {
            var nearbyRoads = roadNetworkGrid.GetObjectsInRadius(position, tolerance);
            return nearbyRoads.Count > 0;
        }

        /// <summary>
        /// Remove an object from collision tracking
        /// </summary>
        public void UnregisterObject(GameObject obj, ObjectType type)
        {
            var grid = GetGridForType(type);
            grid?.RemoveObject(obj);
        }

        /// <summary>
        /// Clear all collision data
        /// </summary>
        public void Clear()
        {
            foreach (var grid in gridsByType.Values)
            {
                grid.Clear();
            }
            roadNetworkGrid.Clear();
        }

        /// <summary>
        /// Get performance statistics
        /// </summary>
        public string GetPerformanceStats()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== City Collision Manager Stats ===");

            foreach (var kvp in gridsByType)
            {
                var gridStats = kvp.Value.GetStats();
                stats.AppendLine($"{kvp.Key}: {gridStats}");
            }

            var roadStats = roadNetworkGrid.GetStats();
            stats.AppendLine($"RoadNetwork: {roadStats}");

            return stats.ToString();
        }

        private SpatialGrid<GameObject> GetGridForType(ObjectType type)
        {
            return gridsByType.ContainsKey(type) ? gridsByType[type] : null;
        }

        private float CalculateObjectRadius(GameObject obj)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds.size.magnitude * 0.5f;
            }

            var collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                return collider.bounds.size.magnitude * 0.5f;
            }

            // Default radius based on scale
            return obj.transform.localScale.magnitude * 0.5f;
        }

        private ObjectType GetMostRestrictiveType(HashSet<ObjectType> excludeTypes)
        {
            // Return the type that's most likely to have collision conflicts
            if (!excludeTypes.Contains(ObjectType.Building)) return ObjectType.Building;
            if (!excludeTypes.Contains(ObjectType.Wall)) return ObjectType.Wall;
            return ObjectType.Street;
        }
    }

    /// <summary>
    /// Types of objects in the city for collision management
    /// </summary>
    public enum ObjectType
    {
        Street,
        Building,
        Wall,
        Gate,
        Tower,
        Decoration
    }
}