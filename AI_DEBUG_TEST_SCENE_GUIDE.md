# AI Debug Test Scene Guide

## Overview
This guide explains how to set up and use the AI debug test scene for analyzing Guard and Citizen behavior in Snowpiercer.

## Features
- **Floating Debug UI**: Real-time AI state and detection information displayed above each entity
- **Detection Progress Slider**: Visual representation of how close AI is to detecting the player
- **State Information**: Current AI state (Patrol, Chase, Alert, etc.)
- **Comprehensive Metrics**: Distance, angles, timers, and behavior flags
- **Interactive Controls**: Reset scene, toggle player movement, teleport player

## Setup Instructions

### 1. Create Debug Prefabs
1. Add `DebugPrefabCreator` component to any GameObject in scene
2. Assign your Guard and Citizen prefabs to the component
3. Use Context Menu options:
   - "Create Debug Guard Prefab"
   - "Create Debug Citizen Prefab" 
   - "Create All Debug Prefabs"
4. Save the created objects as prefabs

### 2. Setup Test Scene
1. Create new empty scene or use existing scene
2. Add `AITestSceneController` component to a GameObject
3. Assign your debug prefabs to the controller
4. Configure spawn positions and waypoint settings:
   - `randomizeGroupPositions`: Whether to randomly place waypoint groups
   - `minGroupDistance`: Minimum distance between Guard and Citizen groups
   - `spawnAreaSize`: Area for random positioning
   - `waypointRadius`: Max radius for waypoints within each group
   - `waypointSpacing`: Minimum spacing between waypoints
5. Enable `autoSetupManagers` to automatically create required managers

### 3. Required Managers
The following managers are automatically created if `autoSetupManagers` is enabled:
- **DebugUIManager**: Handles debug UI canvas and global controls
- **CitizenManager**: Performance-optimized citizen management
- **SpatialGrid**: Spatial partitioning for efficient queries
- **GameManager**: Core game management (if needed)

## Debug UI Information

### Guard Debug Panel
- **Entity Name**: Guard identifier and type
- **State**: Current GuardState (Patrol, Chase, Attack, Alert, Search)
- **Detection Slider**: Progress towards detecting player (0-100%)
- **Distance to Player**: Real-time distance measurement
- **Angle to Player**: Viewing angle between guard forward direction and player
- **View Distance**: Maximum detection range
- **Field of View**: Detection cone angle
- **Detection Time**: Time required for full detection
- **Has Spotted**: Whether guard has spotted player recently
- **Lost Timer**: Time since player was last seen (during chase)
- **Alert Timer**: Time spent in alert state
- **Line of Sight**: Whether obstacles block view to player
- **Agent Info**: NavMesh agent speed, remaining distance, stopped state

### Citizen Debug Panel
- **Entity Name**: Citizen identifier and rarity type
- **State**: Current behavior (Patrolling, Alerting, Panicking, Social, etc.)
- **Detection Slider**: Progress towards detecting player (0-100%)
- **Rarity**: Citizen type (Peasant, Merchant, Priest, Noble, Royalty)
- **Personality**: Personality type and trait values
- **Blood Amount**: Blood value for vampire feeding
- **Social Info**: Nearest citizen for interaction, social range
- **Memory Slots**: Used/total memory slots
- **Environmental Settings**: Noise/light reaction ranges
- **Schedule**: Whether citizen has assigned schedule

## Controls

### Global Controls
- **F1**: Toggle all debug UI on/off
- **R**: Reset entire test scene
- **T**: Toggle player movement on/off
- **Space**: Teleport player to random position
- **G**: Toggle random waypoint group positioning

### Player Movement (when enabled)
- **WASD**: Move player
- **Left Shift**: Sprint (faster movement)
- **Right Click + Mouse**: Look around

## Usage Scenarios

### Testing Detection Systems
1. Position player within guard's view range
2. Watch detection slider fill up over time
3. Move player to test different angles and distances
4. Observe state transitions from Patrol → Chase → Attack

### Testing Citizen Behavior
1. Approach citizen slowly to test detection thresholds
2. Watch for state changes (Patrol → Alert → Panicking)
3. Test social interactions between multiple citizens
4. Observe personality effects on detection times

### Testing Performance Improvements
1. Spawn multiple AI entities using scene controller
2. Monitor frame rate and update frequencies
3. Compare performance with/without Phase 1 optimizations
4. Use Unity Profiler alongside debug UI for detailed analysis

### Testing AI Communication
1. Get detected by one guard
2. Watch other guards receive alerts
3. Observe coordination and formation behaviors
4. Test search patterns when player escapes

## Advanced Features

### Spatial Grid Visualization
- Enable `showGrid` and `showEntities` on SpatialGrid component
- Visualizes spatial partitioning cells and entity positions
- Helps debug spatial query performance

### Waypoint Visualization
- Enable `showWaypointConnections` on AITestSceneController
- Shows patrol routes for guards and citizens
- Helps debug pathfinding and waypoint assignment

### Custom Debug Information
- Implement `IDebugProvider` interface on custom AI components
- Add custom debug entries via `GetDebugData()` method
- Color-code entries for better visual organization

## Troubleshooting

### Debug UI Not Appearing
1. Check that `DebugUIManager.Instance` exists in scene
2. Verify `AIDebugUI` component is attached to AI entities
3. Ensure debug UI is enabled (press F1 to toggle)
4. Check console for missing component warnings

### Performance Issues
1. Reduce debug UI update frequency in `DebugUIManager`
2. Disable expensive debug visualizations
3. Limit number of active debug entities
4. Use Unity Profiler to identify bottlenecks

### AI Not Behaving Correctly
1. Check that AI components have required references (player, waypoints)
2. Verify NavMesh is properly baked for the scene
3. Ensure required managers are present and initialized
4. Check console for missing component errors

## Best Practices

### Testing Workflow
1. Start with single Guard + Player scenario
2. Add Citizen when Guard behavior is working
3. Test edge cases (corners, obstacles, multiple entities)
4. Document findings and adjust AI parameters

### Performance Testing
1. Use `PerformanceProfiler` component alongside debug UI
2. Test with increasing numbers of AI entities
3. Compare before/after performance metrics
4. Profile on target hardware platforms

### Debug Data Analysis
1. Focus on key metrics during testing sessions
2. Record state transition patterns
3. Note any unexpected behaviors or edge cases
4. Use debug data to validate AI design goals

## Extension Points

### Custom Debug Providers
```csharp
public class MyAIDebugProvider : MonoBehaviour, IDebugProvider
{
    public string GetEntityName() { /* implementation */ }
    public string GetCurrentState() { /* implementation */ }
    public float GetDetectionProgress() { /* implementation */ }
    public Vector3 GetPosition() { /* implementation */ }
    public Dictionary<string, object> GetDebugData() { /* implementation */ }
}
```

### Custom Debug UI Elements
- Extend `AIDebugUI` to add specialized UI components
- Create custom debug visualizations using `OnDrawGizmos`
- Add scene-specific debug tools and controls

This debug system provides comprehensive tools for analyzing and optimizing AI behavior in Snowpiercer. Use it to validate the Phase 1 performance improvements and guide future AI development.