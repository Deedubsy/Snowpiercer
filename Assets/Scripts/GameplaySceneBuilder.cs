using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


#if UNITY_EDITOR
#endif

/// <summary>
/// Medieval City Builder - Creates a walled city using cube primitives
/// Player spawns at city gates, with districts and patrol routes inside walls
/// </summary>
/// </summary>
public class GamePlaySceneBuilder : MonoBehaviour
{
    [Header("Scene Configuration")]
    public bool autoSetupOnStart = false;
    public bool createNewScene = false;

    [Header("Terrain Settings")]
    public Vector3 terrainSize = new Vector3(800, 100, 600);
    public float castleHillHeight = 40f;
    public Vector3 castlePosition = new Vector3(200, 40, 150);
    public Vector3 townCenterPosition = new Vector3(400, 5, 300);

    [Header("Lighting Settings")]
    public Color ambientSkyColor = new Color(0.5f, 0.6f, 0.8f);
    public Color ambientEquatorColor = new Color(0.4f, 0.4f, 0.6f);
    public Color ambientGroundColor = new Color(0.2f, 0.3f, 0.4f);

    [Header("Prefab References")]
    public GameObject playerPrefab;
    public GameObject managersPrefab;
    public GameObject guardPrefab;
    public GameObject citizenPrefab;

    [Header("Asset Paths")]
    public string castleAssetsPath = "Assets/Ventuar/MedievalCastlePack/Prefabs/Castle";
    public string environmentAssetsPath = "Assets/Ventuar/MedievalCastlePack/Prefabs/Environment";
    public string structuresAssetsPath = "Assets/Ventuar/MedievalCastlePack/Prefabs/Structures";

    void Start()
    {
        if (autoSetupOnStart)
        {
            BuildGamePlayScene();
        }
    }

    [ContextMenu("Build GamePlay Scene")]
    public void BuildGamePlayScene()
    {
        Debug.Log("=== SP-011: Building GamePlay Scene ===");

#if UNITY_EDITOR
        if (createNewScene)
        {
            CreateNewGamePlayScene();
        }

        SetupBasicScene();
        CreateTerrain();
        SetupLighting();
        SetupCamera();
        PlaceManagerSystems();
        BuildCastleArea();
        BuildTownArea();
        SetupPhysicsLayers();

        Debug.Log("✅ GamePlay scene construction complete!");
        Debug.Log("Next steps: Run SP-020 (Physics Validation), then SP-013 (NavMesh Setup)");
#else
        Debug.LogWarning("Scene building only available in Unity Editor");
#endif
    }

#if UNITY_EDITOR
    void CreateNewGamePlayScene()
    {
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        newScene.name = "GamePlay";

        string scenePath = "Assets/Scenes/GamePlay.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);

        Debug.Log($"✅ Created new GamePlay scene at {scenePath}");
    }

    void SetupBasicScene()
    {
        Debug.Log("--- Setting up basic scene structure ---");

        // Create main directional light
        GameObject sunLight = new GameObject("Directional Light");
        Light sun = sunLight.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.shadows = LightShadows.Soft;
        sun.intensity = 1.0f;
        sun.color = Color.white;
        sunLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Create organization parent objects
        CreateOrganizationStructure();
    }

    void CreateOrganizationStructure()
    {
        // Create parent objects for organization
        GameObject environments = new GameObject("--- ENVIRONMENT ---");
        GameObject gameplay = new GameObject("--- GAMEPLAY ---");
        GameObject systems = new GameObject("--- SYSTEMS ---");
        GameObject lighting = new GameObject("--- LIGHTING ---");

        // Create sub-categories
        GameObject castle = new GameObject("Castle");
        GameObject town = new GameObject("Town");
        GameObject terrain = new GameObject("Terrain");
        GameObject transitions = new GameObject("Transitions");

        castle.transform.SetParent(environments.transform);
        town.transform.SetParent(environments.transform);
        terrain.transform.SetParent(environments.transform);
        transitions.transform.SetParent(environments.transform);

        Debug.Log("✅ Scene organization structure created");
    }

    void CreateTerrain()
    {
        Debug.Log("--- Creating terrain ---");

        // Create terrain data
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = 513;
        terrainData.size = terrainSize;

        // Create basic height map with castle hill
        CreateBasicHeightmap(terrainData);

        // Create terrain GameObject
        GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
        terrainGO.name = "Main Terrain";

        // Set parent
        GameObject terrainParent = GameObject.Find("Terrain");
        if (terrainParent != null)
        {
            terrainGO.transform.SetParent(terrainParent.transform);
        }

        Debug.Log("✅ Basic terrain created with castle hill");
    }

    void CreateBasicHeightmap(TerrainData terrainData)
    {
        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        float[,] heights = new float[width, height];

        // Normalize castle position to terrain coordinates
        float castleX = castlePosition.x / terrainSize.x;
        float castleZ = castlePosition.z / terrainSize.z;
        float hillRadius = 0.15f; // 15% of terrain size

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width;
                float yCoord = (float)y / height;

                // Calculate distance from castle position
                float distanceFromCastle = Vector2.Distance(new Vector2(xCoord, yCoord), new Vector2(castleX, castleZ));

                // Create hill around castle
                if (distanceFromCastle < hillRadius)
                {
                    float hillHeight = Mathf.Lerp(castleHillHeight / terrainSize.y, 0f, distanceFromCastle / hillRadius);
                    heights[x, y] = hillHeight;
                }
                else
                {
                    // Gentle rolling terrain for town area
                    float noise = Mathf.PerlinNoise(xCoord * 5f, yCoord * 5f) * 0.02f;
                    heights[x, y] = noise;
                }
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }

    void SetupLighting()
    {
        Debug.Log("--- Setting up lighting ---");

        // Configure ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        RenderSettings.ambientIntensity = 1.0f;

        // Configure fog for atmosphere
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.5f, 0.6f, 0.8f, 1f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.001f;

        Debug.Log("✅ Lighting configured for day/night cycle");
    }

    void SetupCamera()
    {
        Debug.Log("--- Setting up main camera ---");

        // Find or create main camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraGO = new GameObject("Main Camera");
            mainCamera = cameraGO.AddComponent<Camera>();
            cameraGO.tag = "MainCamera";
        }

        // Position camera for scene overview
        mainCamera.transform.position = new Vector3(400, 60, 200);
        mainCamera.transform.rotation = Quaternion.Euler(20f, 0f, 0f);

        // Configure camera settings
        mainCamera.fieldOfView = 60f;
        mainCamera.nearClipPlane = 0.3f;
        mainCamera.farClipPlane = 1000f;

        Debug.Log("✅ Main camera positioned for scene overview");
    }

    void PlaceManagerSystems()
    {
        Debug.Log("--- Placing manager systems ---");

        GameObject systemsParent = GameObject.Find("--- SYSTEMS ---");

        // Place Managers prefab if available
        if (managersPrefab != null)
        {
            GameObject managers = Instantiate(managersPrefab);
            managers.name = "Managers";
            managers.transform.SetParent(systemsParent.transform);
            Debug.Log("✅ Managers prefab placed");
        }
        else
        {
            Debug.LogWarning("⚠️ Managers prefab not assigned - place manually");
        }

        // Create spawn points parent
        GameObject spawnPoints = new GameObject("Spawn Points");
        spawnPoints.transform.SetParent(systemsParent.transform);

        CreateSpawnPoint("Castle Player Spawn", castlePosition + Vector3.up * 2f, spawnPoints.transform);
        CreateSpawnPoint("Castle Guard Spawn 1", castlePosition + new Vector3(10, 2, 10), spawnPoints.transform);
        CreateSpawnPoint("Castle Guard Spawn 2", castlePosition + new Vector3(-10, 2, 10), spawnPoints.transform);

        Debug.Log("✅ Basic spawn points created");
    }

    void CreateSpawnPoint(string name, Vector3 position, Transform parent)
    {
        GameObject spawnPoint = new GameObject(name);
        spawnPoint.transform.position = position;
        spawnPoint.transform.SetParent(parent);

        // Add visual indicator
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = "Indicator";
        indicator.transform.SetParent(spawnPoint.transform);
        indicator.transform.localPosition = Vector3.zero;
        indicator.transform.localScale = Vector3.one * 0.5f;

        // Make it wireframe for visibility
        Renderer renderer = indicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material indicatorMat = new Material(Shader.Find("Standard"));
            indicatorMat.color = name.Contains("Player") ? Color.green : Color.red;
            indicatorMat.SetFloat("_Mode", 2); // Transparent
            Color color = indicatorMat.color;
            color.a = 0.5f;
            indicatorMat.color = color;
            renderer.material = indicatorMat;
        }
    }

    void BuildCastleArea()
    {
        Debug.Log("--- Building castle area ---");

        GameObject castleParent = GameObject.Find("Castle");

        // Create castle placeholder structure
        GameObject castleStructure = CreateCastlePlaceholder();
        castleStructure.transform.SetParent(castleParent.transform);
        castleStructure.transform.position = castlePosition;

        // Create gate trigger area
        CreateCastleGate(castleParent.transform);

        Debug.Log("✅ Castle area basic structure created");
    }

    GameObject CreateCastlePlaceholder()
    {
        GameObject castle = new GameObject("Castle Main Structure");

        // Main keep
        GameObject keep = GameObject.CreatePrimitive(PrimitiveType.Cube);
        keep.name = "Keep";
        keep.transform.SetParent(castle.transform);
        keep.transform.localPosition = Vector3.zero;
        keep.transform.localScale = new Vector3(20, 30, 20);

        // Courtyard
        GameObject courtyard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        courtyard.name = "Courtyard";
        courtyard.transform.SetParent(castle.transform);
        courtyard.transform.localPosition = new Vector3(0, -10, 0);
        courtyard.transform.localScale = new Vector3(50, 10, 40);

        // Walls
        CreateCastleWall("North Wall", castle.transform, new Vector3(0, 5, 25), new Vector3(50, 10, 5));
        CreateCastleWall("South Wall", castle.transform, new Vector3(0, 5, -25), new Vector3(50, 10, 5));
        CreateCastleWall("East Wall", castle.transform, new Vector3(25, 5, 0), new Vector3(5, 10, 50));
        CreateCastleWall("West Wall", castle.transform, new Vector3(-25, 5, 0), new Vector3(5, 10, 50));

        return castle;
    }

    void CreateCastleWall(string name, Transform parent, Vector3 localPos, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.localPosition = localPos;
        wall.transform.localScale = scale;
    }

    void CreateCastleGate(Transform parent)
    {
        GameObject gate = new GameObject("Castle Gate");
        gate.transform.SetParent(parent);
        gate.transform.position = castlePosition + new Vector3(0, 0, -30);

        // Add CityGateTrigger component
        CityGateTrigger gateTrigger = gate.AddComponent<CityGateTrigger>();

        // Add collider for trigger
        BoxCollider gateCollider = gate.AddComponent<BoxCollider>();
        gateCollider.isTrigger = true;
        gateCollider.size = new Vector3(10, 10, 5);

        // Add visual indicator
        GameObject gateVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gateVisual.name = "Gate Visual";
        gateVisual.transform.SetParent(gate.transform);
        gateVisual.transform.localPosition = Vector3.zero;
        gateVisual.transform.localScale = new Vector3(8, 8, 2);

        Renderer gateRenderer = gateVisual.GetComponent<Renderer>();
        if (gateRenderer != null)
        {
            Material gateMat = new Material(Shader.Find("Standard"));
            gateMat.color = Color.blue;
            gateRenderer.material = gateMat;
        }

        Debug.Log("✅ Castle gate with trigger created");
    }

    void BuildTownArea()
    {
        Debug.Log("--- Building town area ---");

        GameObject townParent = GameObject.Find("Town");

        // Create town districts
        CreateTownDistrict("Market Square", townCenterPosition, townParent.transform);
        CreateTownDistrict("Residential Quarter", townCenterPosition + new Vector3(-100, 0, 0), townParent.transform);
        CreateTownDistrict("Artisan Quarter", townCenterPosition + new Vector3(100, 0, 0), townParent.transform);
        CreateTownDistrict("Noble Quarter", townCenterPosition + new Vector3(0, 0, 100), townParent.transform);

        Debug.Log("✅ Town districts basic structure created");
    }

    void CreateTownDistrict(string districtName, Vector3 position, Transform parent)
    {
        GameObject district = new GameObject(districtName);
        district.transform.SetParent(parent);
        district.transform.position = position;

        // Create placeholder buildings
        for (int i = 0; i < 5; i++)
        {
            Vector3 buildingPos = position + new Vector3(
                Random.Range(-30f, 30f),
                0,
                Random.Range(-30f, 30f)
            );

            CreatePlaceholderBuilding($"{districtName} Building {i + 1}", buildingPos, district.transform);
        }

        // Create central plaza/feature
        GameObject plaza = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        plaza.name = $"{districtName} Plaza";
        plaza.transform.SetParent(district.transform);
        plaza.transform.position = position;
        plaza.transform.localScale = new Vector3(15, 1, 15);

        Renderer plazaRenderer = plaza.GetComponent<Renderer>();
        if (plazaRenderer != null)
        {
            Material plazaMat = new Material(Shader.Find("Standard"));
            plazaMat.color = new Color(0.8f, 0.8f, 0.6f); // Stone color
            plazaRenderer.material = plazaMat;
        }
    }

    void CreatePlaceholderBuilding(string name, Vector3 position, Transform parent)
    {
        GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building.name = name;
        building.transform.SetParent(parent);
        building.transform.position = position;
        building.transform.localScale = new Vector3(
            Random.Range(8f, 15f),
            Random.Range(8f, 20f),
            Random.Range(8f, 15f)
        );

        // Random building color
        Renderer buildingRenderer = building.GetComponent<Renderer>();
        if (buildingRenderer != null)
        {
            Material buildingMat = new Material(Shader.Find("Standard"));
            buildingMat.color = new Color(
                Random.Range(0.6f, 0.9f),
                Random.Range(0.6f, 0.9f),
                Random.Range(0.5f, 0.8f)
            );
            buildingRenderer.material = buildingMat;
        }
    }

    void SetupPhysicsLayers()
    {
        Debug.Log("--- Setting up physics layers ---");

        // This would typically be done in Project Settings, but we can log what needs to be set
        Debug.Log("📋 Physics Layer Setup Required:");
        Debug.Log("   Layer 8: Player");
        Debug.Log("   Layer 9: Guard");
        Debug.Log("   Layer 10: Citizen");
        Debug.Log("   Layer 11: Interactive");
        Debug.Log("   Layer 12: Shadow");
        Debug.Log("   Layer 13: IndoorArea");
        Debug.Log("⚠️ Manual setup required in Project Settings → Tags and Layers");
    }

    [ContextMenu("Quick Test Scene")]
    public void CreateQuickTestVersion()
    {
        Debug.Log("Creating quick test version of GamePlay scene...");

        SetupBasicScene();
        CreateTerrain();
        SetupLighting();
        SetupCamera();

        Debug.Log("✅ Quick test scene created - basic terrain and lighting only");
    }
#endif
}