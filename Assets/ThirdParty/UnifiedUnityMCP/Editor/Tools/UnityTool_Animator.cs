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
        private static UnityModuleTool CreateAnimatorModule()
                {
                    var docs = CreateDocs(
                        "animator_create_controller",
                        "animator_add_parameter",
                        "animator_get_parameters",
                        "animator_set_parameter",
                        "animator_play",
                        "animator_get_info",
                        "animator_assign_controller",
                        "animator_list_states",
                        "bridge");
        
                    var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["bridge"] = CreateBridgeHandler("unity_animator"),
                        ["animator_set_parameter"] = InvokeOnObject("SetFloat", "Animator"),
                        ["animator_play"] = InvokeOnObject("Play", "Animator")
                    };
        
                    return new UnityModuleTool(
                        "unity_animator",
                        "Animator workflows and controller operations.",
                        docs,
                        handlers,
                        "Animator flows can be orchestrated through unity_component_property (including invoke) and unity_component_manage.");
                }
    }
}
