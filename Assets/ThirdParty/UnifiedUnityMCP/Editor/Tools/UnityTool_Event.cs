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
        private static UnityModuleTool CreateEventModule()
                {
                    return CreateBridgeOnlyModule(
                        "unity_event",
                        "UnityEvent inspection and editing module.",
                        "Use bridge + component/property reflection for event data access.",
                        "event_get_listeners",
                        "event_add_listener",
                        "event_remove_listener",
                        "event_invoke");
                }
    }
}
