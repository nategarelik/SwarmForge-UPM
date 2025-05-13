using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

using SwarmForge.Editor;

public class SwarmForgeWindow : EditorWindow
{
    private TextField promptField;
    private Button sendButton;
    private ScrollView tasksScrollView;
    private TextField scriptPreviewField;
    private ScrollView logScrollView;
    private Label statusLabel;
    private Label modeDescriptionLabel;

    private List<CustomMode> customModes;
    private PopupField<string> modesDropdown;
    private Button runModeButton;
    private CustomMode currentMode;

    private ClientWebSocket webSocket;

    [MenuItem("Window/SwarmForge")]
    public static void ShowWindow()
    {
        var window = GetWindow<SwarmForgeWindow>();
        window.titleContent = new GUIContent("SwarmForge");
    }

    public void CreateGUI()
    {
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/SwarmForgeStyles.uss");
        rootVisualElement.styleSheets.Add(styleSheet);
        rootVisualElement.AddToClassList("root");

        // Header
        var header = new Label("SwarmForge AI Assistant");
        header.AddToClassList("header");
        rootVisualElement.Add(header);

        // Prompt section
        var promptSection = new VisualElement();
        promptSection.AddToClassList("section");
        
        var promptLabel = new Label("Enter your prompt");
        promptLabel.AddToClassList("section-label");
        promptSection.Add(promptLabel);

        promptField = new TextField();
        promptField.multiline = false;
        promptField.AddToClassList("prompt-field");
        promptSection.Add(promptField);

        sendButton = new Button(() => SendRequest(promptField.value)) { text = "Send" };
        sendButton.AddToClassList("send-button");
        promptSection.Add(sendButton);
        
        rootVisualElement.Add(promptSection);

        // Tasks section
        var tasksSection = new VisualElement();
        tasksSection.AddToClassList("section");
        
        var tasksLabel = new Label("AI-Generated Tasks");
        tasksLabel.AddToClassList("section-label");
        tasksSection.Add(tasksLabel);

        tasksScrollView = new ScrollView();
        tasksScrollView.AddToClassList("scroll-view");
        tasksSection.Add(tasksScrollView);
        
        rootVisualElement.Add(tasksSection);

        // Script preview section
        var previewSection = new VisualElement();
        previewSection.AddToClassList("section");
        
        var previewLabel = new Label("Script Preview");
        previewLabel.AddToClassList("section-label");
        previewSection.Add(previewLabel);

        scriptPreviewField = new TextField() { multiline = true, isReadOnly = true };
        scriptPreviewField.AddToClassList("script-preview");
        previewSection.Add(scriptPreviewField);
        
        rootVisualElement.Add(previewSection);

        // Log section
        var logSection = new VisualElement();
        logSection.AddToClassList("section");
        
        var logLabel = new Label("Log Output");
        logLabel.AddToClassList("section-label");
        logSection.Add(logLabel);

        logScrollView = new ScrollView();
        logScrollView.AddToClassList("scroll-view");
        logSection.Add(logScrollView);
        
        rootVisualElement.Add(logSection);

        // Status section
        var statusSection = new VisualElement();
        statusSection.AddToClassList("section");
        
        var statusHeaderLabel = new Label("WebSocket Status");
        statusHeaderLabel.AddToClassList("section-label");
        statusSection.Add(statusHeaderLabel);

        statusLabel = new Label("Disconnected");
        statusLabel.AddToClassList("status-label");
        statusSection.Add(statusLabel);
        
        rootVisualElement.Add(statusSection);

        // Modes section
        var modesSection = new VisualElement();
        modesSection.AddToClassList("section");
        
        var modesLabel = new Label("Custom Modes");
        modesLabel.AddToClassList("section-label");
        modesSection.Add(modesLabel);

        customModes = new List<CustomMode>(CustomModeManager.Instance.GetModes());
        var modeNames = customModes.ConvertAll(m => m.name);
        modesDropdown = new PopupField<string>("", modeNames, 0);
        modesDropdown.RegisterValueChangedCallback(evt => OnModeSelected(evt.newValue));
        modesDropdown.AddToClassList("modes-dropdown");
        modesSection.Add(modesDropdown);

        runModeButton = new Button(() => RunCustomMode(modesDropdown.value)) { text = "Run Mode" };
        runModeButton.AddToClassList("run-mode-button");
        modesSection.Add(runModeButton);
        
        rootVisualElement.Add(modesSection);

        InitializeWebSocket();
        SendCustomModesRequest();
    }

    private async void InitializeWebSocket()
    {
        webSocket = new ClientWebSocket();
        try
        {
            await webSocket.ConnectAsync(new Uri("ws://localhost:8765"), CancellationToken.None);
            statusLabel.text = "Connected";
            statusLabel.RemoveFromClassList("status-error");
            statusLabel.AddToClassList("status-connected");
            StartReceiving();
            logScrollView.Add(new Label("Connected to orchestrator"));
        }
        catch (Exception e)
        {
            statusLabel.text = "Error";
            statusLabel.RemoveFromClassList("status-connected");
            statusLabel.AddToClassList("status-error");
            logScrollView.Add(new Label($"WebSocket error: {e.Message}"));
        }
    }

    private async void StartReceiving()
    {
        var buffer = new ArraySegment<byte>(new byte[4096]);
        while (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                statusLabel.text = "Disconnected";
                break;
            }
            var message = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
            OnMessageReceived(message);
        }
    }

    private async void SendJson(string json)
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            logScrollView.Add(new Label($"Sent: {json}"));
        }
        else
        {
            logScrollView.Add(new Label("WebSocket not connected"));
        }
    }

    private void SendRequest(string prompt)
    {
        if (currentMode != null)
        {
            var formattedPrompt = CustomModeManager.Instance.FormatPrompt(currentMode.name, prompt);
            var systemPrompt = CustomModeManager.Instance.GetSystemPrompt(currentMode.name);
            SendJson($"{{\"action\":\"plan\",\"prompt\":\"{formattedPrompt}\",\"system_prompt\":\"{systemPrompt}\"}}");
        }
        else
        {
            SendJson($"{{\"action\":\"plan\",\"prompt\":\"{prompt}\"}}");
        }
    }

    private void OnModeSelected(string modeName)
    {
        currentMode = CustomModeManager.Instance.GetMode(modeName);
        if (currentMode != null)
        {
            modeDescriptionLabel.text = currentMode.description;
            
            // Clear and populate tasks with default tasks
            tasksScrollView.Clear();
            foreach (var task in currentMode.default_tasks)
            {
                var taskLabel = new Label(task);
                taskLabel.AddToClassList("task-item");
                tasksScrollView.Add(taskLabel);
            }
        }
    }

    private void RunCustomMode(string modeName)
    {
        var mode = CustomModeManager.Instance.GetMode(modeName);
        if (mode != null)
        {
            SendJson($"{{\"action\":\"run_custom_mode\",\"mode\":\"{modeName}\",\"system_prompt\":\"{mode.system_prompt}\"}}");
        }
    }

    private void OnMessageReceived(string json)
    {
        if (json.Contains("\"type\":\"custom_modes\""))
        {
            logScrollView.Add(new Label($"Received custom modes: {json}"));
            try
            {
                var modes = CustomModeManager.Instance.GetModes();
                customModes = new List<CustomMode>(modes);
                var modeNames = customModes.ConvertAll(m => m.name);
                modesDropdown.choices = modeNames;
                if (modeNames.Count > 0)
                {
                    modesDropdown.value = modeNames[0];
                    OnModeSelected(modeNames[0]);
                }
            }
            catch
            {
                logScrollView.Add(new Label("Failed to parse custom modes"));
            }
            return;
        }
        if (json.Contains("\"type\":\"run_custom_mode\""))
        {
            logScrollView.Add(new Label($"Custom mode results: {json}"));
            return;
        }

        try
        {
            var taskList = JsonUtility.FromJson<TaskListWrapper>(json);
            foreach (var t in taskList.tasks)
            {
                var taskLabel = new Label(t.description);
                taskLabel.AddToClassList("task-item");
                tasksScrollView.Add(taskLabel);
            }
        }
        catch
        {
            scriptPreviewField.value = json;
            logScrollView.Add(new Label($"Received: {json}"));
        }
    }

    [Serializable]
    private class CustomModesWrapper
    {
        public string type;
        public ModeEntry[] modes;
        [Serializable]
        public class ModeEntry
        {
            public string name;
        }
    }

    [Serializable]
    private class TaskListWrapper
    {
        public string type;
        public TaskEntry[] tasks;
        [Serializable]
        public class TaskEntry
        {
            public int id;
            public string type;
            public string description;
            public string agent;
        }
    }
}