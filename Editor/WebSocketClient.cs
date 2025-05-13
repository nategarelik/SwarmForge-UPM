using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SwarmForge.Communication; // From MessagePayload.cs
using UnityEngine; // For Debug.Log

namespace SwarmForge.Networking
{
    public class WebSocketClient
    {
        private ClientWebSocket ws;
        private Uri serverUri = new Uri("ws://localhost:8765"); // Default port
        private CancellationTokenSource cts;

        public void UpdateServerPort(int newPort)
        {
            if (IsConnected)
            {
                Debug.LogWarning("[WebSocketClient] Cannot update port while connected. Please disconnect first.");
                return;
            }
            serverUri = new Uri($"ws://localhost:{newPort}");
            Debug.Log($"[WebSocketClient] Server URI updated to: {serverUri}");
        }

        public event Action OnOpened;
        public event Action<string> OnError;
        public event Action<WebSocketCloseStatus?, string> OnClosed;
        
        // Specific message events
        public event Action<WebSocketMessage<TaskUpdateData>> OnTaskUpdateReceived;
        public event Action<WebSocketMessage<CustomModesData>> OnCustomModesReceived;
        public event Action<WebSocketMessage<ErrorData>> OnErrorMessageReceived;
        public event Action<WebSocketMessage<TasksMessageData>> OnTasksReceived; // Added for "tasks" message
        // Add more specific events as needed for other Server -> Editor messages
 
        public bool IsConnected => ws != null && ws.State == WebSocketState.Open;

        public async Task ConnectAsync()
        {
            if (IsConnected) return;

            ws = new ClientWebSocket();
            cts = new CancellationTokenSource();
            try
            {
                Debug.Log($"[WebSocketClient] Connecting to {serverUri}...");
                await ws.ConnectAsync(serverUri, cts.Token);
                Debug.Log("[WebSocketClient] Connected.");
                OnOpened?.Invoke();
                StartListening();
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebSocketClient] Connection error: {e.Message}");
                OnError?.Invoke(e.Message);
                await DisconnectAsync(false); // Clean up
            }
        }

        private async void StartListening()
        {
            var buffer = new byte[4096]; // 4KB buffer
            try
            {
                while (ws.State == WebSocketState.Open && !cts.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.Log($"[WebSocketClient] Server initiated close: {result.CloseStatus}, {result.CloseStatusDescription}");
                        await HandleCloseAsync(result.CloseStatus, result.CloseStatusDescription);
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        // Debug.Log($"[WebSocketClient] Received: {messageJson}");
                        HandleTextMessage(messageJson);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[WebSocketClient] Listening cancelled.");
                await HandleCloseAsync(WebSocketCloseStatus.NormalClosure, "Client initiated disconnect");
            }
            catch (WebSocketException e)
            {
                Debug.LogError($"[WebSocketClient] WebSocketException during listen: {e.Message} (Status: {e.WebSocketErrorCode})");
                OnError?.Invoke(e.Message);
                await HandleCloseAsync(ws.CloseStatus.HasValue ? ws.CloseStatus.Value : WebSocketCloseStatus.InternalServerError, e.Message);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebSocketClient] Exception during listen: {e.Message}");
                OnError?.Invoke(e.Message);
                await HandleCloseAsync(WebSocketCloseStatus.InternalServerError, e.Message);
            }
        }

        private void HandleTextMessage(string jsonMessage)
        {
            try
            {
                // First, try to deserialize to a generic message to get the type
                var genericMessage = JsonConvert.DeserializeObject<WebSocketMessage<object>>(jsonMessage);
                if (genericMessage == null || string.IsNullOrEmpty(genericMessage.Type))
                {
                    Debug.LogWarning($"[WebSocketClient] Received message with no type: {jsonMessage}");
                    return;
                }

                // Debug.Log($"[WebSocketClient] Message type: {genericMessage.Type}");

                switch (genericMessage.Type)
                {
                    case "task_update":
                        var taskUpdateMsg = JsonConvert.DeserializeObject<WebSocketMessage<TaskUpdateData>>(jsonMessage);
                        OnTaskUpdateReceived?.Invoke(taskUpdateMsg);
                        break;
                    case "custom_modes":
                        var customModesMsg = JsonConvert.DeserializeObject<WebSocketMessage<CustomModesData>>(jsonMessage);
                        OnCustomModesReceived?.Invoke(customModesMsg);
                        break;
                    case "error":
                        var errorMsg = JsonConvert.DeserializeObject<WebSocketMessage<ErrorData>>(jsonMessage);
                        OnErrorMessageReceived?.Invoke(errorMsg);
                        break;
                    case "tasks": // Added case for "tasks" message
                        var tasksMsg = JsonConvert.DeserializeObject<WebSocketMessage<TasksMessageData>>(jsonMessage);
                        OnTasksReceived?.Invoke(tasksMsg);
                        break;
                    // Add cases for other Server -> Editor messages from docs/websocket_plugin_architecture.md
                    default:
                        Debug.LogWarning($"[WebSocketClient] Unhandled message type: {genericMessage.Type}");
                        break;
                }
            }
            catch (JsonException e)
            {
                Debug.LogError($"[WebSocketClient] JSON Deserialization error: {e.Message}. Received: {jsonMessage}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebSocketClient] Error handling message: {e.Message}. Received: {jsonMessage}");
            }
        }
        
        public async Task SendMessageAsync<T>(WebSocketMessage<T> message)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[WebSocketClient] Not connected. Cannot send message.");
                OnError?.Invoke("Cannot send message, not connected.");
                return;
            }

            try
            {
                var messageJson = JsonConvert.SerializeObject(message);
                var bytes = Encoding.UTF8.GetBytes(messageJson);
                // Debug.Log($"[WebSocketClient] Sending: {messageJson}");
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebSocketClient] Error sending message: {e.Message}");
                OnError?.Invoke($"Error sending message: {e.Message}");
                // Consider if a disconnect is warranted here or retry logic
            }
        }

        private async Task HandleCloseAsync(WebSocketCloseStatus? status, string description)
        {
            if (ws != null && (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived || ws.State == WebSocketState.CloseSent))
            {
                 // Only try to close if it's in a state where closing is possible/meaningful
                if (ws.State != WebSocketState.Closed && ws.State != WebSocketState.Aborted) {
                    try
                    {
                        if (status.HasValue && status.Value != WebSocketCloseStatus.Empty) // Avoid closing if already closed by server gracefully
                        {
                             await ws.CloseAsync(status.Value, description, CancellationToken.None); // Use CancellationToken.None for cleanup
                        } else if (ws.State == WebSocketState.Open) {
                             await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[WebSocketClient] Exception during explicit close: {ex.Message}");
                    }
                }
            }
            
            var finalStatus = ws?.CloseStatus;
            var finalDescription = ws?.CloseStatusDescription;

            ws?.Dispose();
            ws = null;
            cts?.Cancel(); // Ensure listening loop stops
            cts?.Dispose();
            cts = null;
            
            Debug.Log($"[WebSocketClient] Disconnected. Status: {finalStatus}, Description: {finalDescription}");
            OnClosed?.Invoke(finalStatus, finalDescription);
        }

        public async Task DisconnectAsync(bool graceful = true)
        {
            if (ws == null) return;

            Debug.Log($"[WebSocketClient] Disconnecting (graceful: {graceful})...");
            if (graceful && IsConnected)
            {
                await HandleCloseAsync(WebSocketCloseStatus.NormalClosure, "Client initiated disconnect");
            }
            else
            {
                // For non-graceful, just ensure resources are cleaned up.
                // The listening loop's exception handling should manage the OnClosed event.
                cts?.Cancel(); // This will trigger the OperationCanceledException in StartListening
                // If ws is not null but not open, it might be in an error state or connecting.
                // We still want to dispose it.
                ws?.Dispose(); 
                ws = null;
                cts?.Dispose();
                cts = null;
                if (!graceful) { // If not graceful, and not already handled by listening loop's close
                    OnClosed?.Invoke(WebSocketCloseStatus.EndpointUnavailable, "Forced disconnect");
                }
            }
        }

        // Consider adding a Dispose method for IDisposable if this class manages unmanaged resources directly
        // or if cts/ws need more explicit cleanup in a Dispose pattern.
        // For now, DisconnectAsync handles the cleanup.
    }
}