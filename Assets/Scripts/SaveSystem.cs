using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class VampireSaveData
{
    public int currentNight = 1;
    public float totalBlood = 0f;
    public float currentBlood = 0f;
    public List<string> unlockedUpgrades = new List<string>();
    public float spotDistance = 10f;
    public float walkSpeed = 5f;
    public float crouchSpeed = 2f;
    public float killDrainRange = 2f;
    public float bloodDrainSpeed = 2f;
    public float sprintDuration = 5f;
    public float shadowCloakTime = 10f;

    // Scene transition data
    public string currentScene = "";
    public Vector3 playerPosition = Vector3.zero;
    public Vector3 playerRotation = Vector3.zero;
    public bool hasPendingPosition = false;
}

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }
    private const string SaveKey = "VampireSaveData";

    // Temporary storage for scene transitions
    private Vector3 pendingPlayerPosition = Vector3.zero;
    private Vector3 pendingPlayerRotation = Vector3.zero;
    private bool hasPendingPosition = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Check for pending position after scene load
        if (hasPendingPosition)
        {
            ApplyPendingPlayerPosition();
        }
    }

    public void SaveGame()
    {
        VampireSaveData data = new VampireSaveData();

        // Get data from GameManager and VampireStats
        if (GameManager.instance != null)
        {
            data.currentNight = GameManager.instance.currentDay;
            data.currentBlood = GameManager.instance.currentBlood;
        }

        if (VampireStats.instance != null)
        {
            data.totalBlood = VampireStats.instance.totalBlood;
            data.unlockedUpgrades = VampireStats.instance.GetUnlockedUpgrades();
            data.spotDistance = VampireStats.instance.spotDistance;
            data.walkSpeed = VampireStats.instance.walkSpeed;
            data.crouchSpeed = VampireStats.instance.crouchSpeed;
            data.killDrainRange = VampireStats.instance.killDrainRange;
            data.bloodDrainSpeed = VampireStats.instance.bloodDrainSpeed;
            data.sprintDuration = VampireStats.instance.sprintDuration;
            data.shadowCloakTime = VampireStats.instance.shadowCloakTime;
        }

        // Save current scene and player position
        data.currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        SavePlayerPosition(data);

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
        Debug.Log("Game saved: " + json);
    }

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            Debug.Log("No save data found.");
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey);
        VampireSaveData data = JsonUtility.FromJson<VampireSaveData>(json);

        // Apply to GameManager and VampireStats
        if (GameManager.instance != null)
        {
            GameManager.instance.currentDay = data.currentNight;
            GameManager.instance.currentBlood = data.currentBlood;
        }

        if (VampireStats.instance != null)
        {
            VampireStats.instance.totalBlood = data.totalBlood;
            VampireStats.instance.SetUnlockedUpgrades(data.unlockedUpgrades);
            VampireStats.instance.spotDistance = data.spotDistance;
            VampireStats.instance.walkSpeed = data.walkSpeed;
            VampireStats.instance.crouchSpeed = data.crouchSpeed;
            VampireStats.instance.killDrainRange = data.killDrainRange;
            VampireStats.instance.bloodDrainSpeed = data.bloodDrainSpeed;
            VampireStats.instance.sprintDuration = data.sprintDuration;
            VampireStats.instance.shadowCloakTime = data.shadowCloakTime;
        }

        // Load player position if available
        LoadPlayerPosition(data);

        Debug.Log("Game loaded: " + json);
    }

    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        Debug.Log("Save data deleted.");
    }

    // Scene transition support methods
    public void SetPendingPlayerPosition(Vector3 position, Vector3 rotation = default)
    {
        pendingPlayerPosition = position;
        pendingPlayerRotation = rotation;
        hasPendingPosition = true;
        Debug.Log($"Pending player position set: {position}");
    }

    void SavePlayerPosition(VampireSaveData data)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            data.playerPosition = player.transform.position;
            data.playerRotation = player.transform.eulerAngles;
            Debug.Log($"Player position saved: {data.playerPosition}");
        }
    }

    void LoadPlayerPosition(VampireSaveData data)
    {
        if (data.playerPosition != Vector3.zero)
        {
            SetPendingPlayerPosition(data.playerPosition, data.playerRotation);
        }
    }

    void ApplyPendingPlayerPosition()
    {
        if (!hasPendingPosition) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Disable CharacterController temporarily if present
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            // Set position and rotation
            player.transform.position = pendingPlayerPosition;
            if (pendingPlayerRotation != Vector3.zero)
            {
                player.transform.eulerAngles = pendingPlayerRotation;
            }

            // Re-enable CharacterController
            if (controller != null)
            {
                controller.enabled = true;
            }

            Debug.Log($"Player positioned at: {pendingPlayerPosition}");
        }
        else
        {
            Debug.LogWarning("Player not found when trying to apply pending position");
        }

        // Clear pending position
        hasPendingPosition = false;
        pendingPlayerPosition = Vector3.zero;
        pendingPlayerRotation = Vector3.zero;
    }

    // Public methods for CityGateTrigger integration
    public void SetPendingPlayerPosition(Vector3 position)
    {
        SetPendingPlayerPosition(position, Vector3.zero);
    }

    public bool HasPendingPosition()
    {
        return hasPendingPosition;
    }

    public Vector3 GetPendingPosition()
    {
        return pendingPlayerPosition;
    }
} 