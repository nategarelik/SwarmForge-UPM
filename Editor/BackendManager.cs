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
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.swarmforge.tool/Editor/SwarmForgeStyles.uss");
        rootVisualElement.styleSheets.Add(styleSheet);

        var container = new VisualElement { name = "MainContainer" };
        container.AddToClassList("main-container");
        rootVisualElement.Add(container);

        CreateHeaderSection(container);
        CreateConfigSection(container);
        CreateServicesSection(container);
        CreateStatusSection(container);
    }

    private void CreateHeaderSection(VisualElement parent)
    {
        var header = new VisualElement { name = "Header" };
        header.AddToClassList("header");
        
        var title = new Label("SwarmForge Backend") { name = "Title" };
        title.AddToClassList("title");
        header.Add(title);

        var subtitle = new Label("Service Manager") { name = "Subtitle" };
        subtitle.AddToClassList("subtitle");
        header.Add(subtitle);

        parent.Add(header);
    }

    private void CreateConfigSection(VisualElement parent)
    {
        var section = new VisualElement { name = "ConfigSection" };
        section.AddToClassList("section");

        var header = new Label("Configuration") { name = "ConfigHeader" };
        header.AddToClassList("section-header");
        section.Add(header);

        var pathField = new TextField("SwarmForge Project Path") {
            value = swarmForgeProjectPath,
            name = "ProjectPathField"
        };
        pathField.RegisterValueChangedCallback(evt => swarmForgeProjectPath = evt.newValue);
        pathField.AddToClassList("path-field");
        section.Add(pathField);

        var saveButton = new Button(() => {
            EditorPrefs.SetString(SwarmForgeProjectPathKey, swarmForgeProjectPath);
            UnityEngine.Debug.Log("SwarmForge project path saved: " + swarmForgeProjectPath);
            ShowNotification(new GUIContent("Project path saved!"));
        }) { text = "Save Path" };
        saveButton.AddToClassList("primary-button");
        section.Add(saveButton);

        var helpBox = new HelpBox(
            "Ensure Docker is running and the 'swarmforge-mcp' image has been built:\ncd [Project Path]/python/mcp && docker build -t swarmforge-mcp .",
            HelpBoxMessageType.Info
        );
        helpBox.AddToClassList("help-box");
        section.Add(helpBox);

        parent.Add(section);
    }

    private void CreateServicesSection(VisualElement parent)
    {
        var section = new VisualElement { name = "ServicesSection" };
        section.AddToClassList("section");

        var header = new Label("Backend Services") { name = "ServicesHeader" };
        header.AddToClassList("section-header");
        section.Add(header);

        var mcpButton = new Button(StartMCPContainer) { text = "Start MCP Server" };
        mcpButton.AddToClassList("service-button");
        section.Add(mcpButton);

        var orchestratorButton = new Button(StartOrchestrator) { text = "Start Orchestrator" };
        orchestratorButton.AddToClassList("service-button");
        section.Add(orchestratorButton);

        parent.Add(section);
    }

    private void CreateStatusSection(VisualElement parent)
    {
        var section = new VisualElement { name = "StatusSection" };
        section.AddToClassList("section");

        var header = new Label("Service Status") { name = "StatusHeader" };
        header.AddToClassList("section-header");
        section.Add(header);

        var statusContainer = new VisualElement { name = "StatusContainer" };
        statusContainer.AddToClassList("status-container");

        // Add status indicators here when implementing service status monitoring
        section.Add(statusContainer);

        parent.Add(section);
    }
    }

    public void StartMCPContainer()
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

    public void StartOrchestrator()
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
            process.ErrorDataReceived += (sender, e) => { if (e.Data != null) UnityEngine.Debug.Log($"[{processName}]: {e.Data}"); };

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