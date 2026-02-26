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
        private static UnityModuleTool CreateComponentModule()
                {
                    var docs = CreateDocs(
                        "component_add",
                        "component_remove",
                        "component_list",
                        "component_set_property",
                        "component_get_properties",
                        "component_add_batch",
                        "component_remove_batch",
                        "component_set_property_batch",
                        "bridge");
        
                    var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["bridge"] = CreateBridgeHandler("unity_component"),
                        ["component_add"] = Forward("unity_component_manage", raw => ExtractArgumentsWithAction(raw, "add")),
                        ["component_remove"] = Forward("unity_component_manage", raw => ExtractArgumentsWithAction(raw, "remove")),
                        ["component_list"] = Forward("unity_component_manage", raw => ExtractArgumentsWithAction(raw, "list")),
                        ["component_set_property"] = Forward("unity_component_property", raw => ExtractArgumentsWithAction(raw, "set")),
                        ["component_get_properties"] = Forward("unity_component_property", raw => ExtractArgumentsWithAction(raw, "dump"))
                    };
        
                    return new UnityModuleTool("unity_component", "Component add/remove/introspection module.", docs, handlers, "Batch operations can be orchestrated by repeated calls from MCP client side.");
                }
    }
}
