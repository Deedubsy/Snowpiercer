using UnityEngine;
using System;

namespace CityGeneration.Core
{
    /// <summary>
    /// Handles progress reporting for generation modules
    /// </summary>
    public class ProgressReporter : IDisposable
    {
        public event Action<GenerationProgress> OnProgress;

        private string generatorName;
        private bool enableReporting;
        private float startTime;
        private GenerationProgress currentProgress;

        public ProgressReporter(string generatorName, bool enableReporting = true)
        {
            this.generatorName = generatorName;
            this.enableReporting = enableReporting;
            this.startTime = Time.realtimeSinceStartup;
            this.currentProgress = new GenerationProgress
            {
                generatorName = generatorName,
                progress = 0f,
                currentPhase = "Starting",
                statusMessage = "Initializing...",
                startTime = startTime
            };

            if (enableReporting)
            {
                OnProgress?.Invoke(currentProgress);
            }
        }

        public void UpdateProgress(float progress, string statusMessage)
        {
            if (!enableReporting) return;

            currentProgress.progress = Mathf.Clamp01(progress);
            currentProgress.statusMessage = statusMessage;
            currentProgress.elapsedTime = Time.realtimeSinceStartup - startTime;

            OnProgress?.Invoke(currentProgress);
        }

        public void SetPhase(string phaseName)
        {
            if (!enableReporting) return;

            currentProgress.currentPhase = phaseName;
            OnProgress?.Invoke(currentProgress);
        }

        public void Complete()
        {
            if (!enableReporting) return;

            currentProgress.progress = 1f;
            currentProgress.currentPhase = "Complete";
            currentProgress.statusMessage = "Generation completed successfully";
            currentProgress.elapsedTime = Time.realtimeSinceStartup - startTime;
            currentProgress.isComplete = true;

            OnProgress?.Invoke(currentProgress);
        }

        public void Error(string errorMessage)
        {
            if (!enableReporting) return;

            currentProgress.currentPhase = "Error";
            currentProgress.statusMessage = errorMessage;
            currentProgress.elapsedTime = Time.realtimeSinceStartup - startTime;
            currentProgress.hasError = true;

            OnProgress?.Invoke(currentProgress);
            Debug.LogError($"[{generatorName}] Generation error: {errorMessage}");
        }

        public void Dispose()
        {
            OnProgress = null;
        }
    }

    /// <summary>
    /// Progress information for a single generation module
    /// </summary>
    [System.Serializable]
    public class GenerationProgress
    {
        public string generatorName;
        public float progress; // 0-1
        public string currentPhase;
        public string statusMessage;
        public float startTime;
        public float elapsedTime;
        public int objectsGenerated;
        public bool isComplete;
        public bool hasError;

        public float EstimatedTimeRemaining
        {
            get
            {
                if (progress <= 0f) return 0f;
                return (elapsedTime / progress) - elapsedTime;
            }
        }
    }

    /// <summary>
    /// Exception thrown during generation
    /// </summary>
    public class GenerationException : Exception
    {
        public GenerationException(string message) : base(message) { }
        public GenerationException(string message, Exception innerException) : base(message, innerException) { }
    }
}