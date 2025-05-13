using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor.PackageManager; // Added for PackageInfo

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
        string relativePathInPackage = Path.Combine("Editor", targetFileName); // "Editor/custom_modes.json"

        try
        {
            // Get the MonoScript for the current class
            // MonoScript selfScript = MonoScript.FromType(typeof(CustomModeManager));
            MonoScript selfScript = null;
            string[] guids = AssetDatabase.FindAssets("t:MonoScript CustomModeManager");
            foreach (string guid in guids)
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (script != null && script.GetClass() == typeof(CustomModeManager))
                {
                    selfScript = script;
                    break;
                }
            }
            if (selfScript == null)
            {
                Debug.LogError("Could not find MonoScript for CustomModeManager. Cannot determine package path.");
                return null;
            }

            string selfAssetPath = AssetDatabase.GetAssetPath(selfScript);
            if (string.IsNullOrEmpty(selfAssetPath))
            {
                Debug.LogError($"Could not get asset path for CustomModeManager's script. Path was '{selfAssetPath}'. Cannot determine package path.");
                return null;
            }

            // Find the package info for this asset path
            // Requires 'using UnityEditor.PackageManager;'
            UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(selfAssetPath);
            if (packageInfo == null)
            {
                Debug.LogError($"Could not find UnityEditor.PackageManager.PackageInfo for asset path '{selfAssetPath}'. This script might not be part of a package. Ensure it's in a UPM package.");
                return null;
            }
            
            string packageRootAssetPath = packageInfo.assetPath;
            string configAssetPath = Path.Combine(packageRootAssetPath, relativePathInPackage);
            configAssetPath = configAssetPath.Replace("\\", "/"); // Normalize to forward slashes

            if (File.Exists(configAssetPath))
            {
                return configAssetPath;
            }
            else
            {
                string fullSystemPathAttempt = "[Could not determine full path]";
                try { fullSystemPathAttempt = Path.GetFullPath(configAssetPath); } catch { /* ignore */ }
                Debug.LogError($"Constructed config path '{configAssetPath}' (system path: '{fullSystemPathAttempt}') but file does not exist or is not accessible. Custom modes functionality will be impaired.");
                return null;
            }
        }
        catch (Exception e) // Catches System.Exception
        {
            Debug.LogError($"Exception while trying to determine config path for '{relativePathInPackage}': {e.Message}\n{e.StackTrace}");
            return null;
        }
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