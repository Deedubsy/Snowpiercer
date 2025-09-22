# Snowpiercer - Vampire Stealth Game

A Unity 6000.0.40f1 vampire stealth/survival game where players must collect blood from citizens while avoiding detection by guards, featuring advanced disguise mechanics, bell alarm systems, and a dynamic day/night cycle requiring return to the castle before sunrise.

## Table of Contents

- [Project Overview](#project-overview)
- [Installation & Setup](#installation--setup)
- [Architecture & Key Systems](#architecture--key-systems)
- [Usage Examples](#usage-examples)
- [Configuration & Tuning](#configuration--tuning)
- [AI Debug System](#ai-debug-system)
- [Contributing Guidelines](#contributing-guidelines)
- [Troubleshooting & FAQ](#troubleshooting--faq)
- [Credits & License](#credits--license)

## Project Overview

### Core Premise
Play as a vampire infiltrating medieval towns to collect blood while maintaining stealth. The game combines traditional stealth mechanics with unique vampire-themed systems including disguises, bell alarms, and social manipulation.

### Core Gameplay Loop
1. **Infiltrate** - Use disguises and stealth to enter town districts
2. **Hunt** - Locate and drain citizens while managing suspicion levels
3. **Evade** - Avoid or sabotage bell towers when suspicion builds
4. **Escape** - Return to castle before sunrise or face escalating alert levels

### Unique Features
- **Disguise System**: Hooded cloak reduces detection ranges and suspicion buildup
- **Per-NPC Suspicion**: Individual suspicion meters (0-100) that trigger bell-ringing behavior
- **Multi-Stage Bell Alarms**: Calm → Yellow → Orange → Red alert states with dynamic AI responses
- **Sabotage Mechanics**: Wire cutters and tools to disable bell towers
- **Ward System**: Town divided into districts with gates requiring lockpicking/bribery
- **Advanced AI**: Citizens with personalities, memories, and guard communication networks
- **Vampire Powers**: Shadowstep teleportation, wall vision, blood frenzy buffs

### Blood Collection & Progression
- **Daily Goal**: Collect 100 blood units each night (10 days total)
- **Citizen Rarity**: Peasant → Merchant → Priest → Noble → Royalty (increasing blood value)
- **Vampire Buffs**: Random temporary upgrades from rare citizen blood
- **Skill Progression**: Sabotage levels, tool upgrades, enhanced abilities

## Installation & Setup

### Prerequisites
- **Unity Version**: 6000.0.40f1 (required)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Input System**: Unity Input System (new)
- **NavMesh Components**: Unity AI Navigation package

### Step-by-Step Setup

#### 1. Project Import
```bash
1. Clone/download the Snowpiercer project
2. Open Unity Hub → Open → Select project folder
3. Ensure Unity 6000.0.40f1 is installed
4. Wait for initial import and compilation
```

#### 2. Package Dependencies
```
Window → Package Manager → Install:
- Universal RP (11.0.0+)
- AI Navigation (1.1.0+)
- Input System (1.4.0+)
- Cinemachine (2.8.0+)
```

#### 3. Layer Configuration
```
Edit → Project Settings → Tags and Layers:

Layers:
- Layer 8: Player
- Layer 9: Guard  
- Layer 10: Citizen
- Layer 11: Interactive
- Layer 12: Shadow
- Layer 13: IndoorArea

Tags:
- Player
- Guard
- Citizen
- House
- Shadow
- CityGate
- Wall
- BellTower
```

#### 4. NavMesh Baking
```
Window → AI → Navigation:
1. Select all walkable surfaces
2. Mark as "Navigation Static"
3. Bake tab → Bake NavMesh
4. Verify blue overlay covers playable areas
```

#### 5. Scene Setup
```
Main Scenes:
- Assets/Scenes/GamePlay.unity (primary game scene)
- Assets/Scenes/AI Demo Scene.unity (AI testing)

Required GameObjects:
- Player (with PlayerController, VampireStats, VampireAbilities)
- GameManager (game state, day/night cycle)
- NoiseManager (sound propagation)
- GlobalAlertSystem (multi-stage alerts)
- GuardAlertnessManager (guard coordination)
```

#### 6. UI Configuration
```
Assign UI elements in Inspector:
- VampireStats.bloodSlider → Blood progress bar
- VampireStats.dayText → Day counter text
- VampireStats.winScreenUI → Victory screen
- PlayerController.interactPromptUI → Interaction prompts
```

## Architecture & Key Systems

### Core Systems Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   PlayerStats   │◄──►│ VampireAbilities│◄──►│  DisguiseSystem │
│  (enhanced)     │    │   (buffs)       │    │   (cloaking)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ SuspicionMeter  │◄──►│   BellTower     │◄──►│GlobalAlertSystem│
│ (per-NPC)       │    │ (interactive)   │    │ (4-stage alerts)│
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Citizen AI    │◄──►│   Guard AI      │◄──►│   Ward System   │
│ (personalities) │    │ (state machine) │    │ (town districts)│
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### Key Components

#### VampireStats (Enhanced)
- **Location**: `Assets/Scripts/VampireStats.cs:42-305`
- **Purpose**: Central player statistics with disguise and sabotage systems
- **Features**: Detection modifiers, tool management, shadow detection

#### SuspicionMeter Component
- **Location**: `Assets/Scripts/SuspicionMeter.cs`
- **Purpose**: Per-NPC suspicion tracking (0-100 scale)
- **Triggers**: Lurking, witnessing drains, loud noises, sprinting
- **Behavior**: Automatic bell-seeking at 100% suspicion

#### BellTower System
- **Location**: `Assets/Scripts/BellTower.cs`
- **Purpose**: Interactive alarm system with sabotage mechanics
- **Features**: Limited tolls, cooldowns, wire cutter sabotage
- **Integration**: Triggers GlobalAlertSystem advancement

#### GlobalAlertSystem
- **Location**: `Assets/Scripts/GlobalAlertSystem.cs`
- **Purpose**: Four-stage alert coordination (Calm→Yellow→Orange→Red)
- **Spawning**: Search dogs (Orange), Elite guards (Red)
- **Effects**: Speed/detection multipliers, gate locking, NPC fleeing

#### AI State Machines
- **Guard AI**: `Assets/Scripts/GuardAI.cs:1692-1845` (enhanced)
  - States: Patrol, Chase, Attack, Alert, Search
  - Detection: 90° FOV, 140° peripheral vision
  - Communication: Alert sharing between guards
  
- **Citizen AI**: `Assets/Scripts/Citizen.cs:1701-1768` (enhanced)
  - Personalities: Affect behavior patterns
  - Memory System: Tracks suspicious events
  - Social Interactions: NPC-to-NPC communication

### Runtime Interaction Flow

1. **Player Actions** → VampireStats calculates effective detection ranges
2. **AI Detection** → SuspicionMeter accumulates based on behavior
3. **Suspicion Overflow** → NPC seeks nearest BellTower
4. **Bell Activation** → GlobalAlertSystem advances alert level
5. **Alert Effects** → AI behavior modifications, spawning, gate locking
6. **System Feedback** → Player must adapt tactics or escape

## Usage Examples

### Adding New Vampire Buffs

```csharp
// 1. Add to VampireAbilities.UpgradeType enum
public enum UpgradeType
{
    // ... existing types
    BloodRage,      // New buff type
    ShadowCloak,
    VampireSpeed
}

// 2. Implement in ApplyUpgrade method
case UpgradeType.BloodRage:
    stats.walkSpeed *= 2f;
    stats.bloodDrainSpeed *= 0.3f; // Faster drain
    break;

// 3. Add removal logic
case UpgradeType.BloodRage:
    stats.walkSpeed /= 2f;
    stats.bloodDrainSpeed /= 0.3f;
    break;

// 4. Set duration
case UpgradeType.BloodRage: return 45f; // 45 seconds
```

### Bell Tower Sabotage Setup

```csharp
// Enable sabotage on BellTower component
void SetupBellSabotage()
{
    BellTower bell = GetComponent<BellTower>();
    
    // Assign visual elements
    bell.ropeVisual = bellRopeGameObject;
    bell.cutRopeVisual = cutRopeGameObject;
    
    // Set sabotage parameters
    bell.sabotageTime = 5f;           // Time to sabotage
    bell.maxTolls = 3;                // Tolls before exhaustion
    bell.tollCooldown = 30f;          // Cooldown between tolls
}

// Player sabotage interaction
if (Input.GetKeyDown(KeyCode.F) && vampireStats.CanSabotage())
{
    bellTower.StartSabotage();
}
```

### Creating Custom Waypoint Routes

```csharp
// Setup patrol waypoints for guards/citizens
public void CreatePatrolRoute()
{
    // Create waypoint group
    GameObject waypointGroup = new GameObject("PatrolGroup");
    WaypointGroup group = waypointGroup.AddComponent<WaypointGroup>();
    
    // Add individual waypoints
    Vector3[] positions = {
        new Vector3(10, 0, 10),
        new Vector3(20, 0, 10), 
        new Vector3(20, 0, 20),
        new Vector3(10, 0, 20)
    };
    
    foreach (Vector3 pos in positions)
    {
        GameObject wp = new GameObject("Waypoint");
        wp.transform.position = pos;
        Waypoint waypoint = wp.AddComponent<Waypoint>();
        waypoint.waitTime = 3f;
        waypoint.allowedTypes = NPCType.Guard; // or NPCType.Citizen
    }
    
    // Assign to AI
    guard.assignedWaypointGroup = group;
}
```

### Configuring Detection Systems

```csharp
// Customize guard detection parameters
public void SetupGuardDetection(GuardAI guard)
{
    // Field of view
    guard.fieldOfView = 90f;              // Detection cone
    guard.peripheralVisionAngle = 140f;   // Peripheral awareness
    
    // Detection ranges
    guard.viewDistance = 25f;             // Max sight distance
    guard.closeRangeDistance = 5f;        // Instant detection range
    
    // Detection timing
    guard.detectionTime = 2f;             // Time to full detection
    guard.closeRangeDetectionTime = 0.1f; // Quick detection when close
    
    // Alert behavior
    guard.guardAlertRadius = 30f;         // Communication range
    guard.enableGuardCommunication = true;
}
```

## Configuration & Tuning

### VampireStats Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `dailyBloodGoal` | 100f | Blood required per night |
| `spotDistance` | 10f | Base detection range |
| `disguiseDetectionModifier` | 0.5f | Detection reduction when disguised |
| `crouchDetectionModifier` | 0.6f | Detection reduction when crouching |
| `sabotageSkillLevel` | 1 | Sabotage proficiency (1-5) |
| `sabotageToolUses` | 3 | Tools remaining |

### SuspicionMeter Tuning

| Parameter | Default | Description |
|-----------|---------|-------------|
| `maxSuspicion` | 100f | Maximum suspicion level |
| `suspicionDecayRate` | 5f | Decay per second |
| `lurkingSuspicionRate` | 10f | Buildup when lurking |
| `witnessBloodDrainSuspicion` | 50f | Instant suspicion for witnessing |
| `loudNoiseSuspicion` | 20f | Suspicion per noise event |

### GlobalAlertSystem Configuration

| Alert Level | Guard Speed | Detection Range | Audio Sensitivity | Special Effects |
|-------------|-------------|-----------------|-------------------|-----------------|
| Calm | 1.0x | 1.0x | 1.0x | Normal patrol |
| Yellow | 2.0x | 1.2x | 1.3x | Increased vigilance |
| Orange | 2.5x | 1.5x | 1.5x | Spawn search dogs |
| Red | 3.0x | 2.0x | 2.0x | Elite guards, locked gates |

### BellTower Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| `bellRadius` | 100f | Sound travel distance |
| `maxTolls` | 3 | Tolls before exhaustion |
| `tollCooldown` | 30f | Seconds between tolls |
| `sabotageTime` | 5f | Time to sabotage |

### Balancing Guidelines

#### Detection Ranges
- **Close Range**: 5-7m (instant detection)
- **Normal Range**: 15-25m (progressive detection)
- **Peripheral**: 120-140° (slower detection)
- **Disguised**: 50% reduction across all ranges

#### Blood Economy
- **Peasant**: 15-20 blood
- **Merchant**: 25-35 blood  
- **Priest**: 35-45 blood
- **Noble**: 45-60 blood
- **Royalty**: 60-80 blood

#### Alert Timing
- **Alert Decay**: 300s per level
- **Bell Cooldown**: 30s minimum
- **Sabotage Duration**: 5-10s based on skill

## AI Debug System

*The following content is preserved from the existing AI debug documentation:*

### Overview
Comprehensive debug system for analyzing Guard and Citizen behavior with real-time visualization and interactive controls.

### Features
- **Floating Debug UI**: Real-time AI state and detection information
- **Detection Progress Slider**: Visual representation of player detection
- **State Information**: Current AI state (Patrol, Chase, Alert, etc.)
- **Interactive Controls**: Scene reset, player teleport, movement toggle

### Setup Instructions

#### 1. Create Debug Prefabs
```csharp
// Add DebugPrefabCreator component
// Use Context Menu options:
// - "Create Debug Guard Prefab"
// - "Create Debug Citizen Prefab"
// - "Create All Debug Prefabs"
```

#### 2. Setup Test Scene
```csharp
// Add AITestSceneController component
// Configure spawn settings:
AITestSceneController controller = GetComponent<AITestSceneController>();
controller.randomizeGroupPositions = true;
controller.minGroupDistance = 20f;
controller.autoSetupManagers = true;
```

### Debug Controls
- **F1**: Toggle all debug UI on/off
- **R**: Reset entire test scene
- **T**: Toggle player movement
- **Space**: Teleport player to random position
- **G**: Toggle random waypoint positioning

### Debug Information Displayed

#### Guard Debug Panel
- Entity name and current state
- Detection progress (0-100%)
- Distance and angle to player
- View distance and field of view
- Detection timing and alert status
- NavMesh agent information

#### Citizen Debug Panel  
- Entity name and rarity type
- Current behavior state
- Personality type and traits
- Blood amount and social info
- Memory slot usage
- Schedule assignment status

## Contributing Guidelines

### Adding New Buffs
1. **Enum Addition**: Add to `VampireAbilities.UpgradeType`
2. **Apply Logic**: Implement in `ApplyUpgrade()` method
3. **Remove Logic**: Add cleanup in `RemoveUpgrade()`
4. **Duration**: Set in `GetUpgradeDuration()`
5. **Testing**: Use AI Debug Scene for validation

### Adding Alert States
1. **Enum**: Extend `GlobalAlertSystem.AlertState`
2. **Config**: Add to `alertConfigs` array
3. **Effects**: Implement in `ApplyAlertStateEffects()`
4. **Spawning**: Define in `HandleAlertSpawning()`
5. **Integration**: Update AI response methods

### Code Style Conventions
- **Naming**: PascalCase for public, camelCase for private
- **Comments**: Document public APIs and complex algorithms
- **Regions**: Group related functionality
- **Serialization**: Use `[SerializeField]` for Inspector fields
- **Null Checks**: Always verify component references

### Branching Strategy
```
main (stable)
├── develop (integration)
├── feature/new-buff-system
├── feature/ward-expansion
└── bugfix/detection-accuracy
```

### Pull Request Checklist
- [ ] Code compiles without warnings
- [ ] All existing tests pass
- [ ] New features include debug support
- [ ] Documentation updated
- [ ] Performance impact assessed
- [ ] AI behavior validated in debug scene

## Troubleshooting & FAQ

### Common Issues

#### Cursor Lock Problems
```csharp
// Fix stuck cursor in editor
Cursor.lockState = CursorLockMode.None;
Cursor.visible = true;
```

#### NavMesh Bake Errors
1. Check Navigation window settings
2. Verify surfaces marked as "Navigation Static"
3. Ensure Agent Radius allows pathfinding
4. Clear and rebake if corrupted

#### Missing Tags/Layers
```
Error: "Tag 'Player' not found"
Solution: Edit → Project Settings → Tags and Layers
Add required tags: Player, Guard, Citizen, House, Shadow
```

#### AI Not Detecting Player
1. Verify Player layer assignment
2. Check LayerMask settings on AI components
3. Ensure LineOfSight raycast not blocked
4. Test with AI Debug UI enabled

#### Performance Issues
1. Reduce AI update frequencies
2. Limit active debug UI panels
3. Use object pooling for spawned entities
4. Check for expensive FindObjectOfType calls

### Quick Fixes

#### Reset Player Stats
```csharp
VampireStats.instance.currentBlood = 0f;
VampireStats.instance.currentDay = 1;
VampireStats.instance.RemoveDisguise();
```

#### Force Alert State
```csharp
GlobalAlertSystem.Instance.ForceAlertState(GlobalAlertSystem.AlertState.Red);
```

#### Emergency Scene Reset
```csharp
// In AI Debug Scene
AITestSceneController controller = FindObjectOfType<AITestSceneController>();
controller.ResetScene();
```

### Performance Optimization

#### AI Update Frequency
```csharp
// Reduce update frequency for distant AI
if (distanceToPlayer > 50f)
{
    updateInterval = 0.5f; // Update every 0.5 seconds
}
```

#### Spatial Partitioning
```csharp
// Use SpatialGrid for efficient neighbor queries
SpatialGrid grid = SpatialGrid.Instance;
List<ISpatialEntity> nearbyEntities = grid.GetEntitiesInRadius(position, radius);
```

## Credits & License

### Third-Party Assets
- **Polygon Fantasy Characters**: Synty Studios
- **Polygon Knights**: Synty Studios  
- **BOXOPHOBIC Vegetation**: BOXOPHOBIC
- **ExplosiveLLC Components**: ExplosiveLLC
- **Handpainted Grass Textures**: Various artists

### Core Systems
- **AI Framework**: Custom implementation with state machines
- **Spatial Partitioning**: Custom SpatialGrid system
- **Debug Tools**: Custom real-time visualization system
- **Navigation**: Unity NavMesh with custom waypoint system

### Contributors
- **Primary Development**: Snowpiercer Team
- **AI Systems**: Enhanced with performance optimizations
- **Debug Tools**: Comprehensive visualization framework
- **Documentation**: Community contributions welcome

### License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

#### MIT License Summary
```
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
```

### Contact & Support
- **Issues**: Submit via GitHub Issues
- **Discussions**: GitHub Discussions for questions
- **Contributions**: Pull requests welcome
- **Documentation**: Wiki contributions appreciated

---

*Last Updated: January 2025*
*Unity Version: 6000.0.40f1*
*Project Version: 1.0.0*