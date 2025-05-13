using UnityEngine;
using UnityEditor; // Required for Editor specific APIs
using System.Threading.Tasks; // For async operations if needed

namespace SwarmForge.UnityIntegration
{
    public class UnityApiBridge
    {
        // --- Scene Management ---

        public static GameObject CreatePrimitive(PrimitiveType type, Vector3 position, string name = "New Primitive")
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.transform.position = position;
            obj.name = name;
            Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
            Selection.activeObject = obj;
            Debug.Log($"[UnityApiBridge] Created primitive: {name} of type {type} at {position}");
            return obj;
        }

        public static void SetObjectPosition(GameObject obj, Vector3 newPosition)
        {
            if (obj == null)
            {
                Debug.LogError("[UnityApiBridge] Cannot set position: GameObject is null.");
                return;
            }
            Undo.RecordObject(obj.transform, $"Set Position {obj.name}");
            obj.transform.position = newPosition;
            Debug.Log($"[UnityApiBridge] Set position of {obj.name} to {newPosition}");
        }

        public static void RenameObject(GameObject obj, string newName)
        {
            if (obj == null)
            {
                Debug.LogError("[UnityApiBridge] Cannot rename: GameObject is null.");
                return;
            }
            Undo.RecordObject(obj, $"Rename {obj.name}");
            obj.name = newName;
            Debug.Log($"[UnityApiBridge] Renamed object to {newName}");
        }

        // --- Asset Database ---

        public static void CreateFolder(string parentPath, string folderName)
        {
            if (string.IsNullOrEmpty(parentPath) || !AssetDatabase.IsValidFolder(parentPath))
            {
                Debug.LogError($"[UnityApiBridge] Invalid parent path: {parentPath}");
                return;
            }
            string fullPath = System.IO.Path.Combine(parentPath, folderName);
            if (AssetDatabase.IsValidFolder(fullPath))
            {
                Debug.LogWarning($"[UnityApiBridge] Folder already exists: {fullPath}");
                return;
            }
            AssetDatabase.CreateFolder(parentPath, folderName);
            Debug.Log($"[UnityApiBridge] Created folder: {fullPath}");
        }
        
        public static void SaveScriptAsset(string scriptContent, string pathInAssets)
        {
            if (!pathInAssets.StartsWith("Assets/"))
            {
                Debug.LogError("[UnityApiBridge] Script path must be inside the 'Assets' folder and start with 'Assets/'.");
                return;
            }
            
            try
            {
                // Ensure directory exists
                string directory = System.IO.Path.GetDirectoryName(pathInAssets);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                System.IO.File.WriteAllText(pathInAssets, scriptContent);
                AssetDatabase.ImportAsset(pathInAssets); // Tell Unity to import the new script
                Debug.Log($"[UnityApiBridge] Saved and imported script: {pathInAssets}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UnityApiBridge] Error saving script asset: {e.Message}");
            }
        }

        // Placeholder for more complex asset creation (e.g., materials, prefabs)
        // public static async Task<string> CreateMaterialAsset(string materialName, Color color) { ... }

        // --- Editor Utilities ---
        public static void ShowNotification(string message)
        {
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.ShowNotification(new GUIContent(message));
            }
            else
            {
                Debug.Log($"[UnityApiBridge Notification] {message}");
            }
        }
        
        // Add more methods as needed for interacting with Unity APIs
        // e.g., GetSelectedObject, GetComponent, AddComponent, etc.
    }
}