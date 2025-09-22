# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Snowpiercer** is a Unity 6000.0.40f1 vampire stealth/survival game where players must collect blood from citizens while avoiding detection by guards, with a day/night cycle mechanic requiring return to the castle before sunrise.

## Development Commands

### Unity Setup
- **Unity Version**: 6000.0.40f1 (required)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Input System**: Unity Input System (new)

### Building the Game
1. Open Unity Hub and load the project
2. File → Build Settings
3. Ensure scenes are in correct order:
   - `Assets/Scenes/GamePlay.unity` (main scene)
   - `Assets/AdvancedMobileHorror/Scenes/Scene_MainMenu.unity` (menu)
   - `Assets/AdvancedMobileHorror/Scenes/Enemy_AI.unity` (AI testing)
4. Select target platform and click "Build"

### Running in Editor
1. Open `Assets/Scenes/GamePlay.unity`
2. Press Play button in Unity Editor
3. Use WASD for movement, E for interactions, Shift to sprint, Ctrl to crouch

### Testing
- No automated test framework currently configured
- Manual testing through Unity Editor Play mode
- Use `Assets/AdvancedMobileHorror/Scenes/Enemy_AI.unity` for AI behavior testing

## Architecture Overview

### Script Organization (91 total scripts)

**1. Core Game Management (7 scripts)**
- `GameManager.cs`: Central orchestrator managing day/night cycles, win/lose conditions, and system coordination
- `DifficultyProgression.cs`: Dynamic scaling system affecting all game systems
- `SaveSystem.cs`: Game state persistence using PlayerPrefs
- `GameLogger.cs`: Centralized logging system
- `AchievementSystem.cs`: Achievement tracking and unlocking
- `TutorialSystem.cs`: Tutorial management and progression
- `GameOverScreen.cs`: End game UI and state management

**2. Player Systems (7 scripts)**
- `PlayerController.cs`: Movement (sprint/crouch/jump), stamina, and interactions
- `PlayerHealth.cs`: Damage and health management
- `PlayerHiding.cs`: Stealth mechanics and hiding spots
- `VampireStats.cs`: Manages vampire stats, blood collection (100 units daily goal)
- `VampireAbilities.cs`: Handles blood drinking, temporary upgrades from citizen rarity
- `VampireUpgradeManager.cs`: Upgrade system coordination
- `FirstPersonCamera.cs`: Camera control and perspective

**3. AI Systems (8 scripts)**
- `GuardAI.cs`: Complex patrol/chase/attack states with FOV detection and communication (1,655 lines)
- `Citizen.cs`: NPCs with personality types, memory systems, and rarity tiers (1,650 lines)
- `VampireHunter.cs`: Special enemy type with unique behavior
- `GuardAlertnessManager.cs`: Global guard awareness coordination
- `CitizenPersonality.cs`: Personality system affecting NPC behavior
- `CitizenSchedule.cs`: Individual NPC scheduling
- `CitizenScheduleManager.cs`: Coordinate multiple NPC schedules
- `AISystemIntegrator.cs`: AI system coordination and integration

**4. Navigation/Waypoints (5 scripts)**
- `Waypoint.cs`: Basic waypoint definition and behavior
- `WaypointGroup.cs`: Multi-entity waypoint management
- `WaypointArea.cs`: Area-based waypoint zones
- `WaypointGenerator.cs`: Procedural waypoint generation with collision detection
- `Spawner.cs`: Basic entity spawning system

**5. Object Pooling/Spawning (5 scripts)**
- `EnhancedSpawner.cs`: Advanced entity spawning with sophisticated logic (1,039 lines)
- `ObjectPool.cs`: Performance optimization through object pooling
- `PooledSpawner.cs`: Pool-based spawning system
- `ProjectilePool.cs`: Specialized projectile pooling
- `Projectile.cs`: Projectile behavior and physics

**6. Event/Random Systems (6 scripts)**
- `RandomEventManager.cs`: Dynamic event system managing gameplay events
- `RandomEvent.cs`: Event definitions and configuration
- `ActiveEvent.cs`: Event state tracking and lifecycle
- `ExampleRandomEvents.cs`: Sample event implementations
- `EventUI.cs`: Event UI display and interaction
- `AreaEffect.cs`: Area-based effect system

**7. Environmental/Interaction (9 scripts)**
- `NoiseManager.cs`: Sound detection and propagation system
- `InteractiveObject.cs`: Base class for interactive objects
- `Door.cs`: Door interaction mechanics
- `CityGateTrigger.cs`: Level transition system
- `ShadowTrigger.cs`: Shadow-based hiding mechanics
- `Highlightable.cs`: Object highlighting system
- `DayNightLightingController.cs`: Dynamic lighting system
- Trap Systems: `GarlicTrap.cs`, `HolySymbolTrap.cs`, `SpikeTrap.cs`

**8. Audio Systems (5 scripts)**
- `AudioManager.cs`: Central audio management
- `AudioMixerController.cs`: Audio mixing and control
- `AudioTrigger.cs`: Event-based audio triggering
- Various audio setup guides and documentation

**9. UI/Debug (10 scripts)**
- `VampireUpgradeUI.cs`: Upgrade interface system
- `VampireStatUpgrade.cs`: Stat upgrade UI components
- `InGameDebugConsole.cs`: Debug interface for testing
- `AIDebugUI.cs`: AI entity debug visualization with world-space UI panels
- `DebugUIManager.cs`: Centralized debug UI management and global toggle (F1)
- `GuardAIDebugProvider.cs`: Debug data provider for GuardAI entities
- `CitizenDebugProvider.cs`: Debug data provider for Citizen entities
- `AITestSceneController.cs`: Automated test scene setup for AI behavior testing
- `DebugPrefabCreator.cs`: Utility for creating debug-enabled prefabs
- Various UI-related setup guides

**10. Utility/Support (33 scripts)**
- Various setup guides, summaries, and utility classes

### Architectural Patterns

**Central Hub Pattern**: GameManager serves as singleton orchestrator
**State Machines**: GuardAI and Citizen implement complex state management
**Observer Pattern**: Event-driven communication between systems
**Object Pooling**: Comprehensive pooling for performance optimization
**Component-Based**: Heavy use of Unity's MonoBehaviour composition

### Key Gameplay Flow

1. **Detection Chain**: Citizen/Guard → Progressive detection → Alert → Communication → GameManager tracking
2. **Blood Collection**: Input → VampireAbilities → Citizen drain → Stats update → Progress check → Potential upgrade
3. **Day/Night Cycle**: Timer countdown → Castle return → Daily reset → Difficulty adjustment → Save progress

### Citizen Rarity System
- Peasant → Merchant → Priest → Noble → Royalty
- Higher rarity = more blood + better upgrade chances

### Guard Detection Mechanics
- 90° FOV cone, 140° peripheral vision
- Close range instant detection (5m)
- Progressive detection timers based on distance

## Code Organization

- `/Assets/Scripts/`: All gameplay scripts
- `/Assets/Prefabs/`: Reusable game objects (Player, Guards, Citizens, etc.)
- `/Assets/Scenes/`: Game scenes
- `/Assets/AdvancedMobileHorror/`: Third-party horror game framework
- `/Assets/Polygon*/`: Art assets from Polygon series

## Important Considerations

- Always test AI behaviors in both GamePlay and Enemy_AI scenes
- Guard communication system affects difficulty significantly
- Citizen personality types affect their behavior patterns
- Save system persists between play sessions - clear PlayerPrefs to reset
- Object pooling is critical for performance with many NPCs
- Day/night lighting controlled by `DayNightLightingController.cs`

## Common Development Tasks

### Adding New Citizen Types
1. Create prefab variant from existing citizen prefabs
2. Set `citizenRarity` and `personalityType` in Citizen component
3. Adjust `bloodAmount` and `upgradeChance` values
4. Add to spawner configurations

### Modifying Difficulty
- Edit `DifficultyProgression.cs` for day-by-day changes
- Adjust `GuardAlertnessManager` thresholds
- Modify spawn rates in `EnhancedSpawner` components

### Adding New Vampire Abilities
1. Add to `VampireAbilities.UpgradeType` enum
2. Implement effect in `ApplyUpgrade()` method
3. Add UI representation if needed
4. Configure drop chances in citizen prefabs

## Known Issues and Technical Debt

### Critical Issues

**1. Monolithic Classes**
- `GuardAI.cs` (1,655 lines) - Too complex, needs decomposition
- `Citizen.cs` (1,650 lines) - Multiple responsibilities, hard to maintain
- `EnhancedSpawner.cs` (1,039 lines) - Overly complex spawning logic
- **Impact**: Difficult to test, debug, and extend

**2. Performance Issues**
- `GuardAI.cs:207-325` - Heavy Update() method processing
- `Citizen.cs:1125-1153` - Social interactions every frame
- Multiple `FindObjectsOfType()` calls in Update methods
- **Impact**: Frame rate drops with many NPCs

**3. Tight Coupling**
- `GameManager` has excessive direct dependencies
- `DifficultyProgression` violates single responsibility (lines 318-483)
- Hard-coded references throughout AI systems
- **Impact**: Difficult to test and modify systems independently

### Architecture Issues

**4. Singleton Abuse**
- Multiple singletons: GameManager, VampireStats, NoiseManager, ObjectPool, SaveSystem
- Hidden dependencies and initialization order issues
- **Recommendation**: Implement dependency injection pattern

**5. Error Handling**
- Minimal error handling in most systems
- `SaveSystem` only handles file I/O errors
- No recovery strategies for AI state corruption
- **Impact**: Game crashes and undefined behavior

**6. Code Quality**
- Mixed naming conventions (camelCase/PascalCase)
- Magic numbers throughout codebase
- Minimal inline documentation for complex algorithms
- **Impact**: Maintainability and onboarding difficulties

### Performance Optimization Areas

**7. Spatial Optimization**
- No spatial partitioning for AI detection
- Expensive collision checks in `WaypointGenerator`
- Inefficient guard communication system
- **Recommendation**: Implement spatial hashing or octree

**8. Memory Management**
- Limited object pooling implementation
- Frequent allocations in Update methods
- No garbage collection optimization
- **Impact**: Memory pressure and GC spikes

### Development Recommendations

**Immediate Actions (High Priority)**
1. Refactor `GuardAI` into smaller components (GuardMovement, GuardDetection, etc.)
2. Remove `FindObjectsOfType` calls from Update methods
3. Implement proper error handling and logging
4. Add configuration system for magic numbers

**Short-term Improvements**
1. Implement dependency injection container
2. Add comprehensive unit tests for core systems
3. Create performance profiling tools
4. Implement spatial partitioning for AI

**Long-term Architecture**
1. Consider Entity Component System (ECS) for AI
2. Implement formal state machine framework
3. Add modular behavior tree system
4. Create comprehensive debugging tools

### Testing Strategy
- Currently no automated tests
- Manual testing through Unity Editor only
- **AI Testing**: Use `AITestSceneController` in AI Demo Scene for isolated AI behavior testing
- **Recommendation**: Implement unit tests for core game logic
- Add integration tests for AI behavior
- Create automated performance regression tests

## AI Debug System

### Debug UI Components
- **AIDebugUI**: World-space debug panels that float above AI entities
- **DebugUIManager**: Singleton managing all debug UI with global F1 toggle
- **GuardAIDebugProvider**: Exposes GuardAI debug data (state, detection, patrol info)
- **CitizenDebugProvider**: Exposes Citizen debug data (personality, schedule, social state)

### AI Test Scene Setup
The `AITestSceneController` provides automated test environment setup:

**Required Setup:**
1. Create empty scene with terrain and directional light
2. Add `AITestSceneController` component to empty GameObject
3. Assign prefab references: Guard, Citizen, Player prefabs
4. Configure spawn settings or use defaults

**Auto-Created Components:**
- DebugUIManager with world-space canvas
- CitizenManager, SpatialGrid, GameManager
- Waypoint groups with clustered patterns
- Debug UI components on AI entities

**Controls:**
- **F1**: Toggle AI debug UI visibility globally
- **R**: Reset/regenerate scene
- **T**: Toggle AITestSceneController movement (disable for proper PlayerController)
- **Space**: Teleport player to random position
- **G**: Toggle random group positioning

### Debug UI Features
- **Real-time state display**: Color-coded AI states (green=patrol, red=chase, etc.)
- **Detection visualization**: Progress bars with color gradient
- **Dynamic debug entries**: Key-value pairs for detailed AI info
- **Automatic UI management**: Self-registering components, cleanup on destroy
- **Performance optimized**: Configurable update frequency (default 0.1s)

### Recent Fixes (2024)
- Fixed AIDebugUI to automatically get debug panel prefab from DebugUIManager
- Added proper registration system for global debug toggle
- Resolved "None" references issue in debug UI components
- Improved cleanup and memory management for debug panels

### Debug UI Troubleshooting
- **No debug UI visible**: Press F1 to toggle, ensure DebugUIManager exists
- **UI components None**: Fixed automatically - system creates prefabs programmatically
- **Player movement conflicts**: Press T to disable AITestSceneController movement when using proper Player prefab
- **Performance issues**: Adjust update frequency in DebugUIManager settings