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
        // Root styling
        rootVisualElement.style.flexDirection = FlexDirection.Column;
        rootVisualElement.style.paddingLeft = 10;
        rootVisualElement.style.paddingRight = 10;
        rootVisualElement.style.paddingTop = 10;

        // Prompt input
        promptField = new TextField("Prompt");
        promptField.multiline = false;
        rootVisualElement.Add(promptField);

        sendButton = new Button(() => SendRequest(promptField.value))
        {
            text = "Send"
        };
        rootVisualElement.Add(sendButton);

        // Tasks list
        rootVisualElement.Add(new Label("AI-Generated Tasks"));
        tasksScrollView = new ScrollView();
        tasksScrollView.style.flexGrow = 1;
        tasksScrollView.name = "TasksScrollView";
        rootVisualElement.Add(tasksScrollView);

        // Script preview
        scriptPreviewField = new TextField("Script Preview")
        {
            multiline = true,
            isReadOnly = true
        };
        scriptPreviewField.style.flexGrow = 1;
        scriptPreviewField.name = "ScriptPreviewField";
        rootVisualElement.Add(scriptPreviewField);

        // Log output
        rootVisualElement.Add(new Label("Log Output"));
        logScrollView = new ScrollView();
        logScrollView.style.flexGrow = 1;
        logScrollView.name = "LogScrollView";
        rootVisualElement.Add(logScrollView);

        // WebSocket status
        rootVisualElement.Add(new Label("WebSocket Status"));
        statusLabel = new Label("Disconnected");
        statusLabel.name = "WebSocketStatusLabel";
        rootVisualElement.Add(statusLabel);

        // Custom modes dropdown
        rootVisualElement.Add(new Label("Custom Modes"));
        customModes = new List<string>();
        modesDropdown = new PopupField<string>("Modes", customModes, 0) { name = "CustomModesDropdown" };
        rootVisualElement.Add(modesDropdown);

        runModeButton = new Button(() => RunCustomMode(modesDropdown.value)) { text = "Run Mode" };
        rootVisualElement.Add(runModeButton);

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