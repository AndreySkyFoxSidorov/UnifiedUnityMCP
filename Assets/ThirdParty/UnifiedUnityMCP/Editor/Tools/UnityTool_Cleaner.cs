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
        private static UnityModuleTool CreateCleanerModule()
                {
                    return CreateBridgeOnlyModule(
                        "unity_cleaner",
                        "Project cleanup and diagnostics module.",
                        "Use unity_validation for core checks and asset tools for explicit cleanup commands.",
                        "cleaner_find_unused_assets",
                        "cleaner_find_duplicates",
                        "cleaner_find_missing_references",
                        "cleaner_delete_assets",
                        "cleaner_get_asset_usage");
                }
    }
}
