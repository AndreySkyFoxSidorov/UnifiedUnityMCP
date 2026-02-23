using System;
using System.IO;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;
using UnityEngine;

namespace Mcp.Editor.Tools
{
    public class AssetCreateTool : ITool
    {
        public string Name => "unity.asset.create";
        public string Description => "Create new assets (Material, Folder). Actions: 'material', 'folder'.";

        public JSONObject InputSchema
        {
            get
            {
                var props = new JSONObject();
                props["action"] = McpMessages.CreateStringProperty("Action: 'material', 'folder'.");
                props["path"] = McpMessages.CreateStringProperty("Target asset path (e.g. Assets/Materials/MyMat.mat or Assets/NewFolder)");
                props["shader"] = McpMessages.CreateStringProperty("Shader name for material (default: 'Standard' or 'Universal Render Pipeline/Lit').");
                
                var required = new JSONArray();
                required.Add("action");
                required.Add("path");

                return McpMessages.CreateToolSchema(Name, Description, props, required);
            }
        }

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            string action = arguments.GetString("action")?.ToLower();
            string path = arguments.GetString("path");

            if (string.IsNullOrEmpty(path))
            {
                sendError("Missing parameter: 'path'");
                return;
            }

            MainThreadDispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (action == "folder")
                    {
                        string parentFolder = Path.GetDirectoryName(path)?.Replace("\\", "/");
                        string newFolderName = Path.GetFileName(path);
                        
                        if (string.IsNullOrEmpty(parentFolder)) parentFolder = "Assets";

                        string guid = AssetDatabase.CreateFolder(parentFolder, newFolderName);
                        if (string.IsNullOrEmpty(guid))
                        {
                            sendError($"Failed to create folder at '{path}'. Check if parent exists.");
                        }
                        else
                        {
                            sendResponse(McpMessages.CreateToolResult($"{{\"status\":\"Folder created\", \"guid\":\"{guid}\"}}"));
                        }
                        return;
                    }

                    if (action == "material")
                    {
                        string shaderName = arguments.GetString("shader", "Standard");
                        var shader = Shader.Find(shaderName);
                        if (shader == null)
                        {
                            // fallback for URP if Standard isn't found/appropriate
                            shader = Shader.Find("Universal Render Pipeline/Lit");
                        }
                        if (shader == null)
                        {
                            sendError($"Shader '{shaderName}' not found.");
                            return;
                        }

                        var material = new Material(shader);
                        AssetDatabase.CreateAsset(material, path);
                        AssetDatabase.SaveAssets();

                        sendResponse(McpMessages.CreateToolResult($"{{\"status\":\"Material created\", \"path\":\"{path}\"}}"));
                        return;
                    }

                    sendError($"Invalid action '{action}'.");
                }
                catch (Exception e)
                {
                    sendError($"Asset creation failed: {e.Message}");
                }
            });
        }
    }
}
