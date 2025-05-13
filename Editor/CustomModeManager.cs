using UnityEngine;
using UnityEditor; // Added for PackageInfo
using UnityEditor.PackageManager; // Added for PackageInfo
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
    private const string CONFIG_PATH = "Editor/custom_modes.json";

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

    private void LoadConfig()
    {
        try
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(CustomModeManager).Assembly);
            if (packageInfo == null)
            {
                Debug.LogError("Could not find package information for CustomModeManager. Custom modes will not be loaded.");
                config = new CustomModeConfig { modes = new CustomMode[0] };
                return;
            }
            string fullConfigPath = Path.Combine(packageInfo.resolvedPath, CONFIG_PATH);

            if (!File.Exists(fullConfigPath))
            {
                Debug.LogWarning($"Custom modes configuration file not found at {fullConfigPath}. A new one will be created if modes are added/saved.");
                config = new CustomModeConfig { modes = new CustomMode[0] }; // Initialize with empty config
                return;
            }

            string json = File.ReadAllText(fullConfigPath);
            config = JsonUtility.FromJson<CustomModeConfig>(json);
            if (config == null) // JsonUtility.FromJson can return null if JSON is malformed or empty
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
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(CustomModeManager).Assembly);
            if (packageInfo == null)
            {
                Debug.LogError("Could not find package information for CustomModeManager. Custom modes cannot be saved.");
                return;
            }
            string fullConfigPath = Path.Combine(packageInfo.resolvedPath, CONFIG_PATH);
            
            // Ensure directory exists
            string directoryPath = Path.GetDirectoryName(fullConfigPath);
            if (!Directory.Exists(directoryPath))
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