using UnityEngine;

public class WaypointSetupGuide : MonoBehaviour
{
    [TextArea(15, 30)]
    public string setupInstructions = @"WAYPOINT SYSTEM SETUP GUIDE

=== Quick Setup ===
1. Add WaypointSystemSetup component to a GameObject in your scene
2. Assign waypointPrefab and waypointGroupPrefab if you have them
3. Click 'Setup Complete Waypoint System' in the context menu or inspector
4. All waypoint areas and patrol routes will be automatically generated

=== Generated Waypoint Types ===

CITIZENS:
• Market Square - Peasants (8 waypoints, 3 max entities)
• Residential District - Peasants (12 waypoints, 4 max entities)
• Artisan Quarter - Merchants (6 waypoints, 2 max entities)
• Noble Quarter - Nobles (5 waypoints, 3 max entities)
• Castle Grounds - Nobles (10 waypoints, 2 max entities)
• Castle Interior - Royalty (8 waypoints, 3 max entities)
• Chapel Area - Priests (3 waypoints, 1 max entity)

GUARDS:
• Main Gate Guard Post (4 waypoints, 2 max entities)
• Castle Wall East/West (3 waypoints each, 1 max entity)
• Town Patrol Areas (5 waypoints, 1 max entity)

HOUSES:
• 15 house areas distributed across residential zones
• 2 waypoints each (inside/outside)
• 1 max entity per house

=== Configuration Options ===

District Waypoint Counts:
- marketSquareWaypoints: 1-15 (default 8)
- residentialWaypoints: 1-20 (default 12)
- artisanWaypoints: 1-10 (default 6)
- nobleWaypoints: 1-8 (default 5)
- castleGroundsWaypoints: 1-15 (default 10)
- castleInteriorWaypoints: 1-12 (default 8)

Guard Patrol Counts:
- mainGateGuardWaypoints: 2-8 (default 4)
- castleWallGuardWaypoints: 2-6 (default 3)
- townPatrolWaypoints: 2-8 (default 5)

House Settings:
- totalHouseWaypoints: 5-30 (default 15)

=== Context Menu Commands ===

• Setup Complete Waypoint System: Full automatic setup
• Clear All Waypoints: Remove all generated waypoints
• Validate Current Waypoints: Check current waypoint status
• Generate Quick Test Setup: Smaller setup for testing

=== Integration with Other Systems ===

This system automatically integrates with:
• EnhancedSpawner - Uses waypoint groups for entity spawning
• GuardAI - Uses Guard waypoint groups for patrols
• Citizen - Uses citizen waypoint groups for movement
• CitizenScheduleManager - Uses House waypoints for sleep cycles
• DayNightLightingController - Waypoints provide lighting context

=== Performance Considerations ===

The system is designed for:
• Total waypoints: 100-200 individual waypoints
• Waypoint groups: 20-30 groups
• Validation and collision checking built-in
• NavMesh integration for pathfinding validation

=== Troubleshooting ===

If waypoints don't generate:
1. Check NavMesh is baked in the scene
2. Verify terrain or ground colliders exist
3. Adjust maxTerrainSlope if terrain is steep
4. Increase maxPlacementAttempts for complex areas
5. Check obstacle layer mask settings

Common Issues:
• No NavMesh: Waypoints won't validate properly
• Steep terrain: Reduce maxTerrainSlope
• Overlapping areas: Increase minDistanceBetweenWaypoints
• Missing prefabs: System creates basic GameObjects instead

=== Advanced Customization ===

To customize waypoint generation:
1. Modify CreateCitizenWaypointAreas() for citizen routes
2. Modify CreateGuardWaypointAreas() for guard patrols
3. Modify CreateHouseWaypointAreas() for house placement
4. Adjust WaypointGenerator settings in GenerateAllWaypoints()

Each waypoint area supports:
• Custom gizmo colors for visual organization
• Placement modes (Random/Linear)
• Multi-entity support
• Patrol patterns (Sequential/Distributed/RandomStart/Opposite)
• Entity spacing and collision settings";

    [ContextMenu("Show Setup Instructions")]
    void ShowInstructions()
    {
        Debug.Log(setupInstructions);
    }
}