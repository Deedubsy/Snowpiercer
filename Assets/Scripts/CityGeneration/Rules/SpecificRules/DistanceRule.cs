using UnityEngine;
using CityGeneration.Core;

namespace CityGeneration.Rules.SpecificRules
{
    /// <summary>
    /// Rule that enforces distance constraints between objects
    /// Example: Markets should be near residential areas but not too close to military districts
    /// </summary>
    [CreateAssetMenu(fileName = "New Distance Rule", menuName = "City Rules/Distance Rule")]
    public class DistanceRule : PlacementRule
    {
        [Header("Distance Configuration")]
        public DistrictType targetDistrictType;
        public ObjectType targetObjectType = ObjectType.Building;

        [Header("Distance Constraints")]
        public float minDistance = 10f;
        public float maxDistance = 50f;
        public bool mustBeNearby = true;

        [Header("Distance Preferences")]
        public AnimationCurve desirabilityByDistance = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public bool invertDesirability = false;

        public override bool CanPlace(Vector3 position, PlacementContext context)
        {
            float distanceToTarget = GetDistanceToTarget(position, context);

            // If we can't find the target and it's required, fail
            if (mustBeNearby && distanceToTarget == float.MaxValue)
            {
                LogDebug($"Required target {targetDistrictType} not found for position {position}");
                return false;
            }

            // Check distance constraints
            bool withinMinDistance = distanceToTarget >= minDistance;
            bool withinMaxDistance = distanceToTarget <= maxDistance || maxDistance <= 0f;

            bool canPlace = withinMinDistance && withinMaxDistance;

            LogDebug($"Position {position}: distance={distanceToTarget:F1}, min={minDistance}, max={maxDistance}, canPlace={canPlace}");

            return canPlace;
        }

        public override float GetDesirability(Vector3 position, PlacementContext context)
        {
            float distanceToTarget = GetDistanceToTarget(position, context);

            if (distanceToTarget == float.MaxValue)
            {
                return mustBeNearby ? 0f : 0.5f; // Neutral if target not found and not required
            }

            // Normalize distance to 0-1 range based on min/max
            float normalizedDistance;
            if (maxDistance > minDistance && maxDistance > 0f)
            {
                normalizedDistance = Mathf.Clamp01((distanceToTarget - minDistance) / (maxDistance - minDistance));
            }
            else
            {
                normalizedDistance = distanceToTarget / 100f; // Fallback normalization
                normalizedDistance = Mathf.Clamp01(normalizedDistance);
            }

            // Evaluate desirability curve
            float desirability = desirabilityByDistance.Evaluate(normalizedDistance);

            // Invert if needed (for "prefer far away" rules)
            if (invertDesirability)
            {
                desirability = 1f - desirability;
            }

            LogDebug($"Position {position}: distance={distanceToTarget:F1}, normalized={normalizedDistance:F2}, desirability={desirability:F2}");

            return desirability;
        }

        public override float GetInfluenceRadius(PlacementContext context)
        {
            return Mathf.Max(minDistance, maxDistance);
        }

        private float GetDistanceToTarget(Vector3 position, PlacementContext context)
        {
            GameObject nearestTarget = null;
            float nearestDistance = float.MaxValue;

            // Search for target objects
            var nearbyObjects = context.GetObjectsInRadius(position, GetInfluenceRadius(context) * 2f, targetObjectType);

            foreach (var obj in nearbyObjects)
            {
                // Check if this object matches our target district type
                if (MatchesTargetType(obj))
                {
                    float distance = Vector3.Distance(position, obj.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestTarget = obj;
                    }
                }
            }

            // Also check existing districts if we're looking for districts
            if (targetObjectType == ObjectType.Building && context.existingDistricts != null)
            {
                foreach (var district in context.existingDistricts)
                {
                    if (district != null && MatchesTargetType(district))
                    {
                        float distance = Vector3.Distance(position, district.transform.position);
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestTarget = district;
                        }
                    }
                }
            }

            return nearestTarget != null ? nearestDistance : float.MaxValue;
        }

        private bool MatchesTargetType(GameObject obj)
        {
            // Check for district type component
            var districtInfo = obj.GetComponent<DistrictInfo>();
            if (districtInfo != null)
            {
                return districtInfo.districtType == targetDistrictType;
            }

            // Check for building type component
            var buildingInfo = obj.GetComponent<CityGeneration.Generators.BuildingInfo>();
            if (buildingInfo != null)
            {
                // Map building types to district types
                DistrictType buildingDistrict = GetDistrictTypeFromBuilding(buildingInfo.buildingType);
                return buildingDistrict == targetDistrictType;
            }

            // Check by name as fallback
            string objName = obj.name.ToLower();
            string targetName = targetDistrictType.ToString().ToLower();

            return objName.Contains(targetName);
        }

        private DistrictType GetDistrictTypeFromBuilding(BuildingType buildingType)
        {
            switch (buildingType)
            {
                case BuildingType.Castle:
                    return DistrictType.Castle;
                case BuildingType.Cathedral:
                    return DistrictType.Religious;
                case BuildingType.Shop:
                    return DistrictType.Market;
                case BuildingType.Workshop:
                    return DistrictType.Artisan;
                case BuildingType.Barracks:
                    return DistrictType.Military;
                case BuildingType.House:
                    return DistrictType.Residential;
                case BuildingType.Tavern:
                    return DistrictType.Market;
                default:
                    return DistrictType.Residential;
            }
        }

        public override bool ValidateRule()
        {
            if (!base.ValidateRule()) return false;

            if (minDistance < 0f)
            {
                Debug.LogError($"Distance Rule {ruleName}: minDistance cannot be negative");
                return false;
            }

            if (maxDistance > 0f && maxDistance < minDistance)
            {
                Debug.LogError($"Distance Rule {ruleName}: maxDistance must be greater than minDistance");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Component to identify district types on GameObjects
    /// </summary>
    public class DistrictInfo : MonoBehaviour
    {
        public DistrictType districtType;
        public string districtName;
        public float districtRadius = 30f;
        public bool isPrimaryDistrict = true;
    }
}