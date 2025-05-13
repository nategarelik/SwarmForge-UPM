using System;
using System.Threading.Tasks;
using SwarmForge.Communication;
using SwarmForge.Networking;
using SwarmForge.UnityIntegration;
using SwarmForge.Assets; // For IAssetGenerator, AssetGenerationManager, EnhancedAssetGenerationParams etc.
using SwarmForge.Assets.Stubs; // For Stubbed Generators
using SwarmForge.Scripting; // For IScriptGenerator
using UnityEngine; // For Debug.Log
using UnityEditor; // For EditorApplication

namespace SwarmForge.Core
{
    public class SwarmForgeCoreService
    {
        private static SwarmForgeCoreService instance;
        public static SwarmForgeCoreService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SwarmForgeCoreService();
                }
                return instance;
            }
        }

        public WebSocketClient WsClient { get; private set; }
        public IAssetGenerator AssetGenerator { get; private set; } // This is the WebSocketAssetGenerator for remote tasks
        public AssetGenerationManager LocalAssetManager { get; private set; } // For local asset generation
        public IScriptGenerator ScriptGenerator { get; private set; }
        
        public event Action OnConnectionOpened;
        public event Action<string> OnConnectionError;
        public event Action<System.Net.WebSockets.WebSocketCloseStatus?, string> OnConnectionClosed;
        public event Action<WebSocketMessage<TaskUpdateData>> OnTaskUpdate;
        public event Action<WebSocketMessage<CustomModesData>> OnCustomModesUpdate;


        private SwarmForgeCoreService()
        {
            WsClient = new WebSocketClient();
            AssetGenerator = new WebSocketAssetGenerator(WsClient); // For remote generation
            ScriptGenerator = new WebSocketScriptGenerator(WsClient);

            // Initialize stub generators
            var proBuilderStub = new StubProBuilderGenerator();
            var blenderStub = new StubBlenderBridge();
            var aiImageStub = new StubAIImageGenerator();
            var proceduralStub = new StubProceduralGenerator();

            // Initialize Local Asset Manager with stubbed generators
            LocalAssetManager = new AssetGenerationManager(proBuilderStub, blenderStub, aiImageStub, proceduralStub);
            Debug.Log("[SwarmForgeCoreService] LocalAssetManager initialized with stub generators.");

            SubscribeToWebSocketEvents();
            EditorApplication.quitting += OnEditorQuitting;
        }

        private void SubscribeToWebSocketEvents()
        {
            WsClient.OnOpened += HandleConnectionOpened;
            WsClient.OnError += HandleConnectionError;
            WsClient.OnClosed += HandleConnectionClosed;
            
            WsClient.OnTaskUpdateReceived += HandleTaskUpdateReceived;
            WsClient.OnCustomModesReceived += HandleCustomModesReceived;
            WsClient.OnErrorMessageReceived += HandleErrorMessageReceived;
        }

        public async Task ConnectAsync()
        {
            if (!WsClient.IsConnected)
            {
                await WsClient.ConnectAsync();
            }
        }

        public async Task DisconnectAsync()
        {
            if (WsClient.IsConnected)
            {
                await WsClient.DisconnectAsync();
            }
        }

        private void HandleConnectionOpened()
        {
            Debug.Log("[SwarmForgeCoreService] WebSocket connection opened.");
            OnConnectionOpened?.Invoke();
            // Request initial data like custom modes list
            RequestCustomModes();
        }

        private void HandleConnectionError(string errorMessage)
        {
            Debug.LogError($"[SwarmForgeCoreService] WebSocket connection error: {errorMessage}");
            OnConnectionError?.Invoke(errorMessage);
        }

        private void HandleConnectionClosed(System.Net.WebSockets.WebSocketCloseStatus? status, string description)
        {
            Debug.Log($"[SwarmForgeCoreService] WebSocket connection closed. Status: {status}, Description: {description}");
            OnConnectionClosed?.Invoke(status, description);
        }

        private void HandleTaskUpdateReceived(WebSocketMessage<TaskUpdateData> message)
        {
            Debug.Log($"[SwarmForgeCoreService] Task Update Received. TaskId: {message.Data.TaskId}, Status: {message.Data.Status}, RequestId: {message.RequestId}");
            // Enhanced logging for details
            if (message.Data.Details != null)
            {
                Debug.Log($"[SwarmForgeCoreService] Task Update Details: {message.Data.Details}");
            }
            else
            {
                Debug.LogWarning($"[SwarmForgeCoreService] Task Update Details object is null for TaskId: {message.Data.TaskId}");
            }

            OnTaskUpdate?.Invoke(message);
            // Potentially update UI or trigger Unity API actions based on task status
            if (message.Data.Status == "CompletedSuccessfully_CreatePrimitive") // Example specific status
            {
                // This is a simplistic example. In reality, the 'details' might contain parameters.
                // And the server should send a more generic "action_request" type message.
                // For now, let's assume 'details' could be "Cube,0,1,0,MyNewCube"
                try 
                {
                    var parts = message.Data.Details.Split(',');
                    if (parts.Length >= 5 && Enum.TryParse<PrimitiveType>(parts[0], true, out var primitiveType))
                    {
                        float x = float.Parse(parts[1]);
                        float y = float.Parse(parts[2]);
                        float z = float.Parse(parts[3]);
                        string name = parts[4];
                        UnityApiBridge.CreatePrimitive(primitiveType, new Vector3(x,y,z), name);
                    }
                }
                catch (Exception e)
                {
                     Debug.LogError($"[SwarmForgeCoreService] Error processing task detail for CreatePrimitive: {e.Message}");
                }
            }
        }

        private void HandleCustomModesReceived(WebSocketMessage<CustomModesData> message)
        {
            Debug.Log($"[SwarmForgeCoreService] Received {message.Data.Modes?.Length ?? 0} custom modes.");
            OnCustomModesUpdate?.Invoke(message);
            // Update CustomModeManager or UI directly
            // For now, just log. The UI will subscribe to OnCustomModesUpdate.
        }
        
        private void HandleErrorMessageReceived(WebSocketMessage<ErrorData> message)
        {
            Debug.LogError($"[SwarmForgeCoreService] Received error from server: {message.Data.Message} (Code: {message.Data.Code})");
            // Potentially show this in UI
            UnityApiBridge.ShowNotification($"Server Error: {message.Data.Message}");
        }

        // --- Methods to send requests to the server ---

        public void RequestCustomModes()
        {
            if (!WsClient.IsConnected) return;
            var message = new WebSocketMessage<EmptyData>("get_custom_modes", new EmptyData());
            _ = WsClient.SendMessageAsync(message);
        }

        public void SendRunCustomModeRequest(string modeName, string systemPromptOverride = null)
        {
            if (!WsClient.IsConnected) return;
            
            var mode = CustomModeManager.Instance.GetMode(modeName);
            if (mode == null)
            {
                Debug.LogError($"[SwarmForgeCoreService] Mode '{modeName}' not found.");
                return;
            }

            var data = new RunCustomModeData
            {
                Mode = modeName,
                SystemPrompt = systemPromptOverride ?? mode.system_prompt 
            };
            var message = new WebSocketMessage<RunCustomModeData>("run_custom_mode", data, Guid.NewGuid().ToString());
            _ = WsClient.SendMessageAsync(message);
            Debug.Log($"[SwarmForgeCoreService] Sent run_custom_mode request for mode: {modeName}");
        }
        
        public void SendPlanRequest(string prompt, string systemPrompt)
        {
            if (!WsClient.IsConnected) return;
            var data = new PlanData { Prompt = prompt, SystemPrompt = systemPrompt };
            var message = new WebSocketMessage<PlanData>("plan", data, Guid.NewGuid().ToString());
            _ = WsClient.SendMessageAsync(message);
            Debug.Log($"[SwarmForgeCoreService] Sent plan request.");
        }

        // Placeholder for asset generation request
        public void SendGenerateAssetRequest(string assetType, object parameters)
        {
            if (!WsClient.IsConnected) return;
            var data = new GenerateAssetData { AssetType = assetType, Parameters = parameters };
            var message = new WebSocketMessage<GenerateAssetData>("generate_asset", data, Guid.NewGuid().ToString());
            _ = WsClient.SendMessageAsync(message);
            Debug.Log($"[SwarmForgeCoreService] Sent generate_asset request for type: {assetType}");
        }


        private void OnEditorQuitting()
        {
            Debug.Log("[SwarmForgeCoreService] Editor quitting. Disconnecting WebSocket.");
            // Run disconnect synchronously if possible, or ensure it's initiated.
            // Task.Run(async () => await DisconnectAsync()).Wait(); // This can be problematic in OnDestroy/OnQuitting
            _ = DisconnectAsync(); // Fire and forget, hope it completes.
        }

        public void Dispose()
        {
            EditorApplication.quitting -= OnEditorQuitting;
            _ = DisconnectAsync();
            WsClient.OnOpened -= HandleConnectionOpened;
            WsClient.OnError -= HandleConnectionError;
            WsClient.OnClosed -= HandleConnectionClosed;
            WsClient.OnTaskUpdateReceived -= HandleTaskUpdateReceived;
            WsClient.OnCustomModesReceived -= HandleCustomModesReceived;
            WsClient.OnErrorMessageReceived -= HandleErrorMessageReceived;
            instance = null;
        }
    }
}