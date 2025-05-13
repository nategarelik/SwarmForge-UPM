using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine; // Required for GameObject, Texture2D

namespace SwarmForge.Assets
{
    public class AssetGenerationException : Exception
    {
        public string GeneratorType { get; }
        public string OperationType { get; }
        public Dictionary<string, object> Context { get; }

        public AssetGenerationException(string message) : base(message)
        {
            Context = new Dictionary<string, object>();
        }

        public AssetGenerationException(string message, Exception innerException) : base(message, innerException)
        {
            Context = new Dictionary<string, object>();
        }

        public AssetGenerationException(string message, string generatorType, string operationType, Dictionary<string, object> context = null) : base(message)
        {
            GeneratorType = generatorType;
            OperationType = operationType;
            Context = context ?? new Dictionary<string, object>();
        }

        public AssetGenerationException(string message, Exception innerException, string generatorType, string operationType, Dictionary<string, object> context = null) : base(message, innerException)
        {
            GeneratorType = generatorType;
            OperationType = operationType;
            Context = context ?? new Dictionary<string, object>();
        }
    }

    public class AssetGenerationManager
    {
        private readonly IProBuilderGenerator _proBuilderGenerator;
        private readonly IBlenderBridge _blenderBridge;
        private readonly IAIImageGenerator _aiImageGenerator;
        private readonly IProceduralGenerator _proceduralGenerator;

        // Dependencies will be injected (e.g., via constructor or a service locator)
        public AssetGenerationManager(
            IProBuilderGenerator proBuilderGenerator,
            IBlenderBridge blenderBridge,
            IAIImageGenerator aiImageGenerator,
            IProceduralGenerator proceduralGenerator)
        {
            _proBuilderGenerator = proBuilderGenerator;
            _blenderBridge = blenderBridge;
            _aiImageGenerator = aiImageGenerator;
            _proceduralGenerator = proceduralGenerator;
        }

        public async Task<bool> ValidateParameters(EnhancedAssetGenerationParams assetParams)
        {
            if (assetParams == null)
            {
                Debug.LogError("AssetGenerationManager: EnhancedAssetGenerationParams cannot be null.");
                return false;
            }

            if (string.IsNullOrEmpty(assetParams.AssetType))
            {
                Debug.LogError("AssetGenerationManager: AssetType must be specified.");
                return false;
            }
            
            // Add more validation logic based on AssetType and specific tool params
            // For example:
            // if (assetParams.AssetType == "3DModel_ProBuilder" && assetParams.ProBuilder == null)
            // {
            //     Debug.LogError("AssetGenerationManager: ProBuilderParams are required for ProBuilder asset type.");
            //     return false;
            // }

            // This is a basic validation. More specific validation should be added.
            await Task.CompletedTask; // To make the method async as per design, though current validation is sync
            return true;
        }

        public async Task<AssetGenerationResult> GenerateAsset(EnhancedAssetGenerationParams assetParams, string requestId)
        {
            if (!await ValidateParameters(assetParams))
            {
                return new AssetGenerationResult { Success = false, ErrorMessage = "Invalid parameters." };
            }

            try
            {
                switch (assetParams.AssetType?.ToLower()) // Use ToLower for case-insensitive comparison
                {
                    case "3dmodel_probuilder":
                        if (assetParams.ProBuilder == null) return new AssetGenerationResult { Success = false, ErrorMessage = "ProBuilder parameters not provided." };
                        if (_proBuilderGenerator == null) return new AssetGenerationResult { Success = false, ErrorMessage = "ProBuilder generator not initialized." };
                        
                        // Example: Create a primitive. More complex logic will be needed.
                        GameObject primitive = await _proBuilderGenerator.CreatePrimitive(assetParams.ProBuilder, requestId);
                        if (primitive != null)
                        {
                            // Further operations like ModifyMesh or ExportToAsset would go here
                            // For now, let's assume direct export if a path is given or just return success
                            if (!string.IsNullOrEmpty(assetParams.AssetName)) // Assuming AssetName can be used as part of path
                            {
                                // This is a simplified export path. In reality, you'd want a more robust path generation.
                                string exportPath = $"Assets/Generated/{assetParams.AssetName}.prefab"; 
                                AssetGenerationResult exportResult = await _proBuilderGenerator.ExportToAsset(primitive, exportPath, requestId);
                                if(exportResult.Success)
                                {
                                   UnityEngine.Object.DestroyImmediate(primitive); // Clean up temporary GameObject if export was successful
                                }
                                return exportResult;
                            }
                            return new AssetGenerationResult { Success = true, AssetPath = $"In-memory GameObject: {primitive.name}" }; // Placeholder path
                        }
                        return new AssetGenerationResult { Success = false, ErrorMessage = "ProBuilder primitive creation failed." };

                    case "3dmodel_blender":
                        if (assetParams.Blender == null) return new AssetGenerationResult { Success = false, ErrorMessage = "Blender parameters not provided." };
                        if (_blenderBridge == null) return new AssetGenerationResult { Success = false, ErrorMessage = "Blender bridge not initialized." };
                        return await _blenderBridge.ExecuteBlenderScript(assetParams.Blender, requestId);

                    case "texture_ai":
                        if (assetParams.ImageGen == null) return new AssetGenerationResult { Success = false, ErrorMessage = "AI Image parameters not provided." };
                        if (_aiImageGenerator == null) return new AssetGenerationResult { Success = false, ErrorMessage = "AI Image generator not initialized." };
                        
                        Texture2D texture = await _aiImageGenerator.GenerateImage(assetParams.ImageGen, requestId);
                        if (texture != null)
                        {
                            // TODO: Save Texture2D to an asset file (e.g., PNG) and return its path.
                            // For now, returning a success message.
                            // Example: string texturePath = SaveTextureAsPNG(texture, $"Assets/Generated/Textures/{assetParams.AssetName}.png");
                            return new AssetGenerationResult { Success = true, AssetPath = $"In-memory Texture: {texture.name}" }; // Placeholder
                        }
                        return new AssetGenerationResult { Success = false, ErrorMessage = "AI texture generation failed." };

                    case "procedural_unity":
                        if (assetParams.Procedural == null) return new AssetGenerationResult { Success = false, ErrorMessage = "Procedural parameters not provided." };
                        if (_proceduralGenerator == null) return new AssetGenerationResult { Success = false, ErrorMessage = "Procedural generator not initialized." };
                        
                        GameObject proceduralAsset = await _proceduralGenerator.GenerateProcedural(assetParams.Procedural, requestId);
                        if (proceduralAsset != null)
                        {
                            // TODO: Save GameObject to an asset file (e.g., Prefab) and return its path.
                            return new AssetGenerationResult { Success = true, AssetPath = $"In-memory GameObject: {proceduralAsset.name}" }; // Placeholder
                        }
                        return new AssetGenerationResult { Success = false, ErrorMessage = "Procedural asset generation failed." };

                    default:
                        return new AssetGenerationResult { Success = false, ErrorMessage = $"Unsupported asset type: {assetParams.AssetType}" };
                }
            }
            catch (AssetGenerationException agEx)
            {
                Debug.LogError($"AssetGenerationManager Error ({agEx.GeneratorType} - {agEx.OperationType}): {agEx.Message}\nContext: {string.Join(", ", agEx.Context)}");
                return new AssetGenerationResult { Success = false, ErrorMessage = agEx.Message };
            }
            catch (Exception ex)
            {
                Debug.LogError($"AssetGenerationManager - Unexpected error generating asset '{assetParams.AssetName}': {ex.Message}\n{ex.StackTrace}");
                return new AssetGenerationResult { Success = false, ErrorMessage = $"An unexpected error occurred: {ex.Message}" };
            }
        }
        // Note: The CustomEditor part ([CustomEditor(typeof(AssetGenerationManager))]) from the design doc
        // would be in a separate file within an "Editor" folder if this AssetGenerationManager class itself
        // is not an editor script (e.g., if it's part of a runtime system).
        // For now, assuming this manager is part of the core logic and not a MonoBehaviour/ScriptableObject
        // that would typically have a CustomEditor. If it is, it should be in an Editor-specific assembly.
    }
}