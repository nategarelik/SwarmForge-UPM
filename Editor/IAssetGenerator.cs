using System;
using System.Threading.Tasks;

namespace SwarmForge.Assets
{
    // Placeholder parameter and result classes - to be defined based on actual needs
    public class AssetGenerationParams
    {
        public string AssetType { get; set; }
        public string AssetName { get; set; }
        public string Description { get; set; } // Example parameter
        // Add other relevant parameters
    }

    public class AssetGenerationResult
    {
        public bool Success { get; set; }
        public string AssetPath { get; set; } // Path to the generated asset in the project
        public string ErrorMessage { get; set; }
    }

    public class AssetGenerationProgress
    {
        public string RequestId { get; set; }
        public float Progress { get; set; } // 0.0 to 1.0
        public string StatusMessage { get; set; }
    }

    public interface IAssetGenerator
    {
        Task<AssetGenerationResult> GenerateAsset(AssetGenerationParams assetParams, string requestId);
        Task<bool> CancelGeneration(string requestId);
        event Action<AssetGenerationProgress> OnProgressUpdate;
    }
}