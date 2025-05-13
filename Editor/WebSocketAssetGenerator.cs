using System;
using System.Threading.Tasks;
using SwarmForge.Assets;
using SwarmForge.Communication;
using SwarmForge.Networking; // For WebSocketClient
using UnityEngine; // For Debug.Log

namespace SwarmForge.Assets
{
    public class WebSocketAssetGenerator : IAssetGenerator
    {
        private readonly WebSocketClient wsClient;

        public event Action<AssetGenerationProgress> OnProgressUpdate;

        public WebSocketAssetGenerator(WebSocketClient client)
        {
            wsClient = client ?? throw new ArgumentNullException(nameof(client));
            // In a more complex scenario, this class might subscribe to specific messages
            // from wsClient if it's responsible for handling responses/progress directly.
            // For now, SwarmForgeCoreService is expected to route relevant messages or
            // this class's methods will be called upon receiving specific messages.
        }

        public async Task<AssetGenerationResult> GenerateAsset(AssetGenerationParams assetParams, string requestId)
        {
            if (!wsClient.IsConnected)
            {
                Debug.LogError("[WebSocketAssetGenerator] Not connected to WebSocket server.");
                return new AssetGenerationResult { Success = false, ErrorMessage = "Not connected." };
            }

            var data = new GenerateAssetData // From MessagePayload.cs
            {
                AssetType = assetParams.AssetType,
                // Parameters should be structured as expected by the server.
                // This is a generic example; the actual structure might be more complex.
                Parameters = new { 
                    name = assetParams.AssetName, 
                    description = assetParams.Description 
                    // Add other fields from assetParams.Parameters if it's a dictionary or specific object
                } 
            };

            var message = new WebSocketMessage<GenerateAssetData>("generate_asset", data, requestId);
            
            Debug.Log($"[WebSocketAssetGenerator] Sending asset generation request: ID {requestId}, Type: {assetParams.AssetType}");
            await wsClient.SendMessageAsync(message);

            // This method sends the request. The actual result (AssetGenerationResult)
            // will arrive asynchronously as a WebSocket message from the server.
            // The SwarmForgeCoreService (or a dedicated response handler) will need to
            // correlate the requestId from the incoming message with this request
            // and then fulfill a TaskCompletionSource or invoke a callback.
            // For this placeholder, we assume the caller doesn't block on this Task for the *actual* result,
            // but rather uses it to know the request was sent. Progress and final result come via events/callbacks.
            
            Debug.LogWarning($"[WebSocketAssetGenerator] GenerateAsset request ({requestId}) sent. Actual result processing is asynchronous and depends on server response handling.");
            // Returning a non-successful result here as the actual result is async.
            // The UI or calling logic should rely on events/callbacks for the true outcome.
            return new AssetGenerationResult { Success = false, ErrorMessage = "Request sent, awaiting asynchronous server response." };
        }

        public Task<bool> CancelGeneration(string requestId)
        {
            if (!wsClient.IsConnected)
            {
                Debug.LogError("[WebSocketAssetGenerator] Not connected. Cannot cancel generation.");
                return Task.FromResult(false);
            }
            
            // Define a payload for cancellation. The server needs to know what to cancel.
            var cancellationData = new { target_request_id = requestId }; 
            var message = new WebSocketMessage<object>("cancel_task", cancellationData, Guid.NewGuid().ToString()); // New request_id for the cancel message itself
            
            Debug.Log($"[WebSocketAssetGenerator] Sending cancellation request for generation task: {requestId}");
            _ = wsClient.SendMessageAsync(message);

            // The success of cancellation also depends on server response.
            // For now, assume the request to cancel is sent.
            return Task.FromResult(true); 
        }

        // This method would be called by SwarmForgeCoreService when a progress message for assets is received.
        public void TriggerProgressUpdate(AssetGenerationProgress progress)
        {
            OnProgressUpdate?.Invoke(progress);
        }
    }
}