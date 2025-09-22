using UnityEngine;

[System.Serializable]
public class ActiveEvent
{
    public RandomEvent eventData;
    public float startTime;
    public float remainingDuration;
    public bool isActive;
    
    public ActiveEvent(RandomEvent eventData, float startTime)
    {
        this.eventData = eventData;
        this.startTime = startTime;
        this.remainingDuration = eventData.duration;
        this.isActive = true;
    }
    
    public void UpdateDuration(float deltaTime)
    {
        if (isActive)
        {
            remainingDuration -= deltaTime;
            if (remainingDuration <= 0f)
            {
                isActive = false;
            }
        }
    }
} 