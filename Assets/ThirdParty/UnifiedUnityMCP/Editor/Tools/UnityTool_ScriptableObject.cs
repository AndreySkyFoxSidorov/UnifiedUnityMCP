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
        private static UnityModuleTool CreateScriptableObjectModule()
                {
                    return CreateBridgeOnlyModule(
                        "unity_scriptableobject",
                        "ScriptableObject asset management module.",
                        "Use bridge with asset and component tools to inspect and manipulate serialized values.",
                        "scriptableobject_create",
                        "scriptableobject_get",
                        "scriptableobject_set",
                        "scriptableobject_list_types",
                        "scriptableobject_duplicate");
                }
    }
}
