using System;
using Newtonsoft.Json; // Assuming Newtonsoft.Json is available in the project

namespace SwarmForge.Communication
{
    [Serializable]
    public class WebSocketMessage<T>
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }

        [JsonProperty("request_id", NullValueHandling = NullValueHandling.Ignore)]
        public string RequestId { get; set; }

        public WebSocketMessage(string type, T data, string requestId = null)
        {
            Type = type;
            Data = data;
            Timestamp = DateTime.UtcNow.ToString("o"); // ISO 8601 format
            RequestId = requestId;
        }
    }

    // Generic placeholder for data if no specific type is needed for a message
    [Serializable]
    public class EmptyData { }

    // Example specific data structures based on docs/websocket_plugin_architecture.md
    // Editor -> Server
    [Serializable]
    public class RunCustomModeData
    {
        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("system_prompt")]
        public string SystemPrompt { get; set; }
    }

    [Serializable]
    public class PlanData
    {
        [JsonProperty("prompt")]
        public string Prompt { get; set; }

        [JsonProperty("system_prompt")]
        public string SystemPrompt { get; set; }
    }

    [Serializable]
    public class GenerateAssetData
    {
        [JsonProperty("asset_type")]
        public string AssetType { get; set; }

        [JsonProperty("parameters")]
        public object Parameters { get; set; } // Can be a more specific type if known
    }

    // Server -> Editor
    [Serializable]
    public class TaskUpdateData
    {
        [JsonProperty("task_id")]
        public string TaskId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("agent")]
        public string Agent { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("details")]
        public string Details { get; set; }
    }

    [Serializable]
    public class CustomModeInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("system_prompt")]
        public string SystemPrompt { get; set; }
    }
    
    [Serializable]
    public class CustomModesData
    {
        [JsonProperty("modes")]
        public CustomModeInfo[] Modes { get; set; }
    }

    [Serializable]
    public class ErrorData
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }
    }

    // Added for "tasks" message type
    [Serializable]
    public class TaskItemData
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("agent")]
        public string Agent { get; set; }
    }

    [Serializable]
    public class TasksMessageData
    {
        [JsonProperty("tasks")]
        public TaskItemData[] Tasks { get; set; }
    }
}