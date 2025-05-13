using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

public class BackendManager : EditorWindow
{
    private string swarmForgeProjectPath = "";
    private const string SwarmForgeProjectPathKey = "SwarmForge_ProjectPath";

    [MenuItem("SwarmForge/Backend Manager")]
    public static void ShowWindow()
    {
        GetWindow<BackendManager>("SwarmForge Backend Manager");
    }

    void OnEnable()
    {
        // Load the saved path when the window is enabled
        swarmForgeProjectPath = EditorPrefs.GetString(SwarmForgeProjectPathKey, "");
    }

    void OnGUI()
    {
        GUILayout.Label("SwarmForge Backend Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        GUILayout.Label("Path to your main SwarmForge Project (containing orchestrator.py and python/mcp/Dockerfile):");
        swarmForgeProjectPath = EditorGUILayout.TextField("Project Path", swarmForgeProjectPath);

        if (GUILayout.Button("Save Project Path"))
        {
            EditorPrefs.SetString(SwarmForgeProjectPathKey, swarmForgeProjectPath);
            UnityEngine.Debug.Log("SwarmForge project path saved: " + swarmForgeProjectPath);
            ShowNotification(new GUIContent("Project path saved!"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Ensure Docker is running and the 'swarmforge-mcp' image has been built once manually from your main SwarmForge project: \ncd [Your SwarmForge Project Path]/python/mcp\ndocker build -t swarmforge-mcp .", MessageType.Info);
        EditorGUILayout.Space();

        GUILayout.Label("Manage Backend Services", EditorStyles.boldLabel);

        if (GUILayout.Button("Start MCP Docker Container (swarmforge-mcp)"))
        {
            StartMCPContainer();
        }

        if (GUILayout.Button("Start Python Orchestrator (orchestrator.py)"))
        {
            StartOrchestrator();
        }
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
            process.ErrorDataReceived += (sender, e) => { if (e.Data != null) UnityEngine.Debug.LogError($"[{processName} Error]: {e.Data}"); };

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