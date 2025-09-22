using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CityGeneration.Core;
using CityGeneration.Rules.SpecificRules;

namespace CityGeneration.Rules
{
    /// <summary>
    /// Engine that applies placement rules to generate intelligent city layouts
    /// Uses rule-based evaluation to find optimal positions for districts and buildings
    /// </summary>
    [System.Serializable]
    public class ProceduralRuleEngine
    {
        [Header("Rule Configuration")]
        public PlacementRule[] globalRules = new PlacementRule[0];
        public DistrictRuleSet[] districtRuleSets = new DistrictRuleSet[0];

        [Header("Search Parameters")]
        public int maxPlacementAttempts = 100;
        public float searchGridSize = 10f;
        public int candidatesPerPosition = 5;
        public bool useAdaptiveSearch = true;

        [Header("Performance")]
        public bool enableParallelEvaluation = false;
        public int evaluationBatchSize = 20;

        [Header("Debug")]
        public bool enableDebugVisualization = false;
        public bool logPlacementDecisions = false;

        private PlacementContext currentContext;
        private List<PlacementCandidate> evaluatedCandidates = new List<PlacementCandidate>();

        /// <summary>
        /// Find the best position for a district using rule evaluation
        /// </summary>
        public async Task<PlacementResult> FindBestDistrictPosition(DistrictType districtType, PlacementContext context)
        {
            currentContext = context;
            context.targetType = PlacementType.District;
            context.targetDistrict = districtType;

            LogDebug($"Finding position for district: {districtType}");

            // Get rules for this district type
            var applicableRules = GetRulesForDistrict(districtType);

            if (applicableRules.Length == 0)
            {
                LogDebug($"No rules found for district {districtType}, using fallback placement");
                return await FindFallbackPosition(context);
            }

            // Generate placement candidates
            var candidates = await GeneratePlacementCandidates(context);

            if (candidates.Count == 0)
            {
                LogDebug($"No valid candidates found for district {districtType}");
                return new PlacementResult { success = false, errorMessage = "No valid positions found" };
            }

            // Evaluate candidates against rules
            var evaluatedCandidates = await EvaluateCandidates(candidates, applicableRules, context);

            // Select best candidate
            var bestCandidate = SelectBestCandidate(evaluatedCandidates);

            if (bestCandidate == null)
            {
                LogDebug($"No suitable candidate found for district {districtType}");
                return new PlacementResult { success = false, errorMessage = "No suitable positions passed rule evaluation" };
            }

            LogDebug($"Selected position for {districtType}: {bestCandidate.position} (score: {bestCandidate.totalScore:F2})");

            return new PlacementResult
            {
                success = true,
                position = bestCandidate.position,
                score = bestCandidate.totalScore,
                ruleResults = bestCandidate.ruleResults
            };
        }

        /// <summary>
        /// Find the best position for a building using rule evaluation
        /// </summary>
        public async Task<PlacementResult> FindBestBuildingPosition(CityGeneration.Generators.BuildingType buildingType, Vector3 districtCenter, float districtRadius, PlacementContext context)
        {
            currentContext = context;
            context.targetType = PlacementType.Building;
            context.targetBuilding = buildingType;

            LogDebug($"Finding position for building: {buildingType} near {districtCenter}");

            // Get rules for this building type
            var applicableRules = GetRulesForBuilding(buildingType);

            // Generate candidates around district center
            var candidates = await GenerateBuildingCandidates(districtCenter, districtRadius, context);

            if (candidates.Count == 0)
            {
                LogDebug($"No valid candidates found for building {buildingType}");
                return new PlacementResult { success = false, errorMessage = "No valid building positions found" };
            }

            // Evaluate candidates
            var evaluatedCandidates = await EvaluateCandidates(candidates, applicableRules, context);

            // Select best candidate
            var bestCandidate = SelectBestCandidate(evaluatedCandidates);

            if (bestCandidate == null)
            {
                return new PlacementResult { success = false, errorMessage = "No suitable building positions found" };
            }

            return new PlacementResult
            {
                success = true,
                position = bestCandidate.position,
                score = bestCandidate.totalScore,
                ruleResults = bestCandidate.ruleResults
            };
        }

        private async Task<List<PlacementCandidate>> GeneratePlacementCandidates(PlacementContext context)
        {
            var candidates = new List<PlacementCandidate>();

            if (context.cityBounds.HasValue)
            {
                Bounds bounds = context.cityBounds.Value;

                // Generate grid-based candidates
                for (float x = bounds.min.x; x <= bounds.max.x; x += searchGridSize)
                {
                    for (float z = bounds.min.z; z <= bounds.max.z; z += searchGridSize)
                    {
                        Vector3 candidate = new Vector3(x, 0f, z);

                        // Basic validity check using collision manager
                        if (context.collisionManager?.IsPositionValid(candidate, 10f, ObjectType.Building) ?? true)
                        {
                            candidates.Add(new PlacementCandidate { position = candidate });
                        }
                    }

                    // Yield control periodically
                    if (candidates.Count % evaluationBatchSize == 0)
                    {
                        await Task.Yield();
                    }
                }
            }
            else
            {
                // Fallback: generate candidates in a circular pattern
                float radius = 50f;
                int ringCount = 5;

                for (int ring = 1; ring <= ringCount; ring++)
                {
                    float ringRadius = (radius / ringCount) * ring;
                    int pointsInRing = ring * 8; // More points in outer rings

                    for (int point = 0; point < pointsInRing; point++)
                    {
                        float angle = (point / (float)pointsInRing) * 360f * Mathf.Deg2Rad;
                        Vector3 candidate = new Vector3(
                            Mathf.Cos(angle) * ringRadius,
                            0f,
                            Mathf.Sin(angle) * ringRadius
                        );

                        if (context.collisionManager?.IsPositionValid(candidate, 10f, ObjectType.Building) ?? true)
                        {
                            candidates.Add(new PlacementCandidate { position = candidate });
                        }
                    }
                }
            }

            LogDebug($"Generated {candidates.Count} placement candidates");
            return candidates;
        }

        private async Task<List<PlacementCandidate>> GenerateBuildingCandidates(Vector3 center, float radius, PlacementContext context)
        {
            var candidates = new List<PlacementCandidate>();
            int attempts = 0;
            int maxAttempts = 50;

            while (candidates.Count < candidatesPerPosition && attempts < maxAttempts)
            {
                // Generate random position within district radius
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(5f, radius * 0.8f);

                Vector3 candidate = center + new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance
                );

                // Basic validity check
                if (context.collisionManager?.IsPositionValid(candidate, 5f, ObjectType.Building) ?? true)
                {
                    candidates.Add(new PlacementCandidate { position = candidate });
                }

                attempts++;

                if (attempts % 10 == 0)
                {
                    await Task.Yield();
                }
            }

            return candidates;
        }

        private async Task<List<PlacementCandidate>> EvaluateCandidates(List<PlacementCandidate> candidates, PlacementRule[] rules, PlacementContext context)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];

                // Check if position passes all required rules
                bool passesAllRequired = true;
                float totalScore = 0f;
                float totalWeight = 0f;

                foreach (var rule in rules)
                {
                    if (rule == null) continue;

                    bool canPlace = rule.CanPlace(candidate.position, context);
                    float desirability = canPlace ? rule.GetDesirability(candidate.position, context) : 0f;

                    var ruleResult = new RuleResult
                    {
                        rule = rule,
                        canPlace = canPlace,
                        desirability = desirability,
                        weight = rule.priority
                    };

                    candidate.ruleResults.Add(ruleResult);

                    // If required rule fails, candidate is invalid
                    if (rule.isRequired && !canPlace)
                    {
                        passesAllRequired = false;
                        break;
                    }

                    // Add to weighted score
                    totalScore += desirability * rule.priority;
                    totalWeight += rule.priority;
                }

                candidate.passesAllRequired = passesAllRequired;
                candidate.totalScore = totalWeight > 0f ? totalScore / totalWeight : 0f;

                // Apply position modifications from rules
                if (passesAllRequired)
                {
                    Vector3 modifiedPosition = candidate.position;
                    foreach (var rule in rules)
                    {
                        modifiedPosition = rule.ModifyPosition(modifiedPosition, context);
                    }
                    candidate.modifiedPosition = modifiedPosition;
                }

                // Yield control periodically
                if (i % evaluationBatchSize == 0)
                {
                    await Task.Yield();
                }
            }

            // Return only candidates that pass all required rules
            var validCandidates = candidates.Where(c => c.passesAllRequired).ToList();
            LogDebug($"Evaluated {candidates.Count} candidates, {validCandidates.Count} passed all required rules");

            return validCandidates;
        }

        private PlacementCandidate SelectBestCandidate(List<PlacementCandidate> candidates)
        {
            if (candidates.Count == 0) return null;

            // Sort by total score (descending)
            candidates.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));

            var best = candidates[0];

            LogDebug($"Selected candidate with score {best.totalScore:F2} from {candidates.Count} options");

            // Store for debugging
            evaluatedCandidates = candidates;

            return best;
        }

        private PlacementRule[] GetRulesForDistrict(DistrictType districtType)
        {
            var rules = new List<PlacementRule>(globalRules);

            // Find specific rules for this district type
            var districtRuleSet = districtRuleSets.FirstOrDefault(rs => rs.districtType == districtType);
            if (districtRuleSet != null)
            {
                rules.AddRange(districtRuleSet.rules);
            }

            return rules.Where(r => r != null).ToArray();
        }

        private PlacementRule[] GetRulesForBuilding(CityGeneration.Generators.BuildingType buildingType)
        {
            var rules = new List<PlacementRule>(globalRules);

            // Map building type to district type and get rules
            DistrictType associatedDistrict = GetDistrictTypeFromBuilding(buildingType);
            var districtRuleSet = districtRuleSets.FirstOrDefault(rs => rs.districtType == associatedDistrict);
            if (districtRuleSet != null)
            {
                rules.AddRange(districtRuleSet.rules);
            }

            return rules.Where(r => r != null).ToArray();
        }

        private DistrictType GetDistrictTypeFromBuilding(CityGeneration.Generators.BuildingType buildingType)
        {
            switch (buildingType)
            {
                case CityGeneration.Generators.BuildingType.Castle: return DistrictType.Castle;
                case CityGeneration.Generators.BuildingType.Cathedral: return DistrictType.Religious;
                case CityGeneration.Generators.BuildingType.Shop: return DistrictType.Market;
                case CityGeneration.Generators.BuildingType.Workshop: return DistrictType.Artisan;
                case CityGeneration.Generators.BuildingType.Barracks: return DistrictType.Military;
                case CityGeneration.Generators.BuildingType.Tavern: return DistrictType.Market;
                default: return DistrictType.Residential;
            }
        }

        private async Task<PlacementResult> FindFallbackPosition(PlacementContext context)
        {
            // Simple fallback: find any valid position
            if (context.cityBounds.HasValue)
            {
                Vector3 center = context.cityBounds.Value.center;
                if (context.collisionManager?.IsPositionValid(center, 10f, ObjectType.Building) ?? true)
                {
                    return new PlacementResult
                    {
                        success = true,
                        position = center,
                        score = 0.5f
                    };
                }
            }

            await Task.Yield();
            return new PlacementResult { success = false, errorMessage = "No fallback position available" };
        }

        private void LogDebug(string message)
        {
            if (logPlacementDecisions)
            {
                Debug.Log($"[ProceduralRuleEngine] {message}");
            }
        }

        /// <summary>
        /// Get visualization data for debugging
        /// </summary>
        public PlacementVisualizationData GetVisualizationData()
        {
            return new PlacementVisualizationData
            {
                evaluatedCandidates = evaluatedCandidates.ToArray(),
                searchGridSize = searchGridSize
            };
        }
    }

    /// <summary>
    /// Configuration for district-specific rules
    /// </summary>
    [System.Serializable]
    public class DistrictRuleSet
    {
        public DistrictType districtType;
        public string districtName;
        public PlacementRule[] rules;
    }

    /// <summary>
    /// Result of a placement evaluation
    /// </summary>
    [System.Serializable]
    public class PlacementResult
    {
        public bool success;
        public Vector3 position;
        public float score;
        public string errorMessage;
        public List<RuleResult> ruleResults = new List<RuleResult>();
    }

    /// <summary>
    /// Candidate position for placement
    /// </summary>
    [System.Serializable]
    public class PlacementCandidate
    {
        public Vector3 position;
        public Vector3 modifiedPosition;
        public float totalScore;
        public bool passesAllRequired;
        public List<RuleResult> ruleResults = new List<RuleResult>();
    }

    /// <summary>
    /// Result of applying a single rule
    /// </summary>
    [System.Serializable]
    public class RuleResult
    {
        public PlacementRule rule;
        public bool canPlace;
        public float desirability;
        public float weight;
        public string reasoning;
    }

    /// <summary>
    /// Data for visualizing placement decisions
    /// </summary>
    [System.Serializable]
    public class PlacementVisualizationData
    {
        public PlacementCandidate[] evaluatedCandidates;
        public float searchGridSize;
    }
}