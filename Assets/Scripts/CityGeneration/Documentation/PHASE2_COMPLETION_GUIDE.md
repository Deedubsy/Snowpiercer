# Phase 2 Completion Guide

## Overview

Phase 2 enhancements have been successfully implemented, transforming the MedievalCityBuilder into an intelligent, modular city generation system. This guide provides setup instructions, testing procedures, and integration details.

## Phase 2 Enhancements Implemented

### 1. Procedural Rule System ✅
- **Files**: `PlacementRule.cs`, `ProceduralRuleEngine.cs`, specific rules in `Rules/SpecificRules/`
- **Features**:
  - Rule-based placement evaluation with weighted scoring
  - Terrain, accessibility, and distance rules
  - District-specific rule sets
  - Parallel candidate evaluation
- **Integration**: Used by `IntelligentDistrictGenerator` for realistic urban planning

### 2. Template-Based Building Variations ✅
- **Files**: `BuildingTemplate.cs`, `BuildingFeature.cs`
- **Features**:
  - ScriptableObject-based building templates
  - Procedural architectural features (chimneys, balconies, signs)
  - Context-aware feature placement
  - Material and weathering systems
- **Integration**: Plugs into existing `BuildingGenerator` workflow

### 3. Automatic NavMesh Integration ✅
- **Files**: `AutoNavMeshGenerator.cs`
- **Features**:
  - Context-aware navigation mesh generation
  - Multi-agent type support (guards vs citizens)
  - Off-mesh link generation for gates and entrances
  - Navigation area classification
  - Connectivity validation testing
- **Integration**: Runs automatically after all city objects are placed

## Quick Setup Guide

### Method 1: Using Phase2Integration Component (Recommended)

1. **Add the Integration Component**:
   ```csharp
   // Add to any GameObject in your scene
   var integration = gameObject.AddComponent<Phase2Integration>();
   ```

2. **Auto-Setup** (if `autoSetupOnStart = true`):
   - All Phase 2 systems will be automatically configured
   - Missing generator components will be added
   - Systems will be ready immediately

3. **Generate Enhanced City**:
   ```csharp
   // Via Inspector context menu: "Generate Enhanced City"
   // Or via code:
   var cityLayout = await integration.GenerateEnhancedCity();
   ```

### Method 2: Manual Setup

1. **Add Core Components**:
   ```csharp
   gameObject.AddComponent<ModularCityGenerator>();
   gameObject.AddComponent<TerrainGenerator>();
   gameObject.AddComponent<WallGenerator>();
   gameObject.AddComponent<StreetGenerator>();
   gameObject.AddComponent<BuildingGenerator>();
   gameObject.AddComponent<IntelligentDistrictGenerator>();
   gameObject.AddComponent<AutoNavMeshGenerator>();
   ```

2. **Configure Generation Order**:
   - The `ModularCityGenerator` automatically handles correct execution order
   - NavMesh generation runs last after all objects are placed

3. **Generate City**:
   ```csharp
   var cityGen = GetComponent<ModularCityGenerator>();
   var cityLayout = await cityGen.GenerateCity();
   ```

## Testing Phase 2 Enhancements

### Automated Testing

Use the `Phase2Integration` component's built-in test methods:

```csharp
// Test all Phase 2 systems
await integration.TestPhase2Systems();

// Test individual systems
await integration.TestRuleSystem();
await integration.TestDistrictGeneration();
await integration.TestNavMeshIntegration();
```

### Manual Testing Checklist

#### 1. Rule System Validation
- [ ] Districts are placed according to terrain preferences (castles on high ground)
- [ ] Markets are placed near road access points
- [ ] Residential areas maintain appropriate distances from other districts
- [ ] Rule evaluation provides reasonable placement scores (0.0-1.0)

#### 2. Building Template System
- [ ] Buildings show visual variety within districts
- [ ] Architectural features are context-appropriate
- [ ] Materials match district wealth levels
- [ ] Weathering effects are applied consistently

#### 3. NavMesh Integration
- [ ] NavMesh covers all accessible areas
- [ ] Off-mesh links connect gates and building entrances
- [ ] Navigation areas are properly classified
- [ ] AI agents can pathfind between major city areas

### Performance Testing

#### Generation Performance
```csharp
// Time the generation process
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
var cityLayout = await cityGenerator.GenerateCity();
stopwatch.Stop();
Debug.Log($"Generation time: {stopwatch.ElapsedMilliseconds}ms");
```

#### Runtime Performance
- Monitor frame rate during generation (should maintain 60fps with progressive generation)
- Check collision system performance using `GetPerformanceStats()`
- Validate memory usage doesn't spike during NavMesh generation

## Integration with Existing Snowpiercer Systems

### AI Integration
The new NavMesh system integrates seamlessly with existing AI:

```csharp
// GuardAI.cs and Citizen.cs will automatically use the generated NavMesh
var navAgent = GetComponent<NavMeshAgent>();
navAgent.SetDestination(targetPosition); // Uses enhanced navigation
```

### Spawning Integration
Update spawners to use the new spatial systems:

```csharp
// Use collision manager for spawn validation
var context = FindObjectOfType<ModularCityGenerator>().currentContext;
bool validPosition = context.collisionManager.IsPositionValid(
    spawnPosition, entityRadius, ObjectType.Character);
```

### Building Feature Integration
Leverage building features for gameplay:

```csharp
// Find hiding spots created by building features
var hidingSpots = FindObjectsOfType<BuildingFeatureComponent>()
    .Where(f => f.providesHiding)
    .ToArray();
```

## Advanced Configuration

### Custom Rule Creation
Create custom placement rules by extending `PlacementRule`:

```csharp
[CreateAssetMenu(fileName = "MyCustomRule", menuName = "City Generation/Rules/Custom Rule")]
public class MyCustomRule : PlacementRule
{
    public override bool CanPlace(Vector3 position, PlacementContext context)
    {
        // Custom placement logic
        return true;
    }

    public override float GetDesirability(Vector3 position, PlacementContext context)
    {
        // Custom scoring logic (0.0 - 1.0)
        return 0.5f;
    }
}
```

### Building Template Customization
Create building templates via ScriptableObject:

```csharp
[CreateAssetMenu(fileName = "BuildingTemplate", menuName = "City Generation/Building Template")]
public class CustomBuildingTemplate : BuildingTemplate
{
    // Override methods for custom building generation
}
```

### NavMesh Configuration
Customize navigation for different AI types:

```csharp
// Configure agent types in AutoNavMeshGenerator
var navMeshGen = GetComponent<AutoNavMeshGenerator>();
navMeshGen.guardAgentTypeID = 0;  // Guards use agent type 0
navMeshGen.citizenAgentTypeID = 1; // Citizens use agent type 1
```

## Performance Optimizations

### Collision System Tuning
```csharp
// Adjust grid cell size for optimal performance
var collision = context.collisionManager;
collision.cellSize = 10f; // Larger cells = less memory, less precision
```

### Rule Engine Optimization
```csharp
// Reduce candidate evaluation for performance
var ruleEngine = districtGen.ruleEngine;
ruleEngine.candidatesPerPosition = 3; // Reduce from default 5
ruleEngine.enableParallelEvaluation = true; // Enable for multi-core
```

### NavMesh Performance
```csharp
// Reduce NavMesh complexity for performance
var navMeshGen = GetComponent<AutoNavMeshGenerator>();
navMeshGen.maxBuildTime = 5f; // Limit build time
navMeshGen.tileSize = 256; // Larger tiles = faster build
```

## Troubleshooting

### Common Issues

1. **"No rules found for district X"**
   - Ensure `createDefaultRules = true` in `IntelligentDistrictGenerator`
   - Or manually assign rule sets in the inspector

2. **"NavMesh generation failed"**
   - Check that terrain exists before NavMesh generation
   - Ensure NavMesh settings are compatible with city scale
   - Verify NavMesh components are present in scene

3. **"Collision manager performance warning"**
   - Increase `cellSize` in collision manager
   - Reduce number of objects being tracked
   - Consider using object pooling for dynamic objects

4. **Generation hangs or freezes**
   - Enable `generateProgressively = true`
   - Reduce `maxGenerationTimePerFrame`
   - Check for infinite loops in custom rules

### Debug Tools

Use the built-in debug and statistics tools:

```csharp
// Show comprehensive generation statistics
integration.ShowEnhancedStatistics();

// Get collision system performance data
var stats = context.collisionManager.GetPerformanceStats();

// Visualize rule evaluation (if debug enabled)
var vizData = ruleEngine.GetVisualizationData();
```

## Next Steps

### Integration Testing
1. Test with existing Snowpiercer AI systems
2. Validate performance with multiple AI agents
3. Ensure save/load compatibility

### Extension Opportunities
1. **Dynamic City Evolution**: Rules that change city over time
2. **Player-Driven Placement**: Interactive city building mode
3. **Advanced AI Behaviors**: Context-aware AI using building features
4. **Procedural Events**: Events that modify city layout

## File Structure Summary

```
Assets/Scripts/CityGeneration/
├── Core/
│   ├── BaseGenerator.cs
│   ├── SpatialGrid.cs
│   ├── CityCollisionManager.cs
│   ├── CityGenerationContext.cs
│   └── GenerationResult.cs
├── Generators/
│   ├── ModularCityGenerator.cs
│   ├── IntelligentDistrictGenerator.cs
│   ├── TerrainGenerator.cs
│   ├── WallGenerator.cs
│   ├── StreetGenerator.cs
│   └── BuildingGenerator.cs
├── Rules/
│   ├── PlacementRule.cs
│   ├── ProceduralRuleEngine.cs
│   └── SpecificRules/
│       ├── TerrainRule.cs
│       ├── DistanceRule.cs
│       └── AccessibilityRule.cs
├── Buildings/
│   ├── BuildingTemplate.cs
│   └── BuildingFeature.cs
├── Navigation/
│   └── AutoNavMeshGenerator.cs
├── Testing/
│   └── ModularCityTester.cs
├── Documentation/
│   ├── PHASE1_COMPLETION_GUIDE.md
│   └── PHASE2_COMPLETION_GUIDE.md
└── Phase2Integration.cs
```

---

## Conclusion

Phase 2 enhancements provide a solid foundation for intelligent, contextual city generation. The modular architecture ensures maintainability while the rule system enables realistic urban planning. The NavMesh integration ensures AI compatibility, and the template system provides endless building variety.

The system is now ready for integration with the main Snowpiercer game and can serve as a foundation for future city generation enhancements.