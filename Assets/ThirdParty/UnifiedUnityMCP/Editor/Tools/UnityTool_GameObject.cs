using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Mcp.Editor.Tools
{
    internal static partial class UnitySkillModuleTools
    {
        private static UnityModuleTool CreateGameObjectModule()
                {
                    var docs = CreateDocs(
                        "gameobject_create",
                        "gameobject_delete",
                        "gameobject_duplicate",
                        "gameobject_rename",
                        "gameobject_find",
                        "gameobject_get_info",
                        "gameobject_set_transform",
                        "gameobject_set_parent",
                        "gameobject_set_active",
                        "gameobject_create_batch",
                        "gameobject_delete_batch",
                        "gameobject_duplicate_batch",
                        "gameobject_rename_batch",
                        "gameobject_set_transform_batch",
                        "gameobject_set_active_batch",
                        "gameobject_set_parent_batch",
                        "gameobject_set_layer_batch",
                        "gameobject_set_tag_batch",
                        "bridge");
        
                    var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["bridge"] = CreateBridgeHandler("unity_gameobject"),
                        ["gameobject_find"] = Forward("unity_gameobject_manage", raw => ExtractArgumentsWithAction(raw, "find")),
                        ["gameobject_create"] = Forward("unity_gameobject_manage", raw => ExtractArgumentsWithAction(raw, "create")),
                        ["gameobject_delete"] = Forward("unity_gameobject_manage", raw =>
                        {
                            var payload = ExtractArguments(raw);
                            payload["action"] = "destroy";
                            return payload;
                        }),
                        ["gameobject_set_transform"] = Forward("unity_component_property", raw =>
                        {
                            var payload = ExtractArguments(raw);
                            payload["action"] = "set";
        
                            string targetProperty = payload.GetString("propertyName");
                            if (string.IsNullOrEmpty(targetProperty))
                            {
                                bool hasPosition = payload.HasKey("posX") || payload.HasKey("posY") || payload.HasKey("posZ");
                                bool hasScale = payload.HasKey("scaleX") || payload.HasKey("scaleY") || payload.HasKey("scaleZ");
                                bool hasRotation = payload.HasKey("rotX") || payload.HasKey("rotY") || payload.HasKey("rotZ");
        
                                if (hasScale)
                                {
                                    targetProperty = "localScale";
                                }
                                else if (hasRotation)
                                {
                                    targetProperty = "eulerAngles";
                                }
                                else if (hasPosition)
                                {
                                    targetProperty = "position";
                                }
                                else
                                {
                                    targetProperty = "position";
                                }
                            }
        
                            var value = new JSONObject();
        
                            if (targetProperty == "localScale")
                            {
                                value["x"] = payload.HasKey("scaleX") ? payload["scaleX"].AsFloat : 1f;
                                value["y"] = payload.HasKey("scaleY") ? payload["scaleY"].AsFloat : 1f;
                                value["z"] = payload.HasKey("scaleZ") ? payload["scaleZ"].AsFloat : 1f;
                            }
                            else if (targetProperty == "eulerAngles")
                            {
                                value["x"] = payload.HasKey("rotX") ? payload["rotX"].AsFloat : 0f;
                                value["y"] = payload.HasKey("rotY") ? payload["rotY"].AsFloat : 0f;
                                value["z"] = payload.HasKey("rotZ") ? payload["rotZ"].AsFloat : 0f;
                            }
                            else
                            {
                                targetProperty = "position";
                                value["x"] = payload.HasKey("posX") ? payload["posX"].AsFloat : 0f;
                                value["y"] = payload.HasKey("posY") ? payload["posY"].AsFloat : 0f;
                                value["z"] = payload.HasKey("posZ") ? payload["posZ"].AsFloat : 0f;
                            }
        
                            payload["property"] = targetProperty;
                            payload["value"] = value;
                            return payload;
                        }),
                        ["gameobject_set_active"] = (arguments, sendResponse, sendError) =>
                        {
                            var payload = ExtractArguments(arguments);
                            int instanceId = payload.GetInt("instanceId", 0);
        
                            if (payload.HasKey("name") && !payload.HasKey("instanceId"))
                            {
                                var go = FindSceneObjectByName(payload["name"].Value);
                                if (go != null)
                                {
                                    instanceId = go.GetInstanceID();
                                }
                            }
        
                            if (instanceId == 0)
                            {
                                sendError("Missing valid target object. Provide 'instanceId' or resolvable 'name'.");
                                return;
                            }
        
        #pragma warning disable CS0618
                            var goObj = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
        #pragma warning restore CS0618
                            if (goObj == null)
                            {
                                sendError($"GameObject with instance ID {instanceId} not found.");
                                return;
                            }
        
                            bool active = payload.GetBool("active", true);
                            Undo.RecordObject(goObj, "Set Active");
                            goObj.SetActive(active);
                            EditorUtility.SetDirty(goObj);
        
                            var result = new JSONObject();
                            result["instanceId"] = instanceId;
                            result["active"] = goObj.activeSelf;
                            SendResult(sendResponse, result);
                        }
                    };
        
                    return new UnityModuleTool("unity_gameobject", "GameObject creation and query module.", docs, handlers, "For transform/tag/layer batch updates use component/property reflection with instanceIds.");
                }
    }
}
