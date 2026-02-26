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
        private static UnityModuleTool CreateSmartModule()
                {
                    return CreateBridgeOnlyModule(
                        "unity_smart",
                        "Smart query/layout/binding module.",
                        "Compose smart flows by chaining gameobject/component/property actions with client-side filtering.",
                        "smart_scene_query",
                        "smart_scene_layout",
                        "smart_reference_bind");
                }
    }
}
