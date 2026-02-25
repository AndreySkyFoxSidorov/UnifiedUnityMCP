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
        private static UnityModuleTool CreateTerrainModule()
                {
                    return CreateBridgeOnlyModule(
                        "unity_terrain",
                        "Terrain editing and generation module.",
                        "Terrain operations can be added incrementally via dedicated TerrainData handlers.",
                        "terrain_create",
                        "terrain_get_info",
                        "terrain_get_height",
                        "terrain_set_height",
                        "terrain_set_heights_batch",
                        "terrain_add_hill",
                        "terrain_generate_perlin",
                        "terrain_smooth",
                        "terrain_flatten",
                        "terrain_paint_texture");
                }
    }
}
