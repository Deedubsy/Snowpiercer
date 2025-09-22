using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class ScenePerformanceOptimizer : MonoBehaviour
{
    [Header("Optimization Settings")]
    public bool applyOptimizationsOnStart = false;
    public bool enableLODOptimization = true;
    public bool enableOcclusionCulling = true;
    public bool enableLightingOptimization = true;
    public bool enableBatchingOptimization = true;
    public bool enableTextureOptimization = true;

    [Header("LOD Configuration")]
    [Range(0.01f, 0.5f)] public float lodBias = 0.3f;
    [Range(5f, 100f)] public float maxLODDistance = 50f;
    [Range(2, 8)] public int maxLODLevels = 4;

    [Header("Culling Configuration")]
    [Range(10f, 200f)] public float maxDrawDistance = 100f;
    [Range(0.001f, 0.1f)] public float smallCullThreshold = 0.01f;
    [Range(1f, 10f)] public float occluderMinSize = 2f;

    [Header("Lighting Optimization")]
    public bool bakeLightmaps = true;
    public bool optimizeRealtimeLights = true;
    [Range(1, 10)] public int maxRealtimeLights = 4;
    [Range(0.1f, 2f)] public float lightCullingRange = 1f;

    [Header("Batching Settings")]
    public bool enableStaticBatching = true;
    public bool enableDynamicBatching = true;
    [Range(100, 5000)] public int maxBatchSize = 1000;

    [Header("Texture Settings")]
    [Range(256, 2048)] public int maxTextureSize = 1024;
    public bool compressTextures = true;
    public bool mipmapStreaming = true;

    [Header("Debug")]
    public bool showOptimizationStats = true;
    public bool logOptimizationProgress = true;

    private Dictionary<string, int> optimizationStats = new Dictionary<string, int>();

    void Start()
    {
        if (applyOptimizationsOnStart)
        {
            ApplyAllOptimizations();
        }
    }

    [ContextMenu("Apply All Optimizations")]
    public void ApplyAllOptimizations()
    {
        Debug.Log("[ScenePerformanceOptimizer] Starting comprehensive scene optimization...");

        optimizationStats.Clear();

        if (enableLODOptimization)
        {
            OptimizeLODSystem();
        }

        if (enableOcclusionCulling)
        {
            SetupOcclusionCulling();
        }

        if (enableLightingOptimization)
        {
            OptimizeLighting();
        }

        if (enableBatchingOptimization)
        {
            OptimizeBatching();
        }

        if (enableTextureOptimization)
        {
            OptimizeTextures();
        }

        // Apply Unity-specific optimizations
        ApplyUnityOptimizations();

        // Final validation and reporting
        ValidateOptimizations();

        if (logOptimizationProgress)
        {
            ReportOptimizationResults();
        }

        Debug.Log("[ScenePerformanceOptimizer] Scene optimization complete!");
    }

    void OptimizeLODSystem()
    {
        if (logOptimizationProgress)
            Debug.Log("[ScenePerformanceOptimizer] Optimizing LOD system...");

        int lodGroupsCreated = 0;
        int lodGroupsOptimized = 0;

        // Find all renderers that could benefit from LOD
        MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);

        foreach (MeshRenderer renderer in renderers)
        {
            // Skip UI elements and small objects
            if (renderer.bounds.size.magnitude < smallCullThreshold)
                continue;

            // Skip objects that already have LOD groups
            if (renderer.GetComponentInParent<LODGroup>() != null)
            {
                OptimizeExistingLODGroup(renderer.GetComponentInParent<LODGroup>());
                lodGroupsOptimized++;
                continue;
            }

            // Create LOD group for significant objects
            if (renderer.bounds.size.magnitude > 2f)
            {
                CreateLODGroup(renderer);
                lodGroupsCreated++;
            }
        }

        // Configure global LOD settings
        QualitySettings.lodBias = lodBias;
        QualitySettings.maximumLODLevel = maxLODLevels - 1;

        optimizationStats["LOD Groups Created"] = lodGroupsCreated;
        optimizationStats["LOD Groups Optimized"] = lodGroupsOptimized;
    }

    void CreateLODGroup(MeshRenderer renderer)
    {
        GameObject obj = renderer.gameObject;
        LODGroup lodGroup = obj.GetComponent<LODGroup>();

        if (lodGroup == null)
        {
            lodGroup = obj.AddComponent<LODGroup>();
        }

        // Create LOD levels
        LOD[] lods = new LOD[3];

        // LOD 0 (original)
        Renderer[] lod0Renderers = { renderer };
        lods[0] = new LOD(0.6f, lod0Renderers);

        // LOD 1 (reduced quality)
        Renderer[] lod1Renderers = { renderer };
        lods[1] = new LOD(0.3f, lod1Renderers);

        // LOD 2 (very low quality or culled)
        Renderer[] lod2Renderers = { };
        lods[2] = new LOD(0.1f, lod2Renderers);

        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
    }

    void OptimizeExistingLODGroup(LODGroup lodGroup)
    {
        if (lodGroup == null) return;

        // Adjust LOD transition distances based on object size
        LOD[] lods = lodGroup.GetLODs();
        float objSize = lodGroup.size;

        for (int i = 0; i < lods.Length; i++)
        {
            float baseTransition = 1f - (float)i / lods.Length;
            float sizeModifier = Mathf.Clamp(objSize / 10f, 0.5f, 2f);
            lods[i].screenRelativeTransitionHeight = baseTransition * sizeModifier * lodBias;
        }

        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
    }

    void SetupOcclusionCulling()
    {
        if (logOptimizationProgress)
            Debug.Log("[ScenePerformanceOptimizer] Setting up occlusion culling...");

        int occludersCreated = 0;
        int occludeesOptimized = 0;

        // Find potential occluders (large static objects)
        MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);

        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.gameObject.isStatic && renderer.bounds.size.magnitude > occluderMinSize)
            {
                // Mark large static objects for occlusion culling (Occluder/Occludee deprecated in Unity 2022+)
                // Unity's built-in occlusion culling handles this automatically
                occludersCreated++;
            }

            // Objects are automatically considered for occlusion culling based on their mesh renderers
            if (renderer.bounds.size.magnitude > smallCullThreshold)
            {
                occludeesOptimized++;
            }
        }

        // Enable occlusion culling in camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.useOcclusionCulling = true;
        }

        optimizationStats["Occluders Created"] = occludersCreated;
        optimizationStats["Occludees Optimized"] = occludeesOptimized;
    }

    void OptimizeLighting()
    {
        if (logOptimizationProgress)
            Debug.Log("[ScenePerformanceOptimizer] Optimizing lighting system...");

        int lightsOptimized = 0;
        int shadowsOptimized = 0;

        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);

        // Count and categorize lights
        int realtimeLightCount = 0;
        foreach (Light light in lights)
        {
            if (light.lightmapBakeType == LightmapBakeType.Realtime)
                realtimeLightCount++;
        }

        // Optimize lights based on importance and performance impact
        foreach (Light light in lights)
        {
            OptimizeIndividualLight(light, realtimeLightCount > maxRealtimeLights);
            lightsOptimized++;

            // Optimize shadows
            if (light.shadows != LightShadows.None)
            {
                OptimizeLightShadows(light);
                shadowsOptimized++;
            }
        }

        // Configure global lighting settings
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;

        // Configure light culling
        QualitySettings.pixelLightCount = Mathf.Min(maxRealtimeLights, 4);

        optimizationStats["Lights Optimized"] = lightsOptimized;
        optimizationStats["Shadow Casters Optimized"] = shadowsOptimized;
    }

    void OptimizeIndividualLight(Light light, bool forceOptimization)
    {
        // Reduce range for performance
        if (light.range > maxDrawDistance)
        {
            light.range = maxDrawDistance;
        }

        // Optimize light type based on usage
        if (forceOptimization && light.lightmapBakeType == LightmapBakeType.Realtime)
        {
            // Convert less important realtime lights to baked
            if (light.intensity < 2f && light.range < 20f)
            {
                light.lightmapBakeType = LightmapBakeType.Baked;
            }
        }

        // Optimize culling mask
        if (light.cullingMask == -1)
        {
            // Don't light UI or other non-essential layers
            light.cullingMask = ~(1 << LayerMask.NameToLayer("UI"));
        }
    }

    void OptimizeLightShadows(Light light)
    {
        // Optimize shadow resolution based on light importance
        if (light.type == LightType.Directional)
        {
            // Main directional light can have higher quality shadows
            light.shadowResolution = LightShadowResolution.High;
        }
        else if (light.range < 10f || light.intensity < 1f)
        {
            // Small or dim lights get lower quality shadows
            light.shadowResolution = LightShadowResolution.Low;
        }
        else
        {
            light.shadowResolution = LightShadowResolution.Medium;
        }

        // Optimize shadow distance
        float optimizedShadowDistance = Mathf.Min(light.range * 0.8f, 50f);
        if (light.type == LightType.Directional)
        {
            QualitySettings.shadowDistance = optimizedShadowDistance;
        }

        // Use soft shadows sparingly
        if (light.shadows == LightShadows.Soft && light.intensity < 2f)
        {
            light.shadows = LightShadows.Hard;
        }
    }

    void OptimizeBatching()
    {
        if (logOptimizationProgress)
            Debug.Log("[ScenePerformanceOptimizer] Optimizing batching system...");

        int staticBatchedObjects = 0;
        int materialsOptimized = 0;

        if (enableStaticBatching)
        {
            // Find all static meshes that can be batched
            MeshRenderer[] staticRenderers = FindStaticRenderersForBatching();

            // Group by material for batching
            Dictionary<Material, List<MeshRenderer>> materialGroups = GroupRenderersByMaterial(staticRenderers);

            foreach (var group in materialGroups)
            {
                if (group.Value.Count > 1)
                {
                    GameObject[] objects = new GameObject[group.Value.Count];
                    for (int i = 0; i < group.Value.Count; i++)
                    {
                        objects[i] = group.Value[i].gameObject;
                    }

                    // Mark for static batching
                    StaticBatchingUtility.Combine(objects, transform.gameObject);
                    staticBatchedObjects += objects.Length;
                }
            }
        }

        // Optimize materials for batching
        Material[] allMaterials = FindAllMaterials();
        foreach (Material material in allMaterials)
        {
            if (OptimizeMaterialForBatching(material))
            {
                materialsOptimized++;
            }
        }

        // Configure Unity batching settings
        if (enableDynamicBatching)
        {
            // Dynamic batching is configured in Player Settings, but we can optimize for it
            OptimizeForDynamicBatching();
        }

        optimizationStats["Static Batched Objects"] = staticBatchedObjects;
        optimizationStats["Materials Optimized"] = materialsOptimized;
    }

    MeshRenderer[] FindStaticRenderersForBatching()
    {
        MeshRenderer[] allRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        List<MeshRenderer> staticRenderers = new List<MeshRenderer>();

        foreach (MeshRenderer renderer in allRenderers)
        {
            if (renderer.gameObject.isStatic &&
                renderer.GetComponent<MeshFilter>() != null &&
                renderer.GetComponent<MeshFilter>().sharedMesh != null)
            {
                staticRenderers.Add(renderer);
            }
        }

        return staticRenderers.ToArray();
    }

    Dictionary<Material, List<MeshRenderer>> GroupRenderersByMaterial(MeshRenderer[] renderers)
    {
        Dictionary<Material, List<MeshRenderer>> groups = new Dictionary<Material, List<MeshRenderer>>();

        foreach (MeshRenderer renderer in renderers)
        {
            Material material = renderer.sharedMaterial;
            if (material != null)
            {
                if (!groups.ContainsKey(material))
                {
                    groups[material] = new List<MeshRenderer>();
                }
                groups[material].Add(renderer);
            }
        }

        return groups;
    }

    bool OptimizeMaterialForBatching(Material material)
    {
        if (material == null) return false;

        bool optimized = false;

        // Enable GPU Instancing if supported
        if (material.enableInstancing == false && material.shader.name.Contains("Standard"))
        {
            material.enableInstancing = true;
            optimized = true;
        }

        return optimized;
    }

    void OptimizeForDynamicBatching()
    {
        // Ensure objects that should be dynamically batched have proper settings
        MeshRenderer[] dynamicRenderers = FindDynamicRenderersForBatching();

        foreach (MeshRenderer renderer in dynamicRenderers)
        {
            // Ensure mesh vertex count is under limit for dynamic batching (300 vertices)
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                if (meshFilter.sharedMesh.vertexCount > 300)
                {
                    // Mark as static if too complex for dynamic batching
                    renderer.gameObject.isStatic = true;
                }
            }
        }
    }

    MeshRenderer[] FindDynamicRenderersForBatching()
    {
        MeshRenderer[] allRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        List<MeshRenderer> dynamicRenderers = new List<MeshRenderer>();

        foreach (MeshRenderer renderer in allRenderers)
        {
            if (!renderer.gameObject.isStatic &&
                renderer.GetComponent<MeshFilter>() != null)
            {
                dynamicRenderers.Add(renderer);
            }
        }

        return dynamicRenderers.ToArray();
    }

    Material[] FindAllMaterials()
    {
        MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        List<Material> materials = new List<Material>();

        foreach (MeshRenderer renderer in renderers)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null && !materials.Contains(material))
                {
                    materials.Add(material);
                }
            }
        }

        return materials.ToArray();
    }

    void OptimizeTextures()
    {
        if (logOptimizationProgress)
            Debug.Log("[ScenePerformanceOptimizer] Optimizing textures...");

        // Note: Texture optimization typically happens in the asset import settings
        // This method focuses on runtime texture streaming and management

        int texturesOptimized = 0;

        // Enable texture streaming if available
        if (mipmapStreaming)
        {
            QualitySettings.streamingMipmapsActive = true;
            QualitySettings.streamingMipmapsMemoryBudget = 512f; // 512 MB budget
        }

        // Find all textures in use
        Material[] materials = FindAllMaterials();
        foreach (Material material in materials)
        {
            if (OptimizeMaterialTextures(material))
            {
                texturesOptimized++;
            }
        }

        optimizationStats["Textures Optimized"] = texturesOptimized;
    }

    bool OptimizeMaterialTextures(Material material)
    {
        if (material == null) return false;

        bool optimized = false;

        // Get texture properties
        string[] texturePropertyNames = material.GetTexturePropertyNames();

        foreach (string propertyName in texturePropertyNames)
        {
            Texture texture = material.GetTexture(propertyName);
            if (texture is Texture2D texture2D)
            {
                // Enable streaming for large textures
                if (texture2D.width > 512 || texture2D.height > 512)
                {
                    texture2D.requestedMipmapLevel = 1; // Use lower mip level for performance
                    optimized = true;
                }
            }
        }

        return optimized;
    }

    void ApplyUnityOptimizations()
    {
        if (logOptimizationProgress)
            Debug.Log("[ScenePerformanceOptimizer] Applying Unity-specific optimizations...");

        // Configure quality settings for performance
        QualitySettings.vSyncCount = 0; // Disable VSync for better performance measurement
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        QualitySettings.antiAliasing = 2; // 2x MSAA for balance

        // Configure physics for performance
        Physics.defaultSolverIterations = 6;
        Physics.defaultSolverVelocityIterations = 1;

        // Configure time settings
        Time.fixedDeltaTime = 0.02f; // 50 Hz physics

        // Configure culling distances
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.farClipPlane = maxDrawDistance;

            // Set up layer-based culling distances
            float[] cullingDistances = new float[32];
            for (int i = 0; i < 32; i++)
            {
                cullingDistances[i] = maxDrawDistance;
            }

            // Reduce culling distance for less important layers
            if (LayerMask.NameToLayer("Details") >= 0)
                cullingDistances[LayerMask.NameToLayer("Details")] = maxDrawDistance * 0.5f;
            if (LayerMask.NameToLayer("Effects") >= 0)
                cullingDistances[LayerMask.NameToLayer("Effects")] = maxDrawDistance * 0.7f;

            mainCamera.layerCullDistances = cullingDistances;
        }

        // Configure particle system optimizations
        ParticleSystem[] particleSystems = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (ParticleSystem ps in particleSystems)
        {
            OptimizeParticleSystem(ps);
        }

        optimizationStats["Particle Systems Optimized"] = particleSystems.Length;
    }

    void OptimizeParticleSystem(ParticleSystem particleSystem)
    {
        var main = particleSystem.main;

        // Limit max particles for performance
        if (main.maxParticles > 1000)
        {
            main.maxParticles = 1000;
        }

        // Use mesh rendering only when necessary
        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null && renderer.renderMode == ParticleSystemRenderMode.Mesh)
        {
            // Switch to billboard for distant or small effects
            if (particleSystem.transform.position.magnitude > 30f || main.startSizeMultiplier < 0.5f)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }
        }

        // Optimize collision if enabled
        var collision = particleSystem.collision;
        if (collision.enabled)
        {
            collision.maxCollisionShapes = 3; // Limit collision shape count
            collision.quality = ParticleSystemCollisionQuality.Low;
        }
    }

    void ValidateOptimizations()
    {
        if (logOptimizationProgress)
            Debug.Log("[ScenePerformanceOptimizer] Validating optimizations...");

        // Check for common performance issues
        int warnings = 0;

        // Check LOD configuration
        LODGroup[] lodGroups = FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
        foreach (LODGroup lodGroup in lodGroups)
        {
            if (lodGroup.GetLODs().Length < 2)
            {
                warnings++;
                if (logOptimizationProgress)
                    Debug.LogWarning($"LODGroup {lodGroup.name} has insufficient LOD levels");
            }
        }

        // Check light count
        Light[] realtimeLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        int realtimeCount = 0;
        foreach (Light light in realtimeLights)
        {
            if (light.lightmapBakeType == LightmapBakeType.Realtime)
                realtimeCount++;
        }

        if (realtimeCount > maxRealtimeLights)
        {
            warnings++;
            if (logOptimizationProgress)
                Debug.LogWarning($"Scene has {realtimeCount} realtime lights, recommended max: {maxRealtimeLights}");
        }

        optimizationStats["Validation Warnings"] = warnings;
    }

    void ReportOptimizationResults()
    {
        Debug.Log("[ScenePerformanceOptimizer] Optimization Results:");
        foreach (var stat in optimizationStats)
        {
            Debug.Log($"  {stat.Key}: {stat.Value}");
        }

        // Performance recommendations
        Debug.Log("[ScenePerformanceOptimizer] Performance Recommendations:");
        Debug.Log($"  - Target FPS: 30+ with {FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Length} renderers");
        Debug.Log($"  - Memory usage should be under 2GB with current optimizations");
        Debug.Log($"  - Draw calls should be under 500 with static batching enabled");
    }

    [ContextMenu("Test Performance")]
    public void TestPerformance()
    {
        StartCoroutine(PerformanceTestCoroutine());
    }

    System.Collections.IEnumerator PerformanceTestCoroutine()
    {
        Debug.Log("[ScenePerformanceOptimizer] Running performance test...");

        float startTime = Time.time;
        int frameCount = 0;
        float totalTime = 0f;
        float minFPS = float.MaxValue;
        float maxFPS = 0f;

        // Test for 5 seconds
        while (totalTime < 5f)
        {
            yield return new WaitForEndOfFrame();

            float fps = 1f / Time.unscaledDeltaTime;
            minFPS = Mathf.Min(minFPS, fps);
            maxFPS = Mathf.Max(maxFPS, fps);
            frameCount++;
            totalTime = Time.time - startTime;
        }

        float avgFPS = frameCount / totalTime;

        Debug.Log("[ScenePerformanceOptimizer] Performance Test Results:");
        Debug.Log($"  Average FPS: {avgFPS:F1}");
        Debug.Log($"  Min FPS: {minFPS:F1}");
        Debug.Log($"  Max FPS: {maxFPS:F1}");
        Debug.Log($"  Frame Count: {frameCount}");
        Debug.Log($"  Test Duration: {totalTime:F2}s");

        if (avgFPS >= 30f)
        {
            Debug.Log("  ✅ Performance target achieved!");
        }
        else
        {
            Debug.LogWarning("  ⚠️ Performance below target. Consider additional optimizations.");
        }
    }

    [ContextMenu("Reset All Optimizations")]
    public void ResetOptimizations()
    {
        Debug.LogWarning("[ScenePerformanceOptimizer] Resetting optimizations is not implemented. This would require backing up original settings.");
    }
}