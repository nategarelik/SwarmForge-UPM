using UnityEngine;
using System.Collections.Generic;

namespace SwarmForge.Assets
{
    // Placeholder for AssetMetadata - to be defined or replaced
    public class AssetMetadata
{
    // TODO: Define properties for AssetMetadata
    public string Author { get; set; }
    public System.DateTime CreationDate { get; set; }
    public string Version { get; set; }
}

// Placeholder for ProBuilderOperation - to be defined or replaced
public class ProBuilderOperation
{
    // TODO: Define properties for ProBuilderOperation
    public string OperationType { get; set; } // e.g., "Extrude", "Bevel"
    public Dictionary<string, object> Parameters { get; set; }
}

public class EnhancedAssetGenerationParams : AssetGenerationParams
{
    // Tool-specific properties
    public ProBuilderParams ProBuilder { get; set; }
    public BlenderParams Blender { get; set; }
    public AIImageParams ImageGen { get; set; }
    public ProceduralParams Procedural { get; set; }
    
    // Common properties
    public Dictionary<string, object> CustomProperties { get; set; }
    public AssetMetadata Metadata { get; set; }

    public EnhancedAssetGenerationParams()
    {
        CustomProperties = new Dictionary<string, object>();
        Metadata = new AssetMetadata();
    }
}

public class ProBuilderParams
{
    public string PrimitiveType { get; set; }  // Cube, Sphere, etc.
    public Vector3 Dimensions { get; set; }
    public List<ProBuilderOperation> Operations { get; set; }

    public ProBuilderParams()
    {
        Operations = new List<ProBuilderOperation>();
    }
}

public class BlenderParams
{
    public string BlendFilePath { get; set; } // Path to a .blend file to use as a base or template
    public string PythonScriptPath { get; set; } // Path to a Python script for Blender to execute
    public Dictionary<string, object> Parameters { get; set; } // Parameters to pass to the Python script

    public BlenderParams()
    {
        Parameters = new Dictionary<string, object>();
    }
}

public class AIImageParams
{
    public string Prompt { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Style { get; set; } // e.g., "Photorealistic", "Cartoon", "Impressionistic"
    public Dictionary<string, float> Parameters { get; set; } // e.g., "cfg_scale", "steps"

    public AIImageParams()
    {
        Parameters = new Dictionary<string, float>();
    }
}

public class ProceduralParams
{
    public string GeneratorType { get; set; } // e.g., "Terrain", "LSystemTree", "Building"
    public Dictionary<string, object> Parameters { get; set; }
    public List<string> Dependencies { get; set; } // e.g., other assets or scripts required

    public ProceduralParams()
    {
        Parameters = new Dictionary<string, object>();
        Dependencies = new List<string>();
    }
}
} // Close SwarmForge.Assets namespace