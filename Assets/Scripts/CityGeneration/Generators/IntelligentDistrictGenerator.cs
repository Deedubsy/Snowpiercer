using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using CityGeneration.Core;
using CityGeneration.Rules;
using CityGeneration.Rules.SpecificRules;

namespace CityGeneration.Generators
{
    /// <summary>
    /// Enhanced district generator that uses the procedural rule system
    /// for intelligent district placement based on realistic urban planning principles
    /// </summary>
    public class IntelligentDistrictGenerator : BaseGenerator
    {
        [Header("Rule-Based Configuration")]
        public ProceduralRuleEngine ruleEngine;
        public bool useRuleBasedPlacement = true;
        public bool createDefaultRules = true;

        [Header("District Configuration")]
        public DistrictConfiguration[] districtConfigurations;
        public bool autoCreateDistricts = true;
        public float defaultDistrictRadius = 25f;

        [Header("District Dependencies")]
        public bool respectDistrictOrder = true;
        public DistrictType[] generationOrder = {
            DistrictType.Castle,
            DistrictType.Market,
            DistrictType.Residential,
            DistrictType.Religious,
            DistrictType.Artisan,
            DistrictType.Military
        };

        [Header("Performance")]
        public int maxDistrictsPerFrame = 2;

        private Dictionary<DistrictType, GameObject> createdDistricts = new Dictionary<DistrictType, GameObject>();

        protected override async Task<GenerationResult> GenerateInternal(CityGenerationContext context)
        {
            var result = new DistrictGenerationResult();

            // Initialize rule engine if not configured
            if (ruleEngine == null)
            {
                ruleEngine = new ProceduralRuleEngine();
            }

            if (createDefaultRules && (ruleEngine.globalRules == null || ruleEngine.globalRules.Length == 0))
            {
                CreateDefaultRules();
            }

            // Create districts based on configuration
            if (autoCreateDistricts && districtConfigurations.Length == 0)
            {
                CreateDefaultDistrictConfigurations();
            }

            Transform districtParent = CreateCategoryParent("Districts");

            try
            {
                if (respectDistrictOrder)
                {
                    await GenerateDistrictsInOrder(result, districtParent, context);
                }
                else
                {
                    await GenerateDistrictsParallel(result, districtParent, context);
                }

                result.objectsGenerated = result.districts.Count;
                LogDebug($"Generated {result.objectsGenerated} districts using rule-based placement");

                return result;
            }
            catch (System.Exception ex)
            {
                result.MarkAsError($"District generation failed: {ex.Message}");
                throw;
            }
        }

        private async Task GenerateDistrictsInOrder(DistrictGenerationResult result, Transform parent, CityGenerationContext context)
        {
            var placementContext = new PlacementContext(context)
            {
                cityBounds = GetCityBounds(context),
                terrain = GetTerrain(context)
            };

            int districtsGenerated = 0;

            foreach (var districtType in generationOrder)
            {
                var config = GetDistrictConfiguration(districtType);
                if (config == null) continue;

                UpdateProgress((float)districtsGenerated / generationOrder.Length, $"Placing {districtType} district...");

                await GenerateDistrict(districtType, config, result, parent, placementContext);

                // Update placement context with newly created district
                UpdatePlacementContext(placementContext);

                districtsGenerated++;

                // Yield control periodically
                if (districtsGenerated % maxDistrictsPerFrame == 0)
                {
                    await Task.Yield();
                }
            }
        }

        private async Task GenerateDistrictsParallel(DistrictGenerationResult result, Transform parent, CityGenerationContext context)
        {
            var placementContext = new PlacementContext(context)
            {
                cityBounds = GetCityBounds(context),
                terrain = GetTerrain(context)
            };

            var tasks = new List<Task>();

            foreach (var config in districtConfigurations)
            {
                tasks.Add(GenerateDistrict(config.districtType, config, result, parent, placementContext));
            }

            await Task.WhenAll(tasks);
        }

        private async Task GenerateDistrict(DistrictType districtType, DistrictConfiguration config, DistrictGenerationResult result, Transform parent, PlacementContext placementContext)
        {
            PlacementResult placement;

            if (useRuleBasedPlacement)
            {
                // Use rule engine to find optimal position
                placement = await ruleEngine.FindBestDistrictPosition(districtType, placementContext);
            }
            else
            {
                // Fallback to simple placement
                placement = await FindSimpleDistrictPosition(districtType, placementContext);
            }

            if (!placement.success)
            {
                LogDebug($"Failed to place district {districtType}: {placement.errorMessage}");
                return;
            }

            // Create district GameObject
            GameObject district = await CreateDistrictObject(districtType, config, placement.position, parent);

            if (district != null)
            {
                result.districts.Add(district);
                createdDistricts[districtType] = district;

                // Add DistrictInfo component
                var districtInfo = district.GetComponent<DistrictInfo>();
                if (districtInfo == null)
                {
                    districtInfo = district.AddComponent<DistrictInfo>();
                }

                districtInfo.districtType = districtType;
                districtInfo.districtName = config.districtName;
                districtInfo.districtRadius = config.radius;

                // Register with collision system
                collisionManager.RegisterStaticObject(district, ObjectType.Building, config.radius);

                LogDebug($"Created {districtType} district at {placement.position} with score {placement.score:F2}");
            }
        }

        private async Task<GameObject> CreateDistrictObject(DistrictType districtType, DistrictConfiguration config, Vector3 position, Transform parent)
        {
            string districtName = $"District_{districtType}";
            GameObject district = new GameObject(districtName);
            district.transform.SetParent(parent);
            district.transform.position = position;

            // Create visual representation
            await CreateDistrictVisuals(district, config);

            // Create district bounds
            CreateDistrictBounds(district, config);

            await Task.Yield();
            return district;
        }

        private async Task CreateDistrictVisuals(GameObject district, DistrictConfiguration config)
        {
            // Create a simple visual marker for the district
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "DistrictMarker";
            marker.transform.SetParent(district.transform);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = new Vector3(config.radius * 2f, 0.1f, config.radius * 2f);

            // Apply district color
            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = config.visualColor;
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Smoothness", 0.3f);
                renderer.material = material;
            }

            // Remove collider from visual marker
            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
#if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(collider);
#else
                UnityEngine.Object.Destroy(collider);
#endif
            }

            await Task.Yield();
        }

        private void CreateDistrictBounds(GameObject district, DistrictConfiguration config)
        {
            // Create invisible bounds object for spatial queries
            GameObject bounds = new GameObject("DistrictBounds");
            bounds.transform.SetParent(district.transform);
            bounds.transform.localPosition = Vector3.zero;

            var sphereCollider = bounds.AddComponent<SphereCollider>();
            sphereCollider.radius = config.radius;
            sphereCollider.isTrigger = true;

            // Add zone component for gameplay
            var districtZone = bounds.AddComponent<DistrictZone>();
            districtZone.districtType = config.districtType;
            districtZone.allowedBuildingTypes = config.allowedBuildingTypes;
        }

        private void CreateDefaultRules()
        {
            var rules = new List<PlacementRule>();

            // Create terrain rule for castles (prefer high ground)
            var castleTerrainRule = ScriptableObject.CreateInstance<TerrainRule>();
            castleTerrainRule.ruleName = "Castle High Ground";
            castleTerrainRule.preferHighGround = true;
            castleTerrainRule.maxSlope = 0.4f;
            castleTerrainRule.priority = 3f;
            rules.Add(castleTerrainRule);

            // Create accessibility rule for markets
            var marketAccessRule = ScriptableObject.CreateInstance<AccessibilityRule>();
            marketAccessRule.ruleName = "Market Road Access";
            marketAccessRule.requiresRoadAccess = true;
            marketAccessRule.preferMainRoads = true;
            marketAccessRule.priority = 2.5f;
            rules.Add(marketAccessRule);

            // Create distance rule for residential (near market, away from military)
            var residentialDistanceRule = ScriptableObject.CreateInstance<DistanceRule>();
            residentialDistanceRule.ruleName = "Residential Placement";
            residentialDistanceRule.targetDistrictType = DistrictType.Market;
            residentialDistanceRule.minDistance = 15f;
            residentialDistanceRule.maxDistance = 40f;
            residentialDistanceRule.priority = 2f;
            rules.Add(residentialDistanceRule);

            ruleEngine.globalRules = rules.ToArray();

            // Create district-specific rule sets
            var districtRuleSets = new List<DistrictRuleSet>();

            districtRuleSets.Add(new DistrictRuleSet
            {
                districtType = DistrictType.Castle,
                districtName = "Castle District",
                rules = new PlacementRule[] { castleTerrainRule }
            });

            districtRuleSets.Add(new DistrictRuleSet
            {
                districtType = DistrictType.Market,
                districtName = "Market District",
                rules = new PlacementRule[] { marketAccessRule }
            });

            ruleEngine.districtRuleSets = districtRuleSets.ToArray();

            LogDebug("Created default placement rules");
        }

        private void CreateDefaultDistrictConfigurations()
        {
            districtConfigurations = new DistrictConfiguration[]
            {
                new DistrictConfiguration
                {
                    districtType = DistrictType.Castle,
                    districtName = "Castle District",
                    radius = 30f,
                    visualColor = new Color(0.6f, 0.6f, 0.8f),
                    allowedBuildingTypes = new[] { BuildingType.Castle, BuildingType.Barracks }
                },
                new DistrictConfiguration
                {
                    districtType = DistrictType.Market,
                    districtName = "Market Square",
                    radius = 25f,
                    visualColor = new Color(0.8f, 0.7f, 0.4f),
                    allowedBuildingTypes = new[] { BuildingType.Shop, BuildingType.Tavern }
                },
                new DistrictConfiguration
                {
                    districtType = DistrictType.Residential,
                    districtName = "Residential Quarter",
                    radius = 35f,
                    visualColor = new Color(0.7f, 0.8f, 0.6f),
                    allowedBuildingTypes = new[] { BuildingType.House }
                },
                new DistrictConfiguration
                {
                    districtType = DistrictType.Religious,
                    districtName = "Cathedral District",
                    radius = 20f,
                    visualColor = new Color(0.9f, 0.9f, 0.8f),
                    allowedBuildingTypes = new[] { BuildingType.Cathedral }
                },
                new DistrictConfiguration
                {
                    districtType = DistrictType.Artisan,
                    districtName = "Artisan Quarter",
                    radius = 22f,
                    visualColor = new Color(0.8f, 0.6f, 0.5f),
                    allowedBuildingTypes = new[] { BuildingType.Workshop }
                },
                new DistrictConfiguration
                {
                    districtType = DistrictType.Military,
                    districtName = "Military Compound",
                    radius = 18f,
                    visualColor = new Color(0.6f, 0.5f, 0.5f),
                    allowedBuildingTypes = new[] { BuildingType.Barracks }
                }
            };

            LogDebug("Created default district configurations");
        }

        private DistrictConfiguration GetDistrictConfiguration(DistrictType districtType)
        {
            foreach (var config in districtConfigurations)
            {
                if (config.districtType == districtType)
                    return config;
            }
            return null;
        }

        private async Task<PlacementResult> FindSimpleDistrictPosition(DistrictType districtType, PlacementContext context)
        {
            // Simple fallback placement logic
            Vector3 position = Vector3.zero;

            switch (districtType)
            {
                case DistrictType.Castle:
                    position = Vector3.zero; // Center
                    break;
                case DistrictType.Market:
                    position = new Vector3(0, 0, -20);
                    break;
                case DistrictType.Residential:
                    position = new Vector3(-25, 0, 15);
                    break;
                default:
                    position = Random.insideUnitSphere * 30f;
                    position.y = 0f;
                    break;
            }

            await Task.Yield();

            return new PlacementResult
            {
                success = true,
                position = position,
                score = 0.5f
            };
        }

        private Bounds GetCityBounds(CityGenerationContext context)
        {
            if (context.config != null)
            {
                float size = context.config.GetCitySize();
                return new Bounds(Vector3.zero, Vector3.one * size);
            }

            return new Bounds(Vector3.zero, Vector3.one * 100f);
        }

        private Terrain GetTerrain(CityGenerationContext context)
        {
            // Try to find terrain in the city layout
            if (context.cityLayout?.terrain?.terrain != null)
            {
                return context.cityLayout.terrain.terrain.GetComponent<Terrain>();
            }

            return null;
        }

        private void UpdatePlacementContext(PlacementContext context)
        {
            // Update existing districts array
            var districtList = new List<GameObject>();
            foreach (var kvp in createdDistricts)
            {
                if (kvp.Value != null)
                {
                    districtList.Add(kvp.Value);
                }
            }
            context.existingDistricts = districtList.ToArray();
        }

        public GameObject GetDistrict(DistrictType districtType)
        {
            return createdDistricts.ContainsKey(districtType) ? createdDistricts[districtType] : null;
        }

        public Vector3 GetDistrictCenter(DistrictType districtType)
        {
            var district = GetDistrict(districtType);
            return district != null ? district.transform.position : Vector3.zero;
        }
    }

    /// <summary>
    /// Configuration for a specific district type
    /// </summary>
    [System.Serializable]
    public class DistrictConfiguration
    {
        public DistrictType districtType;
        public string districtName;
        public float radius = 25f;
        public Color visualColor = Color.white;
        public BuildingType[] allowedBuildingTypes;
        public int maxBuildings = 5;
        public bool isPrimary = true;
    }

    /// <summary>
    /// Result from district generation
    /// </summary>
    public class DistrictGenerationResult : GenerationResult
    {
        public List<GameObject> districts = new List<GameObject>();

        public override bool IsValid()
        {
            return base.IsValid() && districts.Count > 0;
        }
    }

    /// <summary>
    /// Component that marks district zones for gameplay
    /// </summary>
    public class DistrictZone : MonoBehaviour
    {
        public DistrictType districtType;
        public BuildingType[] allowedBuildingTypes;

        private void OnTriggerEnter(Collider other)
        {
            // Handle player or AI entering district
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // Could trigger district-specific events or UI updates
                Debug.Log($"Player entered {districtType} district");
            }
        }
    }
}