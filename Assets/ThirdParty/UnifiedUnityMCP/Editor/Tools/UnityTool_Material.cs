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
        private static UnityModuleTool CreateMaterialModule()
                {
                    return CreateBridgeOnlyModule(
                        "unity_material",
                        "Material and shader property module.",
                        "Use unity_asset_create + unity_component_property for assignment and property updates.",
                        "material_create",
                        "material_create_batch",
                        "material_assign",
                        "material_assign_batch",
                        "material_set_color",
                        "material_set_colors_batch",
                        "material_set_emission",
                        "material_set_emission_batch",
                        "material_set_texture",
                        "material_set_float",
                        "material_set_int",
                        "material_set_keyword",
                        "material_get_properties",
                        "material_get_keywords",
                        "material_duplicate",
                        "material_set_shader",
                        "material_set_vector",
                        "material_set_texture_offset",
                        "material_set_texture_scale",
                        "material_set_render_queue",
                        "material_set_gi_flags");
                }
    }
}
