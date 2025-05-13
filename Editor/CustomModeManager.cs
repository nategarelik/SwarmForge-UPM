using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

[Serializable]
public class CustomMode
{
    public string name;
    public string description;
    public string prompt_template;
    public string system_prompt;
    public string[] default_tasks;
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

    public string FormatPrompt(string modeName, string input)
    {
        var mode = GetMode(modeName);
        if (mode == null) return input;
        return mode.prompt_template.Replace("{input}", input);
    }

    public string GetSystemPrompt(string modeName)
    {
        var mode = GetMode(modeName);
        return mode?.system_prompt ?? "";
    }

    public string[] GetDefaultTasks(string modeName)
    {
        var mode = GetMode(modeName);
        return mode?.default_tasks ?? new string[0];
    }

    private void LoadConfig()
    {
        try
        {
            string json = File.ReadAllText(CONFIG_PATH);
            config = JsonUtility.FromJson<CustomModeConfig>(json);
            Debug.Log("Custom modes loaded successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading custom modes: {e.Message}");
            config = new CustomModeConfig { modes = new CustomMode[0] };
        }
    }

    public void SaveConfig()
    {
        try
        {
            string json = JsonUtility.ToJson(config, true);
            File.WriteAllText(CONFIG_PATH, json);
            Debug.Log("Custom modes saved successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving custom modes: {e.Message}");
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