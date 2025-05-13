using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

[Serializable]
public class CustomMode
{
    public string name;
    public string description;
    public string system_prompt;
}

[Serializable]
public class CustomModeConfig
{
    public CustomMode[] modes;
}

public class CustomModeManager
{
    private static CustomModeManager instance;
    private CustomModeConfig config;

    public static CustomModeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new CustomModeManager();
                instance.LoadConfig();
            }
            return instance;
        }
    }

    public CustomMode[] GetModes()
    {
        return config?.modes ?? new CustomMode[0];
    }

    public CustomMode GetMode(string name)
    {
        if (config?.modes == null) return null;
        return Array.Find(config.modes, m => m.name == name);
    }

    public string GetSystemPrompt(string modeName)
    {
        var mode = GetMode(modeName);
        return mode?.system_prompt ?? "";
    }

    private string GetConfigPath()
    {
        string targetFileName = "custom_modes.json";
        string targetPathSuffix = Path.Combine("Editor", targetFileName); // e.g., "Editor/custom_modes.json"
        string packageName = "com.swarmforge.tool"; // The name of your package

        // Find assets by name and type. This is more robust than just name.
        string[] guids = AssetDatabase.FindAssets($"{Path.GetFileNameWithoutExtension(targetFileName)} t:TextAsset");
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            // assetPath is relative to the Project root, e.g., "Packages/com.swarmforge.tool/Editor/custom_modes.json"
            // or "Assets/SomeFolder/custom_modes.json"

            // Check if the found asset is the one we're looking for in our package
            // Ensure it's in a "Packages" folder, our specific package, and has the correct suffix.
            if (assetPath.StartsWith($"Packages/{packageName}/") && assetPath.EndsWith(targetPathSuffix))
            {
                // AssetDatabase.GetAssetPath returns a path relative to the project root.
                // File.IO operations generally work well with these project-relative paths.
                // For extra safety, or if issues arise, Path.GetFullPath() can convert it.
                // return Path.GetFullPath(assetPath); // Use this if relative paths cause issues
                return assetPath; // Return project-relative path
            }
        }

        Debug.LogError($"Could not find '{targetPathSuffix}' in package '{packageName}' using AssetDatabase.FindAssets. Custom modes functionality will be impaired.");
        return null;
    }

    private void LoadConfig()
    {
        try
        {
            string fullConfigPath = GetConfigPath();

            if (string.IsNullOrEmpty(fullConfigPath))
            {
                // GetConfigPath already logged an error
                config = new CustomModeConfig { modes = new CustomMode[0] };
                return;
            }
            
            if (!File.Exists(fullConfigPath))
            {
                Debug.LogWarning($"Custom modes configuration file not found at {fullConfigPath}. A new one will be created if modes are added/saved.");
                config = new CustomModeConfig { modes = new CustomMode[0] };
                return;
            }

            string json = File.ReadAllText(fullConfigPath);
            config = JsonUtility.FromJson<CustomModeConfig>(json);

            if (config == null)
            {
                Debug.LogWarning($"Failed to parse custom modes configuration from {fullConfigPath}. Initializing with empty config.");
                config = new CustomModeConfig { modes = new CustomMode[0] };
            }
            else
            {
                Debug.Log($"Custom modes loaded successfully from {fullConfigPath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading custom modes: {e.Message}\nStackTrace: {e.StackTrace}");
            config = new CustomModeConfig { modes = new CustomMode[0] };
        }
    }

    public void SaveConfig()
    {
        try
        {
            string fullConfigPath = GetConfigPath();

            if (string.IsNullOrEmpty(fullConfigPath))
            {
                // GetConfigPath already logged an error, so no need to save.
                Debug.LogError("Cannot save custom modes: configuration file path could not be determined.");
                return;
            }
            
            // Ensure directory exists (Path.GetDirectoryName works with relative paths too)
            string directoryPath = Path.GetDirectoryName(fullConfigPath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string json = JsonUtility.ToJson(config, true);
            File.WriteAllText(fullConfigPath, json);
            Debug.Log($"Custom modes saved successfully to {fullConfigPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving custom modes: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }

    public void AddMode(CustomMode mode)
    {
        var list = new List<CustomMode>(config.modes);
        list.Add(mode);
        config.modes = list.ToArray();
        SaveConfig();
    }

    public void UpdateMode(string name, CustomMode updatedMode)
    {
        for (int i = 0; i < config.modes.Length; i++)
        {
            if (config.modes[i].name == name)
            {
                config.modes[i] = updatedMode;
                SaveConfig();
                return;
            }
        }
    }

    public void DeleteMode(string name)
    {
        var list = new List<CustomMode>(config.modes);
        list.RemoveAll(m => m.name == name);
        config.modes = list.ToArray();
        SaveConfig();
    }
}