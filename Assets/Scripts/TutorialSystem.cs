using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class TutorialStep
{
    public string stepId;
    public string title;
    public string description;
    public string[] instructions;
    public bool requiresAction;
    public string requiredAction;
    public bool isCompleted;
    public float displayTime = 5f;
    public Vector3 highlightPosition = Vector3.zero;
    public bool highlightPlayer = false;
    public bool highlightUI = false;
    public string uiElementName = "";
}

public class TutorialSystem : MonoBehaviour
{
    public static TutorialSystem Instance { get; private set; }

    [Header("Tutorial Settings")]
    public bool enableTutorial = true;
    public bool skipTutorial = false;
    public bool showTutorialOnFirstLaunch = true;

    [Header("UI References")]
    public GameObject tutorialPanel;
    public Text titleText;
    public Text descriptionText;
    public Text instructionText;
    public Button nextButton;
    public Button skipButton;
    public Button closeButton;
    public Image highlightImage;
    public GameObject progressBar;
    public Slider progressSlider;

    [Header("Tutorial Steps")]
    public List<TutorialStep> tutorialSteps = new List<TutorialStep>();

    [Header("Audio")]
    public AudioClip tutorialSound;
    public AudioClip completionSound;

    [Header("Debug")]
    public bool debugMode = false;
    public bool logTutorialProgress = true;

    private int currentStepIndex = 0;
    private bool tutorialActive = false;
    private bool isFirstLaunch = true;
    private AudioSource audioSource;

    public event System.Action OnTutorialStarted;
    public event System.Action OnTutorialCompleted;
    public event System.Action OnTutorialSkipped;
    public event System.Action<int> OnStepChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTutorialSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeTutorialSystem()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Check if this is the first launch
        isFirstLaunch = PlayerPrefs.GetInt("TutorialCompleted", 0) == 0;

        // Create default tutorial steps if none exist
        if (tutorialSteps.Count == 0)
        {
            CreateDefaultTutorialSteps();
        }

        // Set up UI event listeners
        if (nextButton != null)
            nextButton.onClick.AddListener(NextStep);
        if (skipButton != null)
            skipButton.onClick.AddListener(SkipTutorial);
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseTutorial);

        if (debugMode)
        {
            Debug.Log("TutorialSystem initialized");
        }
    }

    void Start()
    {
        if (enableTutorial && showTutorialOnFirstLaunch && isFirstLaunch && !skipTutorial)
        {
            StartCoroutine(StartTutorialDelayed(2f));
        }
    }

    void CreateDefaultTutorialSteps()
    {
        tutorialSteps.Clear();

        // Step 1: Welcome
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "welcome",
            title = "Welcome, Vampire!",
            description = "You are a vampire who must feed on citizens to regain your strength.",
            instructions = new string[] { "You have 8 hours each night to collect blood", "Return to the castle before sunrise or you'll die" },
            requiresAction = false,
            displayTime = 8f
        });

        // Step 2: Movement
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "movement",
            title = "Movement Controls",
            description = "Learn how to move around the city.",
            instructions = new string[] { "WASD or Arrow Keys to move", "Shift to sprint (uses stamina)", "Ctrl to crouch (stealth mode)" },
            requiresAction = true,
            requiredAction = "movement",
            displayTime = 10f,
            highlightPlayer = true
        });

        // Step 3: Feeding
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "feeding",
            title = "Feeding on Citizens",
            description = "Approach citizens and drain their blood.",
            instructions = new string[] { "Get close to a citizen", "Press E to drain blood", "Different citizens give different amounts of blood" },
            requiresAction = true,
            requiredAction = "feeding",
            displayTime = 12f
        });

        // Step 4: Stealth
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "stealth",
            title = "Stealth Mechanics",
            description = "Stay hidden from guards and citizens.",
            instructions = new string[] { "Stay in shadows to remain hidden", "Crouch to reduce visibility", "Avoid being spotted by guards" },
            requiresAction = false,
            displayTime = 8f
        });

        // Step 5: Abilities
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "abilities",
            title = "Vampire Abilities",
            description = "Use your supernatural powers.",
            instructions = new string[] { "Press Q for Night Vision", "Press R for Hypnotic Gaze", "These abilities cost blood" },
            requiresAction = true,
            requiredAction = "abilities",
            displayTime = 10f
        });

        // Step 6: Day/Night Cycle
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "daynight",
            title = "Day/Night Cycle",
            description = "Understand the time system.",
            instructions = new string[] { "You have 8 in-game hours each night", "Return to the castle before sunrise", "Each night you must collect enough blood" },
            requiresAction = false,
            displayTime = 8f
        });

        // Step 7: UI Elements
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "ui",
            title = "User Interface",
            description = "Learn about the game interface.",
            instructions = new string[] { "Top-left: Time remaining", "Top-right: Blood collected", "Bottom: Health and abilities" },
            requiresAction = false,
            displayTime = 8f,
            highlightUI = true
        });

        // Step 8: Completion
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "completion",
            title = "You're Ready!",
            description = "You now know the basics of being a vampire.",
            instructions = new string[] { "Good luck feeding on the citizens!", "Remember to stay hidden", "Return to the castle before sunrise!" },
            requiresAction = false,
            displayTime = 6f
        });
    }

    public void StartTutorial()
    {
        if (!enableTutorial || tutorialActive) return;

        tutorialActive = true;
        currentStepIndex = 0;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        ShowCurrentStep();
        OnTutorialStarted?.Invoke();

        if (logTutorialProgress)
        {
            Debug.Log("Tutorial started");
        }
    }

    public void NextStep()
    {
        if (currentStepIndex < tutorialSteps.Count - 1)
        {
            currentStepIndex++;
            ShowCurrentStep();
            OnStepChanged?.Invoke(currentStepIndex);
        }
        else
        {
            CompleteTutorial();
        }
    }

    public void PreviousStep()
    {
        if (currentStepIndex > 0)
        {
            currentStepIndex--;
            ShowCurrentStep();
            OnStepChanged?.Invoke(currentStepIndex);
        }
    }

    public void SkipTutorial()
    {
        tutorialActive = false;
        
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        OnTutorialSkipped?.Invoke();

        if (logTutorialProgress)
        {
            Debug.Log("Tutorial skipped");
        }
    }

    public void CompleteTutorial()
    {
        tutorialActive = false;
        
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        if (completionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(completionSound);
        }

        OnTutorialCompleted?.Invoke();

        if (logTutorialProgress)
        {
            Debug.Log("Tutorial completed");
        }
    }

    public void CloseTutorial()
    {
        tutorialActive = false;
        
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    void ShowCurrentStep()
    {
        if (currentStepIndex >= tutorialSteps.Count) return;

        TutorialStep step = tutorialSteps[currentStepIndex];
        step.isCompleted = false;

        // Update UI
        if (titleText != null)
            titleText.text = step.title;
        if (descriptionText != null)
            descriptionText.text = step.description;

        // Show instructions
        if (instructionText != null && step.instructions.Length > 0)
        {
            string instructions = "";
            for (int i = 0; i < step.instructions.Length; i++)
            {
                instructions += "• " + step.instructions[i];
                if (i < step.instructions.Length - 1)
                    instructions += "\n";
            }
            instructionText.text = instructions;
        }

        // Update progress
        if (progressSlider != null)
        {
            progressSlider.value = (float)(currentStepIndex + 1) / tutorialSteps.Count;
        }

        // Handle highlighting
        HandleStepHighlighting(step);

        // Play sound
        if (tutorialSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(tutorialSound);
        }

        // Auto-advance if no action required
        if (!step.requiresAction)
        {
            StartCoroutine(AutoAdvanceStep(step.displayTime));
        }

        if (logTutorialProgress)
        {
            Debug.Log($"Tutorial step {currentStepIndex + 1}/{tutorialSteps.Count}: {step.title}");
        }
    }

    void HandleStepHighlighting(TutorialStep step)
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }

        if (step.highlightPlayer)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && highlightImage != null)
            {
                highlightImage.gameObject.SetActive(true);
                // Position highlight around player
                Vector3 screenPos = Camera.main.WorldToScreenPoint(player.transform.position);
                highlightImage.rectTransform.position = screenPos;
            }
        }
        else if (step.highlightUI && !string.IsNullOrEmpty(step.uiElementName))
        {
            GameObject uiElement = GameObject.Find(step.uiElementName);
            if (uiElement != null && highlightImage != null)
            {
                highlightImage.gameObject.SetActive(true);
                highlightImage.rectTransform.position = uiElement.transform.position;
            }
        }
        else if (step.highlightPosition != Vector3.zero && highlightImage != null)
        {
            highlightImage.gameObject.SetActive(true);
            Vector3 screenPos = Camera.main.WorldToScreenPoint(step.highlightPosition);
            highlightImage.rectTransform.position = screenPos;
        }
    }

    IEnumerator AutoAdvanceStep(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (tutorialActive && currentStepIndex < tutorialSteps.Count)
        {
            TutorialStep step = tutorialSteps[currentStepIndex];
            if (!step.requiresAction)
            {
                NextStep();
            }
        }
    }

    IEnumerator StartTutorialDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartTutorial();
    }

    // Action validation methods
    public void ValidateAction(string action)
    {
        if (!tutorialActive || currentStepIndex >= tutorialSteps.Count) return;

        TutorialStep step = tutorialSteps[currentStepIndex];
        if (step.requiresAction && step.requiredAction == action)
        {
            step.isCompleted = true;
            NextStep();
        }
    }

    public void ValidateMovement()
    {
        ValidateAction("movement");
    }

    public void ValidateFeeding()
    {
        ValidateAction("feeding");
    }

    public void ValidateAbilities()
    {
        ValidateAction("abilities");
    }

    // Public methods for external control
    public void ResetTutorial()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 0);
        PlayerPrefs.Save();
        isFirstLaunch = true;
    }

    public bool IsTutorialActive()
    {
        return tutorialActive;
    }

    public int GetCurrentStep()
    {
        return currentStepIndex;
    }

    public int GetTotalSteps()
    {
        return tutorialSteps.Count;
    }

    public float GetProgress()
    {
        return (float)(currentStepIndex + 1) / tutorialSteps.Count;
    }

    [ContextMenu("Start Tutorial")]
    public void StartTutorialFromContext()
    {
        StartTutorial();
    }

    [ContextMenu("Reset Tutorial")]
    public void ResetTutorialFromContext()
    {
        ResetTutorial();
    }

    [ContextMenu("Skip Tutorial")]
    public void SkipTutorialFromContext()
    {
        SkipTutorial();
    }
} 