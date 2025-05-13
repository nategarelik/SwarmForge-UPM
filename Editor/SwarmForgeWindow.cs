using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

public class SwarmForgeWindow : EditorWindow
{
    private TextField promptField;
    private Button sendButton;
    private ScrollView tasksScrollView;
    private TextField scriptPreviewField;
    private ScrollView logScrollView;
    private Label statusLabel;

    private List<string> customModes;
    private PopupField<string> modesDropdown;
    private Button runModeButton;

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
        
        var container = new VisualElement { name = "MainContainer" };
        container.AddToClassList("main-container");
        rootVisualElement.Add(container);

        CreateHeaderSection(container);
        CreatePromptSection(container);
        CreateTaskSection(container);
        CreateModeSection(container);
        CreateOutputSection(container);
        CreateStatusSection(container);

        InitializeWebSocket();
        SendCustomModesRequest();
    }

    private void CreateHeaderSection(VisualElement parent)
    {
        var header = new VisualElement { name = "Header" };
        header.AddToClassList("header");
        
        var title = new Label("SwarmForge AI") { name = "Title" };
        title.AddToClassList("title");
        header.Add(title);

        var subtitle = new Label("Unity AI Assistant") { name = "Subtitle" };
        subtitle.AddToClassList("subtitle");
        header.Add(subtitle);

        parent.Add(header);
    }

    private void CreatePromptSection(VisualElement parent)
    {
        var section = new VisualElement { name = "PromptSection" };
        section.AddToClassList("section");

        promptField = new TextField("What would you like me to do?") { name = "PromptField" };
        promptField.multiline = true;
        promptField.AddToClassList("prompt-field");
        section.Add(promptField);

        sendButton = new Button(() => SendRequest(promptField.value)) { text = "Generate" };
        sendButton.AddToClassList("primary-button");
        section.Add(sendButton);

        parent.Add(section);
    }

    private void CreateTaskSection(VisualElement parent)
    {
        var section = new VisualElement { name = "TaskSection" };
        section.AddToClassList("section");

        var header = new Label("Generated Tasks") { name = "TasksHeader" };
        header.AddToClassList("section-header");
        section.Add(header);

        tasksScrollView = new ScrollView { name = "TasksScrollView" };
        tasksScrollView.AddToClassList("scroll-view");
        section.Add(tasksScrollView);

        parent.Add(section);
    }

    private void CreateModeSection(VisualElement parent)
    {
        var section = new VisualElement { name = "ModeSection" };
        section.AddToClassList("section");

        var header = new Label("AI Modes") { name = "ModesHeader" };
        header.AddToClassList("section-header");
        section.Add(header);

        customModes = new List<string>();
        modesDropdown = new PopupField<string>("Select Mode", customModes, 0) { name = "ModesDropdown" };
        modesDropdown.AddToClassList("modes-dropdown");
        section.Add(modesDropdown);

        runModeButton = new Button(() => RunCustomMode(modesDropdown.value)) { text = "Run Selected Mode" };
        runModeButton.AddToClassList("secondary-button");
        section.Add(runModeButton);

        parent.Add(section);
    }

    private void CreateOutputSection(VisualElement parent)
    {
        var section = new VisualElement { name = "OutputSection" };
        section.AddToClassList("section");

        var header = new Label("Output") { name = "OutputHeader" };
        header.AddToClassList("section-header");
        section.Add(header);

        scriptPreviewField = new TextField { name = "ScriptPreview", multiline = true, isReadOnly = true };
        scriptPreviewField.AddToClassList("output-field");
        section.Add(scriptPreviewField);

        logScrollView = new ScrollView { name = "LogScrollView" };
        logScrollView.AddToClassList("scroll-view");
        section.Add(logScrollView);

        parent.Add(section);
    }

    private void CreateStatusSection(VisualElement parent)
    {
        var section = new VisualElement { name = "StatusSection" };
        section.AddToClassList("section");

        var statusContainer = new VisualElement { name = "StatusContainer" };
        statusContainer.AddToClassList("status-container");

        statusLabel = new Label("Disconnected") { name = "StatusLabel" };
        statusLabel.AddToClassList("status-label");
        statusContainer.Add(statusLabel);

        var backendManager = GetWindow<BackendManager>();
        
        var dockerButton = new Button(() => backendManager.StartMCPContainer()) { text = "Start MCP Server" };
        dockerButton.AddToClassList("service-button");
        statusContainer.Add(dockerButton);

        var orchestratorButton = new Button(() => backendManager.StartOrchestrator()) { text = "Start Orchestrator" };
        orchestratorButton.AddToClassList("service-button");
        statusContainer.Add(orchestratorButton);

        section.Add(statusContainer);
        parent.Add(section);
    }

    private async void InitializeWebSocket()
    {
        webSocket = new ClientWebSocket();
        try
        {
            await webSocket.ConnectAsync(new Uri("ws://localhost:8765"), CancellationToken.None);
            statusLabel.text = "Connected";
            StartReceiving();
            logScrollView.Add(new Label("Connected to orchestrator"));
        }
        catch (Exception e)
        {
            statusLabel.text = "Error";
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
        SendJson($"{{\"action\":\"plan\",\"prompt\":\"{prompt}\"}}");
    }

    private void SendCustomModesRequest()
    {
        SendJson("{\"action\":\"get_custom_modes\"}");
    }

    private void RunCustomMode(string mode)
    {
        SendJson($"{{\"action\":\"run_custom_mode\",\"mode\":\"{mode}\"}}");
    }

    private void OnMessageReceived(string json)
    {
        if (json.Contains("\"type\":\"custom_modes\""))
        {
            logScrollView.Add(new Label($"Received custom modes: {json}"));
            try
            {
                var wrapper = JsonUtility.FromJson<CustomModesWrapper>(json);
                customModes.Clear();
                foreach (var m in wrapper.modes)
                    customModes.Add(m.name);
                modesDropdown.choices = customModes;
                if (customModes.Count > 0)
                    modesDropdown.value = customModes[0];
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
                tasksScrollView.Add(new Label(t.description));
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