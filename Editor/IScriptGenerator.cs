using System;
using System.Threading.Tasks;

namespace SwarmForge.Scripting
{
    // Placeholder parameter and result classes - to be defined based on actual needs
    public class ScriptGenerationParams
    {
        public string ScriptName { get; set; }
        public string Description { get; set; } // Detailed description of script functionality
        public string TargetGameObject { get; set; } // Optional: GameObject to attach script to
        // Add other relevant parameters like required methods, variables, etc.
    }

    public class ScriptGenerationResult
    {
        public bool Success { get; set; }
        public string ScriptContent { get; set; }
        public string FilePath { get; set; } // Path where the script was saved
        public string ErrorMessage { get; set; }
    }

    public class ScriptGenerationProgress
    {
        public string RequestId { get; set; }
        public float Progress { get; set; } // 0.0 to 1.0
        public string StatusMessage { get; set; }
    }

    public interface IScriptGenerator
    {
        Task<ScriptGenerationResult> GenerateScript(ScriptGenerationParams scriptParams, string requestId);
        Task<bool> ValidateScript(string scriptContent, string requestId); // Potentially send to backend for validation
        event Action<ScriptGenerationProgress> OnProgressUpdate;
    }
}