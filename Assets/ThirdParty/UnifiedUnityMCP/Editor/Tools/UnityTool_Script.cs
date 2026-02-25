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
        private static UnityModuleTool CreateScriptModule()
                {
                    return CreateBridgeOnlyModule(
                        "unity_script",
                        "C# script management module.",
                        "Script file creation/editing can be handled by external file tools plus unity_asset_manage refresh.",
                        "script_create",
                        "script_create_batch",
                        "script_read",
                        "script_delete",
                        "script_find_in_file",
                        "script_append");
                }
    }
}
