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
        private static UnityModuleTool CreateOptimizationModule()
                {
                    return CreateBridgeOnlyModule(
                        "unity_optimization",
                        "Optimization workflows module.",
                        "Combine importer and validation modules to enforce optimization policies.",
                        "optimize_textures",
                        "optimize_mesh_compression");
                }
    }
}
