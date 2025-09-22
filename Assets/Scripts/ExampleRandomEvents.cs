using UnityEngine;

public class ExampleRandomEvents : MonoBehaviour
{
    [Header("Example Events")]
    public RandomEvent curfewEvent;
    public RandomEvent festivalEvent;
    public RandomEvent stormEvent;
    public RandomEvent vampireHunterEvent;
    public RandomEvent marketDayEvent;
    public RandomEvent guardShiftChangeEvent;

    [ContextMenu("Create Example Events")]
    public void CreateExampleEvents()
    {
        CreateCurfewEvent();
        CreateFestivalEvent();
        CreateStormEvent();
        CreateVampireHunterEvent();
        CreateMarketDayEvent();
        CreateGuardShiftChangeEvent();
    }

    void CreateCurfewEvent()
    {
        curfewEvent = ScriptableObject.CreateInstance<RandomEvent>();
        curfewEvent.name = "Curfew";
        curfewEvent.eventName = "Curfew";
        curfewEvent.description = "A curfew has been declared. Citizens must return to their homes.";
        curfewEvent.duration = 180f; // 3 minutes
        curfewEvent.minTimeToTrigger = 120f; // 2 minutes into night
        curfewEvent.maxTimeToTrigger = 600f; // 10 minutes into night
        curfewEvent.triggerChance = 0.4f;
        curfewEvent.affectsCitizens = true;
        curfewEvent.affectsGuards = true;
        curfewEvent.guardAlertnessChange = GuardAlertnessLevel.Panic;
        curfewEvent.citizensGoInside = true;
        curfewEvent.increaseGuardPatrols = true;

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(curfewEvent, "Assets/Scripts/Events/CurfewEvent.asset");
#endif
    }

    void CreateFestivalEvent()
    {
        festivalEvent = ScriptableObject.CreateInstance<RandomEvent>();
        festivalEvent.name = "Festival";
        festivalEvent.eventName = "Festival";
        festivalEvent.description = "A festival is happening in the town square. More citizens are outside.";
        festivalEvent.duration = 300f; // 5 minutes
        festivalEvent.minTimeToTrigger = 60f; // 1 minute into night
        festivalEvent.maxTimeToTrigger = 480f; // 8 minutes into night
        festivalEvent.triggerChance = 0.3f;
        festivalEvent.affectsCitizens = true;
        festivalEvent.affectsGuards = true;
        curfewEvent.guardAlertnessChange = GuardAlertnessLevel.Alert;
        festivalEvent.citizenSpeedMultiplier = 1.2f;
        festivalEvent.triggerFestival = true;

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(festivalEvent, "Assets/Scripts/Events/FestivalEvent.asset");
#endif
    }

    void CreateStormEvent()
    {
        stormEvent = ScriptableObject.CreateInstance<RandomEvent>();
        stormEvent.name = "Storm";
        stormEvent.eventName = "Storm";
        stormEvent.description = "A storm has rolled in, reducing visibility and making movement difficult.";
        stormEvent.duration = 240f; // 4 minutes
        stormEvent.minTimeToTrigger = 180f; // 3 minutes into night
        stormEvent.maxTimeToTrigger = 540f; // 9 minutes into night
        stormEvent.triggerChance = 0.25f;
        stormEvent.affectsCitizens = true;
        stormEvent.affectsGuards = true;
        stormEvent.guardAlertnessChange = GuardAlertnessLevel.Suspicious;
        stormEvent.citizenSpeedMultiplier = 0.8f;
        stormEvent.guardSpeedMultiplier = 0.8f;
        stormEvent.createStorm = true;

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(stormEvent, "Assets/Scripts/Events/StormEvent.asset");
#endif
    }

    void CreateVampireHunterEvent()
    {
        vampireHunterEvent = ScriptableObject.CreateInstance<RandomEvent>();
        vampireHunterEvent.name = "Vampire Hunter";
        vampireHunterEvent.eventName = "Vampire Hunter";
        vampireHunterEvent.description = "A vampire hunter has arrived in town. Be extra careful!";
        vampireHunterEvent.duration = 360f; // 6 minutes
        vampireHunterEvent.minTimeToTrigger = 300f; // 5 minutes into night
        vampireHunterEvent.maxTimeToTrigger = 720f; // 12 minutes into night
        vampireHunterEvent.triggerChance = 0.2f;
        vampireHunterEvent.affectsCitizens = false;
        vampireHunterEvent.affectsGuards = true;
        vampireHunterEvent.guardAlertnessChange = GuardAlertnessLevel.Panic;
        vampireHunterEvent.increaseGuardPatrols = true;
        vampireHunterEvent.spawnVampireHunter = true;

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(vampireHunterEvent, "Assets/Scripts/Events/VampireHunterEvent.asset");
#endif
    }

    void CreateMarketDayEvent()
    {
        marketDayEvent = ScriptableObject.CreateInstance<RandomEvent>();
        marketDayEvent.name = "Market Day";
        marketDayEvent.eventName = "Market Day";
        marketDayEvent.description = "It's market day! More merchants and citizens are in the market area.";
        marketDayEvent.duration = 420f; // 7 minutes
        marketDayEvent.minTimeToTrigger = 90f; // 1.5 minutes into night
        marketDayEvent.maxTimeToTrigger = 600f; // 10 minutes into night
        marketDayEvent.triggerChance = 0.35f;
        marketDayEvent.affectsCitizens = true;
        marketDayEvent.affectsGuards = true;
        marketDayEvent.guardAlertnessChange = GuardAlertnessLevel.Normal;
        marketDayEvent.citizenSpeedMultiplier = 1.1f;

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(marketDayEvent, "Assets/Scripts/Events/MarketDayEvent.asset");
#endif
    }

    void CreateGuardShiftChangeEvent()
    {
        guardShiftChangeEvent = ScriptableObject.CreateInstance<RandomEvent>();
        guardShiftChangeEvent.name = "Guard Shift Change";
        guardShiftChangeEvent.eventName = "Guard Shift Change";
        guardShiftChangeEvent.description = "The guards are changing shifts. Patrol patterns are temporarily disrupted.";
        guardShiftChangeEvent.duration = 120f; // 2 minutes
        guardShiftChangeEvent.minTimeToTrigger = 240f; // 4 minutes into night
        guardShiftChangeEvent.maxTimeToTrigger = 480f; // 8 minutes into night
        guardShiftChangeEvent.triggerChance = 0.5f;
        guardShiftChangeEvent.affectsCitizens = false;
        guardShiftChangeEvent.affectsGuards = true;
        guardShiftChangeEvent.guardAlertnessChange = GuardAlertnessLevel.Normal;
        guardShiftChangeEvent.guardSpeedMultiplier = 0.7f;

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(guardShiftChangeEvent, "Assets/Scripts/Events/GuardShiftChangeEvent.asset");
#endif
    }
}