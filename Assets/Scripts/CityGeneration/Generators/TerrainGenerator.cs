using UnityEngine;
using System.Threading.Tasks;
using CityGeneration.Core;

namespace CityGeneration.Generators
{
    /// <summary>
    /// Generates terrain foundation for the medieval city
    /// Extracted and modernized from MedievalCityBuilder
    /// </summary>
    public class TerrainGenerator : BaseGenerator
    {
        [Header("Terrain Configuration")]
        public bool generateTerrain = true;
        public int terrainResolution = 129;
        public float terrainHeight = 5f;
        public bool addTerrainVariation = true;
        [Range(0f, 1f)] public float terrainRoughness = 0.3f;

        [Header("Terrain Textures")]
        public Texture2D grassTexture;
        public Texture2D dirtTexture;
        public Texture2D stoneTexture;

        private GameObject generatedTerrain;

        protected override async Task<GenerationResult> GenerateInternal(CityGenerationContext context)
        {
            var result = new TerrainGenerationResult();

            if (!generateTerrain)
            {
                LogDebug("Terrain generation disabled");
                result.objectsGenerated = 0;
                return result;
            }

            // Copy configuration from context if available
            ApplyContextConfiguration(context);

            try
            {
                UpdateProgress(0f, "Creating terrain...");

                // Create terrain GameObject
                generatedTerrain = await CreateBaseTerrain(context);

                UpdateProgress(0.3f, "Generating height data...");

                // Generate height data
                await GenerateHeightData(generatedTerrain, context);

                UpdateProgress(0.6f, "Applying textures...");

                // Apply textures
                await ApplyTerrainTextures(generatedTerrain);

                UpdateProgress(0.9f, "Finalizing terrain...");

                // Set terrain bounds
                result.terrain = generatedTerrain;
                result.terrainBounds = CalculateTerrainBounds(context);
                result.objectsGenerated = 1;

                LogDebug($"Generated terrain with resolution {terrainResolution}x{terrainResolution}");

                return result;
            }
            catch (System.Exception ex)
            {
                result.MarkAsError($"Terrain generation failed: {ex.Message}");
                throw;
            }
        }

        private async Task<GameObject> CreateBaseTerrain(CityGenerationContext context)
        {
            // Create terrain GameObject
            GameObject terrainObject = new GameObject("Generated_Terrain");
            terrainObject.transform.SetParent(context.cityParent);

            // Add Terrain component
            Terrain terrain = terrainObject.AddComponent<Terrain>();
            TerrainCollider terrainCollider = terrainObject.AddComponent<TerrainCollider>();

            // Create TerrainData
            TerrainData terrainData = new TerrainData();

            // Calculate terrain size based on city configuration
            float citySize = context.config?.GetCitySize() ?? 100f;
            float terrainSize = citySize * 1.5f; // Make terrain larger than city

            terrainData.heightmapResolution = terrainResolution;
            terrainData.size = new Vector3(terrainSize, terrainHeight, terrainSize);

            // Position terrain so city is centered
            terrainObject.transform.position = new Vector3(-terrainSize * 0.5f, 0f, -terrainSize * 0.5f);

            // Assign terrain data
            terrain.terrainData = terrainData;
            terrainCollider.terrainData = terrainData;

            await Task.Yield();
            return terrainObject;
        }

        private async Task GenerateHeightData(GameObject terrainObject, CityGenerationContext context)
        {
            Terrain terrain = terrainObject.GetComponent<Terrain>();
            TerrainData terrainData = terrain.terrainData;

            int width = terrainData.heightmapResolution;
            int height = terrainData.heightmapResolution;
            float[,] heights = new float[width, height];

            // Get city center in terrain coordinates
            Vector3 cityCenter = Vector3.zero;
            float citySize = context.config?.GetCitySize() ?? 100f;
            Vector3 terrainSize = terrainData.size;

            // Convert city coordinates to terrain coordinates
            float centerX = (cityCenter.x + terrainSize.x * 0.5f) / terrainSize.x;
            float centerZ = (cityCenter.z + terrainSize.z * 0.5f) / terrainSize.z;

            // Generate height data with city area flatter
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    float xCoord = (float)x / (width - 1);
                    float zCoord = (float)z / (height - 1);

                    // Calculate distance from city center
                    float distanceFromCenter = Vector2.Distance(
                        new Vector2(xCoord, zCoord),
                        new Vector2(centerX, centerZ)
                    );

                    // Base height with gentle slope away from center
                    float baseHeight = 0.02f + distanceFromCenter * 0.05f;

                    if (addTerrainVariation)
                    {
                        // Add noise for natural variation, but reduced near city
                        float noiseScale = 0.1f;
                        float cityFlatteningRadius = 0.3f; // Flatten area around city

                        if (distanceFromCenter < cityFlatteningRadius)
                        {
                            // Reduce noise in city area
                            float flatteningFactor = 1f - (distanceFromCenter / cityFlatteningRadius);
                            noiseScale *= (1f - flatteningFactor * 0.8f);
                        }

                        float noise = Mathf.PerlinNoise(xCoord * 4f, zCoord * 4f) * terrainRoughness * noiseScale;
                        baseHeight += noise;
                    }

                    heights[x, z] = Mathf.Clamp01(baseHeight);
                }

                // Progressive update
                if (x % 10 == 0)
                {
                    float progress = 0.3f + (x / (float)width) * 0.3f;
                    UpdateProgress(progress, $"Generating height data... {x}/{width}");
                    await Task.Yield();
                }
            }

            terrainData.SetHeights(0, 0, heights);
            await Task.Yield();
        }

        private async Task ApplyTerrainTextures(GameObject terrainObject)
        {
            Terrain terrain = terrainObject.GetComponent<Terrain>();
            TerrainData terrainData = terrain.terrainData;

            // Create default textures if none provided
            if (grassTexture == null) grassTexture = CreateDefaultTexture(Color.green);
            if (dirtTexture == null) dirtTexture = CreateDefaultTexture(new Color(0.6f, 0.4f, 0.2f));
            if (stoneTexture == null) stoneTexture = CreateDefaultTexture(Color.gray);

            // Create terrain layers
            TerrainLayer[] terrainLayers = new TerrainLayer[3];

            // Grass layer
            terrainLayers[0] = new TerrainLayer();
            terrainLayers[0].diffuseTexture = grassTexture;
            terrainLayers[0].tileSize = new Vector2(15f, 15f);

            // Dirt layer
            terrainLayers[1] = new TerrainLayer();
            terrainLayers[1].diffuseTexture = dirtTexture;
            terrainLayers[1].tileSize = new Vector2(10f, 10f);

            // Stone layer
            terrainLayers[2] = new TerrainLayer();
            terrainLayers[2].diffuseTexture = stoneTexture;
            terrainLayers[2].tileSize = new Vector2(8f, 8f);

            terrainData.terrainLayers = terrainLayers;

            // Generate texture splatmap
            await GenerateTextureSplatmap(terrainData);
        }

        private async Task GenerateTextureSplatmap(TerrainData terrainData)
        {
            int alphamapWidth = terrainData.alphamapWidth;
            int alphamapHeight = terrainData.alphamapHeight;
            int numTextures = terrainData.alphamapLayers;

            float[,,] splatmapData = new float[alphamapWidth, alphamapHeight, numTextures];

            for (int x = 0; x < alphamapWidth; x++)
            {
                for (int z = 0; z < alphamapHeight; z++)
                {
                    // Get height at this position
                    float normalizedX = (float)x / (alphamapWidth - 1);
                    float normalizedZ = (float)z / (alphamapHeight - 1);
                    float height = terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
                    float normalizedHeight = height / terrainData.size.y;

                    // Calculate texture weights based on height and slope
                    float[] weights = new float[numTextures];

                    // Grass (dominant at low heights)
                    weights[0] = Mathf.Clamp01(1f - normalizedHeight * 2f);

                    // Dirt (middle heights)
                    weights[1] = Mathf.Clamp01(1f - Mathf.Abs(normalizedHeight - 0.3f) * 3f);

                    // Stone (high heights)
                    weights[2] = Mathf.Clamp01(normalizedHeight - 0.4f);

                    // Normalize weights
                    float totalWeight = weights[0] + weights[1] + weights[2];
                    if (totalWeight > 0)
                    {
                        for (int i = 0; i < numTextures; i++)
                        {
                            weights[i] /= totalWeight;
                        }
                    }
                    else
                    {
                        weights[0] = 1f; // Default to grass
                    }

                    // Assign weights to splatmap
                    for (int i = 0; i < numTextures; i++)
                    {
                        splatmapData[x, z, i] = weights[i];
                    }
                }

                // Progressive update
                if (x % 8 == 0)
                {
                    float progress = 0.6f + (x / (float)alphamapWidth) * 0.3f;
                    UpdateProgress(progress, $"Applying textures... {x}/{alphamapWidth}");
                    await Task.Yield();
                }
            }

            terrainData.SetAlphamaps(0, 0, splatmapData);
            await Task.Yield();
        }

        private Texture2D CreateDefaultTexture(Color color)
        {
            Texture2D texture = new Texture2D(64, 64);
            Color[] colors = new Color[64 * 64];

            for (int i = 0; i < colors.Length; i++)
            {
                // Add slight variation
                float variation = Random.Range(-0.1f, 0.1f);
                colors[i] = new Color(
                    Mathf.Clamp01(color.r + variation),
                    Mathf.Clamp01(color.g + variation),
                    Mathf.Clamp01(color.b + variation),
                    1f
                );
            }

            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        private Bounds CalculateTerrainBounds(CityGenerationContext context)
        {
            float citySize = context.config?.GetCitySize() ?? 100f;
            float terrainSize = citySize * 1.5f;

            return new Bounds(
                Vector3.zero,
                new Vector3(terrainSize, terrainHeight, terrainSize)
            );
        }

        private void ApplyContextConfiguration(CityGenerationContext context)
        {
            if (context?.config != null)
            {
                // Terrain configuration could be added to CityConfiguration if needed
                // For now, use default values
            }
        }

        protected override Task<bool> ValidatePreConditions()
        {
            if (generateTerrain)
            {
                if (terrainResolution < 33 || terrainResolution > 513)
                {
                    LogDebug("Invalid terrain resolution. Must be between 33 and 513");
                    return Task.FromResult(false);
                }

                if (terrainHeight <= 0)
                {
                    LogDebug("Invalid terrain height. Must be greater than 0");
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(true);
        }

        protected override Task ValidateResult(GenerationResult result)
        {
            var terrainResult = result as TerrainGenerationResult;

            if (generateTerrain && terrainResult?.terrain == null)
            {
                LogDebug("Error: Terrain generation was enabled but no terrain was created");
            }

            return Task.CompletedTask;
        }

        protected override async Task OptimizeResult(GenerationResult result)
        {
            if (generatedTerrain != null)
            {
                // Set terrain settings for optimal performance
                Terrain terrain = generatedTerrain.GetComponent<Terrain>();
                if (terrain != null)
                {
                    terrain.heightmapPixelError = 5f; // Reduce LOD pixel error for better performance
                    terrain.basemapDistance = 1000f; // Distance for using base map
                }
            }

            await Task.Yield();
        }

        public void ClearTerrain()
        {
            if (generatedTerrain != null)
            {
#if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(generatedTerrain);
#else
                UnityEngine.Object.Destroy(generatedTerrain);
#endif
                generatedTerrain = null;
            }
        }
    }
}