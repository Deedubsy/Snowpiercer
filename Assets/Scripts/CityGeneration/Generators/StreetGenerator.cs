using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using CityGeneration.Core;

namespace CityGeneration.Generators
{
    /// <summary>
    /// Generates street networks for the medieval city
    /// Extracted and modernized from MedievalCityBuilder
    /// </summary>
    public class StreetGenerator : BaseGenerator
    {
        [Header("Street Configuration")]
        public float streetWidth = 4f;
        public float mainRoadWidth = 6f;
        [Range(4, 12)] public int districtGridSize = 8;

        [Header("Street Colors")]
        public Color streetColor = new Color(0.5f, 0.4f, 0.3f);
        public Color mainRoadColor = new Color(0.4f, 0.35f, 0.25f);

        [Header("Performance")]
        public int streetBatchSize = 8; // Streets per batch for progressive generation

        private List<GameObject> generatedStreets = new List<GameObject>();
        private List<GameObject> mainRoads = new List<GameObject>();
        private List<GameObject> secondaryStreets = new List<GameObject>();

        protected override async Task<GenerationResult> GenerateInternal(CityGenerationContext context)
        {
            var result = new StreetGenerationResult();
            generatedStreets.Clear();
            mainRoads.Clear();
            secondaryStreets.Clear();

            // Copy configuration from context if available
            ApplyContextConfiguration(context);

            Transform streetParent = CreateCategoryParent("Streets");

            try
            {
                // Determine wall shape from context
                WallShape wallShape = context.config?.wallShape ?? WallShape.Square;

                // Generate street network based on wall shape
                UpdateProgress(0f, "Planning street network...");

                if (wallShape == WallShape.Circular)
                {
                    await GenerateCircularStreetNetwork(streetParent, context);
                }
                else
                {
                    await GenerateSquareStreetNetwork(streetParent, context);
                }

                // Combine results
                result.mainRoads = new List<GameObject>(mainRoads);
                result.secondaryStreets = new List<GameObject>(secondaryStreets);

                // Register with collision system
                UpdateProgress(0.9f, "Registering street collisions...");
                RegisterStreetCollisions(result);

                result.objectsGenerated = generatedStreets.Count;
                LogDebug($"Generated {result.objectsGenerated} street segments");

                return result;
            }
            catch (System.Exception ex)
            {
                result.MarkAsError($"Street generation failed: {ex.Message}");
                throw;
            }
        }

        private async Task GenerateCircularStreetNetwork(Transform parent, CityGenerationContext context)
        {
            float cityRadius = context.config?.cityRadius ?? 50f;

            // Create main radial roads (30% of progress)
            UpdateProgress(0f, "Creating main radial roads...");
            await CreateRadialRoads(parent, cityRadius);

            // Create concentric ring roads (40% of progress)
            UpdateProgress(0.3f, "Creating ring roads...");
            await CreateRingRoads(parent, cityRadius);

            // Create secondary connecting streets (30% of progress)
            UpdateProgress(0.7f, "Creating connecting streets...");
            await CreateConnectingStreets(parent, cityRadius);
        }

        private async Task GenerateSquareStreetNetwork(Transform parent, CityGenerationContext context)
        {
            Vector2 wallSize = context.config?.squareWallSize ?? new Vector2(100f, 80f);

            // Create main cross roads (40% of progress)
            UpdateProgress(0f, "Creating main roads...");
            await CreateMainCrossRoads(parent, wallSize);

            // Create grid streets (60% of progress)
            UpdateProgress(0.4f, "Creating grid streets...");
            await CreateGridStreets(parent, wallSize);
        }

        private async Task CreateRadialRoads(Transform parent, float cityRadius)
        {
            int roadCount = 6; // Six main roads radiating from center
            float angleStep = 360f / roadCount;

            for (int i = 0; i < roadCount; i++)
            {
                float angle = i * angleStep;
                Vector3 direction = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad),
                    0f,
                    Mathf.Cos(angle * Mathf.Deg2Rad)
                );

                await CreateRadialRoad(parent, direction, cityRadius * 0.8f, $"Radial_Road_{i}");

                // Progressive update
                float progress = (i + 1) / (float)roadCount * 0.3f;
                UpdateProgress(progress, $"Created radial road {i + 1}/{roadCount}");
                await Task.Yield();
            }
        }

        private async Task CreateRadialRoad(Transform parent, Vector3 direction, float length, string roadName)
        {
            int segments = Mathf.CeilToInt(length / mainRoadWidth);
            Vector3 currentPos = Vector3.zero;

            for (int i = 0; i < segments; i++)
            {
                Vector3 segmentPos = currentPos + direction * (mainRoadWidth * 0.5f);
                currentPos += direction * mainRoadWidth;

                GameObject roadSegment = CreateStreetSegment(
                    $"{roadName}_Segment_{i}",
                    segmentPos,
                    mainRoadWidth,
                    direction,
                    parent,
                    true
                );

                mainRoads.Add(roadSegment);
                generatedStreets.Add(roadSegment);

                // Register with collision manager as road point
                collisionManager.RegisterRoadPoint(segmentPos, mainRoadWidth);
            }

            await Task.Yield();
        }

        private async Task CreateRingRoads(Transform parent, float cityRadius)
        {
            float[] ringRadii = { cityRadius * 0.3f, cityRadius * 0.6f };

            for (int ring = 0; ring < ringRadii.Length; ring++)
            {
                float radius = ringRadii[ring];
                await CreateRingRoad(parent, radius, $"Ring_Road_{ring}");

                float progress = 0.3f + (ring + 1) / (float)ringRadii.Length * 0.4f;
                UpdateProgress(progress, $"Created ring road {ring + 1}/{ringRadii.Length}");
            }
        }

        private async Task CreateRingRoad(Transform parent, float radius, string roadName)
        {
            int segments = Mathf.CeilToInt(2 * Mathf.PI * radius / streetWidth);
            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep;
                Vector3 position = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                    0f,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius
                );

                Vector3 tangent = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    0f,
                    -Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                GameObject roadSegment = CreateStreetSegment(
                    $"{roadName}_Segment_{i}",
                    position,
                    streetWidth,
                    tangent,
                    parent,
                    false
                );

                secondaryStreets.Add(roadSegment);
                generatedStreets.Add(roadSegment);

                // Register with collision manager
                collisionManager.RegisterRoadPoint(position, streetWidth);

                if (i % streetBatchSize == 0)
                {
                    await Task.Yield();
                }
            }
        }

        private async Task CreateConnectingStreets(Transform parent, float cityRadius)
        {
            // Create some connecting streets between radial and ring roads
            // Simplified implementation
            int connectingStreets = 12;

            for (int i = 0; i < connectingStreets; i++)
            {
                float angle = Random.Range(0f, 360f);
                float startRadius = Random.Range(cityRadius * 0.2f, cityRadius * 0.4f);
                float endRadius = Random.Range(cityRadius * 0.5f, cityRadius * 0.7f);

                Vector3 direction = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad),
                    0f,
                    Mathf.Cos(angle * Mathf.Deg2Rad)
                );

                Vector3 startPos = direction * startRadius;
                Vector3 endPos = direction * endRadius;

                await CreateStraightStreet(parent, startPos, endPos, $"Connecting_Street_{i}");

                if (i % streetBatchSize == 0)
                {
                    float progress = 0.7f + (i + 1) / (float)connectingStreets * 0.3f;
                    UpdateProgress(progress, $"Created connecting street {i + 1}/{connectingStreets}");
                    await Task.Yield();
                }
            }
        }

        private async Task CreateMainCrossRoads(Transform parent, Vector2 wallSize)
        {
            float halfWidth = wallSize.x * 0.5f;
            float halfDepth = wallSize.y * 0.5f;

            // Main horizontal road (East-West)
            Vector3 horizontalStart = new Vector3(-halfWidth * 0.8f, 0f, 0f);
            Vector3 horizontalEnd = new Vector3(halfWidth * 0.8f, 0f, 0f);
            await CreateStraightStreet(parent, horizontalStart, horizontalEnd, "Main_Road_EW", true);

            // Main vertical road (North-South)
            Vector3 verticalStart = new Vector3(0f, 0f, -halfDepth * 0.8f);
            Vector3 verticalEnd = new Vector3(0f, 0f, halfDepth * 0.8f);
            await CreateStraightStreet(parent, verticalStart, verticalEnd, "Main_Road_NS", true);

            UpdateProgress(0.4f, "Main cross roads completed");
        }

        private async Task CreateGridStreets(Transform parent, Vector2 wallSize)
        {
            float halfWidth = wallSize.x * 0.5f;
            float halfDepth = wallSize.y * 0.5f;
            float gridSpacing = Mathf.Min(wallSize.x, wallSize.y) / districtGridSize;

            int streetCount = 0;
            int totalStreets = (districtGridSize - 1) * 2; // Approximate

            // Create vertical grid lines
            for (int i = 1; i < districtGridSize; i++)
            {
                float x = -halfWidth + (i * gridSpacing);
                Vector3 start = new Vector3(x, 0f, -halfDepth * 0.8f);
                Vector3 end = new Vector3(x, 0f, halfDepth * 0.8f);

                await CreateStraightStreet(parent, start, end, $"Grid_Street_V_{i}");
                streetCount++;

                if (streetCount % streetBatchSize == 0)
                {
                    float progress = 0.4f + (streetCount / (float)totalStreets) * 0.6f;
                    UpdateProgress(progress, $"Created grid street {streetCount}/{totalStreets}");
                    await Task.Yield();
                }
            }

            // Create horizontal grid lines
            for (int i = 1; i < districtGridSize; i++)
            {
                float z = -halfDepth + (i * gridSpacing);
                Vector3 start = new Vector3(-halfWidth * 0.8f, 0f, z);
                Vector3 end = new Vector3(halfWidth * 0.8f, 0f, z);

                await CreateStraightStreet(parent, start, end, $"Grid_Street_H_{i}");
                streetCount++;

                if (streetCount % streetBatchSize == 0)
                {
                    float progress = 0.4f + (streetCount / (float)totalStreets) * 0.6f;
                    UpdateProgress(progress, $"Created grid street {streetCount}/{totalStreets}");
                    await Task.Yield();
                }
            }
        }

        private async Task CreateStraightStreet(Transform parent, Vector3 start, Vector3 end, string streetName, bool isMainRoad = false)
        {
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);
            float width = isMainRoad ? mainRoadWidth : streetWidth;
            int segments = Mathf.CeilToInt(distance / width);

            for (int i = 0; i < segments; i++)
            {
                float t = (i + 0.5f) / segments;
                Vector3 segmentPos = Vector3.Lerp(start, end, t);

                GameObject streetSegment = CreateStreetSegment(
                    $"{streetName}_Segment_{i}",
                    segmentPos,
                    width,
                    direction,
                    parent,
                    isMainRoad
                );

                if (isMainRoad)
                {
                    mainRoads.Add(streetSegment);
                }
                else
                {
                    secondaryStreets.Add(streetSegment);
                }

                generatedStreets.Add(streetSegment);

                // Register with collision manager
                collisionManager.RegisterRoadPoint(segmentPos, width);
            }

            await Task.Yield();
        }

        private GameObject CreateStreetSegment(string name, Vector3 position, float width, Vector3 direction, Transform parent, bool isMainRoad)
        {
            GameObject street = CreateCube(name, position, parent);

            // Scale street segment
            street.transform.localScale = new Vector3(width, 0.1f, width);
            street.transform.position = position + Vector3.down * 0.05f; // Slightly below ground level

            // Orient street in direction of travel
            if (direction != Vector3.zero)
            {
                street.transform.LookAt(position + direction);
            }

            // Apply appropriate color
            Color color = isMainRoad ? mainRoadColor : streetColor;
            ApplyMaterial(street, color, false);

            return street;
        }

        private void RegisterStreetCollisions(StreetGenerationResult result)
        {
            foreach (var street in result.mainRoads)
            {
                collisionManager.RegisterStaticObject(street, ObjectType.Street);
            }

            foreach (var street in result.secondaryStreets)
            {
                collisionManager.RegisterStaticObject(street, ObjectType.Street);
            }
        }

        private void ApplyContextConfiguration(CityGenerationContext context)
        {
            if (context?.config != null)
            {
                var config = context.config;
                streetWidth = config.streetWidth;
                mainRoadWidth = config.mainRoadWidth;
            }
        }

        protected override Task<bool> ValidatePreConditions()
        {
            // Check that we have reasonable street widths
            if (streetWidth <= 0 || mainRoadWidth <= 0)
            {
                LogDebug("Invalid street width configuration");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        protected override Task ValidateResult(GenerationResult result)
        {
            var streetResult = result as StreetGenerationResult;

            if (streetResult != null)
            {
                int totalStreets = streetResult.mainRoads.Count + streetResult.secondaryStreets.Count;
                if (totalStreets == 0)
                {
                    LogDebug("Warning: No streets were generated");
                }
                else
                {
                    LogDebug($"Generated {streetResult.mainRoads.Count} main roads and {streetResult.secondaryStreets.Count} secondary streets");
                }
            }

            return Task.CompletedTask;
        }
    }
}