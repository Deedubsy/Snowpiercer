using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class StringCache
{
    private static readonly Dictionary<string, string> cache = new Dictionary<string, string>();
    private static readonly StringBuilder stringBuilder = new StringBuilder(64);
    
    // Clear cache periodically to prevent memory leaks
    private static int cacheAccessCount = 0;
    private const int CACHE_CLEAR_FREQUENCY = 1000;
    
    public static string GetTimeString(float timeInSeconds)
    {
        int totalMinutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;
        
        string key = $"time_{hours}_{minutes}";
        
        if (!cache.TryGetValue(key, out string cachedString))
        {
            stringBuilder.Clear();
            stringBuilder.Append("Night: ");
            stringBuilder.Append((hours + 1).ToString("00"));
            stringBuilder.Append(":");
            stringBuilder.Append(minutes.ToString("00"));
            stringBuilder.Append(" until sunrise");
            
            cachedString = stringBuilder.ToString();
            cache[key] = cachedString;
        }
        
        CheckCacheClear();
        return cachedString;
    }
    
    public static string GetDayString(int currentDay, int maxDays)
    {
        string key = $"day_{currentDay}_{maxDays}";
        
        if (!cache.TryGetValue(key, out string cachedString))
        {
            stringBuilder.Clear();
            stringBuilder.Append("Day ");
            stringBuilder.Append(currentDay.ToString());
            stringBuilder.Append(" / ");
            stringBuilder.Append(maxDays.ToString());
            
            cachedString = stringBuilder.ToString();
            cache[key] = cachedString;
        }
        
        CheckCacheClear();
        return cachedString;
    }
    
    public static string GetBloodString(float currentBlood, float goalBlood)
    {
        // Round to avoid too many cache entries for similar values
        int roundedCurrent = Mathf.FloorToInt(currentBlood);
        int roundedGoal = Mathf.FloorToInt(goalBlood);
        
        string key = $"blood_{roundedCurrent}_{roundedGoal}";
        
        if (!cache.TryGetValue(key, out string cachedString))
        {
            stringBuilder.Clear();
            stringBuilder.Append("Blood: ");
            stringBuilder.Append(roundedCurrent.ToString());
            stringBuilder.Append(" / ");
            stringBuilder.Append(roundedGoal.ToString());
            
            cachedString = stringBuilder.ToString();
            cache[key] = cachedString;
        }
        
        CheckCacheClear();
        return cachedString;
    }
    
    public static string GetFormattedFloat(float value, int decimals = 1)
    {
        string format = decimals == 0 ? "F0" : $"F{decimals}";
        string key = $"float_{value.ToString(format)}";
        
        if (!cache.TryGetValue(key, out string cachedString))
        {
            cachedString = value.ToString(format);
            cache[key] = cachedString;
        }
        
        CheckCacheClear();
        return cachedString;
    }
    
    public static string GetFormattedInt(int value)
    {
        string key = $"int_{value}";
        
        if (!cache.TryGetValue(key, out string cachedString))
        {
            cachedString = value.ToString();
            cache[key] = cachedString;
        }
        
        CheckCacheClear();
        return cachedString;
    }
    
    public static string GetHealthString(float currentHealth, float maxHealth)
    {
        int roundedCurrent = Mathf.FloorToInt(currentHealth);
        int roundedMax = Mathf.FloorToInt(maxHealth);
        
        string key = $"health_{roundedCurrent}_{roundedMax}";
        
        if (!cache.TryGetValue(key, out string cachedString))
        {
            stringBuilder.Clear();
            stringBuilder.Append("Health: ");
            stringBuilder.Append(roundedCurrent.ToString());
            stringBuilder.Append(" / ");
            stringBuilder.Append(roundedMax.ToString());
            
            cachedString = stringBuilder.ToString();
            cache[key] = cachedString;
        }
        
        CheckCacheClear();
        return cachedString;
    }
    
    public static string GetPercentageString(float percentage)
    {
        int roundedPercentage = Mathf.FloorToInt(percentage);
        string key = $"percent_{roundedPercentage}";
        
        if (!cache.TryGetValue(key, out string cachedString))
        {
            stringBuilder.Clear();
            stringBuilder.Append(roundedPercentage.ToString());
            stringBuilder.Append("%");
            
            cachedString = stringBuilder.ToString();
            cache[key] = cachedString;
        }
        
        CheckCacheClear();
        return cachedString;
    }
    
    // Generic cached string formatting
    public static string GetCachedString(string key, System.Func<string> stringGenerator)
    {
        if (!cache.TryGetValue(key, out string cachedString))
        {
            cachedString = stringGenerator();
            cache[key] = cachedString;
        }
        
        CheckCacheClear();
        return cachedString;
    }
    
    private static void CheckCacheClear()
    {
        cacheAccessCount++;
        if (cacheAccessCount >= CACHE_CLEAR_FREQUENCY)
        {
            ClearCache();
            cacheAccessCount = 0;
        }
    }
    
    public static void ClearCache()
    {
        cache.Clear();
        Debug.Log($"[StringCache] Cache cleared. Memory freed.");
    }
    
    public static void LogCacheStats()
    {
        Debug.Log($"[StringCache] Cache entries: {cache.Count}, Access count: {cacheAccessCount}");
    }
    
    // Preload common strings at game start
    public static void PreloadCommonStrings()
    {
        // Preload common time strings (0-10 hours, 0-59 minutes)
        for (int h = 0; h <= 10; h++)
        {
            for (int m = 0; m < 60; m += 5) // Every 5 minutes
            {
                GetTimeString(h * 3600 + m * 60);
            }
        }
        
        // Preload common day strings
        for (int day = 1; day <= 20; day++)
        {
            GetDayString(day, 10);
            GetDayString(day, 15);
            GetDayString(day, 20);
        }
        
        // Preload common blood values
        for (int blood = 0; blood <= 300; blood += 5)
        {
            GetBloodString(blood, 100);
            GetBloodString(blood, 150);
            GetBloodString(blood, 200);
        }
        
        Debug.Log($"[StringCache] Preloaded {cache.Count} common strings");
    }
}