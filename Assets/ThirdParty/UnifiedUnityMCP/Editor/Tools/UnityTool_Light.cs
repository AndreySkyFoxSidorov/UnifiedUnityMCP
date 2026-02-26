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
        private static UnityModuleTool CreateLightModule()
                {
                    return CreateBridgeOnlyModule(
                        "unity_light",
                        "Lighting creation and tuning module.",
                        "Use unity_gameobject + unity_component + unity_component_property for precise light setup.",
                        "light_create",
                        "light_set_properties",
                        "light_set_properties_batch",
                        "light_set_enabled",
                        "light_set_enabled_batch",
                        "light_get_info",
                        "light_find_all");
                }
    }
}
