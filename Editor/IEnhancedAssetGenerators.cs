using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using SwarmForge.Assets; // For IAssetGenerator, ProBuilderParams, BlenderParams etc.

namespace SwarmForge.Assets
{
    // Placeholder for BlenderImportSettings - to be defined or replaced
    public class BlenderImportSettings
    {
        // TODO: Define properties for BlenderImportSettings
        public bool ImportCameras { get; set; }
        public bool ImportLights { get; set; }
        public float ScaleFactor { get; set; } = 1.0f;
    }

    public interface IProBuilderGenerator : IAssetGenerator
    {
        Task<GameObject> CreatePrimitive(ProBuilderParams proBuilderParams, string requestId);
        Task<bool> ModifyMesh(GameObject target, List<ProBuilderOperation> operations, string requestId);
        Task<AssetGenerationResult> ExportToAsset(GameObject source, string assetPath, string requestId); // Changed to return AssetGenerationResult for consistency
    }

    public interface IBlenderBridge : IAssetGenerator
    {
        Task<AssetGenerationResult> ExecuteBlenderScript(BlenderParams blenderParams, string requestId); // Changed to use BlenderParams and return AssetGenerationResult
        Task<AssetGenerationResult> ImportBlendFile(string blendPath, BlenderImportSettings settings, string requestId); // Changed to return AssetGenerationResult
        Task<bool> ExportToBlender(GameObject source, string blendPath, string requestId);
    }

    public interface IAIImageGenerator : IAssetGenerator
    {
        Task<Texture2D> GenerateImage(AIImageParams imageParams, string requestId);
        Task<List<string>> GetAvailableStyles(string requestId);
        Task<bool> ApplyStyleTransfer(Texture2D source, string style, string requestId);
    }

    public interface IProceduralGenerator : IAssetGenerator
    {
        Task<GameObject> GenerateProcedural(ProceduralParams proceduralParams, string requestId);
        Task<bool> ModifyProceduralAsset(GameObject target, Dictionary<string, object> parameters, string requestId);
        Task<List<string>> GetAvailableGenerators(string requestId);
    }
}