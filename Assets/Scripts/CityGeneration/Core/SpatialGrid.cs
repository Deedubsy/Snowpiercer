using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CityGeneration.Core
{
    /// <summary>
    /// High-performance spatial partitioning system for collision detection
    /// Replaces expensive O(n) collision checks with O(1) average case lookups
    /// </summary>
    public class SpatialGrid<T> where T : class
    {
        private Dictionary<Vector2Int, List<SpatialObject<T>>> grid;
        private float cellSize;
        private Bounds bounds;

        public SpatialGrid(float cellSize = 10f, Bounds? bounds = null)
        {
            this.cellSize = cellSize;
            this.bounds = bounds ?? new Bounds(Vector3.zero, Vector3.one * 1000f);
            this.grid = new Dictionary<Vector2Int, List<SpatialObject<T>>>();
        }

        /// <summary>
        /// Add an object to the spatial grid
        /// </summary>
        public void AddObject(T obj, Vector3 position, float radius = 1f)
        {
            var spatialObj = new SpatialObject<T>
            {
                obj = obj,
                position = position,
                radius = radius,
                bounds = new Bounds(position, Vector3.one * radius * 2f)
            };

            var cells = GetCellsForObject(spatialObj);
            foreach (var cell in cells)
            {
                if (!grid.ContainsKey(cell))
                {
                    grid[cell] = new List<SpatialObject<T>>();
                }
                grid[cell].Add(spatialObj);
            }
        }

        /// <summary>
        /// Remove an object from the spatial grid
        /// </summary>
        public void RemoveObject(T obj)
        {
            var cellsToClean = new List<Vector2Int>();

            foreach (var kvp in grid)
            {
                kvp.Value.RemoveAll(spatialObj => spatialObj.obj.Equals(obj));
                if (kvp.Value.Count == 0)
                {
                    cellsToClean.Add(kvp.Key);
                }
            }

            foreach (var cell in cellsToClean)
            {
                grid.Remove(cell);
            }
        }

        /// <summary>
        /// Get all objects within radius of a position
        /// </summary>
        public List<SpatialObject<T>> GetObjectsInRadius(Vector3 position, float radius)
        {
            var result = new List<SpatialObject<T>>();
            var cells = GetCellsInRadius(position, radius);

            foreach (var cell in cells)
            {
                if (grid.ContainsKey(cell))
                {
                    foreach (var spatialObj in grid[cell])
                    {
                        float distance = Vector3.Distance(spatialObj.position, position);
                        if (distance <= (spatialObj.radius + radius))
                        {
                            result.Add(spatialObj);
                        }
                    }
                }
            }

            return result.Distinct().ToList();
        }

        /// <summary>
        /// Check if a position is valid (no collisions within radius)
        /// </summary>
        public bool IsPositionValid(Vector3 position, float radius, System.Func<T, bool> filter = null)
        {
            var nearbyObjects = GetObjectsInRadius(position, radius);

            foreach (var spatialObj in nearbyObjects)
            {
                if (filter != null && !filter(spatialObj.obj))
                    continue;

                float distance = Vector3.Distance(spatialObj.position, position);
                if (distance < (spatialObj.radius + radius))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Find the nearest valid position around a desired location
        /// </summary>
        public Vector3 FindNearestValidPosition(Vector3 desired, float radius, int maxAttempts = 20, System.Func<T, bool> filter = null)
        {
            if (IsPositionValid(desired, radius, filter))
                return desired;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                float searchRadius = radius * attempt * 0.5f;
                int samples = Mathf.Max(8, attempt * 4);

                for (int i = 0; i < samples; i++)
                {
                    float angle = (i / (float)samples) * 360f * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle) * searchRadius,
                        0f,
                        Mathf.Sin(angle) * searchRadius
                    );

                    Vector3 candidate = desired + offset;

                    if (bounds.Contains(candidate) && IsPositionValid(candidate, radius, filter))
                    {
                        return candidate;
                    }
                }
            }

            return desired; // Return original if no valid position found
        }

        /// <summary>
        /// Get all objects of a specific type within radius
        /// </summary>
        public List<SpatialObject<T>> GetObjectsOfType<TSpecific>(Vector3 position, float radius) where TSpecific : class, T
        {
            return GetObjectsInRadius(position, radius)
                .Where(obj => obj.obj is TSpecific)
                .ToList();
        }

        /// <summary>
        /// Get nearest object to a position
        /// </summary>
        public SpatialObject<T> GetNearestObject(Vector3 position, System.Func<T, bool> filter = null)
        {
            float searchRadius = cellSize;
            SpatialObject<T> nearest = null;
            float nearestDistance = float.MaxValue;

            // Expand search radius until we find something
            for (int i = 0; i < 10; i++) // Max 10 iterations
            {
                var objects = GetObjectsInRadius(position, searchRadius);

                foreach (var obj in objects)
                {
                    if (filter != null && !filter(obj.obj))
                        continue;

                    float distance = Vector3.Distance(obj.position, position);
                    if (distance < nearestDistance)
                    {
                        nearest = obj;
                        nearestDistance = distance;
                    }
                }

                if (nearest != null)
                    break;

                searchRadius *= 2f;
            }

            return nearest;
        }

        /// <summary>
        /// Clear all objects from the grid
        /// </summary>
        public void Clear()
        {
            grid.Clear();
        }

        /// <summary>
        /// Get statistics about the spatial grid
        /// </summary>
        public SpatialGridStats GetStats()
        {
            var stats = new SpatialGridStats
            {
                totalCells = grid.Count,
                totalObjects = grid.Values.Sum(list => list.Count),
                averageObjectsPerCell = grid.Count > 0 ? grid.Values.Average(list => list.Count) : 0f,
                maxObjectsInCell = grid.Count > 0 ? grid.Values.Max(list => list.Count) : 0,
                cellSize = cellSize
            };

            return stats;
        }

        private Vector2Int GetCell(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / cellSize),
                Mathf.FloorToInt(position.z / cellSize)
            );
        }

        private List<Vector2Int> GetCellsForObject(SpatialObject<T> obj)
        {
            var cells = new List<Vector2Int>();
            var bounds = obj.bounds;

            Vector2Int minCell = GetCell(bounds.min);
            Vector2Int maxCell = GetCell(bounds.max);

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int z = minCell.y; z <= maxCell.y; z++)
                {
                    cells.Add(new Vector2Int(x, z));
                }
            }

            return cells;
        }

        private List<Vector2Int> GetCellsInRadius(Vector3 position, float radius)
        {
            var cells = new List<Vector2Int>();
            var center = GetCell(position);

            int cellRadius = Mathf.CeilToInt(radius / cellSize);

            for (int x = center.x - cellRadius; x <= center.x + cellRadius; x++)
            {
                for (int z = center.y - cellRadius; z <= center.y + cellRadius; z++)
                {
                    cells.Add(new Vector2Int(x, z));
                }
            }

            return cells;
        }
    }

    /// <summary>
    /// Wrapper for objects stored in the spatial grid
    /// </summary>
    [System.Serializable]
    public class SpatialObject<T>
    {
        public T obj;
        public Vector3 position;
        public float radius;
        public Bounds bounds;
    }

    /// <summary>
    /// Statistics about spatial grid performance
    /// </summary>
    [System.Serializable]
    public class SpatialGridStats
    {
        public int totalCells;
        public int totalObjects;
        public float averageObjectsPerCell;
        public int maxObjectsInCell;
        public float cellSize;

        public override string ToString()
        {
            return $"SpatialGrid Stats - Cells: {totalCells}, Objects: {totalObjects}, " +
                   $"Avg/Cell: {averageObjectsPerCell:F1}, Max/Cell: {maxObjectsInCell}, CellSize: {cellSize}";
        }
    }
}