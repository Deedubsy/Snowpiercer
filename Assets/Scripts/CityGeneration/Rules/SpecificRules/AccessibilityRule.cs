using UnityEngine;
using CityGeneration.Core;

namespace CityGeneration.Rules.SpecificRules
{
    /// <summary>
    /// Rule that ensures buildings and districts have proper road access
    /// Example: All buildings need road access, markets need major road access
    /// </summary>
    [CreateAssetMenu(fileName = "New Accessibility Rule", menuName = "City Rules/Accessibility Rule")]
    public class AccessibilityRule : PlacementRule
    {
        [Header("Road Access Requirements")]
        public bool requiresRoadAccess = true;
        public float maxDistanceToRoad = 15f;
        public bool requiresDirectAccess = false;

        [Header("Road Type Preferences")]
        public bool preferMainRoads = false;
        public bool avoidDeadEnds = false;
        public float mainRoadPreferenceRadius = 25f;

        [Header("Accessibility Scoring")]
        public AnimationCurve roadDistanceDesirability = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        public float mainRoadBonusMultiplier = 1.5f;

        [Header("Path Requirements")]
        public bool requiresClearPath = true;
        public float pathWidth = 3f;
        public int pathComplexityTolerance = 2; // Number of turns allowed in path

        public override bool CanPlace(Vector3 position, PlacementContext context)
        {
            if (!requiresRoadAccess)
            {
                LogDebug($"Position {position}: No road access required");
                return true;
            }

            float distanceToRoad = context.GetDistanceToRoad(position);

            if (distanceToRoad > maxDistanceToRoad)
            {
                LogDebug($"Position {position}: Too far from road ({distanceToRoad:F1}m > {maxDistanceToRoad:F1}m)");
                return false;
            }

            if (requiresDirectAccess || requiresClearPath)
            {
                Vector3 nearestRoadPoint = GetNearestRoadPoint(position, context);
                if (nearestRoadPoint == Vector3.zero)
                {
                    LogDebug($"Position {position}: No road point found");
                    return false;
                }

                if (requiresClearPath && !HasClearPathToRoad(position, nearestRoadPoint, context))
                {
                    LogDebug($"Position {position}: No clear path to road");
                    return false;
                }
            }

            LogDebug($"Position {position}: Road access valid (distance: {distanceToRoad:F1}m)");
            return true;
        }

        public override float GetDesirability(Vector3 position, PlacementContext context)
        {
            float distanceToRoad = context.GetDistanceToRoad(position);

            // Base desirability from road distance
            float normalizedDistance = Mathf.Clamp01(distanceToRoad / maxDistanceToRoad);
            float baseDesirability = roadDistanceDesirability.Evaluate(normalizedDistance);

            // Bonus for main road access
            float mainRoadBonus = 1f;
            if (preferMainRoads)
            {
                mainRoadBonus = GetMainRoadProximityBonus(position, context);
            }

            // Penalty for dead ends
            float deadEndPenalty = 1f;
            if (avoidDeadEnds)
            {
                deadEndPenalty = GetDeadEndPenalty(position, context);
            }

            // Path quality bonus
            float pathQualityBonus = GetPathQualityScore(position, context);

            float totalDesirability = baseDesirability * mainRoadBonus * deadEndPenalty * pathQualityBonus;

            LogDebug($"Position {position}: base={baseDesirability:F2}, mainRoad={mainRoadBonus:F2}, " +
                    $"deadEnd={deadEndPenalty:F2}, pathQuality={pathQualityBonus:F2}, total={totalDesirability:F2}");

            return Mathf.Clamp01(totalDesirability);
        }

        public override Vector3 ModifyPosition(Vector3 originalPosition, PlacementContext context)
        {
            if (!requiresRoadAccess) return originalPosition;

            // Try to find a position with better road access
            Vector3 nearestRoadPoint = GetNearestRoadPoint(originalPosition, context);
            if (nearestRoadPoint == Vector3.zero) return originalPosition;

            // Move slightly towards the road if it improves access
            Vector3 directionToRoad = (nearestRoadPoint - originalPosition).normalized;
            Vector3 adjustedPosition = originalPosition + directionToRoad * 2f;

            // Validate the adjusted position
            if (CanPlace(adjustedPosition, context))
            {
                float originalDesirability = GetDesirability(originalPosition, context);
                float adjustedDesirability = GetDesirability(adjustedPosition, context);

                if (adjustedDesirability > originalDesirability)
                {
                    LogDebug($"Position adjusted for better road access: {originalPosition} -> {adjustedPosition}");
                    return adjustedPosition;
                }
            }

            return originalPosition;
        }

        private Vector3 GetNearestRoadPoint(Vector3 position, PlacementContext context)
        {
            if (context.collisionManager != null)
            {
                return context.collisionManager.GetNearestRoadPoint(position);
            }

            // Fallback: search in roads array
            if (context.roads != null && context.roads.Length > 0)
            {
                float nearestDistance = float.MaxValue;
                Vector3 nearestPoint = Vector3.zero;

                foreach (var road in context.roads)
                {
                    if (road != null)
                    {
                        float distance = Vector3.Distance(position, road.transform.position);
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestPoint = road.transform.position;
                        }
                    }
                }

                return nearestPoint;
            }

            return Vector3.zero;
        }

        private bool HasClearPathToRoad(Vector3 from, Vector3 to, PlacementContext context)
        {
            if (context.collisionManager != null)
            {
                return context.collisionManager.HasClearPath(from, to, pathWidth);
            }

            // Simplified fallback: check if path crosses buildings
            Vector3 direction = (to - from).normalized;
            float distance = Vector3.Distance(from, to);
            int samples = Mathf.CeilToInt(distance / 2f); // Sample every 2 units

            for (int i = 1; i < samples; i++)
            {
                Vector3 samplePoint = from + direction * (i * 2f);

                // Check for buildings at sample point
                var nearbyBuildings = context.GetObjectsInRadius(samplePoint, pathWidth * 0.5f, ObjectType.Building);
                if (nearbyBuildings.Length > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private float GetMainRoadProximityBonus(Vector3 position, PlacementContext context)
        {
            if (context.roads == null) return 1f;

            float nearestMainRoadDistance = float.MaxValue;

            foreach (var road in context.roads)
            {
                if (road != null && IsMainRoad(road))
                {
                    float distance = Vector3.Distance(position, road.transform.position);
                    nearestMainRoadDistance = Mathf.Min(nearestMainRoadDistance, distance);
                }
            }

            if (nearestMainRoadDistance <= mainRoadPreferenceRadius)
            {
                float proximityFactor = 1f - (nearestMainRoadDistance / mainRoadPreferenceRadius);
                return 1f + (proximityFactor * (mainRoadBonusMultiplier - 1f));
            }

            return 1f;
        }

        private float GetDeadEndPenalty(Vector3 position, PlacementContext context)
        {
            Vector3 roadPoint = GetNearestRoadPoint(position, context);
            if (roadPoint == Vector3.zero) return 1f;

            // Count nearby roads - if only one, it might be a dead end
            var nearbyRoads = context.GetObjectsInRadius(roadPoint, 10f, ObjectType.Street);

            if (nearbyRoads.Length <= 2) // Road segment itself + one connection = potential dead end
            {
                return 0.7f; // 30% penalty
            }

            return 1f;
        }

        private float GetPathQualityScore(Vector3 position, PlacementContext context)
        {
            Vector3 roadPoint = GetNearestRoadPoint(position, context);
            if (roadPoint == Vector3.zero) return 0.5f;

            // Simple path quality based on directness
            float directDistance = Vector3.Distance(position, roadPoint);
            float pathDistance = CalculatePathDistance(position, roadPoint, context);

            if (pathDistance <= 0f) return 0.5f;

            float directnessRatio = directDistance / pathDistance;
            return Mathf.Clamp01(directnessRatio);
        }

        private float CalculatePathDistance(Vector3 from, Vector3 to, PlacementContext context)
        {
            // Simplified path calculation - in a full implementation this would use pathfinding
            if (HasClearPathToRoad(from, to, context))
            {
                return Vector3.Distance(from, to);
            }
            else
            {
                // Estimate longer path around obstacles
                return Vector3.Distance(from, to) * 1.5f;
            }
        }

        private bool IsMainRoad(GameObject road)
        {
            // Check road name or scale to determine if it's a main road
            if (road.name.ToLower().Contains("main") || road.name.ToLower().Contains("major"))
            {
                return true;
            }

            // Check road width (main roads are typically wider)
            Vector3 scale = road.transform.localScale;
            float width = Mathf.Max(scale.x, scale.z);
            return width >= 6f; // Assume main roads are 6+ units wide
        }

        public override bool ValidateRule()
        {
            if (!base.ValidateRule()) return false;

            if (maxDistanceToRoad <= 0f)
            {
                Debug.LogError($"Accessibility Rule {ruleName}: maxDistanceToRoad must be positive");
                return false;
            }

            if (pathWidth <= 0f)
            {
                Debug.LogError($"Accessibility Rule {ruleName}: pathWidth must be positive");
                return false;
            }

            if (mainRoadBonusMultiplier < 1f)
            {
                Debug.LogError($"Accessibility Rule {ruleName}: mainRoadBonusMultiplier should be >= 1.0");
                return false;
            }

            return true;
        }
    }
}