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
    private Label modeDescriptionLabel;

    private List<CustomMode> customModes;
    private PopupField<string> modesDropdown;
    private Button runModeButton;
    private CustomMode currentMode;
    private Dictionary<string, Label> taskUiElements = new Dictionary<string, Label>();

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

        modeDescriptionLabel = new Label("Select a mode to see its description.");
        modeDescriptionLabel.AddToClassList("mode-description");
        modesSection.Add(modeDescriptionLabel);
        
        rootVisualElement.Add(modesSection);

        InitializeWebSocket();
        // SendCustomModesRequest(); // Will be requested by orchestrator or upon connection.
                                 // For now, let's assume orchestrator sends modes on connect or we request it.
                                 // The original SendCustomModesRequest is not defined in the provided code.
                                 // Let's assume it's not strictly needed if orchestrator pushes modes.
                                 // If it is needed, it should be: SendJson("{\"action\":\"get_custom_modes\"}");
                                 // For now, I will rely on the orchestrator sending them or add a button if needed later.
                                 // Let's add a call to request modes after connection.
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
            SendJson("{\"action\":\"get_custom_modes\"}"); // Request custom modes on successful connection
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
        // Clear previous tasks when a new plan is requested
        tasksScrollView.Clear();
        taskUiElements.Clear();

        if (currentMode != null)
        {
            // Ensure prompt is properly JSON escaped.
            // Using a library for JSON construction is safer, but for simple cases:
            string escapedPrompt = prompt.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string escapedSystemPrompt = (currentMode.system_prompt ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
            SendJson($"{{\"action\":\"plan\",\"prompt\":\"{escapedPrompt}\",\"system_prompt\":\"{escapedSystemPrompt}\"}}");
        }
        else
        {
            string escapedPrompt = prompt.Replace("\\", "\\\\").Replace("\"", "\\\"");
            SendJson($"{{\"action\":\"plan\",\"prompt\":\"{escapedPrompt}\"}}");
        }
    }

    private void OnModeSelected(string modeName)
    {
        // currentMode = CustomModeManager.Instance.GetMode(modeName); // Old way
        currentMode = customModes.Find(m => m.name == modeName); // Use local list populated from server

        if (currentMode != null)
        {
            if (modeDescriptionLabel != null) // Guard for safety
            {
                modeDescriptionLabel.text = currentMode.description;
            }
            // Default tasks are no longer populated here; they will come from orchestrator if part of a plan.
            // tasksScrollView.Clear(); // This is now handled by SendRequest
            // taskUiElements.Clear();
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
        logScrollView.Add(new Label($"Raw Received: {json}")); // Log raw message for debugging

        string messageType = "";
        try
        {
            var genericMessage = JsonUtility.FromJson<GenericMessage>(json);
            if (genericMessage != null && !string.IsNullOrEmpty(genericMessage.type)) {
                messageType = genericMessage.type;
            } else {
                 logScrollView.Add(new Label($"Could not determine message type from JSON: {json}"));
                 scriptPreviewField.value = json; // Show in script preview as fallback
                 return;
            }
        }
        catch (Exception e)
        {
            logScrollView.Add(new Label($"Error parsing generic message type: {e.Message} - JSON: {json}"));
            scriptPreviewField.value = json; // Show in script preview as fallback
            return;
        }

        switch (messageType)
        {
            case "custom_modes":
                logScrollView.Add(new Label($"Processing custom_modes: {json}"));
                try
                {
                    var modesResponse = JsonUtility.FromJson<CustomModesResponseMessage>(json);
                    if (modesResponse != null && modesResponse.modes != null)
                    {
                        customModes = new List<CustomMode>(modesResponse.modes);
                        var modeNames = customModes.ConvertAll(m => m.name);
                        modesDropdown.choices = modeNames;
                        if (modeNames.Count > 0)
                        {
                            modesDropdown.value = modeNames[0]; // This will trigger OnModeSelected
                        }
                        else
                        {
                            modesDropdown.choices = new List<string> { "No modes available" };
                            modesDropdown.index = 0;
                            if(modeDescriptionLabel != null) modeDescriptionLabel.text = "No custom modes loaded from orchestrator.";
                        }
                    } else {
                        logScrollView.Add(new Label($"Custom modes response or modes list is null. JSON: {json}"));
                    }
                }
                catch (Exception e)
                {
                    logScrollView.Add(new Label($"Failed to parse custom_modes: {e.Message} - JSON: {json}"));
                }
                break;

            case "run_custom_mode": // This is an acknowledgement
                logScrollView.Add(new Label($"Custom mode ack: {json}"));
                // Optionally parse and display status from this ack if needed
                break;

            case "plan_received":
                logScrollView.Add(new Label($"Plan received notification: {json}"));
                try
                {
                    var planMsg = JsonUtility.FromJson<PlanReceivedMessage>(json);
                    if (planMsg != null) {
                        logScrollView.Add(new Label($"Orchestrator: {planMsg.message}"));
                    }
                }
                catch (Exception e)
                {
                    logScrollView.Add(new Label($"Failed to parse plan_received: {e.Message} - JSON: {json}"));
                }
                break;

            case "task_update":
                logScrollView.Add(new Label($"Processing task_update: {json}"));
                try
                {
                    var taskUpdate = JsonUtility.FromJson<TaskUpdateData>(json);
                    if (taskUpdate == null || string.IsNullOrEmpty(taskUpdate.task_id))
                    {
                        logScrollView.Add(new Label($"Invalid task_update message (null or no task_id): {json}"));
                        return;
                    }

                    if (taskUiElements.TryGetValue(taskUpdate.task_id, out Label taskLabel))
                    {
                        // Update existing task
                        taskLabel.text = $"[{taskUpdate.status?.ToUpper()}] ID:{taskUpdate.task_id} - {taskUpdate.description} (Agent: {taskUpdate.agent})";
                        // Update styles
                        taskLabel.RemoveFromClassList("task-pending");
                        taskLabel.RemoveFromClassList("task-in-progress");
                        taskLabel.RemoveFromClassList("task-completed");
                        taskLabel.RemoveFromClassList("task-failed");
                        if (!string.IsNullOrEmpty(taskUpdate.status))
                        {
                            taskLabel.AddToClassList($"task-{taskUpdate.status.ToLowerInvariant()}");
                        }
                    }
                    else
                    {
                        // New task
                        taskLabel = new Label($"[{taskUpdate.status?.ToUpper()}] ID:{taskUpdate.task_id} - {taskUpdate.description} (Agent: {taskUpdate.agent})");
                        taskLabel.AddToClassList("task-item");
                        if (!string.IsNullOrEmpty(taskUpdate.status))
                        {
                            taskLabel.AddToClassList($"task-{taskUpdate.status.ToLowerInvariant()}");
                        }
                        tasksScrollView.Add(taskLabel);
                        taskUiElements[taskUpdate.task_id] = taskLabel;
                    }
                }
                catch (Exception e)
                {
                    logScrollView.Add(new Label($"Error processing task_update: {e.Message} - JSON: {json}"));
                }
                break;

            case "error":
                logScrollView.Add(new Label($"Orchestrator error: {json}"));
                try
                {
                    var errorMsg = JsonUtility.FromJson<ErrorMessage>(json);
                     if (errorMsg != null) {
                        logScrollView.Add(new Label($"ERROR from Orchestrator: {errorMsg.message}"));
                        // Consider showing this more prominently, e.g., in statusLabel or a dedicated error area.
                    }
                }
                catch (Exception e)
                {
                    logScrollView.Add(new Label($"Failed to parse orchestrator error message: {e.Message} - JSON: {json}"));
                }
                break;

            default:
                logScrollView.Add(new Label($"Received unhandled message type '{messageType}': {json}"));
                scriptPreviewField.value = json; // Fallback for unknown types
                break;
        }
    }

    // Helper classes for JSON deserialization
    [Serializable]
    private class GenericMessage
    {
        public string type;
    }

    [Serializable]
    private class TaskUpdateData
    {
        public string type; // Should be "task_update"
        public string task_id;
        public string status;
        public string description;
        public string agent;
        public string timestamp;
        public string details;
    }

    [Serializable]
    private class PlanReceivedMessage
    {
        public string type; // Should be "plan_received"
        public string message;
    }

    [Serializable]
    private class ErrorMessage
    {
        public string type; // Should be "error"
        public string message;
    }
    
    // Assumes CustomMode structure is compatible (e.g., from CustomModeManager or defined here)
    // Ensure CustomMode class is [Serializable]
    // public string name; public string description; public string system_prompt; public List<string> default_tasks;
    [Serializable]
    private class CustomModesResponseMessage
    {
        public string type; // Should be "custom_modes"
        public List<CustomMode> modes;
    }

    // The old CustomModesWrapper and TaskListWrapper are no longer used by the new message flow.
    // They can be removed if they are not used elsewhere, or kept if there's a chance
    // old message formats might still be received from other sources (unlikely for this task).
    // For this change, I'm effectively replacing their functionality within OnMessageReceived.
}