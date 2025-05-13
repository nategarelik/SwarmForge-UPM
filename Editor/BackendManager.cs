using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using SwarmForge.Core; // Added for SwarmForgeCoreService
using System.Text.RegularExpressions; // Added for Regex

namespace SwarmForge.Editor
{
    public class BackendManager : EditorWindow
    {
        private string swarmForgeProjectPath = "";
        private const string SwarmForgeProjectPathKey = "SwarmForge_ProjectPath";

        [MenuItem("SwarmForge/Backend Manager")]
        public static void ShowWindow()
        {
            GetWindow<BackendManager>("SwarmForge Backend Manager");
        }

        private void OnEnable()
        {
            // Load the saved path when the window is enabled
            swarmForgeProjectPath = EditorPrefs.GetString(SwarmForgeProjectPathKey, "");
        }

    void OnGUI()
    {
        GUILayout.Label("SwarmForge Backend Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Project Path Section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Project Configuration", EditorStyles.boldLabel);
        
        GUILayout.Label("Path to your main SwarmForge Project (containing orchestrator.py and python/mcp/Dockerfile):");
        swarmForgeProjectPath = EditorGUILayout.TextField("Project Path", swarmForgeProjectPath);

        if (GUILayout.Button("Save Project Path"))
        {
            EditorPrefs.SetString(SwarmForgeProjectPathKey, swarmForgeProjectPath);
            UnityEngine.Debug.Log("SwarmForge project path saved: " + swarmForgeProjectPath);
            ShowNotification(new GUIContent("Project path saved!"));
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Custom Modes Section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Custom Modes Management", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Sync Custom Modes"))
        {
            SaveCustomModes();
            ShowNotification(new GUIContent("Custom modes synced!"));
        }
        
        EditorGUILayout.HelpBox("Custom modes are automatically synced when starting the orchestrator.", MessageType.Info);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Backend Services Section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Manage Backend Services", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox("Ensure Docker is running and the 'swarmforge-mcp' image has been built once manually from your main SwarmForge project: \ncd [Your SwarmForge Project Path]/python/mcp\ndocker build -t swarmforge-mcp .", MessageType.Info);
        
        if (GUILayout.Button("Start MCP Docker Container (swarmforge-mcp)"))
        {
            StartMCPContainer();
        }

        if (GUILayout.Button("Start Python Orchestrator (orchestrator.py)"))
        {
            SaveCustomModes(); // Sync custom modes before starting
            StartOrchestrator();
        }
        EditorGUILayout.EndVertical();
    }

    private void StartMCPContainer()
    {
        if (string.IsNullOrEmpty(swarmForgeProjectPath))
        {
            UnityEngine.Debug.LogError("SwarmForge Project Path is not set in the Backend Manager.");
            ShowNotification(new GUIContent("Error: Project Path not set!"));
            return;
        }

        // We assume 'swarmforge-mcp' image is already built.
        // Docker typically needs to be run from a context where it can access the Docker daemon.
        // The working directory for docker run itself usually doesn't matter for a pre-built image,
        // unless the image expects volumes mounted relative to a specific path at runtime (not typical for this setup).
        string command = "docker";
        string args = "run -d -p 8000:8000 swarmforge-mcp";

        ExecuteCommand(command, args, "MCP Docker Container");
    }

        private string GetCustomModesPath()
        {
            return Path.Combine(swarmForgeProjectPath, "Editor", "custom_modes.json");
        }

        private void SaveCustomModes()
        {
            if (string.IsNullOrEmpty(swarmForgeProjectPath))
            {
                UnityEngine.Debug.LogError("SwarmForge Project Path is not set.");
                return;
            }

            string customModesPath = GetCustomModesPath();
            if (File.Exists(customModesPath))
            {
                File.Copy(customModesPath, Path.Combine(swarmForgeProjectPath, "python", "custom_modes.json"), true);
                UnityEngine.Debug.Log("Custom modes copied to Python directory");
            }
        }

        private void StartOrchestrator()
    {
        if (string.IsNullOrEmpty(swarmForgeProjectPath))
        {
            UnityEngine.Debug.LogError("SwarmForge Project Path is not set in the Backend Manager.");
            ShowNotification(new GUIContent("Error: Project Path not set!"));
            return;
        }

        if (!File.Exists(Path.Combine(swarmForgeProjectPath, "orchestrator.py")))
        {
            UnityEngine.Debug.LogError("orchestrator.py not found at the specified project path: " + Path.Combine(swarmForgeProjectPath, "orchestrator.py"));
            ShowNotification(new GUIContent("Error: orchestrator.py not found!"));
            return;
        }

        string command = "python";
        string args = "orchestrator.py";
        
        ExecuteCommand(command, args, "Python Orchestrator", swarmForgeProjectPath);
    }

        private void ExecuteCommand(string command, string arguments, string processName, string workingDirectory = "")
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false, // Set to false to redirect output (if needed later)
                RedirectStandardOutput = true, // For logging output
                RedirectStandardError = true,  // For logging errors
                CreateNoWindow = true         // Don't create a visible window
            };

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            Process process = new Process { StartInfo = startInfo };

            process.OutputDataReceived += (sender, e) => { if (e.Data != null) UnityEngine.Debug.Log($"[{processName} Output]: {e.Data}"); };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    UnityEngine.Debug.Log($"[{processName} Error]: {e.Data}");
                    if (processName == "Python Orchestrator" && e.Data.Contains("Orchestrator server started on fallback port"))
                    {
                        // Try to parse the port
                        // Example log: "[Python Orchestrator Error]: INFO:__main__:Orchestrator server started on fallback port 56781"
                        var match = Regex.Match(e.Data, @"fallback port (\d+)");
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int fallbackPort))
                        {
                            UnityEngine.Debug.Log($"[{processName}] Detected fallback port: {fallbackPort}. Updating WebSocketClient.");
                            if (SwarmForgeCoreService.Instance != null && SwarmForgeCoreService.Instance.WsClient != null)
                            {
                                SwarmForgeCoreService.Instance.WsClient.UpdateServerPort(fallbackPort);
                            }
                            else
                            {
                                UnityEngine.Debug.LogError($"[{processName}] SwarmForgeCoreService.Instance or WsClient is null. Cannot update port.");
                            }
                        }
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            // Optionally, we can wait for exit if it's a short command, but for servers, we don't.
            // process.WaitForExit(); // This would block Unity editor

            ShowNotification(new GUIContent($"{processName} started. Check console for output."));
            UnityEngine.Debug.Log($"{processName} started with command: {command} {arguments} in working dir: '{workingDirectory}'");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Error starting {processName}: {ex.Message}");
            ShowNotification(new GUIContent($"Error starting {processName}!"));
        }
    }
    }
}