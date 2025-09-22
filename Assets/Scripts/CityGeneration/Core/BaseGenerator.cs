using UnityEngine;
using System.Threading.Tasks;
using System.Collections;

namespace CityGeneration.Core
{
    /// <summary>
    /// Base class for all city generation modules
    /// Provides common functionality for progressive generation, validation, and optimization
    /// </summary>
    public abstract class BaseGenerator : MonoBehaviour
    {
        [Header("Base Generator Settings")]
        public bool enableProgressReporting = true;
        public bool enableValidation = true;
        public bool enableOptimization = true;
        public bool enableDebugLogging = false;

        protected CityGenerationContext context;
        protected CityCollisionManager collisionManager;
        protected ProgressReporter progressReporter;

        /// <summary>
        /// Main generation method - handles the full generation pipeline
        /// </summary>
        public virtual async Task<GenerationResult> GenerateAsync(CityGenerationContext context)
        {
            this.context = context;
            this.collisionManager = context.collisionManager;
            this.progressReporter = new ProgressReporter(GetType().Name, enableProgressReporting);

            try
            {
                LogDebug($"Starting generation for {GetType().Name}");

                // Pre-generation validation
                if (enableValidation && !await ValidatePreConditions())
                {
                    throw new GenerationException($"Pre-conditions failed for {GetType().Name}");
                }

                progressReporter.SetPhase("Generating");
                var result = await GenerateInternal(context);

                progressReporter.SetPhase("Validating");
                if (enableValidation)
                {
                    await ValidateResult(result);
                }

                progressReporter.SetPhase("Optimizing");
                if (enableOptimization)
                {
                    await OptimizeResult(result);
                }

                progressReporter.Complete();
                LogDebug($"Generation completed for {GetType().Name}");
                return result;
            }
            catch (System.Exception ex)
            {
                progressReporter.Error(ex.Message);
                LogDebug($"Generation failed for {GetType().Name}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Core generation logic - implemented by each generator
        /// </summary>
        protected abstract Task<GenerationResult> GenerateInternal(CityGenerationContext context);

        /// <summary>
        /// Override to add custom pre-condition validation
        /// </summary>
        protected virtual Task<bool> ValidatePreConditions()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Override to add custom result validation
        /// </summary>
        protected virtual Task ValidateResult(GenerationResult result)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Override to add custom optimization
        /// </summary>
        protected virtual Task OptimizeResult(GenerationResult result)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Helper method for progressive generation - yields control periodically
        /// </summary>
        protected async Task YieldControl(int itemsProcessed, int yieldInterval = 5)
        {
            if (itemsProcessed % yieldInterval == 0)
            {
                await Task.Yield();
            }
        }

        /// <summary>
        /// Helper method for updating progress
        /// </summary>
        protected void UpdateProgress(float progress, string message)
        {
            progressReporter?.UpdateProgress(progress, message);
        }

        /// <summary>
        /// Debug logging helper
        /// </summary>
        protected void LogDebug(string message)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[{GetType().Name}] {message}");
            }
        }

        /// <summary>
        /// Create a parent GameObject for organizing generated objects
        /// </summary>
        protected Transform CreateCategoryParent(string categoryName, Transform parent = null)
        {
            var categoryObject = new GameObject(categoryName);

            if (parent != null)
            {
                categoryObject.transform.SetParent(parent);
            }
            else if (context?.cityParent != null)
            {
                categoryObject.transform.SetParent(context.cityParent);
            }

            categoryObject.transform.position = Vector3.zero;
            return categoryObject.transform;
        }

        /// <summary>
        /// Helper method to create basic cube primitives
        /// </summary>
        protected GameObject CreateCube(string name, Vector3 position, Transform parent = null)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;

            if (parent != null)
            {
                cube.transform.SetParent(parent);
            }

            return cube;
        }

        /// <summary>
        /// Helper method to apply materials to objects
        /// </summary>
        protected void ApplyMaterial(GameObject obj, Color color, bool isWall = false)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = color;

                if (isWall)
                {
                    material.SetFloat("_Metallic", 0f);
                    material.SetFloat("_Smoothness", 0.2f);
                }

                renderer.material = material;
            }
        }

        protected virtual void OnDestroy()
        {
            progressReporter?.Dispose();
        }
    }
}