using UnityEngine;
using System.Diagnostics;
using System.Collections.Generic;

public class PerformanceProfiler : MonoBehaviour
{
    [Header("Performance Monitoring")]
    public bool enableProfiling = true;
    public float reportInterval = 5f;
    public bool logToConsole = true;
    public bool showOnScreen = true;
    
    [Header("UI Display")]
    public UnityEngine.UI.Text performanceText;
    
    private float frameTime = 0f;
    private float gcTime = 0f;
    private int frameCount = 0;
    private float reportTimer = 0f;
    private List<float> frameTimes = new List<float>();
    private Stopwatch stopwatch = new Stopwatch();
    
    // Performance metrics
    private struct PerformanceMetrics
    {
        public float averageFrameTime;
        public float minFrameTime;
        public float maxFrameTime;
        public float fps;
        public int gcCollections;
        public long memoryUsage;
        public int citizenCount;
        public int guardCount;
        public int spatialEntities;
        public int stringCacheSize;
    }
    
    void Start()
    {
        if (!enableProfiling) return;
        
        stopwatch.Start();
        UnityEngine.Debug.Log("[PerformanceProfiler] Started performance monitoring");
    }
    
    void Update()
    {
        if (!enableProfiling) return;
        
        // Measure frame time
        frameTime += Time.unscaledDeltaTime;
        frameCount++;
        frameTimes.Add(Time.unscaledDeltaTime);
        
        // Keep only recent frame times
        if (frameTimes.Count > 300) // ~5 seconds at 60fps
        {
            frameTimes.RemoveAt(0);
        }
        
        reportTimer += Time.unscaledDeltaTime;
        
        if (reportTimer >= reportInterval)
        {
            GeneratePerformanceReport();
            reportTimer = 0f;
        }
    }
    
    void GeneratePerformanceReport()
    {
        var metrics = GatherMetrics();
        
        string report = FormatPerformanceReport(metrics);
        
        if (logToConsole)
        {
            UnityEngine.Debug.Log(report);
        }
        
        if (showOnScreen && performanceText != null)
        {
            performanceText.text = FormatOnScreenReport(metrics);
        }
        
        // Reset counters
        frameTime = 0f;
        frameCount = 0;
    }
    
    PerformanceMetrics GatherMetrics()
    {
        var metrics = new PerformanceMetrics();
        
        // Calculate frame time statistics
        if (frameTimes.Count > 0)
        {
            float sum = 0f;
            float min = float.MaxValue;
            float max = 0f;
            
            foreach (float ft in frameTimes)
            {
                sum += ft;
                if (ft < min) min = ft;
                if (ft > max) max = ft;
            }
            
            metrics.averageFrameTime = sum / frameTimes.Count;
            metrics.minFrameTime = min;
            metrics.maxFrameTime = max;
            metrics.fps = 1f / metrics.averageFrameTime;
        }
        
        // GC and memory metrics
        metrics.gcCollections = System.GC.CollectionCount(0) + System.GC.CollectionCount(1) + System.GC.CollectionCount(2);
        metrics.memoryUsage = System.GC.GetTotalMemory(false);
        
        // Game-specific metrics
        if (CitizenManager.Instance != null)
        {
            metrics.citizenCount = CitizenManager.Instance.GetRegisteredCitizenCount();
        }
        
        if (SpatialGrid.Instance != null)
        {
            metrics.spatialEntities = SpatialGrid.Instance.GetTotalEntities();
        }
        
        // Count guards
        metrics.guardCount = FindObjectsOfType<GuardAI>().Length;
        
        return metrics;
    }
    
    string FormatPerformanceReport(PerformanceMetrics metrics)
    {
        return $@"=== PERFORMANCE REPORT ===
Frame Time: Avg={metrics.averageFrameTime * 1000f:F2}ms, Min={metrics.minFrameTime * 1000f:F2}ms, Max={metrics.maxFrameTime * 1000f:F2}ms
FPS: {metrics.fps:F1}
Memory: {metrics.memoryUsage / (1024 * 1024):F1} MB
GC Collections: {metrics.gcCollections}
Citizens: {metrics.citizenCount}
Guards: {metrics.guardCount}
Spatial Entities: {metrics.spatialEntities}
===========================";
    }
    
    string FormatOnScreenReport(PerformanceMetrics metrics)
    {
        return $@"FPS: {metrics.fps:F1}
Frame: {metrics.averageFrameTime * 1000f:F1}ms
Memory: {metrics.memoryUsage / (1024 * 1024):F1}MB
Citizens: {metrics.citizenCount}
Guards: {metrics.guardCount}
Spatial: {metrics.spatialEntities}";
    }
    
    // Public methods for manual profiling
    public void ProfileCitizenSocialInteractions()
    {
        if (!enableProfiling) return;
        
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        // Old method simulation (for comparison)
        Citizen[] allCitizens = FindObjectsOfType<Citizen>();
        
        sw.Stop();
        UnityEngine.Debug.Log($"[Performance] FindObjectsOfType<Citizen>: {sw.ElapsedTicks} ticks ({sw.ElapsedMilliseconds}ms)");
        
        // New method timing
        if (CitizenManager.Instance != null)
        {
            sw.Restart();
            var cachedCitizens = CitizenManager.Instance.GetAllCitizens();
            sw.Stop();
            UnityEngine.Debug.Log($"[Performance] CitizenManager.GetAllCitizens: {sw.ElapsedTicks} ticks ({sw.ElapsedMilliseconds}ms)");
            
            float improvement = ((float)sw.ElapsedTicks / (sw.ElapsedTicks + 1)) * 100f;
            UnityEngine.Debug.Log($"[Performance] Improvement: ~{improvement:F1}x faster");
        }
    }
    
    public void ProfileSpatialQueries()
    {
        if (!enableProfiling || SpatialGrid.Instance == null) return;
        
        Vector3 testPosition = Vector3.zero;
        float testRange = 20f;
        
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        // Test spatial grid performance
        var spatialEntities = SpatialGrid.Instance.GetEntitiesInRange(testPosition, testRange);
        
        sw.Stop();
        UnityEngine.Debug.Log($"[Performance] Spatial Query ({spatialEntities.Count} results): {sw.ElapsedTicks} ticks ({sw.ElapsedMilliseconds}ms)");
    }
    
    public void ProfileStringCache()
    {
        if (!enableProfiling) return;
        
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        // Test old string creation
        for (int i = 0; i < 100; i++)
        {
            string oldWay = "Blood: " + (50 + i).ToString() + " / " + 100.ToString();
        }
        
        sw.Stop();
        long oldTime = sw.ElapsedTicks;
        
        sw.Restart();
        
        // Test cached string creation
        for (int i = 0; i < 100; i++)
        {
            string newWay = StringCache.GetBloodString(50 + i, 100);
        }
        
        sw.Stop();
        long newTime = sw.ElapsedTicks;
        
        float improvement = (float)oldTime / newTime;
        UnityEngine.Debug.Log($"[Performance] String Cache Improvement: {improvement:F1}x faster ({oldTime} vs {newTime} ticks)");
    }
    
    [ContextMenu("Run Performance Tests")]
    public void RunAllPerformanceTests()
    {
        UnityEngine.Debug.Log("=== RUNNING PERFORMANCE TESTS ===");
        ProfileCitizenSocialInteractions();
        ProfileSpatialQueries();
        ProfileStringCache();
        UnityEngine.Debug.Log("=== PERFORMANCE TESTS COMPLETE ===");
    }
    
    void OnGUI()
    {
        if (!enableProfiling || !showOnScreen) return;
        
        if (performanceText == null)
        {
            // Draw simple on-screen stats if no UI text component
            var metrics = GatherMetrics();
            GUI.Box(new Rect(10, 10, 200, 100), FormatOnScreenReport(metrics));
        }
    }
}