using System;
using System.Threading.Tasks;
using SwarmForge.Scripting;
using SwarmForge.Communication;
using SwarmForge.Networking; // For WebSocketClient
using UnityEngine; // For Debug.Log

namespace SwarmForge.Scripting
{
    public class WebSocketScriptGenerator : IScriptGenerator
    {
        private readonly WebSocketClient wsClient;

        public event Action<ScriptGenerationProgress> OnProgressUpdate;

        public WebSocketScriptGenerator(WebSocketClient client)
        {
            wsClient = client ?? throw new ArgumentNullException(nameof(client));
            // Similar to AssetGenerator, response handling is likely managed by SwarmForgeCoreService
        }

        public async Task<ScriptGenerationResult> GenerateScript(ScriptGenerationParams scriptParams, string requestId)
        {
            if (!wsClient.IsConnected)
            {
                Debug.LogError("[WebSocketScriptGenerator] Not connected to WebSocket server.");
                return new ScriptGenerationResult { Success = false, ErrorMessage = "Not connected." };
            }

            // The server will expect a specific payload for script generation.
            // This should align with what the AI agent for script generation expects.
            // For now, we'll send the parameters directly.
            var data = new GenerateAssetData // Re-using GenerateAssetData for simplicity, server should differentiate by 'type'
            {
                AssetType = "script", // Differentiator for the server
                Parameters = new {
                    name = scriptParams.ScriptName,
                    description = scriptParams.Description,
                    target_game_object = scriptParams.TargetGameObject
                    // Add other parameters from scriptParams as needed
                }
            };
            
            var message = new WebSocketMessage<GenerateAssetData>("generate_asset", data, requestId); // Using "generate_asset" type as per current server message types

            Debug.Log($"[WebSocketScriptGenerator] Sending script generation request: ID {requestId}, Name: {scriptParams.ScriptName}");
            await wsClient.SendMessageAsync(message);

            // As with asset generation, the actual result is asynchronous.
            Debug.LogWarning($"[WebSocketScriptGenerator] GenerateScript request ({requestId}) sent. Actual result processing is asynchronous.");
            return new ScriptGenerationResult { Success = false, ErrorMessage = "Request sent, awaiting asynchronous server response." };
        }

        public async Task<bool> ValidateScript(string scriptContent, string requestId)
        {
            if (!wsClient.IsConnected)
            {
                Debug.LogError("[WebSocketScriptGenerator] Not connected. Cannot validate script.");
                return false;
            }

            var validationData = new { script_content = scriptContent };
            // Assuming a message type like "validate_script" exists or will be added on the server.
            var message = new WebSocketMessage<object>("validate_script", validationData, requestId); 

            Debug.Log($"[WebSocketScriptGenerator] Sending script validation request: ID {requestId}");
            await wsClient.SendMessageAsync(message);
            
            // Validation result is also asynchronous.
            Debug.LogWarning($"[WebSocketScriptGenerator] ValidateScript request ({requestId}) sent. Actual result processing is asynchronous.");
            return true; // Indicates request was sent.
        }

        // Called by SwarmForgeCoreService when a script progress message is received.
        public void TriggerProgressUpdate(ScriptGenerationProgress progress)
        {
            OnProgressUpdate?.Invoke(progress);
        }
    }
}