using System.Collections.Generic;
using UnityEngine;

public interface ISpatialEntity
{
    Vector3 Position { get; }
    Transform Transform { get; }
}

public class SpatialGrid : MonoBehaviour
{
    public static SpatialGrid Instance { get; private set; }
    
    [Header("Grid Settings")]
    public float cellSize = 10f;
    public int gridWidth = 100;
    public int gridHeight = 100;
    public Vector3 gridOrigin = Vector3.zero;
    
    [Header("Debug")]
    public bool showGrid = false;
    public bool showEntities = false;
    
    private Dictionary<Vector2Int, HashSet<ISpatialEntity>> grid;
    private Dictionary<ISpatialEntity, Vector2Int> entityToCell;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGrid();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeGrid()
    {
        grid = new Dictionary<Vector2Int, HashSet<ISpatialEntity>>();
        entityToCell = new Dictionary<ISpatialEntity, Vector2Int>();
        
        Debug.Log($"[SpatialGrid] Initialized {gridWidth}x{gridHeight} grid with cell size {cellSize}");
    }
    
    public void RegisterEntity(ISpatialEntity entity)
    {
        if (entity == null) return;
        
        Vector2Int cellCoord = WorldToGrid(entity.Position);
        
        // Add to new cell
        if (!grid.ContainsKey(cellCoord))
        {
            grid[cellCoord] = new HashSet<ISpatialEntity>();
        }
        
        grid[cellCoord].Add(entity);
        entityToCell[entity] = cellCoord;
    }
    
    public void UnregisterEntity(ISpatialEntity entity)
    {
        if (entity == null || !entityToCell.ContainsKey(entity)) return;
        
        Vector2Int cellCoord = entityToCell[entity];
        
        if (grid.ContainsKey(cellCoord))
        {
            grid[cellCoord].Remove(entity);
            
            // Clean up empty cells
            if (grid[cellCoord].Count == 0)
            {
                grid.Remove(cellCoord);
            }
        }
        
        entityToCell.Remove(entity);
    }
    
    public void UpdateEntity(ISpatialEntity entity)
    {
        if (entity == null || !entityToCell.ContainsKey(entity)) return;
        
        Vector2Int newCellCoord = WorldToGrid(entity.Position);
        Vector2Int oldCellCoord = entityToCell[entity];
        
        // If entity moved to a different cell, update it
        if (newCellCoord != oldCellCoord)
        {
            // Remove from old cell
            if (grid.ContainsKey(oldCellCoord))
            {
                grid[oldCellCoord].Remove(entity);
                if (grid[oldCellCoord].Count == 0)
                {
                    grid.Remove(oldCellCoord);
                }
            }
            
            // Add to new cell
            if (!grid.ContainsKey(newCellCoord))
            {
                grid[newCellCoord] = new HashSet<ISpatialEntity>();
            }
            
            grid[newCellCoord].Add(entity);
            entityToCell[entity] = newCellCoord;
        }
    }
    
    public List<ISpatialEntity> GetEntitiesInRange(Vector3 center, float radius)
    {
        List<ISpatialEntity> result = new List<ISpatialEntity>();
        
        // Calculate grid bounds for the search area
        Vector2Int minCell = WorldToGrid(center - Vector3.one * radius);
        Vector2Int maxCell = WorldToGrid(center + Vector3.one * radius);
        
        float radiusSquared = radius * radius;
        
        // Check all cells within bounds
        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                Vector2Int cellCoord = new Vector2Int(x, y);
                
                if (grid.ContainsKey(cellCoord))
                {
                    foreach (var entity in grid[cellCoord])
                    {
                        if (entity == null) continue;
                        
                        float distanceSquared = (entity.Position - center).sqrMagnitude;
                        if (distanceSquared <= radiusSquared)
                        {
                            result.Add(entity);
                        }
                    }
                }
            }
        }
        
        return result;
    }
    
    public List<T> GetEntitiesInRange<T>(Vector3 center, float radius) where T : class, ISpatialEntity
    {
        List<T> result = new List<T>();
        
        Vector2Int minCell = WorldToGrid(center - Vector3.one * radius);
        Vector2Int maxCell = WorldToGrid(center + Vector3.one * radius);
        
        float radiusSquared = radius * radius;
        
        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                Vector2Int cellCoord = new Vector2Int(x, y);
                
                if (grid.ContainsKey(cellCoord))
                {
                    foreach (var entity in grid[cellCoord])
                    {
                        if (entity is T typedEntity && entity != null)
                        {
                            float distanceSquared = (entity.Position - center).sqrMagnitude;
                            if (distanceSquared <= radiusSquared)
                            {
                                result.Add(typedEntity);
                            }
                        }
                    }
                }
            }
        }
        
        return result;
    }
    
    public ISpatialEntity GetNearestEntity(Vector3 center, float maxRange = float.MaxValue)
    {
        ISpatialEntity nearest = null;
        float nearestDistanceSquared = maxRange * maxRange;
        
        // Start with immediate cell and expand outward
        Vector2Int centerCell = WorldToGrid(center);
        int searchRadius = Mathf.CeilToInt(maxRange / cellSize);
        
        for (int radius = 0; radius <= searchRadius; radius++)
        {
            for (int x = centerCell.x - radius; x <= centerCell.x + radius; x++)
            {
                for (int y = centerCell.y - radius; y <= centerCell.y + radius; y++)
                {
                    // Only check border cells for this radius (optimization)
                    if (radius > 0 && x > centerCell.x - radius && x < centerCell.x + radius && 
                        y > centerCell.y - radius && y < centerCell.y + radius)
                        continue;
                    
                    Vector2Int cellCoord = new Vector2Int(x, y);
                    
                    if (grid.ContainsKey(cellCoord))
                    {
                        foreach (var entity in grid[cellCoord])
                        {
                            if (entity == null) continue;
                            
                            float distanceSquared = (entity.Position - center).sqrMagnitude;
                            if (distanceSquared < nearestDistanceSquared)
                            {
                                nearest = entity;
                                nearestDistanceSquared = distanceSquared;
                            }
                        }
                    }
                }
            }
            
            // Early exit if we found something close
            if (nearest != null && nearestDistanceSquared < (radius * cellSize) * (radius * cellSize))
            {
                break;
            }
        }
        
        return nearest;
    }
    
    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 relativePos = worldPos - gridOrigin;
        return new Vector2Int(
            Mathf.FloorToInt(relativePos.x / cellSize),
            Mathf.FloorToInt(relativePos.z / cellSize)
        );
    }
    
    private Vector3 GridToWorld(Vector2Int gridCoord)
    {
        return gridOrigin + new Vector3(
            gridCoord.x * cellSize + cellSize * 0.5f,
            0,
            gridCoord.y * cellSize + cellSize * 0.5f
        );
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGrid && !showEntities) return;
        
        if (showGrid)
        {
            Gizmos.color = Color.white;
            
            // Draw a subset of grid lines for performance
            int step = Mathf.Max(1, gridWidth / 20);
            
            for (int x = 0; x < gridWidth; x += step)
            {
                Vector3 start = GridToWorld(new Vector2Int(x, 0));
                Vector3 end = GridToWorld(new Vector2Int(x, gridHeight));
                start.y = 0;
                end.y = 0;
                Gizmos.DrawLine(start, end);
            }
            
            for (int y = 0; y < gridHeight; y += step)
            {
                Vector3 start = GridToWorld(new Vector2Int(0, y));
                Vector3 end = GridToWorld(new Vector2Int(gridWidth, y));
                start.y = 0;
                end.y = 0;
                Gizmos.DrawLine(start, end);
            }
        }
        
        if (showEntities && grid != null)
        {
            Gizmos.color = Color.green;
            foreach (var kvp in grid)
            {
                Vector3 cellCenter = GridToWorld(kvp.Key);
                Gizmos.DrawWireCube(cellCenter, Vector3.one * cellSize * 0.9f);
                
                Gizmos.color = Color.red;
                foreach (var entity in kvp.Value)
                {
                    if (entity != null)
                    {
                        Gizmos.DrawWireSphere(entity.Position, 0.5f);
                    }
                }
                Gizmos.color = Color.green;
            }
        }
    }
    
    // Performance and debug methods
    public int GetTotalEntities()
    {
        int total = 0;
        foreach (var cell in grid.Values)
        {
            total += cell.Count;
        }
        return total;
    }
    
    public int GetActiveCells()
    {
        return grid.Count;
    }
    
    public void LogGridStats()
    {
        Debug.Log($"[SpatialGrid] Stats:");
        Debug.Log($"  - Total Entities: {GetTotalEntities()}");
        Debug.Log($"  - Active Cells: {GetActiveCells()}");
        Debug.Log($"  - Cell Size: {cellSize}");
        Debug.Log($"  - Grid Size: {gridWidth}x{gridHeight}");
    }
}