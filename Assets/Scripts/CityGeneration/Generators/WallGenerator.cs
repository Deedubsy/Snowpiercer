using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using CityGeneration.Core;

namespace CityGeneration.Generators
{
    /// <summary>
    /// Generates defensive walls, gates, and towers for the medieval city
    /// Extracted and modernized from MedievalCityBuilder
    /// </summary>
    public class WallGenerator : BaseGenerator
    {
        [Header("Wall Configuration")]
        public WallShape wallShape = WallShape.Square;
        public float cityRadius = 50f;
        public Vector2 squareWallSize = new Vector2(100f, 80f);

        [Header("Wall Properties")]
        public float wallThickness = 2f;
        public float wallHeight = 8f;
        public float gateWidth = 6f;
        public Color wallColor = new Color(0.8f, 0.8f, 0.7f);

        [Header("Gate Configuration")]
        public bool includeMainGate = true;
        public bool includeSecondaryGates = true;
        public bool includePosternGates = true;
        public bool includeSallyPorts = false;
        [Range(1, 4)] public int numberOfSecondaryGates = 2;

        [Header("Fortifications")]
        public bool includeInnerWalls = true;
        public bool includeKeep = true;
        public bool includeDefensiveTowers = true;
        public float innerWallRadius = 25f;
        public float keepHeight = 25f;

        [Header("Performance")]
        public bool combineWallMeshes = true;
        public int segmentBatchSize = 10; // Process walls in batches for progressive generation

        private List<GameObject> generatedWalls = new List<GameObject>();
        private List<GameObject> generatedGates = new List<GameObject>();
        private List<GameObject> generatedTowers = new List<GameObject>();

        protected override async Task<GenerationResult> GenerateInternal(CityGenerationContext context)
        {
            var result = new WallGenerationResult();
            generatedWalls.Clear();
            generatedGates.Clear();
            generatedTowers.Clear();

            // Copy configuration from context if available
            ApplyContextConfiguration(context);

            Transform wallParent = CreateCategoryParent("Walls");

            try
            {
                // Generate main walls (50% of progress)
                UpdateProgress(0f, "Generating main walls...");
                if (wallShape == WallShape.Circular)
                {
                    await GenerateCircularWalls(wallParent);
                }
                else
                {
                    await GenerateSquareWalls(wallParent);
                }

                result.wallSegments = new List<GameObject>(generatedWalls);
                UpdateProgress(0.5f, "Main walls completed");

                // Generate gates (25% of progress)
                UpdateProgress(0.5f, "Creating gates...");
                await GenerateGates(wallParent);
                result.gates = new List<GameObject>(generatedGates);
                UpdateProgress(0.75f, "Gates completed");

                // Generate fortifications (25% of progress)
                UpdateProgress(0.75f, "Building fortifications...");
                if (includeInnerWalls || includeKeep || includeDefensiveTowers)
                {
                    await GenerateFortifications(wallParent);
                }
                result.towers = new List<GameObject>(generatedTowers);
                UpdateProgress(0.9f, "Fortifications completed");

                // Register with collision system
                UpdateProgress(0.9f, "Registering collision objects...");
                RegisterWallCollisions(result);

                // Optimize if requested
                if (combineWallMeshes)
                {
                    UpdateProgress(0.95f, "Optimizing wall meshes...");
                    await OptimizeWallMeshes(result);
                }

                result.objectsGenerated = generatedWalls.Count + generatedGates.Count + generatedTowers.Count;
                LogDebug($"Generated {result.objectsGenerated} wall objects ({wallShape} shape)");

                return result;
            }
            catch (System.Exception ex)
            {
                result.MarkAsError($"Wall generation failed: {ex.Message}");
                throw;
            }
        }

        private async Task GenerateCircularWalls(Transform parent)
        {
            int wallSegments = 32;
            float angleStep = 360f / wallSegments;

            // Calculate gate positions
            List<GateInfo> gates = GetCircularGatePositions();

            int processedSegments = 0;
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
                GameObject wallSegment = CreateWallSegment($"Wall_Segment_{i}", wallPos, startPos, endPos, parent);
                generatedWalls.Add(wallSegment);

                processedSegments++;

                // Progressive generation - yield control periodically
                if (processedSegments % segmentBatchSize == 0)
                {
                    float progress = processedSegments / (float)wallSegments * 0.4f; // 40% of wall generation
                    UpdateProgress(progress, $"Generated {processedSegments}/{wallSegments} wall segments");
                    await Task.Yield();
                }
            }
        }

        private async Task GenerateSquareWalls(Transform parent)
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

                // Check if this is the south wall (where main gate goes)
                bool isSouthWall = (i == 0);

                if (isSouthWall && includeMainGate)
                {
                    await CreateSquareWallWithGate(startCorner, endCorner, parent, i);
                }
                else
                {
                    CreateSquareWallSegment(startCorner, endCorner, parent, i);
                }

                // Progressive update
                float progress = (i + 1) / (float)corners.Length * 0.4f;
                UpdateProgress(progress, $"Generated wall {i + 1}/{corners.Length}");
                await Task.Yield();
            }

            // Create corner towers
            if (includeDefensiveTowers)
            {
                CreateCornerTowers(corners, parent);
            }
        }

        private GameObject CreateWallSegment(string name, Vector3 position, Vector3 startPos, Vector3 endPos, Transform parent)
        {
            GameObject wallSegment = CreateCube(name, position, parent);

            // Size the wall segment
            float segmentLength = Vector3.Distance(startPos, endPos);
            wallSegment.transform.localScale = new Vector3(segmentLength, wallHeight, wallThickness);

            // Orient the wall segment
            Vector3 direction = (endPos - startPos).normalized;
            if (direction != Vector3.zero)
            {
                wallSegment.transform.LookAt(position + direction);
                wallSegment.transform.Rotate(0, 90, 0); // Adjust rotation for proper thickness orientation
            }

            ApplyMaterial(wallSegment, wallColor, true);
            return wallSegment;
        }

        private void CreateSquareWallSegment(Vector3 start, Vector3 end, Transform parent, int wallIndex)
        {
            Vector3 wallPos = Vector3.Lerp(start, end, 0.5f);
            wallPos.y = wallHeight * 0.5f;

            GameObject wall = CreateCube($"Wall_{wallIndex}", wallPos, parent);

            float wallLength = Vector3.Distance(start, end);
            Vector3 direction = (end - start).normalized;

            // Scale wall based on direction
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

        private async Task CreateSquareWallWithGate(Vector3 start, Vector3 end, Transform parent, int wallIndex)
        {
            Vector3 wallCenter = Vector3.Lerp(start, end, 0.5f);

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

            await Task.Yield();
        }

        private async Task GenerateGates(Transform parent)
        {
            List<GateInfo> gates;

            if (wallShape == WallShape.Circular)
            {
                gates = GetCircularGatePositions();
            }
            else
            {
                gates = GetSquareGatePositions();
            }

            for (int i = 0; i < gates.Count; i++)
            {
                var gate = gates[i];
                GameObject gateObj = await CreateGateStructure(gate, parent);
                if (gateObj != null)
                {
                    generatedGates.Add(gateObj);
                }

                float progress = (i + 1) / (float)gates.Count;
                UpdateProgress(0.5f + progress * 0.25f, $"Created gate {i + 1}/{gates.Count}");
                await Task.Yield();
            }
        }

        private async Task GenerateFortifications(Transform parent)
        {
            if (includeDefensiveTowers)
            {
                if (wallShape == WallShape.Circular)
                {
                    CreateCircularDefensiveTowers(parent);
                }
                else
                {
                    Vector3[] corners = GetSquareCorners();
                    CreateSquareDefensiveTowers(corners, parent);
                }
            }

            if (includeInnerWalls || includeKeep)
            {
                await CreateInnerFortifications(parent);
            }

            await Task.Yield();
        }

        private async Task<GameObject> CreateGateStructure(GateInfo gate, Transform parent)
        {
            Vector3 gatePosition = wallShape == WallShape.Circular ?
                GetCirclePosition(gate.angle, cityRadius) :
                gate.position;

            GameObject gateParent = new GameObject(gate.name);
            gateParent.transform.SetParent(parent);
            gateParent.transform.position = gatePosition;

            // Create basic gate posts for now
            CreateGatePosts(gateParent.transform, gate);

            await Task.Yield();
            return gateParent;
        }

        private void CreateGatePosts(Transform parent, GateInfo gate)
        {
            float postWidth = 1f;
            float postHeight = wallHeight * 1.2f;

            // Left gate post
            Vector3 leftPostPos = parent.position + Vector3.left * (gate.width * 0.5f + postWidth * 0.5f);
            GameObject leftPost = CreateCube($"{parent.name}_Post_Left", leftPostPos, parent);
            leftPost.transform.localScale = new Vector3(postWidth, postHeight, wallThickness * 1.5f);
            ApplyMaterial(leftPost, wallColor, true);

            // Right gate post
            Vector3 rightPostPos = parent.position + Vector3.right * (gate.width * 0.5f + postWidth * 0.5f);
            GameObject rightPost = CreateCube($"{parent.name}_Post_Right", rightPostPos, parent);
            rightPost.transform.localScale = new Vector3(postWidth, postHeight, wallThickness * 1.5f);
            ApplyMaterial(rightPost, wallColor, true);
        }

        private void CreateCornerTowers(Vector3[] corners, Transform parent)
        {
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 towerPos = corners[i];
                towerPos.y = wallHeight * 0.75f;

                GameObject tower = CreateCube($"Corner_Tower_{i}", towerPos, parent);
                tower.transform.localScale = new Vector3(wallThickness * 2f, wallHeight * 1.5f, wallThickness * 2f);
                ApplyMaterial(tower, wallColor, true);
                generatedTowers.Add(tower);
            }
        }

        private void CreateCircularDefensiveTowers(Transform parent)
        {
            int towerCount = 8;
            for (int i = 0; i < towerCount; i++)
            {
                float angle = i * (360f / towerCount);

                // Skip tower near the main gate
                if (angle >= 170f && angle <= 190f) continue;

                Vector3 towerPos = GetCirclePosition(angle, cityRadius + wallThickness * 0.5f);
                towerPos.y = wallHeight * 0.75f;

                GameObject tower = CreateDefensiveTower($"Defensive_Tower_{i}", towerPos, parent);
                generatedTowers.Add(tower);
            }
        }

        private void CreateSquareDefensiveTowers(Vector3[] corners, Transform parent)
        {
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 startCorner = corners[i];
                Vector3 endCorner = corners[(i + 1) % corners.Length];

                // Skip south wall where gate is located
                if (i == 0) continue;

                float wallLength = Vector3.Distance(startCorner, endCorner);
                int towerCount = Mathf.Max(1, Mathf.FloorToInt(wallLength / 20f));

                for (int j = 1; j <= towerCount; j++)
                {
                    float t = j / (float)(towerCount + 1);
                    Vector3 towerPos = Vector3.Lerp(startCorner, endCorner, t);
                    towerPos.y = wallHeight * 0.75f;

                    GameObject tower = CreateDefensiveTower($"Wall_Tower_{i}_{j}", towerPos, parent);
                    generatedTowers.Add(tower);
                }
            }
        }

        private GameObject CreateDefensiveTower(string name, Vector3 position, Transform parent)
        {
            GameObject tower = CreateCube(name, position, parent);
            tower.transform.localScale = new Vector3(wallThickness * 1.5f, wallHeight * 1.8f, wallThickness * 1.5f);
            ApplyMaterial(tower, wallColor, true);
            return tower;
        }

        private async Task CreateInnerFortifications(Transform parent)
        {
            if (includeInnerWalls)
            {
                // Create simplified inner wall ring
                Transform innerParent = CreateCategoryParent("Inner_Walls", parent);
                int innerSegments = 16;
                float angleStep = 360f / innerSegments;

                for (int i = 0; i < innerSegments; i++)
                {
                    float angle = i * angleStep;
                    float nextAngle = (i + 1) * angleStep;

                    Vector3 startPos = GetCirclePosition(angle, innerWallRadius);
                    Vector3 endPos = GetCirclePosition(nextAngle, innerWallRadius);
                    Vector3 wallPos = Vector3.Lerp(startPos, endPos, 0.5f);

                    GameObject innerWall = CreateWallSegment($"Inner_Wall_{i}", wallPos, startPos, endPos, innerParent);
                    generatedWalls.Add(innerWall);
                }
            }

            if (includeKeep)
            {
                Vector3 keepPos = Vector3.zero;
                keepPos.y = keepHeight * 0.5f;

                GameObject keep = CreateCube("Keep", keepPos, parent);
                keep.transform.localScale = new Vector3(15f, keepHeight, 15f);
                ApplyMaterial(keep, wallColor, true);
                generatedTowers.Add(keep);
            }

            await Task.Yield();
        }

        private void RegisterWallCollisions(WallGenerationResult result)
        {
            foreach (var wall in result.wallSegments)
            {
                collisionManager.RegisterStaticObject(wall, ObjectType.Wall);
            }

            foreach (var gate in result.gates)
            {
                collisionManager.RegisterStaticObject(gate, ObjectType.Gate);
            }

            foreach (var tower in result.towers)
            {
                collisionManager.RegisterStaticObject(tower, ObjectType.Tower);
            }
        }

        private async Task OptimizeWallMeshes(WallGenerationResult result)
        {
            // TODO: Implement mesh combining for better performance
            await Task.Yield();
            LogDebug("Mesh optimization completed");
        }

        private void ApplyContextConfiguration(CityGenerationContext context)
        {
            if (context?.config != null)
            {
                var config = context.config;
                wallShape = config.wallShape;
                cityRadius = config.cityRadius;
                squareWallSize = config.squareWallSize;
                wallThickness = config.wallThickness;
                wallHeight = config.wallHeight;
                gateWidth = config.gateWidth;
            }
        }

        // Helper methods for gate and position calculations
        private List<GateInfo> GetCircularGatePositions()
        {
            var gates = new List<GateInfo>();

            if (includeMainGate)
            {
                gates.Add(new GateInfo
                {
                    type = GateType.Main,
                    angle = 180f,
                    width = gateWidth * 1.5f,
                    name = "Main_Gate"
                });
            }

            if (includeSecondaryGates)
            {
                float[] angles = { 90f, 270f };
                for (int i = 0; i < Mathf.Min(numberOfSecondaryGates, angles.Length); i++)
                {
                    gates.Add(new GateInfo
                    {
                        type = GateType.Secondary,
                        angle = angles[i],
                        width = gateWidth,
                        name = $"Secondary_Gate_{i}"
                    });
                }
            }

            return gates;
        }

        private List<GateInfo> GetSquareGatePositions()
        {
            var gates = new List<GateInfo>();

            if (includeMainGate)
            {
                gates.Add(new GateInfo
                {
                    type = GateType.Main,
                    position = new Vector3(0, 0, -squareWallSize.y * 0.5f),
                    width = gateWidth * 1.5f,
                    name = "Main_Gate"
                });
            }

            return gates;
        }

        private Vector3[] GetSquareCorners()
        {
            float halfWidth = squareWallSize.x * 0.5f;
            float halfDepth = squareWallSize.y * 0.5f;

            return new Vector3[]
            {
                new Vector3(-halfWidth, 0, -halfDepth),
                new Vector3(halfWidth, 0, -halfDepth),
                new Vector3(halfWidth, 0, halfDepth),
                new Vector3(-halfWidth, 0, halfDepth)
            };
        }

        private Vector3 GetCirclePosition(float angle, float radius)
        {
            float radian = angle * Mathf.Deg2Rad;
            return new Vector3(
                Mathf.Sin(radian) * radius,
                0f,
                Mathf.Cos(radian) * radius
            );
        }

        private bool IsSegmentInGateArea(float startAngle, float endAngle, List<GateInfo> gates)
        {
            foreach (var gate in gates)
            {
                float gateAngleRange = (gate.width / (2 * Mathf.PI * cityRadius)) * 360f;
                float gateStartAngle = gate.angle - gateAngleRange;
                float gateEndAngle = gate.angle + gateAngleRange;

                if ((startAngle >= gateStartAngle && startAngle <= gateEndAngle) ||
                    (endAngle >= gateStartAngle && endAngle <= gateEndAngle))
                {
                    return true;
                }
            }
            return false;
        }
    }

    // Supporting classes
    [System.Serializable]
    public class GateInfo
    {
        public GateType type;
        public float angle; // For circular walls
        public Vector3 position; // For square walls
        public float width;
        public string name;
    }

    public enum GateType
    {
        Main,
        Secondary,
        Postern,
        Sally
    }
}