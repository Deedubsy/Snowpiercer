using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Medieval City Builder - Creates a walled city using cube primitives
/// Player spawns at city gates, with districts and patrol routes inside walls
/// </summary>
public class MedievalCityBuilder : MonoBehaviour
{
    //[Header("City Layout")]
    public enum WallShape { Circular, Square }
    public WallShape wallShape = WallShape.Square;

    [Header("Circular Wall Settings")]
    public float cityRadius = 50f;

    [Header("Square Wall Settings")]
    public Vector2 squareWallSize = new Vector2(100f, 80f); // width x depth

    [Header("Wall Properties")]
    public float wallThickness = 2f;
    public float wallHeight = 8f;
    public float gateWidth = 6f;
    public Vector3 playerSpawnOffset = new Vector3(0, 1, -5);

    [Header("Gate Configuration")]
    public bool includeMainGate = true;
    public bool includeSecondaryGates = true;
    public bool includePosternGates = true;
    public bool includeSallyPorts = false;
    [Range(1, 4)] public int numberOfSecondaryGates = 2;

    [Header("Castle Fortifications")]
    public bool includeInnerWalls = true;
    public bool includeKeep = true;
    public bool includeInnerCourtyard = true;
    public float innerWallRadius = 25f;
    public float keepHeight = 25f;

    [Header("Terrain Generation")]
    public bool generateTerrain = true;
    public int terrainResolution = 129;
    public float terrainHeight = 5f;
    public bool addTerrainVariation = true;
    [Range(0f, 1f)] public float terrainRoughness = 0.3f;

    [Header("Districts")]
    public bool includeCastle = true;
    public bool includeCathedral = true;
    public bool includeMarketSquare = true;
    public bool includeNobleQuarter = true;
    public bool includeArtisanQuarter = true;
    public bool includeResidential = true;
    public bool includeTavernDistrict = true;
    public bool includeBarracks = true;

    [Header("Building Settings")]
    [Range(0.3f, 1.0f)] public float buildingDensity = 0.7f;
    [Range(1, 5)] public int maxBuildingsPerDistrict = 3;
    [Range(2f, 8f)] public float minBuildingHeight = 3f;
    [Range(4f, 15f)] public float maxBuildingHeight = 8f;
    [Range(3f, 12f)] public float minBuildingSize = 5f;
    [Range(6f, 20f)] public float maxBuildingSize = 10f;

    [Header("Street Layout")]
    public float streetWidth = 4f;
    public float mainRoadWidth = 6f;
    [Range(4, 12)] public int districtGridSize = 8;

    [Header("Materials & Colors")]
    public Material wallMaterial;
    public Material buildingMaterial;
    public Color wallColor = new Color(0.8f, 0.8f, 0.7f);
    public Color[] buildingColors = new Color[]
    {
        new Color(0.9f, 0.8f, 0.7f), // Cream
        new Color(0.7f, 0.6f, 0.5f), // Brown
        new Color(0.8f, 0.7f, 0.6f), // Tan
        new Color(0.6f, 0.5f, 0.4f)  // Dark Brown
    };

    [Header("Patrol & Gameplay")]
    [Range(1, 8)] public int guardsPerDistrict = 2;
    public float patrolPathWidth = 2f;
    [Range(0.1f, 0.8f)] public float hidingSpotDensity = 0.4f;

    [Header("Performance")]
    public bool combineWallMeshes = true;
    public bool combineBuildingMeshes = false;
    public int maxTotalObjects = 200;

    [Header("Debug")]
    public bool showDistrictBounds = true;
    public bool showPatrolRoutes = true;

    // Generated objects storage
    [SerializeField] private List<GameObject> generatedWalls = new List<GameObject>();
    [SerializeField] private List<GameObject> generatedBuildings = new List<GameObject>();
    [SerializeField] private List<GameObject> generatedStreets = new List<GameObject>();
    [SerializeField] private List<GameObject> generatedTerrain = new List<GameObject>();
    [SerializeField] private GameObject playerSpawn;
    [SerializeField] private Transform cityParent;

    // Building type definitions
    public enum BuildingType
    {
        Castle, Cathedral, House, Shop, Tavern, Barracks, Workshop
    }

    [System.Serializable]
    public class BuildingTemplate
    {
        public BuildingType type;
        public Vector3 size;
        public float height;
        public Color color;
        public bool hasCourtyard;
    }

    // Predefined building templates
    private readonly BuildingTemplate[] buildingTemplates = new BuildingTemplate[]
    {
        new BuildingTemplate { type = BuildingType.Castle, size = new Vector3(15, 12, 15), height = 12f, color = new Color(0.6f, 0.6f, 0.7f), hasCourtyard = true },
        new BuildingTemplate { type = BuildingType.Cathedral, size = new Vector3(12, 15, 20), height = 15f, color = new Color(0.8f, 0.8f, 0.9f), hasCourtyard = false },
        new BuildingTemplate { type = BuildingType.House, size = new Vector3(6, 5, 8), height = 5f, color = new Color(0.9f, 0.8f, 0.7f), hasCourtyard = false },
        new BuildingTemplate { type = BuildingType.Shop, size = new Vector3(8, 4, 6), height = 4f, color = new Color(0.8f, 0.7f, 0.6f), hasCourtyard = false },
        new BuildingTemplate { type = BuildingType.Tavern, size = new Vector3(10, 6, 10), height = 6f, color = new Color(0.7f, 0.5f, 0.3f), hasCourtyard = false },
        new BuildingTemplate { type = BuildingType.Barracks, size = new Vector3(12, 4, 8), height = 4f, color = new Color(0.6f, 0.5f, 0.5f), hasCourtyard = true },
        new BuildingTemplate { type = BuildingType.Workshop, size = new Vector3(7, 4, 9), height = 4f, color = new Color(0.7f, 0.6f, 0.5f), hasCourtyard = false }
    };

    void Start()
    {
        // Don't auto-generate at runtime
    }

    public void GenerateCompleteCity()
    {
        ClearAll();
        SetupCityParent();

        // Generate terrain first as foundation
        if (generateTerrain)
        {
            GenerateTerrain();
        }

        GenerateWalls();
        GenerateStreets();
        GenerateBuildings();
        SetupPlayerSpawn();
        SetupPatrolRoutes();

        Debug.Log($"Generated complete medieval city with {GetTotalObjectCount()} objects");
    }

    public void ClearAll()
    {
        ClearWalls();
        ClearBuildings();
        ClearStreets();
        ClearTerrain();
        if (playerSpawn != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(playerSpawn);
#else
            Destroy(playerSpawn);
#endif
            playerSpawn = null;
        }
        if (cityParent != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(cityParent.gameObject);
#else
            Destroy(cityParent.gameObject);
#endif
            cityParent = null;
        }
    }

    void SetupCityParent()
    {
        if (cityParent == null)
        {
            GameObject parent = new GameObject("Generated_Medieval_City");
            parent.transform.position = transform.position;
            cityParent = parent.transform;
        }
    }

    public void GenerateWalls()
    {
        SetupCityParent();
        Transform wallParent = CreateCategoryParent("Walls");

        if (wallShape == WallShape.Circular)
        {
            GenerateCircularWalls(wallParent);
        }
        else
        {
            GenerateSquareWalls(wallParent);
        }

        // Add inner walls and castle fortifications
        if (includeInnerWalls || includeKeep)
        {
            CreateInnerFortifications(wallParent);
        }

        Debug.Log($"Generated {generatedWalls.Count} wall segments ({wallShape} shape)");
    }

    void GenerateCircularWalls(Transform parent)
    {
        // Generate circular wall with multiple gate types
        int wallSegments = 32;
        float angleStep = 360f / wallSegments;

        // Calculate gate positions
        List<GateInfo> gates = GetCircularGatePositions();

        for (int i = 0; i < wallSegments; i++)
        {
            float angle = i * angleStep;
            float nextAngle = (i + 1) * angleStep;

            // Check if this segment should be skipped for any gate
            bool isGateArea = IsSegmentInGateArea(angle, nextAngle, gates);
            if (isGateArea) continue;

            // Calculate wall segment position
            Vector3 startPos = GetCirclePosition(angle, cityRadius);
            Vector3 endPos = GetCirclePosition(nextAngle, cityRadius);
            Vector3 wallPos = Vector3.Lerp(startPos, endPos, 0.5f);

            // Create wall segment
            GameObject wallSegment = CreateCube($"Wall_Segment_{i}", wallPos, parent);

            // Size the wall segment
            float segmentLength = Vector3.Distance(startPos, endPos);
            wallSegment.transform.localScale = new Vector3(segmentLength, wallHeight, wallThickness);
            wallSegment.transform.LookAt(wallPos + (endPos - startPos).normalized);
            wallSegment.transform.Rotate(0, 90, 0); // Adjust rotation for proper thickness orientation

            ApplyMaterial(wallSegment, wallColor, true);
            generatedWalls.Add(wallSegment);
        }

        // Create all gates for circular walls
        CreateCircularGates(gates, parent);

        // Add defensive towers around circular walls
        CreateCircularDefensiveTowers(parent);
    }

    void GenerateSquareWalls(Transform parent)
    {
        float halfWidth = squareWallSize.x * 0.5f;
        float halfDepth = squareWallSize.y * 0.5f;

        // Define wall corners
        Vector3[] corners = new Vector3[]
        {
            new Vector3(-halfWidth, 0, -halfDepth), // Southwest
            new Vector3(halfWidth, 0, -halfDepth),  // Southeast
            new Vector3(halfWidth, 0, halfDepth),   // Northeast
            new Vector3(-halfWidth, 0, halfDepth)   // Northwest
        };

        // Create walls between corners
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 startCorner = corners[i];
            Vector3 endCorner = corners[(i + 1) % corners.Length];

            // Check if this is the south wall (where gate goes)
            bool isSouthWall = (i == 0); // Between SW and SE corners

            if (isSouthWall)
            {
                CreateSquareWallWithGate(startCorner, endCorner, parent, i);
            }
            else
            {
                CreateSquareWallSegment(startCorner, endCorner, parent, i);
            }
        }

        // Create all gates for square walls
        List<GateInfo> gates = GetSquareGatePositions();
        CreateSquareGates(gates, parent);

        // Add corner towers
        CreateCornerTowers(corners, parent);

        // Add defensive towers along square walls
        CreateSquareDefensiveTowers(corners, parent);
    }

    void CreateSquareWallSegment(Vector3 start, Vector3 end, Transform parent, int wallIndex)
    {
        Vector3 wallPos = Vector3.Lerp(start, end, 0.5f);
        wallPos.y = wallHeight * 0.5f;

        GameObject wall = CreateCube($"Wall_{wallIndex}", wallPos, parent);

        float wallLength = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;

        // Scale wall
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            // Horizontal wall (East-West)
            wall.transform.localScale = new Vector3(wallLength, wallHeight, wallThickness);
        }
        else
        {
            // Vertical wall (North-South)
            wall.transform.localScale = new Vector3(wallThickness, wallHeight, wallLength);
        }

        ApplyMaterial(wall, wallColor, true);
        generatedWalls.Add(wall);
    }

    void CreateSquareWallWithGate(Vector3 start, Vector3 end, Transform parent, int wallIndex)
    {
        Vector3 wallCenter = Vector3.Lerp(start, end, 0.5f);
        float wallLength = Vector3.Distance(start, end);

        // Create left wall segment (from start to gate)
        Vector3 gateStart = wallCenter - Vector3.right * (gateWidth * 0.5f);
        Vector3 leftWallPos = Vector3.Lerp(start, gateStart, 0.5f);
        leftWallPos.y = wallHeight * 0.5f;

        GameObject leftWall = CreateCube($"Wall_{wallIndex}_Left", leftWallPos, parent);
        float leftWallLength = Vector3.Distance(start, gateStart);
        leftWall.transform.localScale = new Vector3(leftWallLength, wallHeight, wallThickness);
        ApplyMaterial(leftWall, wallColor, true);
        generatedWalls.Add(leftWall);

        // Create right wall segment (from gate to end)
        Vector3 gateEnd = wallCenter + Vector3.right * (gateWidth * 0.5f);
        Vector3 rightWallPos = Vector3.Lerp(gateEnd, end, 0.5f);
        rightWallPos.y = wallHeight * 0.5f;

        GameObject rightWall = CreateCube($"Wall_{wallIndex}_Right", rightWallPos, parent);
        float rightWallLength = Vector3.Distance(gateEnd, end);
        rightWall.transform.localScale = new Vector3(rightWallLength, wallHeight, wallThickness);
        ApplyMaterial(rightWall, wallColor, true);
        generatedWalls.Add(rightWall);

        // Create simple gate posts for this wall segment
        CreateSimpleGatePostsForWall(gateStart, gateEnd, parent);
    }

    void CreateCornerTowers(Vector3[] corners, Transform parent)
    {
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 towerPos = corners[i];
            towerPos.y = wallHeight * 0.75f; // Slightly taller than walls

            GameObject tower = CreateCube($"Corner_Tower_{i}", towerPos, parent);
            tower.transform.localScale = new Vector3(wallThickness * 2f, wallHeight * 1.5f, wallThickness * 2f);
            ApplyMaterial(tower, wallColor, true);
            generatedWalls.Add(tower);
        }
    }

    void CreateCircularDefensiveTowers(Transform parent)
    {
        // Place defensive towers around the circular wall at regular intervals
        int towerCount = 8; // Towers every 45 degrees
        for (int i = 0; i < towerCount; i++)
        {
            float angle = i * (360f / towerCount);

            // Skip tower near the gate (facing south)
            if (angle >= 170f && angle <= 190f) continue;

            Vector3 towerPos = GetCirclePosition(angle, cityRadius + wallThickness * 0.5f);
            towerPos.y = wallHeight * 0.75f;

            GameObject tower = CreateDefensiveTower($"Defensive_Tower_{i}", towerPos, parent, TowerType.Watchtower);
        }

        // Add special guard towers at strategic points
        CreateSpecialTowers(parent, TowerType.GuardTower);
    }

    void CreateSquareDefensiveTowers(Vector3[] corners, Transform parent)
    {
        // Place towers along each wall between corners
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 startCorner = corners[i];
            Vector3 endCorner = corners[(i + 1) % corners.Length];

            // Skip south wall where gate is located
            bool isSouthWall = (i == 0);
            if (isSouthWall) continue;

            // Calculate tower positions along the wall
            float wallLength = Vector3.Distance(startCorner, endCorner);
            int towerCount = Mathf.Max(1, Mathf.FloorToInt(wallLength / 20f)); // One tower per 20 units

            for (int j = 1; j <= towerCount; j++)
            {
                float t = j / (float)(towerCount + 1);
                Vector3 towerPos = Vector3.Lerp(startCorner, endCorner, t);

                // Move tower slightly outward from wall
                Vector3 wallDirection = (endCorner - startCorner).normalized;
                Vector3 outwardDirection = new Vector3(-wallDirection.z, 0, wallDirection.x);
                towerPos += outwardDirection * wallThickness * 0.5f;
                towerPos.y = wallHeight * 0.75f;

                GameObject tower = CreateDefensiveTower($"Wall_Tower_{i}_{j}", towerPos, parent, TowerType.Watchtower);
            }
        }

        // Add special guard towers at key positions
        CreateSpecialTowers(parent, TowerType.GuardTower);
    }

    enum TowerType { Watchtower, GuardTower, SignalTower }

    GameObject CreateDefensiveTower(string name, Vector3 position, Transform parent, TowerType towerType)
    {
        GameObject tower = CreateCube(name, position, parent);

        // Different tower types have different sizes and features
        switch (towerType)
        {
            case TowerType.Watchtower:
                tower.transform.localScale = new Vector3(wallThickness * 1.5f, wallHeight * 1.8f, wallThickness * 1.5f);
                break;
            case TowerType.GuardTower:
                tower.transform.localScale = new Vector3(wallThickness * 2.5f, wallHeight * 2.2f, wallThickness * 2.5f);
                // Add battlements on top
                CreateBattlements(tower, parent);
                break;
            case TowerType.SignalTower:
                tower.transform.localScale = new Vector3(wallThickness * 2f, wallHeight * 2.5f, wallThickness * 2f);
                break;
        }

        ApplyMaterial(tower, wallColor, true);
        generatedWalls.Add(tower);

        return tower;
    }

    void CreateSpecialTowers(Transform parent, TowerType towerType)
    {
        if (wallShape == WallShape.Circular)
        {
            // Place guard towers at strategic compass points
            float[] angles = { 0f, 90f, 270f }; // North, East, West (South has gate)
            for (int i = 0; i < angles.Length; i++)
            {
                Vector3 towerPos = GetCirclePosition(angles[i], cityRadius + wallThickness);
                towerPos.y = wallHeight * 0.75f;
                CreateDefensiveTower($"Special_Tower_{i}", towerPos, parent, towerType);
            }
        }
        else
        {
            // Place guard towers at key square positions (not at corners)
            Vector3[] keyPositions = new Vector3[]
            {
                new Vector3(0, 0, squareWallSize.y * 0.5f), // North center
                new Vector3(squareWallSize.x * 0.5f, 0, 0),  // East center
                new Vector3(-squareWallSize.x * 0.5f, 0, 0)  // West center
            };

            for (int i = 0; i < keyPositions.Length; i++)
            {
                Vector3 towerPos = keyPositions[i];
                towerPos.y = wallHeight * 0.75f;
                CreateDefensiveTower($"Special_Square_Tower_{i}", towerPos, parent, towerType);
            }
        }
    }

    void CreateBattlements(GameObject tower, Transform parent)
    {
        // Add small battlement cubes on top of guard towers
        Vector3 towerTop = tower.transform.position;
        towerTop.y += tower.transform.localScale.y * 0.5f + 0.5f;

        for (int i = 0; i < 4; i++)
        {
            Vector3 battlementPos = towerTop + GetCirclePosition(i * 90f, wallThickness * 0.7f);
            GameObject battlement = CreateCube($"{tower.name}_Battlement_{i}", battlementPos, parent);
            battlement.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            ApplyMaterial(battlement, wallColor, true);
            generatedWalls.Add(battlement);
        }
    }

    // Gate System Implementation for Phase 3
    [System.Serializable]
    public class GateInfo
    {
        public GateType type;
        public float angle; // For circular walls
        public Vector3 position; // For square walls
        public float width;
        public string name;
        public bool hasDrawbridge;
        public bool hasPortcullis;
        public bool hasGatehouse;
    }

    public enum GateType
    {
        Main,         // Large ceremonial entrance
        Secondary,    // Standard gates for traffic
        Postern,      // Small hidden gates
        Sally,        // Military sortie gates
        Water         // Gates near water features
    }

    List<GateInfo> GetCircularGatePositions()
    {
        List<GateInfo> gates = new List<GateInfo>();

        // Main gate (always south)
        if (includeMainGate)
        {
            gates.Add(new GateInfo
            {
                type = GateType.Main,
                angle = 180f,
                width = gateWidth * 1.5f,
                name = "Main_Gate",
                hasDrawbridge = true,
                hasPortcullis = true,
                hasGatehouse = true
            });
        }

        // Secondary gates
        if (includeSecondaryGates)
        {
            float[] secondaryAngles = { 90f, 270f }; // East and West
            for (int i = 0; i < Mathf.Min(numberOfSecondaryGates, secondaryAngles.Length); i++)
            {
                gates.Add(new GateInfo
                {
                    type = GateType.Secondary,
                    angle = secondaryAngles[i],
                    width = gateWidth,
                    name = $"Secondary_Gate_{i}",
                    hasPortcullis = true,
                    hasGatehouse = false
                });
            }
        }

        // Postern gates (small hidden gates)
        if (includePosternGates)
        {
            float[] posternAngles = { 45f, 315f }; // Northeast and Northwest
            for (int i = 0; i < posternAngles.Length; i++)
            {
                gates.Add(new GateInfo
                {
                    type = GateType.Postern,
                    angle = posternAngles[i],
                    width = gateWidth * 0.6f,
                    name = $"Postern_Gate_{i}",
                    hasPortcullis = false,
                    hasGatehouse = false
                });
            }
        }

        // Sally ports (military gates)
        if (includeSallyPorts)
        {
            gates.Add(new GateInfo
            {
                type = GateType.Sally,
                angle = 0f, // North
                width = gateWidth * 0.8f,
                name = "Sally_Port",
                hasPortcullis = true,
                hasGatehouse = false
            });
        }

        return gates;
    }

    List<GateInfo> GetSquareGatePositions()
    {
        List<GateInfo> gates = new List<GateInfo>();

        // Main gate (always south center)
        if (includeMainGate)
        {
            gates.Add(new GateInfo
            {
                type = GateType.Main,
                position = new Vector3(0, 0, -squareWallSize.y * 0.5f),
                width = gateWidth * 1.5f,
                name = "Main_Gate",
                hasDrawbridge = true,
                hasPortcullis = true,
                hasGatehouse = true
            });
        }

        // Secondary gates on other walls
        if (includeSecondaryGates)
        {
            Vector3[] secondaryPositions = {
                new Vector3(squareWallSize.x * 0.5f, 0, 0), // East
                new Vector3(-squareWallSize.x * 0.5f, 0, 0) // West
            };

            for (int i = 0; i < Mathf.Min(numberOfSecondaryGates, secondaryPositions.Length); i++)
            {
                gates.Add(new GateInfo
                {
                    type = GateType.Secondary,
                    position = secondaryPositions[i],
                    width = gateWidth,
                    name = $"Secondary_Gate_{i}",
                    hasPortcullis = true,
                    hasGatehouse = false
                });
            }
        }

        // Postern gates (small hidden gates in corners)
        if (includePosternGates)
        {
            Vector3[] posternPositions = {
                new Vector3(squareWallSize.x * 0.3f, 0, squareWallSize.y * 0.5f), // North-East
                new Vector3(-squareWallSize.x * 0.3f, 0, squareWallSize.y * 0.5f) // North-West
            };

            for (int i = 0; i < posternPositions.Length; i++)
            {
                gates.Add(new GateInfo
                {
                    type = GateType.Postern,
                    position = posternPositions[i],
                    width = gateWidth * 0.6f,
                    name = $"Postern_Gate_{i}",
                    hasPortcullis = false,
                    hasGatehouse = false
                });
            }
        }

        return gates;
    }

    bool IsSegmentInGateArea(float startAngle, float endAngle, List<GateInfo> gates)
    {
        foreach (var gate in gates)
        {
            float gateAngleRange = (gate.width / (2 * Mathf.PI * cityRadius)) * 360f;
            float gateStartAngle = gate.angle - gateAngleRange;
            float gateEndAngle = gate.angle + gateAngleRange;

            // Handle angle wraparound
            if (gateStartAngle < 0) gateStartAngle += 360f;
            if (gateEndAngle > 360f) gateEndAngle -= 360f;

            // Check if segment overlaps with gate area
            if ((startAngle >= gateStartAngle && startAngle <= gateEndAngle) ||
                (endAngle >= gateStartAngle && endAngle <= gateEndAngle) ||
                (startAngle <= gateStartAngle && endAngle >= gateEndAngle))
            {
                return true;
            }
        }
        return false;
    }

    void CreateCircularGates(List<GateInfo> gates, Transform parent)
    {
        foreach (var gate in gates)
        {
            CreateGateStructure(gate, parent);
        }
    }

    void CreateSquareGates(List<GateInfo> gates, Transform parent)
    {
        foreach (var gate in gates)
        {
            CreateGateStructure(gate, parent);
        }
    }

    void CreateGateStructure(GateInfo gate, Transform parent)
    {
        Vector3 gatePosition;

        if (wallShape == WallShape.Circular)
        {
            gatePosition = GetCirclePosition(gate.angle, cityRadius);
        }
        else
        {
            gatePosition = gate.position;
        }

        // Create gate parent object
        GameObject gateParent = new GameObject(gate.name);
        gateParent.transform.SetParent(parent);
        gateParent.transform.position = gatePosition;

        // Create different gate components based on type
        switch (gate.type)
        {
            case GateType.Main:
                CreateMainGateStructure(gate, gateParent.transform);
                break;
            case GateType.Secondary:
                CreateSecondaryGateStructure(gate, gateParent.transform);
                break;
            case GateType.Postern:
                CreatePosternGateStructure(gate, gateParent.transform);
                break;
            case GateType.Sally:
                CreateSallyPortStructure(gate, gateParent.transform);
                break;
        }
    }

    void CreateGatePosts(Transform parent)
    {
        if (wallShape == WallShape.Circular)
        {
            CreateCircularGatePosts(parent);
        }
        else
        {
            CreateSquareGatePosts(parent);
        }
    }

    void CreateCircularGatePosts(Transform parent)
    {
        Vector3 gateCenter = GetCirclePosition(180f, cityRadius);
        float postWidth = 1f;
        float postHeight = wallHeight * 1.2f;

        // Left gate post
        Vector3 leftPostPos = gateCenter + Vector3.left * (gateWidth * 0.5f + postWidth * 0.5f);
        GameObject leftPost = CreateCube("Gate_Post_Left", leftPostPos, parent);
        leftPost.transform.localScale = new Vector3(postWidth, postHeight, wallThickness * 1.5f);
        ApplyMaterial(leftPost, wallColor, true);
        generatedWalls.Add(leftPost);

        // Right gate post
        Vector3 rightPostPos = gateCenter + Vector3.right * (gateWidth * 0.5f + postWidth * 0.5f);
        GameObject rightPost = CreateCube("Gate_Post_Right", rightPostPos, parent);
        rightPost.transform.localScale = new Vector3(postWidth, postHeight, wallThickness * 1.5f);
        ApplyMaterial(rightPost, wallColor, true);
        generatedWalls.Add(rightPost);
    }

    void CreateSquareGatePosts(Transform parent)
    {
        // Gate is on the south wall for square layout
        Vector3 gateCenter = new Vector3(0, 0, -squareWallSize.y * 0.5f);
        float postWidth = 1f;
        float postHeight = wallHeight * 1.2f;

        // Left gate post
        Vector3 leftPostPos = gateCenter + Vector3.left * (gateWidth * 0.5f + postWidth * 0.5f);
        GameObject leftPost = CreateCube("Gate_Post_Left", leftPostPos, parent);
        leftPost.transform.localScale = new Vector3(postWidth, postHeight, wallThickness * 1.5f);
        ApplyMaterial(leftPost, wallColor, true);
        generatedWalls.Add(leftPost);

        // Right gate post
        Vector3 rightPostPos = gateCenter + Vector3.right * (gateWidth * 0.5f + postWidth * 0.5f);
        GameObject rightPost = CreateCube("Gate_Post_Right", rightPostPos, parent);
        rightPost.transform.localScale = new Vector3(postWidth, postHeight, wallThickness * 1.5f);
        ApplyMaterial(rightPost, wallColor, true);
        generatedWalls.Add(rightPost);
    }

    public void GenerateStreets()
    {
        SetupCityParent();
        Transform streetParent = CreateCategoryParent("Streets");

        if (wallShape == WallShape.Circular)
        {
            GenerateCircularStreetNetwork(streetParent);
        }
        else
        {
            GenerateSquareStreetNetwork(streetParent);
        }

        Debug.Log($"Generated {generatedStreets.Count} street segments");
    }

    void GenerateCircularStreetNetwork(Transform parent)
    {
        // Main arterial roads from center to gates
        Vector3 gatePosition = GetCirclePosition(180f, cityRadius); // South gate
        CreateMainRoad(Vector3.zero, gatePosition, parent, "Main_Gate_Road");

        // Ring roads at different radii
        CreateRingRoad(cityRadius * 0.3f, parent, "Inner_Ring");
        CreateRingRoad(cityRadius * 0.6f, parent, "Outer_Ring");

        // Radial roads connecting ring roads
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 innerPoint = GetCirclePosition(angle, cityRadius * 0.3f);
            Vector3 outerPoint = GetCirclePosition(angle, cityRadius * 0.8f);
            CreateMainRoad(innerPoint, outerPoint, parent, $"Radial_Road_{i}");
        }

        // District connecting roads
        ConnectDistrictsWithStreets(parent);
    }

    void GenerateSquareStreetNetwork(Transform parent)
    {
        // Main street from gate to castle
        Vector3 gatePos = new Vector3(0, 0, -squareWallSize.y * 0.5f);
        Vector3 castlePos = new Vector3(0, 0, squareWallSize.y * 0.25f);
        CreateMainRoad(gatePos, castlePos, parent, "Main_Castle_Road");

        // Cross streets dividing the city into quarters
        float halfWidth = squareWallSize.x * 0.4f;
        float halfDepth = squareWallSize.y * 0.4f;

        // East-West main street
        CreateMainRoad(new Vector3(-halfWidth, 0, 0), new Vector3(halfWidth, 0, 0), parent, "Main_EW_Street");

        // Additional North-South streets
        CreateMainRoad(new Vector3(-halfWidth * 0.5f, 0, -halfDepth),
                      new Vector3(-halfWidth * 0.5f, 0, halfDepth), parent, "West_NS_Street");
        CreateMainRoad(new Vector3(halfWidth * 0.5f, 0, -halfDepth),
                      new Vector3(halfWidth * 0.5f, 0, halfDepth), parent, "East_NS_Street");

        // Secondary connecting streets
        CreateMainRoad(new Vector3(-halfWidth, 0, halfDepth * 0.5f),
                      new Vector3(halfWidth, 0, halfDepth * 0.5f), parent, "North_EW_Street");
        CreateMainRoad(new Vector3(-halfWidth, 0, -halfDepth * 0.5f),
                      new Vector3(halfWidth, 0, -halfDepth * 0.5f), parent, "South_EW_Street");

        // District connecting roads
        ConnectDistrictsWithStreets(parent);
    }

    void CreateRingRoad(float radius, Transform parent, string name)
    {
        int segments = 16;
        for (int i = 0; i < segments; i++)
        {
            float startAngle = (i * 360f / segments);
            float endAngle = ((i + 1) * 360f / segments);

            Vector3 start = GetCirclePosition(startAngle, radius);
            Vector3 end = GetCirclePosition(endAngle, radius);

            CreateMainRoad(start, end, parent, $"{name}_Segment_{i}");
        }
    }

    void ConnectDistrictsWithStreets(Transform parent)
    {
        var districts = GetDistrictCenters();
        var districtPositions = new List<Vector3>(districts.Values);

        // Connect market square to all other districts
        Vector3 marketCenter = districts["MarketSquare"];

        foreach (var district in districts)
        {
            if (district.Key != "MarketSquare")
            {
                CreateSecondaryStreet(marketCenter, district.Value, parent, $"Market_to_{district.Key}");
            }
        }

        // Connect adjacent districts
        ConnectAdjacentDistricts(districts, parent);
    }

    void ConnectAdjacentDistricts(Dictionary<string, Vector3> districts, Transform parent)
    {
        // Define which districts should be connected directly
        var connections = new List<(string, string)>
        {
            ("Castle", "Cathedral"),
            ("Cathedral", "NobleQuarter"),
            ("NobleQuarter", "Residential"),
            ("ArtisanQuarter", "TavernDistrict"),
            ("TavernDistrict", "Barracks"),
            ("Barracks", "Residential")
        };

        foreach (var (from, to) in connections)
        {
            if (districts.ContainsKey(from) && districts.ContainsKey(to))
            {
                CreateSecondaryStreet(districts[from], districts[to], parent, $"{from}_to_{to}");
            }
        }
    }

    void CreateSecondaryStreet(Vector3 start, Vector3 end, Transform parent, string name)
    {
        // Create narrower streets for district connections
        Vector3 roadPos = Vector3.Lerp(start, end, 0.5f);
        roadPos.y = -0.05f; // Slightly above main roads

        GameObject road = CreateCube(name, roadPos, parent);

        float roadLength = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;

        road.transform.localScale = new Vector3(
            Mathf.Abs(direction.x) > 0.5f ? roadLength : streetWidth,
            0.05f,
            Mathf.Abs(direction.z) > 0.5f ? roadLength : streetWidth
        );

        // Align with direction
        road.transform.LookAt(road.transform.position + direction);

        ApplyMaterial(road, new Color(0.5f, 0.5f, 0.5f), false); // Lighter gray for secondary streets
        generatedStreets.Add(road);
    }

    void CreateMainRoad(Vector3 start, Vector3 end, Transform parent, string name = "Main_Road")
    {
        Vector3 roadPos = Vector3.Lerp(start, end, 0.5f);
        roadPos.y = -0.1f; // Slightly below ground level

        GameObject road = CreateCube(name, roadPos, parent);

        float roadLength = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;

        road.transform.localScale = new Vector3(
            Mathf.Abs(direction.x) > 0.5f ? roadLength : mainRoadWidth,
            0.1f,
            Mathf.Abs(direction.z) > 0.5f ? roadLength : mainRoadWidth
        );

        // Align road with direction for better visual representation
        if (roadLength > 0.1f)
        {
            road.transform.LookAt(road.transform.position + direction);
        }

        ApplyMaterial(road, new Color(0.4f, 0.4f, 0.4f), false); // Dark gray for roads
        generatedStreets.Add(road);
    }

    public void GenerateBuildings()
    {
        SetupCityParent();
        Transform buildingParent = CreateCategoryParent("Buildings");

        // Debug street information
        Debug.Log($"🛣️ Streets available for collision checking: {generatedStreets.Count}");
        for (int i = 0; i < generatedStreets.Count; i++)
        {
            if (generatedStreets[i] != null)
            {
                Debug.Log($"   Street {i}: {generatedStreets[i].name} at {generatedStreets[i].transform.position} scale {generatedStreets[i].transform.localScale}");
            }
        }

        // Define district centers and generate buildings for each
        var districts = GetDistrictCenters();

        foreach (var district in districts)
        {
            if (ShouldIncludeDistrict(district.Key))
            {
                Debug.Log($"🏘️ Including district: {district.Key} at {district.Value}");
                GenerateBuildingsForDistrict(district.Key, district.Value, buildingParent);
            }
            else
            {
                Debug.Log($"❌ Skipping district: {district.Key}");
            }
        }

        Debug.Log($"Generated {generatedBuildings.Count} buildings across districts");
    }

    Dictionary<string, Vector3> GetDistrictCenters()
    {
        if (wallShape == WallShape.Circular)
        {
            return GetCircularDistrictCenters();
        }
        else
        {
            return GetSquareDistrictCenters();
        }
    }

    Dictionary<string, Vector3> GetCircularDistrictCenters()
    {
        float districtRadius = cityRadius * 0.3f;
        return new Dictionary<string, Vector3>
        {
            { "Castle", Vector3.forward * districtRadius * 1.5f },
            { "Cathedral", new Vector3(districtRadius, 0, districtRadius) },
            { "MarketSquare", Vector3.zero },
            { "NobleQuarter", new Vector3(-districtRadius, 0, districtRadius) },
            { "ArtisanQuarter", Vector3.left * districtRadius },
            { "Residential", Vector3.right * districtRadius },
            { "TavernDistrict", new Vector3(-districtRadius, 0, -districtRadius) },
            { "Barracks", new Vector3(districtRadius, 0, -districtRadius) }
        };
    }

    Dictionary<string, Vector3> GetSquareDistrictCenters()
    {
        // Medieval cities typically had castle on elevated position, market at center
        // Religious buildings near castle, defensive structures at gates
        float quarterWidth = squareWallSize.x * 0.25f;
        float quarterHeight = squareWallSize.y * 0.25f;

        return new Dictionary<string, Vector3>
        {
            // Castle in back quarter (north), elevated position
            { "Castle", new Vector3(0, 0, quarterHeight) },

            // Cathedral near castle for protection
            { "Cathedral", new Vector3(-quarterWidth * 0.7f, 0, quarterHeight * 0.7f) },

            // Market square at city center - heart of medieval city
            { "MarketSquare", new Vector3(0, 0, 0) },

            // Noble quarter near castle for proximity to power
            { "NobleQuarter", new Vector3(quarterWidth * 0.7f, 0, quarterHeight * 0.7f) },

            // Artisan quarter near market for trade
            { "ArtisanQuarter", new Vector3(-quarterWidth, 0, 0) },

            // Residential spread across available space
            { "Residential", new Vector3(quarterWidth, 0, 0) },

            // Tavern district near gate for travelers
            { "TavernDistrict", new Vector3(-quarterWidth * 0.7f, 0, -quarterHeight * 0.7f) },

            // Barracks near gate for defense
            { "Barracks", new Vector3(quarterWidth * 0.7f, 0, -quarterHeight * 0.7f) }
        };
    }

    bool ShouldIncludeDistrict(string districtName)
    {
        return districtName switch
        {
            "Castle" => includeCastle,
            "Cathedral" => includeCathedral,
            "MarketSquare" => includeMarketSquare,
            "NobleQuarter" => includeNobleQuarter,
            "ArtisanQuarter" => includeArtisanQuarter,
            "Residential" => includeResidential,
            "TavernDistrict" => includeTavernDistrict,
            "Barracks" => includeBarracks,
            _ => false
        };
    }

    void GenerateBuildingsForDistrict(string districtName, Vector3 center, Transform parent)
    {
        Transform districtParent = new GameObject($"District_{districtName}").transform;
        districtParent.SetParent(parent);
        districtParent.position = center;

        BuildingType primaryType = GetBuildingTypeForDistrict(districtName);

        // Different districts have different building counts and sizes
        DistrictInfo districtInfo = GetDistrictInfo(districtName);
        int buildingCount = Mathf.RoundToInt(districtInfo.buildingCount * buildingDensity);

        Debug.Log($"🏘️ Generating {buildingCount} buildings for {districtName} at {center} (density: {buildingDensity})");

        int successfulBuildings = 0;
        int maxRetries = 3;

        for (int i = 0; i < buildingCount; i++)
        {
            bool buildingPlaced = false;

            // Try multiple positions for each building
            for (int retry = 0; retry < maxRetries && !buildingPlaced; retry++)
            {
                Vector3 buildingPos = center + GetBuildingOffset(i + retry * buildingCount, buildingCount, districtName);

                // Check if within district boundaries first
                if (!IsWithinDistrictBounds(buildingPos, center, districtInfo.radius))
                {
                    Debug.Log($"❌ Building {i} (retry {retry}) outside district bounds: {buildingPos}");
                    continue;
                }

                // Check if inside city walls
                if (!IsPositionInsideWalls(buildingPos))
                {
                    Debug.Log($"❌ Building {i} (retry {retry}) outside city walls: {buildingPos}");
                    continue;
                }

                // Avoid placing buildings on roads (only if roads exist)
                if (generatedStreets.Count > 0 && IsPositionOnRoad(buildingPos))
                {
                    Debug.Log($"❌ Building {i} (retry {retry}) on road: {buildingPos}");
                    continue;
                }

                // Avoid placing too close to walls
                if (IsPositionTooCloseToWalls(buildingPos))
                {
                    Debug.Log($"❌ Building {i} (retry {retry}) too close to walls: {buildingPos}");
                    continue;
                }

                Debug.Log($"✅ Creating building {i} at {buildingPos} (attempt {retry + 1})");
                GameObject building = CreateBuildingOfType(primaryType, buildingPos, districtParent, i);
                if (building != null)
                {
                    generatedBuildings.Add(building);
                    Debug.Log($"✅ Successfully created {building.name}");
                    buildingPlaced = true;
                    successfulBuildings++;
                }
                else
                {
                    Debug.LogWarning($"❌ Failed to create building of type {primaryType}");
                }
            }

            if (!buildingPlaced)
            {
                Debug.LogWarning($"❌ Could not place building {i} in district {districtName} after {maxRetries} attempts");
            }
        }

        Debug.Log($"📊 District {districtName}: {successfulBuildings}/{buildingCount} buildings successfully placed");

        // Add special district features
        AddDistrictSpecialFeatures(districtName, center, districtParent);
    }

    struct DistrictInfo
    {
        public int buildingCount;
        public float radius;
        public bool hasPlaza;
        public bool hasSpecialBuilding;
    }

    DistrictInfo GetDistrictInfo(string districtName)
    {
        return districtName switch
        {
            "Castle" => new DistrictInfo { buildingCount = 1, radius = 15f, hasPlaza = false, hasSpecialBuilding = true },
            "Cathedral" => new DistrictInfo { buildingCount = 2, radius = 12f, hasPlaza = true, hasSpecialBuilding = true },
            "MarketSquare" => new DistrictInfo { buildingCount = 8, radius = 20f, hasPlaza = true, hasSpecialBuilding = false },
            "NobleQuarter" => new DistrictInfo { buildingCount = 4, radius = 15f, hasPlaza = false, hasSpecialBuilding = false },
            "ArtisanQuarter" => new DistrictInfo { buildingCount = 6, radius = 18f, hasPlaza = false, hasSpecialBuilding = false },
            "Residential" => new DistrictInfo { buildingCount = 10, radius = 20f, hasPlaza = false, hasSpecialBuilding = false },
            "TavernDistrict" => new DistrictInfo { buildingCount = 5, radius = 15f, hasPlaza = false, hasSpecialBuilding = false },
            "Barracks" => new DistrictInfo { buildingCount = 3, radius = 12f, hasPlaza = true, hasSpecialBuilding = true },
            _ => new DistrictInfo { buildingCount = 5, radius = 15f, hasPlaza = false, hasSpecialBuilding = false }
        };
    }

    bool IsWithinDistrictBounds(Vector3 position, Vector3 center, float radius)
    {
        return Vector3.Distance(position, center) <= radius;
    }

    void AddDistrictSpecialFeatures(string districtName, Vector3 center, Transform parent)
    {
        DistrictInfo info = GetDistrictInfo(districtName);

        // Add central plaza for districts that have them
        if (info.hasPlaza)
        {
            CreateDistrictPlaza(districtName, center, parent);
        }

        // Add special buildings (wells, monuments, etc.)
        if (districtName == "MarketSquare")
        {
            CreateMarketStalls(center, parent);
        }
        else if (districtName == "Barracks")
        {
            CreateTrainingGround(center, parent);
        }
    }

    void CreateDistrictPlaza(string districtName, Vector3 center, Transform parent)
    {
        GameObject plaza = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        plaza.name = $"{districtName}_Plaza";
        plaza.transform.SetParent(parent);
        plaza.transform.position = center;
        plaza.transform.localScale = new Vector3(8f, 0.2f, 8f);

        // Apply stone plaza material
        ApplyMaterial(plaza, new Color(0.7f, 0.7f, 0.6f), false);
    }

    void CreateMarketStalls(Vector3 center, Transform parent)
    {
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f;
            Vector3 stallPos = center + GetCirclePosition(angle, 6f);

            GameObject stall = CreateCube($"Market_Stall_{i}", stallPos, parent);
            stall.transform.localScale = new Vector3(3f, 2f, 3f);
            ApplyMaterial(stall, new Color(0.8f, 0.6f, 0.4f), false); // Wood color
        }
    }

    void CreateTrainingGround(Vector3 center, Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Training_Ground";
        ground.transform.SetParent(parent);
        ground.transform.position = center + new Vector3(0, -0.1f, 0);
        ground.transform.localScale = new Vector3(12f, 0.1f, 12f);

        ApplyMaterial(ground, new Color(0.6f, 0.5f, 0.3f), false); // Dirt color
    }

    BuildingType GetBuildingTypeForDistrict(string districtName)
    {
        return districtName switch
        {
            "Castle" => BuildingType.Castle,
            "Cathedral" => BuildingType.Cathedral,
            "MarketSquare" => BuildingType.Shop,
            "NobleQuarter" => BuildingType.House,
            "ArtisanQuarter" => BuildingType.Workshop,
            "Residential" => BuildingType.House,
            "TavernDistrict" => BuildingType.Tavern,
            "Barracks" => BuildingType.Barracks,
            _ => BuildingType.House
        };
    }

    Vector3 GetBuildingOffset(int index, int totalBuildings, string districtName)
    {
        if (districtName == "Castle" || districtName == "Cathedral")
        {
            // Special buildings get centered position
            return Vector3.zero;
        }

        // Use grid-based placement with collision detection
        return GetGridBasedBuildingPosition(index, totalBuildings, districtName);
    }

    Vector3 GetGridBasedBuildingPosition(int index, int totalBuildings, string districtName)
    {
        float gridSize = 8f; // Minimum spacing between buildings
        int gridWidth = Mathf.CeilToInt(Mathf.Sqrt(totalBuildings));

        // Calculate grid position
        int gridX = index % gridWidth;
        int gridZ = index / gridWidth;

        // Center the grid around the district center
        float offsetX = (gridX - gridWidth * 0.5f) * gridSize;
        float offsetZ = (gridZ - gridWidth * 0.5f) * gridSize;

        Vector3 proposedPosition = new Vector3(offsetX, 0, offsetZ);

        // Add some randomization while maintaining minimum distance
        proposedPosition += new Vector3(
            Random.Range(-gridSize * 0.3f, gridSize * 0.3f),
            0,
            Random.Range(-gridSize * 0.3f, gridSize * 0.3f)
        );

        // Check for collisions with existing buildings
        return FindValidBuildingPosition(proposedPosition, gridSize);
    }

    Vector3 FindValidBuildingPosition(Vector3 proposedPosition, float minDistance)
    {
        Vector3 bestPosition = proposedPosition;

        // Check if position conflicts with existing buildings
        for (int attempt = 0; attempt < 10; attempt++)
        {
            bool validPosition = true;

            foreach (GameObject building in generatedBuildings)
            {
                if (building != null)
                {
                    float distance = Vector3.Distance(bestPosition, building.transform.position);
                    if (distance < minDistance)
                    {
                        validPosition = false;
                        break;
                    }
                }
            }

            // Check collision with walls
            if (validPosition && !IsPositionInsideWalls(bestPosition))
            {
                validPosition = false;
            }

            if (validPosition)
            {
                return bestPosition;
            }

            // Try a new position with spiral pattern
            float angle = attempt * 60f; // 60 degrees apart
            float radius = (attempt + 1) * 2f;
            bestPosition = proposedPosition + new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius
            );
        }

        return bestPosition; // Return best attempt if no perfect position found
    }

    bool IsPositionInsideWalls(Vector3 position)
    {
        if (wallShape == WallShape.Circular)
        {
            return Vector3.Distance(Vector3.zero, position) < (cityRadius - 5f);
        }
        else
        {
            // Square walls
            float halfWidth = squareWallSize.x * 0.5f - 5f;
            float halfHeight = squareWallSize.y * 0.5f - 5f;
            return Mathf.Abs(position.x) < halfWidth && Mathf.Abs(position.z) < halfHeight;
        }
    }

    bool IsPositionTooCloseToWalls(Vector3 position)
    {
        float minDistanceFromWalls = wallThickness * 2f; // Minimum distance from walls

        if (wallShape == WallShape.Circular)
        {
            float distanceFromCenter = Vector3.Distance(Vector3.zero, position);
            return distanceFromCenter > (cityRadius - minDistanceFromWalls);
        }
        else
        {
            float halfWidth = squareWallSize.x * 0.5f;
            float halfHeight = squareWallSize.y * 0.5f;

            return (Mathf.Abs(position.x) > (halfWidth - minDistanceFromWalls)) ||
                   (Mathf.Abs(position.z) > (halfHeight - minDistanceFromWalls));
        }
    }

    bool IsPositionOnRoad(Vector3 pos)
    {
        // Check against all generated streets
        foreach (GameObject street in generatedStreets)
        {
            if (street != null)
            {
                Vector3 streetPos = street.transform.position;
                Vector3 streetScale = street.transform.localScale;

                // Use more precise street collision detection
                // Check if position is within the street bounds
                float halfStreetWidth = streetScale.x * 0.5f;
                float halfStreetLength = streetScale.z * 0.5f;

                // Calculate relative position to street center
                float deltaX = Mathf.Abs(pos.x - streetPos.x);
                float deltaZ = Mathf.Abs(pos.z - streetPos.z);

                // Check if within street bounds with small buffer
                if (deltaX < halfStreetWidth + 1f && deltaZ < halfStreetLength + 1f)
                {
                    Debug.Log($"Position {pos} conflicts with street at {streetPos} (scale: {streetScale})");
                    return true;
                }
            }
        }

        // If no streets exist yet, don't filter any positions
        // This allows buildings to spawn initially, streets will be placed around them
        return false;
    }

    GameObject CreateBuildingOfType(BuildingType type, Vector3 position, Transform parent, int index)
    {
        BuildingTemplate template = GetTemplateForType(type);
        if (template == null)
        {
            Debug.LogError($"❌ No template found for building type: {type}");
            return null;
        }

        Debug.Log($"🏗️ Creating building of type {type} at {position}");
        GameObject building = CreateCube($"{type}_Building_{index}", position, parent);

        // Apply template sizing with some variation
        Vector3 size = template.size;
        float height = template.height;

        if (type == BuildingType.House || type == BuildingType.Shop)
        {
            // Add variation to common buildings
            size += new Vector3(
                Random.Range(-1f, 1f),
                0,
                Random.Range(-1f, 1f)
            );
            height += Random.Range(-0.5f, 1f);
        }

        building.transform.localScale = new Vector3(size.x, height, size.z);
        building.transform.position = position + Vector3.up * (height * 0.5f);

        // Apply color
        Color buildingColor = template.color;
        if (type == BuildingType.House)
        {
            buildingColor = buildingColors[Random.Range(0, buildingColors.Length)];
        }
        ApplyMaterial(building, buildingColor, false);

        // Add courtyard for special buildings
        if (template.hasCourtyard)
        {
            CreateCourtyard(building, size, parent);
        }

        Debug.Log($"✅ Successfully created building: {building.name} with scale {building.transform.localScale}");
        return building;
    }

    // Simple method to create test buildings without complex checks
    public void CreateTestBuildings()
    {
        SetupCityParent();
        Transform buildingParent = CreateCategoryParent("Test_Buildings");

        // Create a few simple test buildings
        for (int i = 0; i < 5; i++)
        {
            Vector3 pos = new Vector3(i * 10, 0, 0);
            GameObject building = CreateBuildingOfType(BuildingType.House, pos, buildingParent, i);
            if (building != null)
            {
                generatedBuildings.Add(building);
            }
        }

        Debug.Log($"Created {generatedBuildings.Count} test buildings");
    }

    void CreateCourtyard(GameObject building, Vector3 buildingSize, Transform parent)
    {
        Vector3 courtyardPos = building.transform.position + Vector3.forward * (buildingSize.z * 0.7f);
        courtyardPos.y = 0.05f;

        GameObject courtyard = CreateCube("Courtyard", courtyardPos, parent);
        courtyard.transform.localScale = new Vector3(buildingSize.x * 0.8f, 0.1f, buildingSize.z * 0.5f);
        ApplyMaterial(courtyard, new Color(0.3f, 0.6f, 0.3f), false); // Green courtyard
        generatedBuildings.Add(courtyard);
    }

    BuildingTemplate GetTemplateForType(BuildingType type)
    {
        foreach (var template in buildingTemplates)
        {
            if (template.type == type) return template;
        }
        return null;
    }

    public void SetupPlayerSpawn()
    {
        if (playerSpawn != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(playerSpawn);
#else
            Destroy(playerSpawn);
#endif
        }

        Vector3 gatePosition = GetCirclePosition(180f, cityRadius);
        Vector3 spawnPosition = gatePosition + playerSpawnOffset;
        spawnPosition.y = 1f; // Ensure player spawns above ground

        playerSpawn = new GameObject("Player_Spawn_Point");
        playerSpawn.transform.position = spawnPosition;
        playerSpawn.transform.SetParent(cityParent);

        // Add visual indicator
        GameObject spawnIndicator = CreateCube("Spawn_Indicator", spawnPosition, playerSpawn.transform);
        spawnIndicator.transform.localScale = Vector3.one * 0.5f;
        ApplyMaterial(spawnIndicator, Color.green, false);

        Debug.Log($"Player spawn point created at {spawnPosition}");
    }

    public void SetupPatrolRoutes()
    {
        // This will be expanded to integrate with the existing waypoint system
        Debug.Log("Patrol routes setup - integrate with WaypointGenerator system");
    }

    // Utility methods
    Vector3 GetCirclePosition(float angle, float radius)
    {
        float radians = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(radians) * radius, 0, Mathf.Cos(radians) * radius);
    }

    void CreateMainGateStructure(GateInfo gate, Transform parent)
    {
        // Create impressive main gate with gatehouse, drawbridge, portcullis
        Vector3 gatePos = parent.position;

        // Gatehouse towers (twin towers flanking the gate)
        CreateGatehouse(gate, parent);

        // Drawbridge mechanism
        if (gate.hasDrawbridge)
        {
            CreateDrawbridge(gate, parent);
        }

        // Portcullis
        if (gate.hasPortcullis)
        {
            CreatePortcullis(gate, parent);
        }

        // Gate posts
        CreateGatePostsForGate(gate, parent);

        Debug.Log($"Created main gate structure: {gate.name}");
    }

    void CreateSecondaryGateStructure(GateInfo gate, Transform parent)
    {
        // Standard gate with portcullis and basic defenses
        Vector3 gatePos = parent.position;

        if (gate.hasPortcullis)
        {
            CreatePortcullis(gate, parent);
        }

        CreateGatePostsForGate(gate, parent);

        // Add small guard chamber
        CreateGuardChamber(gate, parent);

        Debug.Log($"Created secondary gate structure: {gate.name}");
    }

    void CreatePosternGateStructure(GateInfo gate, Transform parent)
    {
        // Small, hidden gate for discreet access
        Vector3 gatePos = parent.position;

        // Simple gate posts only
        CreateSimpleGatePosts(gate, parent);

        // Hidden door mechanism
        CreateHiddenDoor(gate, parent);

        Debug.Log($"Created postern gate structure: {gate.name}");
    }

    void CreateSallyPortStructure(GateInfo gate, Transform parent)
    {
        // Military gate for sorties and rapid deployment
        Vector3 gatePos = parent.position;

        CreateGatePostsForGate(gate, parent);

        if (gate.hasPortcullis)
        {
            CreatePortcullis(gate, parent);
        }

        // Reinforced design with murder holes
        CreateMurderHoles(gate, parent);

        Debug.Log($"Created sally port structure: {gate.name}");
    }

    void CreateGatehouse(GateInfo gate, Transform parent)
    {
        Vector3 gatePos = parent.position;
        float towerHeight = wallHeight * 2f;
        float towerWidth = gate.width + wallThickness * 2f;

        // Left gatehouse tower
        Vector3 leftTowerPos = gatePos + Vector3.left * (gate.width * 0.5f + wallThickness * 1.5f);
        leftTowerPos.y = towerHeight * 0.5f;
        GameObject leftTower = CreateCube("Gatehouse_Left", leftTowerPos, parent);
        leftTower.transform.localScale = new Vector3(wallThickness * 2f, towerHeight, wallThickness * 3f);
        ApplyMaterial(leftTower, wallColor, true);
        generatedWalls.Add(leftTower);

        // Right gatehouse tower
        Vector3 rightTowerPos = gatePos + Vector3.right * (gate.width * 0.5f + wallThickness * 1.5f);
        rightTowerPos.y = towerHeight * 0.5f;
        GameObject rightTower = CreateCube("Gatehouse_Right", rightTowerPos, parent);
        rightTower.transform.localScale = new Vector3(wallThickness * 2f, towerHeight, wallThickness * 3f);
        ApplyMaterial(rightTower, wallColor, true);
        generatedWalls.Add(rightTower);

        // Connecting bridge between towers
        Vector3 bridgePos = gatePos;
        bridgePos.y = wallHeight * 1.2f;
        GameObject bridge = CreateCube("Gatehouse_Bridge", bridgePos, parent);
        bridge.transform.localScale = new Vector3(gate.width + wallThickness * 3f, wallHeight * 0.4f, wallThickness);
        ApplyMaterial(bridge, wallColor, true);
        generatedWalls.Add(bridge);

        // Add battlements to gatehouse towers
        CreateGatehouseBattlements(leftTower, rightTower, parent);
    }

    void CreateDrawbridge(GateInfo gate, Transform parent)
    {
        Vector3 gatePos = parent.position;

        // Drawbridge deck
        Vector3 bridgePos = gatePos;
        bridgePos.y = -0.2f; // Slightly below ground when down
        bridgePos.z -= wallThickness * 0.5f; // Position in front of gate
        GameObject drawbridge = CreateCube("Drawbridge", bridgePos, parent);
        drawbridge.transform.localScale = new Vector3(gate.width, 0.2f, wallThickness * 2f);
        ApplyMaterial(drawbridge, new Color(0.6f, 0.4f, 0.2f), false); // Wood color
        generatedWalls.Add(drawbridge);

        // Drawbridge chains
        CreateDrawbridgeChains(gate, parent);
    }

    void CreatePortcullis(GateInfo gate, Transform parent)
    {
        Vector3 gatePos = parent.position;
        gatePos.y = wallHeight * 0.3f; // Position above ground

        GameObject portcullis = CreateCube("Portcullis", gatePos, parent);
        portcullis.transform.localScale = new Vector3(gate.width * 0.8f, wallHeight * 0.6f, 0.2f);
        ApplyMaterial(portcullis, new Color(0.3f, 0.3f, 0.3f), false); // Dark metal
        generatedWalls.Add(portcullis);
    }

    void CreateGatePostsForGate(GateInfo gate, Transform parent)
    {
        Vector3 gatePos = parent.position;
        float postHeight = wallHeight * 1.2f;

        // Left post
        Vector3 leftPostPos = gatePos + Vector3.left * (gate.width * 0.5f + wallThickness * 0.5f);
        leftPostPos.y = postHeight * 0.5f;
        GameObject leftPost = CreateCube($"{gate.name}_Post_Left", leftPostPos, parent);
        leftPost.transform.localScale = new Vector3(wallThickness, postHeight, wallThickness * 1.5f);
        ApplyMaterial(leftPost, wallColor, true);
        generatedWalls.Add(leftPost);

        // Right post
        Vector3 rightPostPos = gatePos + Vector3.right * (gate.width * 0.5f + wallThickness * 0.5f);
        rightPostPos.y = postHeight * 0.5f;
        GameObject rightPost = CreateCube($"{gate.name}_Post_Right", rightPostPos, parent);
        rightPost.transform.localScale = new Vector3(wallThickness, postHeight, wallThickness * 1.5f);
        ApplyMaterial(rightPost, wallColor, true);
        generatedWalls.Add(rightPost);
    }

    void CreateSimpleGatePosts(GateInfo gate, Transform parent)
    {
        Vector3 gatePos = parent.position;
        float postHeight = wallHeight * 0.8f; // Smaller for postern gates

        Vector3 leftPostPos = gatePos + Vector3.left * (gate.width * 0.5f);
        leftPostPos.y = postHeight * 0.5f;
        GameObject leftPost = CreateCube($"{gate.name}_Simple_Post_Left", leftPostPos, parent);
        leftPost.transform.localScale = new Vector3(wallThickness * 0.5f, postHeight, wallThickness);
        ApplyMaterial(leftPost, wallColor, true);
        generatedWalls.Add(leftPost);

        Vector3 rightPostPos = gatePos + Vector3.right * (gate.width * 0.5f);
        rightPostPos.y = postHeight * 0.5f;
        GameObject rightPost = CreateCube($"{gate.name}_Simple_Post_Right", rightPostPos, parent);
        rightPost.transform.localScale = new Vector3(wallThickness * 0.5f, postHeight, wallThickness);
        ApplyMaterial(rightPost, wallColor, true);
        generatedWalls.Add(rightPost);
    }

    void CreateSimpleGatePostsForWall(Vector3 gateStart, Vector3 gateEnd, Transform parent)
    {
        float postHeight = wallHeight * 1.2f;

        // Left post at gate start
        Vector3 leftPostPos = gateStart;
        leftPostPos.y = postHeight * 0.5f;
        GameObject leftPost = CreateCube("Wall_Gate_Post_Left", leftPostPos, parent);
        leftPost.transform.localScale = new Vector3(wallThickness, postHeight, wallThickness * 1.5f);
        ApplyMaterial(leftPost, wallColor, true);
        generatedWalls.Add(leftPost);

        // Right post at gate end
        Vector3 rightPostPos = gateEnd;
        rightPostPos.y = postHeight * 0.5f;
        GameObject rightPost = CreateCube("Wall_Gate_Post_Right", rightPostPos, parent);
        rightPost.transform.localScale = new Vector3(wallThickness, postHeight, wallThickness * 1.5f);
        ApplyMaterial(rightPost, wallColor, true);
        generatedWalls.Add(rightPost);
    }

    void CreateGuardChamber(GateInfo gate, Transform parent)
    {
        Vector3 gatePos = parent.position;
        Vector3 chamberPos = gatePos + Vector3.forward * wallThickness * 2f;
        chamberPos.y = wallHeight * 0.5f;

        GameObject chamber = CreateCube($"{gate.name}_Guard_Chamber", chamberPos, parent);
        chamber.transform.localScale = new Vector3(gate.width * 1.5f, wallHeight, wallThickness * 2f);
        ApplyMaterial(chamber, wallColor, true);
        generatedWalls.Add(chamber);
    }

    void CreateHiddenDoor(GateInfo gate, Transform parent)
    {
        Vector3 gatePos = parent.position;
        gatePos.y = wallHeight * 0.4f;

        GameObject door = CreateCube($"{gate.name}_Hidden_Door", gatePos, parent);
        door.transform.localScale = new Vector3(gate.width, wallHeight * 0.8f, 0.3f);
        ApplyMaterial(door, new Color(0.4f, 0.3f, 0.2f), false); // Dark wood
        generatedWalls.Add(door);
    }

    void CreateMurderHoles(GateInfo gate, Transform parent)
    {
        Vector3 gatePos = parent.position;

        // Create overhead structure with murder holes
        Vector3 murderHolePos = gatePos;
        murderHolePos.y = wallHeight + 1f;
        GameObject murderHoleStructure = CreateCube($"{gate.name}_Murder_Holes", murderHolePos, parent);
        murderHoleStructure.transform.localScale = new Vector3(gate.width * 1.2f, 1f, wallThickness);
        ApplyMaterial(murderHoleStructure, wallColor, true);
        generatedWalls.Add(murderHoleStructure);
    }

    void CreateDrawbridgeChains(GateInfo gate, Transform parent)
    {
        Vector3 gatePos = parent.position;

        // Left chain
        Vector3 leftChainPos = gatePos + Vector3.left * (gate.width * 0.4f);
        leftChainPos.y = wallHeight * 0.8f;
        GameObject leftChain = CreateCube($"{gate.name}_Chain_Left", leftChainPos, parent);
        leftChain.transform.localScale = new Vector3(0.2f, wallHeight * 0.6f, 0.2f);
        ApplyMaterial(leftChain, new Color(0.3f, 0.3f, 0.3f), false); // Metal
        generatedWalls.Add(leftChain);

        // Right chain
        Vector3 rightChainPos = gatePos + Vector3.right * (gate.width * 0.4f);
        rightChainPos.y = wallHeight * 0.8f;
        GameObject rightChain = CreateCube($"{gate.name}_Chain_Right", rightChainPos, parent);
        rightChain.transform.localScale = new Vector3(0.2f, wallHeight * 0.6f, 0.2f);
        ApplyMaterial(rightChain, new Color(0.3f, 0.3f, 0.3f), false); // Metal
        generatedWalls.Add(rightChain);
    }

    void CreateGatehouseBattlements(GameObject leftTower, GameObject rightTower, Transform parent)
    {
        // Add battlements to gatehouse towers
        Vector3[] towerPositions = { leftTower.transform.position, rightTower.transform.position };

        for (int t = 0; t < towerPositions.Length; t++)
        {
            Vector3 towerTop = towerPositions[t];
            towerTop.y += leftTower.transform.localScale.y * 0.5f + 0.5f;

            for (int i = 0; i < 3; i++)
            {
                Vector3 battlementPos = towerTop + new Vector3((i - 1) * 1.5f, 0, 0);
                GameObject battlement = CreateCube($"Gatehouse_Battlement_{t}_{i}", battlementPos, parent);
                battlement.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);
                ApplyMaterial(battlement, wallColor, true);
                generatedWalls.Add(battlement);
            }
        }
    }

    // Inner Fortifications and Castle Keep System for Phase 3
    void CreateInnerFortifications(Transform parent)
    {
        // Create inner walls around castle district
        if (includeInnerWalls)
        {
            CreateInnerWalls(parent);
        }

        // Create castle keep with inner courtyard
        if (includeKeep)
        {
            CreateCastleKeep(parent);
        }

        Debug.Log("Created inner fortifications and castle keep");
    }

    void CreateInnerWalls(Transform parent)
    {
        Transform innerWallParent = new GameObject("Inner_Walls").transform;
        innerWallParent.SetParent(parent);

        // Get castle district position
        var districts = GetDistrictCenters();
        Vector3 castleCenter = districts.ContainsKey("Castle") ? districts["Castle"] : Vector3.zero;

        if (wallShape == WallShape.Circular)
        {
            CreateCircularInnerWalls(castleCenter, innerWallParent);
        }
        else
        {
            CreateSquareInnerWalls(castleCenter, innerWallParent);
        }
    }

    void CreateCircularInnerWalls(Vector3 center, Transform parent)
    {
        // Create circular inner wall around castle district
        int segments = 16;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float nextAngle = (i + 1) * angleStep;

            // Skip segment for inner gate (facing main city)
            bool isInnerGateArea = angle >= 170f && angle <= 190f;
            if (isInnerGateArea) continue;

            Vector3 startPos = center + GetCirclePosition(angle, innerWallRadius);
            Vector3 endPos = center + GetCirclePosition(nextAngle, innerWallRadius);
            Vector3 wallPos = Vector3.Lerp(startPos, endPos, 0.5f);

            GameObject wallSegment = CreateCube($"Inner_Wall_Segment_{i}", wallPos, parent);
            float segmentLength = Vector3.Distance(startPos, endPos);
            wallSegment.transform.localScale = new Vector3(segmentLength, wallHeight * 0.8f, wallThickness * 0.8f);
            wallSegment.transform.LookAt(wallPos + (endPos - startPos).normalized);
            wallSegment.transform.Rotate(0, 90, 0);

            ApplyMaterial(wallSegment, wallColor, true);
            generatedWalls.Add(wallSegment);
        }

        // Create inner gate
        CreateInnerGate(center, parent);

        // Add inner wall towers
        CreateInnerWallTowers(center, parent);
    }

    void CreateSquareInnerWalls(Vector3 center, Transform parent)
    {
        float innerWallSize = innerWallRadius * 1.4f; // Convert radius to square dimensions
        float halfSize = innerWallSize * 0.5f;

        Vector3[] innerCorners = new Vector3[]
        {
            center + new Vector3(-halfSize, 0, -halfSize), // SW
            center + new Vector3(halfSize, 0, -halfSize),  // SE
            center + new Vector3(halfSize, 0, halfSize),   // NE
            center + new Vector3(-halfSize, 0, halfSize)   // NW
        };

        // Create inner walls between corners
        for (int i = 0; i < innerCorners.Length; i++)
        {
            Vector3 startCorner = innerCorners[i];
            Vector3 endCorner = innerCorners[(i + 1) % innerCorners.Length];

            // Skip south wall for inner gate
            bool isSouthWall = (i == 0);
            if (isSouthWall)
            {
                CreateInnerWallWithGate(startCorner, endCorner, parent);
            }
            else
            {
                CreateInnerWallSegment(startCorner, endCorner, parent, i);
            }
        }

        // Add inner corner towers
        CreateInnerCornerTowers(innerCorners, parent);
    }

    void CreateInnerWallSegment(Vector3 start, Vector3 end, Transform parent, int wallIndex)
    {
        Vector3 wallPos = Vector3.Lerp(start, end, 0.5f);
        wallPos.y = wallHeight * 0.4f;

        GameObject wall = CreateCube($"Inner_Wall_{wallIndex}", wallPos, parent);
        float wallLength = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            wall.transform.localScale = new Vector3(wallLength, wallHeight * 0.8f, wallThickness * 0.8f);
        }
        else
        {
            wall.transform.localScale = new Vector3(wallThickness * 0.8f, wallHeight * 0.8f, wallLength);
        }

        ApplyMaterial(wall, wallColor, true);
        generatedWalls.Add(wall);
    }

    void CreateInnerWallWithGate(Vector3 start, Vector3 end, Transform parent)
    {
        Vector3 wallCenter = Vector3.Lerp(start, end, 0.5f);
        float wallLength = Vector3.Distance(start, end);
        float innerGateWidth = gateWidth * 0.8f;

        // Create left wall segment
        Vector3 gateStart = wallCenter - Vector3.right * (innerGateWidth * 0.5f);
        Vector3 leftWallPos = Vector3.Lerp(start, gateStart, 0.5f);
        leftWallPos.y = wallHeight * 0.4f;

        GameObject leftWall = CreateCube("Inner_Wall_Left", leftWallPos, parent);
        float leftWallLength = Vector3.Distance(start, gateStart);
        leftWall.transform.localScale = new Vector3(leftWallLength, wallHeight * 0.8f, wallThickness * 0.8f);
        ApplyMaterial(leftWall, wallColor, true);
        generatedWalls.Add(leftWall);

        // Create right wall segment
        Vector3 gateEnd = wallCenter + Vector3.right * (innerGateWidth * 0.5f);
        Vector3 rightWallPos = Vector3.Lerp(gateEnd, end, 0.5f);
        rightWallPos.y = wallHeight * 0.4f;

        GameObject rightWall = CreateCube("Inner_Wall_Right", rightWallPos, parent);
        float rightWallLength = Vector3.Distance(gateEnd, end);
        rightWall.transform.localScale = new Vector3(rightWallLength, wallHeight * 0.8f, wallThickness * 0.8f);
        ApplyMaterial(rightWall, wallColor, true);
        generatedWalls.Add(rightWall);

        // Create inner gate
        CreateSimpleInnerGate(wallCenter, parent, innerGateWidth);
    }

    void CreateInnerGate(Vector3 center, Transform parent)
    {
        Vector3 gatePos = center + GetCirclePosition(180f, innerWallRadius);
        CreateSimpleInnerGate(gatePos, parent, gateWidth * 0.8f);
    }

    void CreateSimpleInnerGate(Vector3 position, Transform parent, float width)
    {
        // Simple inner gate posts
        Vector3 leftPostPos = position + Vector3.left * (width * 0.5f);
        leftPostPos.y = wallHeight * 0.4f;
        GameObject leftPost = CreateCube("Inner_Gate_Post_Left", leftPostPos, parent);
        leftPost.transform.localScale = new Vector3(wallThickness * 0.8f, wallHeight * 0.8f, wallThickness);
        ApplyMaterial(leftPost, wallColor, true);
        generatedWalls.Add(leftPost);

        Vector3 rightPostPos = position + Vector3.right * (width * 0.5f);
        rightPostPos.y = wallHeight * 0.4f;
        GameObject rightPost = CreateCube("Inner_Gate_Post_Right", rightPostPos, parent);
        rightPost.transform.localScale = new Vector3(wallThickness * 0.8f, wallHeight * 0.8f, wallThickness);
        ApplyMaterial(rightPost, wallColor, true);
        generatedWalls.Add(rightPost);
    }

    void CreateInnerWallTowers(Vector3 center, Transform parent)
    {
        // Add towers at key positions around inner walls
        float[] angles = { 0f, 90f, 270f }; // North, East, West (South has gate)
        for (int i = 0; i < angles.Length; i++)
        {
            Vector3 towerPos = center + GetCirclePosition(angles[i], innerWallRadius);
            towerPos.y = wallHeight * 0.6f;

            GameObject tower = CreateCube($"Inner_Wall_Tower_{i}", towerPos, parent);
            tower.transform.localScale = new Vector3(wallThickness * 1.5f, wallHeight * 1.2f, wallThickness * 1.5f);
            ApplyMaterial(tower, wallColor, true);
            generatedWalls.Add(tower);
        }
    }

    void CreateInnerCornerTowers(Vector3[] corners, Transform parent)
    {
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 towerPos = corners[i];
            towerPos.y = wallHeight * 0.6f;

            GameObject tower = CreateCube($"Inner_Corner_Tower_{i}", towerPos, parent);
            tower.transform.localScale = new Vector3(wallThickness * 1.5f, wallHeight * 1.2f, wallThickness * 1.5f);
            ApplyMaterial(tower, wallColor, true);
            generatedWalls.Add(tower);
        }
    }

    void CreateCastleKeep(Transform parent)
    {
        Transform keepParent = new GameObject("Castle_Keep").transform;
        keepParent.SetParent(parent);

        // Get castle district position
        var districts = GetDistrictCenters();
        Vector3 castleCenter = districts.ContainsKey("Castle") ? districts["Castle"] : Vector3.zero;

        // Main keep tower
        Vector3 keepPos = castleCenter;
        keepPos.y = keepHeight * 0.5f;
        GameObject keep = CreateCube("Keep_Main_Tower", keepPos, keepParent);
        keep.transform.localScale = new Vector3(wallThickness * 4f, keepHeight, wallThickness * 4f);
        ApplyMaterial(keep, wallColor, true);
        generatedWalls.Add(keep);

        // Keep battlements
        CreateKeepBattlements(keep, keepParent);

        // Inner courtyard if enabled
        if (includeInnerCourtyard)
        {
            CreateInnerCourtyard(castleCenter, keepParent);
        }

        // Support towers around keep
        CreateKeepSupportTowers(castleCenter, keepParent);

        Debug.Log("Created castle keep with inner courtyard");
    }

    void CreateKeepBattlements(GameObject keep, Transform parent)
    {
        Vector3 keepTop = keep.transform.position;
        keepTop.y += keep.transform.localScale.y * 0.5f + 1f;

        // Create battlements around the top of the keep
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 battlementPos = keepTop + GetCirclePosition(angle, wallThickness * 1.8f);
            GameObject battlement = CreateCube($"Keep_Battlement_{i}", battlementPos, parent);
            battlement.transform.localScale = new Vector3(1f, 1.5f, 1f);
            ApplyMaterial(battlement, wallColor, true);
            generatedWalls.Add(battlement);
        }
    }

    void CreateInnerCourtyard(Vector3 center, Transform parent)
    {
        // Create courtyard floor
        Vector3 courtyardPos = center;
        courtyardPos.y = -0.1f;
        GameObject courtyard = CreateCube("Inner_Courtyard", courtyardPos, parent);
        courtyard.transform.localScale = new Vector3(innerWallRadius * 1.5f, 0.1f, innerWallRadius * 1.5f);
        ApplyMaterial(courtyard, new Color(0.6f, 0.6f, 0.5f), false); // Stone color
        generatedWalls.Add(courtyard);

        // Add courtyard features
        CreateCourtyardFeatures(center, parent);
    }

    void CreateCourtyardFeatures(Vector3 center, Transform parent)
    {
        // Well in center of courtyard
        Vector3 wellPos = center;
        GameObject well = CreateCube("Courtyard_Well", wellPos, parent);
        well.transform.localScale = new Vector3(2f, 1.5f, 2f);
        ApplyMaterial(well, new Color(0.5f, 0.5f, 0.4f), false); // Stone well
        generatedWalls.Add(well);

        // Storage buildings around courtyard
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f + 45f; // Offset from cardinal directions
            Vector3 buildingPos = center + GetCirclePosition(angle, innerWallRadius * 0.7f);
            buildingPos.y = wallHeight * 0.3f;

            GameObject building = CreateCube($"Courtyard_Building_{i}", buildingPos, parent);
            building.transform.localScale = new Vector3(wallThickness * 2f, wallHeight * 0.6f, wallThickness * 3f);
            ApplyMaterial(building, new Color(0.7f, 0.6f, 0.5f), false); // Building color
            generatedWalls.Add(building);
        }
    }

    void CreateKeepSupportTowers(Vector3 center, Transform parent)
    {
        // Add support towers around the main keep
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f;
            Vector3 towerPos = center + GetCirclePosition(angle, wallThickness * 6f);
            towerPos.y = keepHeight * 0.6f;

            GameObject supportTower = CreateCube($"Keep_Support_Tower_{i}", towerPos, parent);
            supportTower.transform.localScale = new Vector3(wallThickness * 2f, keepHeight * 1.2f, wallThickness * 2f);
            ApplyMaterial(supportTower, wallColor, true);
            generatedWalls.Add(supportTower);

            // Add small battlements to support towers
            Vector3 supportTop = towerPos;
            supportTop.y += keepHeight * 0.6f + 0.5f;
            GameObject supportBattlement = CreateCube($"Keep_Support_Battlement_{i}", supportTop, parent);
            supportBattlement.transform.localScale = new Vector3(1f, 1f, 1f);
            ApplyMaterial(supportBattlement, wallColor, true);
            generatedWalls.Add(supportBattlement);
        }
    }

    // Terrain Generation System
    public void GenerateTerrain()
    {
        SetupCityParent();
        Transform terrainParent = CreateCategoryParent("Terrain");

        // Calculate terrain size based on wall shape
        Vector3 terrainSize = GetTerrainSize();

        // Create terrain data
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = terrainResolution;
        terrainData.size = terrainSize;

        // Generate heightmap
        CreateCityHeightmap(terrainData);

        // Create terrain GameObject
        GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
        terrainGO.name = "City_Terrain";
        terrainGO.transform.SetParent(terrainParent);

        // Position terrain to match city layout
        PositionTerrain(terrainGO, terrainSize);

        // Store terrain object
        generatedTerrain.Add(terrainGO);

        Debug.Log($"Generated terrain: {terrainSize.x}x{terrainSize.z} units");
    }

    Vector3 GetTerrainSize()
    {
        if (wallShape == WallShape.Circular)
        {
            float diameter = cityRadius * 2.5f; // Extend beyond walls
            return new Vector3(diameter, terrainHeight, diameter);
        }
        else
        {
            return new Vector3(
                squareWallSize.x * 1.3f, // Extend beyond walls
                terrainHeight,
                squareWallSize.y * 1.3f
            );
        }
    }

    void CreateCityHeightmap(TerrainData terrainData)
    {
        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        float[,] heights = new float[width, height];

        Vector3 terrainSize = terrainData.size;
        Vector3 cityCenter = Vector3.zero;

        // Get district centers for elevation variation
        var districts = GetDistrictCenters();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Convert to world coordinates
                float worldX = (x / (float)(width - 1) - 0.5f) * terrainSize.x;
                float worldZ = (y / (float)(height - 1) - 0.5f) * terrainSize.z;
                Vector3 worldPos = new Vector3(worldX, 0, worldZ);

                float elevation = CalculateElevation(worldPos, districts);
                heights[x, y] = elevation / terrainSize.y; // Normalize to terrain height
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }

    float CalculateElevation(Vector3 worldPos, Dictionary<string, Vector3> districts)
    {
        float baseElevation = 0f;

        // Castle area should be elevated
        if (districts.ContainsKey("Castle"))
        {
            Vector3 castlePos = districts["Castle"];
            float distanceToCastle = Vector3.Distance(worldPos, castlePos);

            if (distanceToCastle < innerWallRadius * 1.5f)
            {
                float castleElevation = Mathf.Lerp(terrainHeight * 0.6f, 0f, distanceToCastle / (innerWallRadius * 1.5f));
                baseElevation += castleElevation;
            }
        }

        // Add terrain variation if enabled
        if (addTerrainVariation)
        {
            float noise1 = Mathf.PerlinNoise(worldPos.x * 0.01f, worldPos.z * 0.01f) * terrainRoughness;
            float noise2 = Mathf.PerlinNoise(worldPos.x * 0.05f, worldPos.z * 0.05f) * terrainRoughness * 0.3f;
            baseElevation += (noise1 + noise2) * terrainHeight;
        }

        // Smooth elevation near walls to prevent wall-terrain conflicts
        baseElevation = SmoothNearWalls(worldPos, baseElevation);

        return Mathf.Max(0f, baseElevation);
    }

    float SmoothNearWalls(Vector3 worldPos, float elevation)
    {
        float distanceToWalls = GetDistanceToWalls(worldPos);

        // Gradually reduce elevation near walls
        if (distanceToWalls < wallThickness * 3f)
        {
            float smoothFactor = distanceToWalls / (wallThickness * 3f);
            elevation *= smoothFactor;
        }

        return elevation;
    }

    float GetDistanceToWalls(Vector3 worldPos)
    {
        if (wallShape == WallShape.Circular)
        {
            return Mathf.Abs(Vector3.Distance(worldPos, Vector3.zero) - cityRadius);
        }
        else
        {
            float halfWidth = squareWallSize.x * 0.5f;
            float halfHeight = squareWallSize.y * 0.5f;

            float distanceToEdge = Mathf.Min(
                Mathf.Min(halfWidth - Mathf.Abs(worldPos.x), halfHeight - Mathf.Abs(worldPos.z)),
                Mathf.Min(Mathf.Abs(worldPos.x) + halfWidth, Mathf.Abs(worldPos.z) + halfHeight)
            );

            return Mathf.Abs(distanceToEdge);
        }
    }

    void PositionTerrain(GameObject terrainGO, Vector3 terrainSize)
    {
        // Center the terrain
        Vector3 terrainPosition = new Vector3(
            -terrainSize.x * 0.5f,
            0f,
            -terrainSize.z * 0.5f
        );

        terrainGO.transform.position = terrainPosition;
    }

    GameObject CreateCube(string name, Vector3 position, Transform parent)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.SetParent(parent);
        return cube;
    }

    Transform CreateCategoryParent(string categoryName)
    {
        GameObject category = new GameObject(categoryName);
        category.transform.SetParent(cityParent);
        category.transform.position = cityParent.position;
        return category.transform;
    }

    void ApplyMaterial(GameObject obj, Color color, bool isWall = false)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material materialToUse = null;

            // Use assigned materials if available
            if (isWall && wallMaterial != null)
            {
                materialToUse = new Material(wallMaterial);
            }
            else if (!isWall && buildingMaterial != null)
            {
                materialToUse = new Material(buildingMaterial);
            }
            else
            {
                // Fallback to creating a new standard material
                materialToUse = new Material(Shader.Find("Standard"));
            }

            materialToUse.color = color;
            renderer.material = materialToUse;
        }
    }

    public void ClearWalls()
    {
        foreach (var wall in generatedWalls)
        {
            if (wall != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(wall);
#else
                Destroy(wall);
#endif
            }
        }
        generatedWalls.Clear();
    }

    public void ClearBuildings()
    {
        foreach (var building in generatedBuildings)
        {
            if (building != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(building);
#else
                Destroy(building);
#endif
            }
        }
        generatedBuildings.Clear();
    }

    public void ClearStreets()
    {
        foreach (var street in generatedStreets)
        {
            if (street != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(street);
#else
                Destroy(street);
#endif
            }
        }
        generatedStreets.Clear();
    }

    public void ClearTerrain()
    {
        foreach (var terrain in generatedTerrain)
        {
            if (terrain != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(terrain);
#else
                Destroy(terrain);
#endif
            }
        }
        generatedTerrain.Clear();
    }

    public int GetTotalObjectCount()
    {
        return generatedWalls.Count + generatedBuildings.Count + generatedStreets.Count + generatedTerrain.Count;
    }

    // Public getters for editor access
    public int GetWallCount() => generatedWalls?.Count ?? 0;
    public int GetBuildingCount() => generatedBuildings?.Count ?? 0;
    public int GetStreetCount() => generatedStreets?.Count ?? 0;
    public int GetTerrainCount() => generatedTerrain?.Count ?? 0;

    void OnDrawGizmos()
    {
        if (showDistrictBounds)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, cityRadius);

            var districts = GetDistrictCenters();
            foreach (var district in districts)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(transform.position + district.Value, Vector3.one * 5f);
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MedievalCityBuilder))]
public class MedievalCityBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MedievalCityBuilder builder = (MedievalCityBuilder)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🏰 Medieval City Generation", EditorStyles.boldLabel);

        // Main generation buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🏗️ Generate Complete City", GUILayout.Height(40)))
        {
            Undo.RecordObject(builder, "Generate Complete City");
            builder.GenerateCompleteCity();
            EditorUtility.SetDirty(builder);
        }
        if (GUILayout.Button("🗑️ Clear All", GUILayout.Height(40)))
        {
            Undo.RecordObject(builder, "Clear All City");
            builder.ClearAll();
            EditorUtility.SetDirty(builder);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Individual Components", EditorStyles.boldLabel);

        // Component-specific buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🧱 Generate Walls"))
        {
            Undo.RecordObject(builder, "Generate Walls");
            builder.ClearWalls();
            builder.GenerateWalls();
            EditorUtility.SetDirty(builder);
        }
        if (GUILayout.Button("🏘️ Generate Buildings"))
        {
            Undo.RecordObject(builder, "Generate Buildings");
            builder.ClearBuildings();
            builder.GenerateBuildings();
            EditorUtility.SetDirty(builder);
        }
        if (GUILayout.Button("🧪 Test Buildings"))
        {
            Undo.RecordObject(builder, "Create Test Buildings");
            builder.ClearBuildings();
            builder.CreateTestBuildings();
            EditorUtility.SetDirty(builder);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🛣️ Generate Streets"))
        {
            Undo.RecordObject(builder, "Generate Streets");
            builder.ClearStreets();
            builder.GenerateStreets();
            EditorUtility.SetDirty(builder);
        }
        if (GUILayout.Button("🏔️ Generate Terrain"))
        {
            Undo.RecordObject(builder, "Generate Terrain");
            builder.ClearTerrain();
            builder.GenerateTerrain();
            EditorUtility.SetDirty(builder);
        }
        if (GUILayout.Button("📍 Setup Player Spawn"))
        {
            Undo.RecordObject(builder, "Setup Player Spawn");
            builder.SetupPlayerSpawn();
            EditorUtility.SetDirty(builder);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("🚶 Setup Patrol Routes"))
        {
            Undo.RecordObject(builder, "Setup Patrol Routes");
            builder.SetupPatrolRoutes();
            EditorUtility.SetDirty(builder);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("📊 Statistics", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField("Walls", builder.GetWallCount());
        EditorGUILayout.IntField("Buildings", builder.GetBuildingCount());
        EditorGUILayout.IntField("Streets", builder.GetStreetCount());
        EditorGUILayout.IntField("Terrain", builder.GetTerrainCount());
        EditorGUILayout.IntField("Total Objects", builder.GetTotalObjectCount());
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "🎯 How to use:\n" +
            "1. Adjust city parameters above\n" +
            "2. Click 'Generate Complete City' for full generation\n" +
            "3. Or use individual component buttons for step-by-step building\n" +
            "4. Player spawns at the south gate\n" +
            "5. All structures use cube primitives with procedural sizing",
            MessageType.Info
        );
    }
}
#endif