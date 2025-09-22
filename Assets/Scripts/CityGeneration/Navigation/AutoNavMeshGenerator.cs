using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using CityGeneration.Core;

namespace CityGeneration.Navigation
{
    /// <summary>
    /// Automatically generates NavMesh data integrated with city generation
    /// Creates context-aware navigation areas for different AI types
    /// </summary>
    public class AutoNavMeshGenerator : BaseGenerator
    {
        [Header("NavMesh Settings")]
        public NavMeshBuildSettings walkableSettings;
        public bool useCustomSettings = true;

        [Header("Area Definitions")]
        public NavMeshAreaConfiguration[] areaConfigurations;
        public bool autoCreateAreas = true;

        [Header("Agent Configuration")]
        public NavMeshAgentConfiguration[] agentConfigurations;
        public bool configureExistingAgents = true;

        [Header("Performance")]
        public bool generateOffMeshLinks = true;
        public bool optimizeForPerformance = true;
        public int maxNavMeshObjects = 1000;

        [Header("Validation")]
        public bool validateConnectivity = true;
        public bool generateDebugVisualization = false;

        private Dictionary<string, int> areaNameToIndex = new Dictionary<string, int>();
        private List<NavMeshBuildSource> buildSources = new List<NavMeshBuildSource>();
        private List<NavMeshBuildMarkup> buildMarkups = new List<NavMeshBuildMarkup>();

        protected override async Task<GenerationResult> GenerateInternal(CityGenerationContext context)
        {
            var result = new NavMeshGenerationResult();

            try
            {
                // Initialize NavMesh settings
                InitializeNavMeshSettings();

                // Create area configurations if needed
                if (autoCreateAreas && (areaConfigurations == null || areaConfigurations.Length == 0))
                {
                    CreateDefaultAreaConfigurations();
                }

                UpdateProgress(0f, "Collecting NavMesh sources...");

                // Collect all NavMesh sources from city objects
                await CollectNavMeshSources(context);

                UpdateProgress(0.3f, "Processing navigation areas...");

                // Process different navigation areas
                await ProcessNavigationAreas(context);

                UpdateProgress(0.6f, "Generating off-mesh connections...");

                // Generate off-mesh connections
                if (generateOffMeshLinks)
                {
                    await GenerateOffMeshConnections(context);
                }

                UpdateProgress(0.8f, "Building NavMesh...");

                // Build the NavMesh
                await BuildNavMesh(context);

                UpdateProgress(0.9f, "Configuring AI agents...");

                // Configure existing AI agents
                if (configureExistingAgents)
                {
                    ConfigureAIAgents(context);
                }

                // Validate NavMesh
                if (validateConnectivity)
                {
                    UpdateProgress(0.95f, "Validating connectivity...");
                    await ValidateNavMeshConnectivity(context);
                }

                result.objectsGenerated = buildSources.Count;
                result.navMeshGenerated = true;
                result.areaCount = areaConfigurations.Length;

                LogDebug($"Generated NavMesh with {buildSources.Count} sources and {areaConfigurations.Length} areas");

                return result;
            }
            catch (System.Exception ex)
            {
                result.MarkAsError($"NavMesh generation failed: {ex.Message}");
                throw;
            }
        }

        private void InitializeNavMeshSettings()
        {
            // Initialize area name to index mapping
            areaNameToIndex.Clear();
            areaNameToIndex["Walkable"] = 0; // Default walkable area

            // Register custom areas
            if (areaConfigurations != null)
            {
                for (int i = 0; i < areaConfigurations.Length; i++)
                {
                    var area = areaConfigurations[i];
                    int areaIndex = i + 1; // Start from 1 (0 is reserved for default walkable)
                    areaNameToIndex[area.areaName] = areaIndex;

                    // Set area cost
                    NavMesh.SetAreaCost(areaIndex, area.cost);

                    LogDebug($"Registered NavMesh area: {area.areaName} (index: {areaIndex}, cost: {area.cost})");
                }
            }

            // Configure build settings
            if (useCustomSettings)
            {
                walkableSettings = NavMesh.GetSettingsByID(0);
                walkableSettings.agentRadius = 0.5f;
                walkableSettings.agentHeight = 2f;
                walkableSettings.agentSlope = 45f;
                walkableSettings.agentClimb = 0.4f;
            }
        }

        private void CreateDefaultAreaConfigurations()
        {
            areaConfigurations = new NavMeshAreaConfiguration[]
            {
                new NavMeshAreaConfiguration
                {
                    areaName = "Street",
                    cost = 1f,
                    includeLayers = LayerMask.GetMask("Default"),
                    requiredTags = new string[] { "Street" },
                    agentTypes = new string[] { "Guard", "Citizen", "Player" }
                },
                new NavMeshAreaConfiguration
                {
                    areaName = "Building",
                    cost = 2f,
                    includeLayers = LayerMask.GetMask("Default"),
                    requiredTags = new string[] { "Building" },
                    agentTypes = new string[] { "Player" } // Only player can enter buildings
                },
                new NavMeshAreaConfiguration
                {
                    areaName = "Restricted",
                    cost = 5f,
                    includeLayers = LayerMask.GetMask("Default"),
                    requiredTags = new string[] { "Castle", "Military" },
                    agentTypes = new string[] { "Guard" } // Only guards allowed
                },
                new NavMeshAreaConfiguration
                {
                    areaName = "Courtyard",
                    cost = 1.5f,
                    includeLayers = LayerMask.GetMask("Default"),
                    requiredTags = new string[] { "Courtyard" },
                    agentTypes = new string[] { "Guard", "Citizen", "Player" }
                }
            };

            LogDebug("Created default NavMesh area configurations");
        }

        private async Task CollectNavMeshSources(CityGenerationContext context)
        {
            buildSources.Clear();
            buildMarkups.Clear();

            // Collect from terrain
            await CollectTerrainSources(context);

            // Collect from streets
            await CollectStreetSources(context);

            // Collect from buildings
            await CollectBuildingSources(context);

            // Collect from walls (as obstacles)
            await CollectWallSources(context);

            LogDebug($"Collected {buildSources.Count} NavMesh build sources");
        }

        private async Task CollectTerrainSources(CityGenerationContext context)
        {
            if (context.cityLayout?.terrain?.terrain != null)
            {
                var terrain = context.cityLayout.terrain.terrain.GetComponent<Terrain>();
                if (terrain != null)
                {
                    var source = new NavMeshBuildSource();
                    source.shape = NavMeshBuildSourceShape.Terrain;
                    source.sourceObject = terrain.terrainData;
                    source.transform = terrain.transform.localToWorldMatrix;
                    source.area = 0; // Default walkable area

                    buildSources.Add(source);

                    LogDebug("Added terrain as NavMesh source");
                }
            }

            await Task.Yield();
        }

        private async Task CollectStreetSources(CityGenerationContext context)
        {
            if (context.cityLayout?.streets != null)
            {
                var streetObjects = new List<GameObject>();

                if (context.cityLayout.streets.mainRoads != null)
                    streetObjects.AddRange(context.cityLayout.streets.mainRoads);

                if (context.cityLayout.streets.secondaryStreets != null)
                    streetObjects.AddRange(context.cityLayout.streets.secondaryStreets);

                foreach (var street in streetObjects)
                {
                    if (street != null)
                    {
                        var meshFilter = street.GetComponent<MeshFilter>();
                        var renderer = street.GetComponent<MeshRenderer>();

                        if (meshFilter?.sharedMesh != null)
                        {
                            var source = new NavMeshBuildSource();
                            source.shape = NavMeshBuildSourceShape.Mesh;
                            source.sourceObject = meshFilter.sharedMesh;
                            source.transform = street.transform.localToWorldMatrix;
                            source.area = GetAreaIndex("Street");

                            buildSources.Add(source);
                        }
                        else
                        {
                            // Create box source for procedural streets
                            var source = new NavMeshBuildSource();
                            source.shape = NavMeshBuildSourceShape.Box;
                            source.size = street.transform.localScale;
                            source.transform = street.transform.localToWorldMatrix;
                            source.area = GetAreaIndex("Street");

                            buildSources.Add(source);
                        }
                    }

                    // Yield periodically
                    if (buildSources.Count % 20 == 0)
                    {
                        await Task.Yield();
                    }
                }

                LogDebug($"Added {streetObjects.Count} streets as NavMesh sources");
            }

            await Task.Yield();
        }

        private async Task CollectBuildingSources(CityGenerationContext context)
        {
            if (context.cityLayout?.buildings?.buildings != null)
            {
                foreach (var building in context.cityLayout.buildings.buildings)
                {
                    if (building != null)
                    {
                        await ProcessBuildingForNavMesh(building);
                    }

                    // Yield periodically
                    if (buildSources.Count % 10 == 0)
                    {
                        await Task.Yield();
                    }
                }

                LogDebug($"Processed {context.cityLayout.buildings.buildings.Count} buildings for NavMesh");
            }
        }

        private async Task ProcessBuildingForNavMesh(GameObject building)
        {
            var buildingInfo = building.GetComponent<EnhancedBuildingInfo>();

            // Building exterior - obstacle by default
            var source = new NavMeshBuildSource();
            source.shape = NavMeshBuildSourceShape.Box;
            source.size = building.transform.localScale;
            source.transform = building.transform.localToWorldMatrix;
            source.area = -1; // Not walkable (obstacle)

            buildSources.Add(source);

            // Building interior - walkable if accessible
            if (buildingInfo?.hasInterior == true)
            {
                // Create interior walkable area
                var interiorSource = new NavMeshBuildSource();
                interiorSource.shape = NavMeshBuildSourceShape.Box;
                interiorSource.size = building.transform.localScale * 0.8f; // Slightly smaller
                interiorSource.size.y = 0.1f; // Flat floor
                interiorSource.transform = Matrix4x4.TRS(
                    building.transform.position + Vector3.up * 0.1f,
                    building.transform.rotation,
                    Vector3.one
                );
                interiorSource.area = GetAreaIndex("Building");

                buildSources.Add(interiorSource);
            }

            // Check for courtyards
            var courtyardObject = building.transform.Find("Courtyard");
            if (courtyardObject != null)
            {
                var courtyardSource = new NavMeshBuildSource();
                courtyardSource.shape = NavMeshBuildSourceShape.Box;
                courtyardSource.size = courtyardObject.localScale;
                courtyardSource.transform = courtyardObject.localToWorldMatrix;
                courtyardSource.area = GetAreaIndex("Courtyard");

                buildSources.Add(courtyardSource);
            }

            await Task.Yield();
        }

        private async Task CollectWallSources(CityGenerationContext context)
        {
            if (context.cityLayout?.walls?.wallSegments != null)
            {
                foreach (var wall in context.cityLayout.walls.wallSegments)
                {
                    if (wall != null)
                    {
                        var source = new NavMeshBuildSource();
                        source.shape = NavMeshBuildSourceShape.Box;
                        source.size = wall.transform.localScale;
                        source.transform = wall.transform.localToWorldMatrix;
                        source.area = -1; // Not walkable (obstacle)

                        buildSources.Add(source);
                    }
                }

                LogDebug($"Added {context.cityLayout.walls.wallSegments.Count} walls as NavMesh obstacles");
            }

            await Task.Yield();
        }

        private async Task ProcessNavigationAreas(CityGenerationContext context)
        {
            // Process each area configuration
            foreach (var areaConfig in areaConfigurations)
            {
                await ProcessNavigationArea(areaConfig, context);
            }
        }

        private async Task ProcessNavigationArea(NavMeshAreaConfiguration areaConfig, CityGenerationContext context)
        {
            // Find objects that match this area configuration
            var matchingObjects = FindObjectsForArea(areaConfig);

            foreach (var obj in matchingObjects)
            {
                // Add area markup
                var markup = new NavMeshBuildMarkup();
                markup.root = obj.transform;
                markup.overrideArea = true;
                markup.area = GetAreaIndex(areaConfig.areaName);

                buildMarkups.Add(markup);
            }

            await Task.Yield();
        }

        private GameObject[] FindObjectsForArea(NavMeshAreaConfiguration areaConfig)
        {
            var matchingObjects = new List<GameObject>();

            // Search by tags
            if (areaConfig.requiredTags != null)
            {
                foreach (var tag in areaConfig.requiredTags)
                {
                    var objectsWithTag = GameObject.FindGameObjectsWithTag(tag);
                    matchingObjects.AddRange(objectsWithTag);
                }
            }

            return matchingObjects.ToArray();
        }

        private async Task GenerateOffMeshConnections(CityGenerationContext context)
        {
            // Generate connections between different areas
            await GenerateGateConnections(context);
            await GenerateBuildingConnections(context);
            await GenerateVerticalConnections(context);

            LogDebug("Generated off-mesh connections");
        }

        private async Task GenerateGateConnections(CityGenerationContext context)
        {
            if (context.cityLayout?.walls?.gates != null)
            {
                foreach (var gate in context.cityLayout.walls.gates)
                {
                    if (gate != null)
                    {
                        // Create connection through gate
                        Vector3 insidePoint = gate.transform.position + gate.transform.forward * 5f;
                        Vector3 outsidePoint = gate.transform.position - gate.transform.forward * 5f;

                        CreateOffMeshConnection(insidePoint, outsidePoint, true, GetAreaIndex("Street"));
                    }
                }
            }

            await Task.Yield();
        }

        private async Task GenerateBuildingConnections(CityGenerationContext context)
        {
            if (context.cityLayout?.buildings?.buildings != null)
            {
                foreach (var building in context.cityLayout.buildings.buildings)
                {
                    var buildingInfo = building.GetComponent<EnhancedBuildingInfo>();
                    if (buildingInfo?.hasInterior == true)
                    {
                        // Create connection from exterior to interior
                        Vector3 exteriorPoint = building.transform.position + building.transform.forward * (building.transform.localScale.z * 0.6f);
                        Vector3 interiorPoint = building.transform.position;

                        CreateOffMeshConnection(exteriorPoint, interiorPoint, true, GetAreaIndex("Building"));
                    }
                }
            }

            await Task.Yield();
        }

        private async Task GenerateVerticalConnections(CityGenerationContext context)
        {
            // Generate connections for stairs, ramps, etc.
            // This would be expanded based on specific vertical navigation needs
            await Task.Yield();
        }

        private void CreateOffMeshConnection(Vector3 start, Vector3 end, bool bidirectional, int areaType)
        {
            GameObject linkObject = new GameObject("OffMeshLink");
            var link = linkObject.AddComponent<OffMeshLink>();

            link.startTransform = CreateLinkPoint(start, linkObject.transform);
            link.endTransform = CreateLinkPoint(end, linkObject.transform);
            link.biDirectional = bidirectional;
            link.area = areaType;
            link.autoUpdatePositions = false;

            // Store for cleanup if needed
            if (context?.cityParent != null)
            {
                linkObject.transform.SetParent(context.cityParent);
            }
        }

        private Transform CreateLinkPoint(Vector3 position, Transform parent)
        {
            GameObject point = new GameObject("LinkPoint");
            point.transform.SetParent(parent);
            point.transform.position = position;
            return point.transform;
        }

        private async Task BuildNavMesh(CityGenerationContext context)
        {
            // Calculate bounds
            Bounds bounds = CalculateNavMeshBounds(context);

            try
            {
                // Build NavMesh asynchronously
                var asyncOp = NavMeshBuilder.BuildNavMeshAsync(
                    walkableSettings,
                    buildSources,
                    bounds
                );

                while (!asyncOp.isDone)
                {
                    await Task.Yield();
                }

                LogDebug($"NavMesh build completed for bounds: {bounds}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"NavMesh build failed: {ex.Message}");
                throw;
            }
        }

        private Bounds CalculateNavMeshBounds(CityGenerationContext context)
        {
            if (context.config != null)
            {
                float size = context.config.GetCitySize() * 1.2f; // Add margin
                return new Bounds(Vector3.zero, new Vector3(size, 50f, size));
            }

            // Fallback: calculate from build sources
            if (buildSources.Count > 0)
            {
                Bounds combinedBounds = new Bounds();
                bool boundsInitialized = false;

                foreach (var source in buildSources)
                {
                    Bounds sourceBounds = source.shape switch
                    {
                        NavMeshBuildSourceShape.Mesh => GetMeshBounds(source),
                        NavMeshBuildSourceShape.Terrain => GetTerrainBounds(source),
                        NavMeshBuildSourceShape.Box => GetBoxBounds(source),
                        _ => new Bounds(source.transform.GetColumn(3), Vector3.one * 10f)
                    };

                    if (!boundsInitialized)
                    {
                        combinedBounds = sourceBounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(sourceBounds);
                    }
                }

                return combinedBounds;
            }

            return new Bounds(Vector3.zero, Vector3.one * 100f);
        }

        private Bounds GetMeshBounds(NavMeshBuildSource source)
        {
            if (source.sourceObject is Mesh mesh)
            {
                return TransformBounds(mesh.bounds, source.transform);
            }
            return new Bounds();
        }

        private Bounds GetTerrainBounds(NavMeshBuildSource source)
        {
            if (source.sourceObject is TerrainData terrainData)
            {
                return TransformBounds(new Bounds(Vector3.zero, terrainData.size), source.transform);
            }
            return new Bounds();
        }

        private Bounds GetBoxBounds(NavMeshBuildSource source)
        {
            return TransformBounds(new Bounds(Vector3.zero, source.size), source.transform);
        }

        private Bounds TransformBounds(Bounds localBounds, Matrix4x4 transform)
        {
            var center = transform.MultiplyPoint3x4(localBounds.center);
            var size = transform.MultiplyVector(localBounds.size);
            return new Bounds(center, size);
        }

        private void ConfigureAIAgents(CityGenerationContext context)
        {
            // Find and configure existing NavMesh agents
            var guards = GameObject.FindObjectsOfType<GuardAI>();
            var citizens = GameObject.FindObjectsOfType<Citizen>();

            // Configure guards
            foreach (var guard in guards)
            {
                var agent = guard.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    ConfigureAgentForType(agent, "Guard");
                }
            }

            // Configure citizens
            foreach (var citizen in citizens)
            {
                var agent = citizen.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    ConfigureAgentForType(agent, "Citizen");
                }
            }

            LogDebug($"Configured {guards.Length} guard agents and {citizens.Length} citizen agents");
        }

        private void ConfigureAgentForType(NavMeshAgent agent, string agentType)
        {
            var config = GetAgentConfiguration(agentType);
            if (config != null)
            {
                agent.areaMask = config.allowedAreas;
                agent.avoidancePriority = config.avoidancePriority;
                agent.speed = config.speed;
                agent.stoppingDistance = config.stoppingDistance;
            }
            else
            {
                // Default configuration
                agent.areaMask = GetDefaultAreaMaskForAgent(agentType);
            }
        }

        private NavMeshAgentConfiguration GetAgentConfiguration(string agentType)
        {
            if (agentConfigurations != null)
            {
                foreach (var config in agentConfigurations)
                {
                    if (config.agentType == agentType)
                        return config;
                }
            }
            return null;
        }

        private int GetDefaultAreaMaskForAgent(string agentType)
        {
            switch (agentType.ToLower())
            {
                case "guard":
                    return NavMesh.AllAreas; // Guards can go anywhere
                case "citizen":
                    return (1 << GetAreaIndex("Street")) |
                           (1 << GetAreaIndex("Courtyard")); // Citizens limited to public areas
                case "player":
                    return NavMesh.AllAreas; // Player can access most areas
                default:
                    return 1; // Default walkable only
            }
        }

        private async Task ValidateNavMeshConnectivity(CityGenerationContext context)
        {
            // Test connectivity between key areas
            var validationResults = new List<string>();

            // Test gate connectivity
            if (context.cityLayout?.walls?.gates != null && context.cityLayout.walls.gates.Count > 0)
            {
                bool gateConnectivity = await TestGateConnectivity(context.cityLayout.walls.gates);
                validationResults.Add($"Gate connectivity: {(gateConnectivity ? "PASS" : "FAIL")}");
            }

            // Test building accessibility
            if (context.cityLayout?.buildings?.buildings != null && context.cityLayout.buildings.buildings.Count > 0)
            {
                bool buildingAccess = await TestBuildingAccess(context.cityLayout.buildings.buildings);
                validationResults.Add($"Building access: {(buildingAccess ? "PASS" : "FAIL")}");
            }

            foreach (var result in validationResults)
            {
                LogDebug($"NavMesh validation: {result}");
            }

            await Task.Yield();
        }

        private async Task<bool> TestGateConnectivity(List<GameObject> gates)
        {
            // Test if agents can navigate through gates
            foreach (var gate in gates)
            {
                if (gate != null)
                {
                    Vector3 inside = gate.transform.position + gate.transform.forward * 5f;
                    Vector3 outside = gate.transform.position - gate.transform.forward * 5f;

                    NavMeshPath path = new NavMeshPath();
                    if (!NavMesh.CalculatePath(inside, outside, NavMesh.AllAreas, path))
                    {
                        LogDebug($"Gate connectivity test failed for gate at {gate.transform.position}");
                        return false;
                    }
                }
            }

            await Task.Yield();
            return true;
        }

        private async Task<bool> TestBuildingAccess(List<GameObject> buildings)
        {
            int accessibleBuildings = 0;

            foreach (var building in buildings)
            {
                var buildingInfo = building.GetComponent<EnhancedBuildingInfo>();
                if (buildingInfo?.hasInterior == true)
                {
                    // Test if building interior is accessible
                    Vector3 exterior = building.transform.position + building.transform.forward * (building.transform.localScale.z * 0.6f);
                    Vector3 interior = building.transform.position;

                    NavMeshPath path = new NavMeshPath();
                    if (NavMesh.CalculatePath(exterior, interior, NavMesh.AllAreas, path))
                    {
                        accessibleBuildings++;
                    }
                }
            }

            await Task.Yield();

            // Consider successful if at least 80% of buildings with interiors are accessible
            int buildingsWithInteriors = buildings.Count(b => b.GetComponent<EnhancedBuildingInfo>()?.hasInterior == true);
            return buildingsWithInteriors == 0 || (float)accessibleBuildings / buildingsWithInteriors >= 0.8f;
        }

        private int GetAreaIndex(string areaName)
        {
            return areaNameToIndex.ContainsKey(areaName) ? areaNameToIndex[areaName] : 0;
        }

        protected override async Task OptimizeResult(GenerationResult result)
        {
            if (optimizeForPerformance)
            {
                // Optimize NavMesh for runtime performance
                // This could include reducing precision for distant areas, etc.
                await Task.Yield();
                LogDebug("NavMesh optimization completed");
            }
        }
    }

    /// <summary>
    /// Configuration for NavMesh areas
    /// </summary>
    [System.Serializable]
    public class NavMeshAreaConfiguration
    {
        public string areaName;
        public float cost = 1f;
        public LayerMask includeLayers = -1;
        public string[] requiredTags;
        public string[] agentTypes; // Which agent types can use this area
    }

    /// <summary>
    /// Configuration for NavMesh agents
    /// </summary>
    [System.Serializable]
    public class NavMeshAgentConfiguration
    {
        public string agentType;
        public int allowedAreas = NavMesh.AllAreas;
        public int avoidancePriority = 50;
        public float speed = 3.5f;
        public float stoppingDistance = 0.5f;
    }

    /// <summary>
    /// Result from NavMesh generation
    /// </summary>
    public class NavMeshGenerationResult : GenerationResult
    {
        public bool navMeshGenerated = false;
        public int areaCount = 0;
        public int offMeshLinkCount = 0;

        public override bool IsValid()
        {
            return base.IsValid() && navMeshGenerated;
        }
    }

    /// <summary>
    /// Enhanced building info component for NavMesh integration
    /// </summary>
    public class EnhancedBuildingInfo : MonoBehaviour
    {
        [Header("Building Properties")]
        public BuildingTemplate buildingTemplate;
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