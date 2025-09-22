using UnityEngine;
using CityGeneration.Core;

namespace CityGeneration.Rules.SpecificRules
{
    /// <summary>
    /// Rule that considers terrain properties for placement decisions
    /// Example: Castles prefer high ground, markets prefer flat areas
    /// </summary>
    [CreateAssetMenu(fileName = "New Terrain Rule", menuName = "City Rules/Terrain Rule")]
    public class TerrainRule : PlacementRule
    {
        [Header("Height Preferences")]
        public bool preferHighGround = false;
        public bool preferLowGround = false;
        public float minElevation = 0f;
        public float maxElevation = 100f;

        [Header("Slope Constraints")]
        public float maxSlope = 0.3f; // Maximum slope (0 = flat, 1 = 45 degrees)
        public bool preferFlatGround = true;

        [Header("Elevation Scoring")]
        public AnimationCurve elevationDesirability = AnimationCurve.Linear(0f, 0.5f, 1f, 1f);
        public AnimationCurve slopeDesirability = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Special Terrain Features")]
        public bool avoidWater = true;
        public bool preferRidges = false;
        public bool avoidValleys = false;

        public override bool CanPlace(Vector3 position, PlacementContext context)
        {
            if (!IsWithinCityBounds(position, context))
            {
                LogDebug($"Position {position} is outside city bounds");
                return false;
            }

            float elevation = GetTerrainHeight(position, context);
            float slope = GetTerrainSlope(position, context);

            // Check elevation constraints
            if (elevation < minElevation || elevation > maxElevation)
            {
                LogDebug($"Position {position}: elevation {elevation:F1} outside range [{minElevation:F1}, {maxElevation:F1}]");
                return false;
            }

            // Check slope constraints
            if (slope > maxSlope)
            {
                LogDebug($"Position {position}: slope {slope:F2} exceeds maximum {maxSlope:F2}");
                return false;
            }

            // Check water avoidance (simplified - check if elevation is very low)
            if (avoidWater && elevation < 1f)
            {
                LogDebug($"Position {position}: avoiding water (elevation {elevation:F1})");
                return false;
            }

            LogDebug($"Position {position}: elevation={elevation:F1}, slope={slope:F2} - VALID");
            return true;
        }

        public override float GetDesirability(Vector3 position, PlacementContext context)
        {
            float elevation = GetTerrainHeight(position, context);
            float slope = GetTerrainSlope(position, context);

            // Normalize elevation to 0-1 range
            float normalizedElevation = 0.5f;
            if (maxElevation > minElevation)
            {
                normalizedElevation = Mathf.Clamp01((elevation - minElevation) / (maxElevation - minElevation));
            }

            // Get base desirability from elevation
            float elevationScore = elevationDesirability.Evaluate(normalizedElevation);

            // Apply elevation preferences
            if (preferHighGround)
            {
                elevationScore = Mathf.Lerp(elevationScore, normalizedElevation, 0.5f);
            }
            else if (preferLowGround)
            {
                elevationScore = Mathf.Lerp(elevationScore, 1f - normalizedElevation, 0.5f);
            }

            // Get slope desirability (normalized slope to 0-1 range)
            float normalizedSlope = Mathf.Clamp01(slope / maxSlope);
            float slopeScore = slopeDesirability.Evaluate(normalizedSlope);

            // Special terrain features
            float featureScore = 1f;

            if (preferRidges)
            {
                featureScore *= GetRidgeScore(position, context);
            }

            if (avoidValleys)
            {
                featureScore *= (1f - GetValleyScore(position, context));
            }

            // Combine scores
            float totalScore = (elevationScore + slopeScore + featureScore) / 3f;

            LogDebug($"Position {position}: elev={elevationScore:F2}, slope={slopeScore:F2}, feature={featureScore:F2}, total={totalScore:F2}");

            return Mathf.Clamp01(totalScore);
        }

        public override Vector3 ModifyPosition(Vector3 originalPosition, PlacementContext context)
        {
            if (!preferFlatGround) return originalPosition;

            // Try to find flatter ground nearby
            float bestSlope = GetTerrainSlope(originalPosition, context);
            Vector3 bestPosition = originalPosition;

            int samples = 8;
            float searchRadius = 5f;

            for (int i = 0; i < samples; i++)
            {
                float angle = (i / (float)samples) * 360f * Mathf.Deg2Rad;
                Vector3 testPos = originalPosition + new Vector3(
                    Mathf.Cos(angle) * searchRadius,
                    0f,
                    Mathf.Sin(angle) * searchRadius
                );

                float testSlope = GetTerrainSlope(testPos, context);

                if (testSlope < bestSlope && CanPlace(testPos, context))
                {
                    bestSlope = testSlope;
                    bestPosition = testPos;
                }
            }

            LogDebug($"Position adjustment: {originalPosition} -> {bestPosition} (slope: {GetTerrainSlope(originalPosition, context):F2} -> {bestSlope:F2})");

            return bestPosition;
        }

        private float GetRidgeScore(Vector3 position, PlacementContext context)
        {
            if (context.terrain == null) return 0.5f;

            float centerHeight = GetTerrainHeight(position, context);
            float sampleDistance = 10f;
            float totalHeightDifference = 0f;
            int samples = 8;

            // Sample heights around the position
            for (int i = 0; i < samples; i++)
            {
                float angle = (i / (float)samples) * 360f * Mathf.Deg2Rad;
                Vector3 samplePos = position + new Vector3(
                    Mathf.Cos(angle) * sampleDistance,
                    0f,
                    Mathf.Sin(angle) * sampleDistance
                );

                float sampleHeight = GetTerrainHeight(samplePos, context);
                totalHeightDifference += Mathf.Max(0f, centerHeight - sampleHeight);
            }

            float averageHeightDifference = totalHeightDifference / samples;
            return Mathf.Clamp01(averageHeightDifference / 5f); // Normalize to 0-1
        }

        private float GetValleyScore(Vector3 position, PlacementContext context)
        {
            if (context.terrain == null) return 0.5f;

            float centerHeight = GetTerrainHeight(position, context);
            float sampleDistance = 10f;
            float totalHeightDifference = 0f;
            int samples = 8;

            // Sample heights around the position
            for (int i = 0; i < samples; i++)
            {
                float angle = (i / (float)samples) * 360f * Mathf.Deg2Rad;
                Vector3 samplePos = position + new Vector3(
                    Mathf.Cos(angle) * sampleDistance,
                    0f,
                    Mathf.Sin(angle) * sampleDistance
                );

                float sampleHeight = GetTerrainHeight(samplePos, context);
                totalHeightDifference += Mathf.Max(0f, sampleHeight - centerHeight);
            }

            float averageHeightDifference = totalHeightDifference / samples;
            return Mathf.Clamp01(averageHeightDifference / 5f); // Normalize to 0-1
        }

        public override bool ValidateRule()
        {
            if (!base.ValidateRule()) return false;

            if (maxElevation < minElevation)
            {
                Debug.LogError($"Terrain Rule {ruleName}: maxElevation must be greater than minElevation");
                return false;
            }

            if (maxSlope < 0f || maxSlope > 2f)
            {
                Debug.LogError($"Terrain Rule {ruleName}: maxSlope should be between 0 and 2");
                return false;
            }

            if (preferHighGround && preferLowGround)
            {
                Debug.LogWarning($"Terrain Rule {ruleName}: preferHighGround and preferLowGround are both enabled - this may cause conflicts");
            }

            return true;
        }
    }
}