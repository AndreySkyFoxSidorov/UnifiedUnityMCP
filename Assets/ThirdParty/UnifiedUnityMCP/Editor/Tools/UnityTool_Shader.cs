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
        private static UnityModuleTool CreateShaderModule()
                {
                    return CreateBridgeOnlyModule(
                        "unity_shader",
                        "Shader file and metadata module.",
                        "Use project_list_shaders and asset operations for shader file lifecycle.",
                        "shader_create",
                        "shader_read",
                        "shader_list",
                        "shader_find",
                        "shader_delete",
                        "shader_get_properties");
                }
    }
}
