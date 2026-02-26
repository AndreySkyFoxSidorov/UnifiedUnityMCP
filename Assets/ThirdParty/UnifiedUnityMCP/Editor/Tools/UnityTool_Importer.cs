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
        private static UnityModuleTool CreateImporterModule()
                {
                    return CreateBridgeOnlyModule(
                        "unity_importer",
                        "Importer settings module.",
                        "Use unity_asset_meta for importer property dump/get/set via SerializedProperty paths.",
                        "texture_get_settings",
                        "texture_set_settings",
                        "texture_set_settings_batch",
                        "texture_set_import_settings",
                        "audio_get_settings",
                        "audio_set_settings",
                        "audio_set_settings_batch",
                        "model_get_settings",
                        "model_set_settings",
                        "model_set_settings_batch",
                        "model_set_import_settings");
                }
    }
}
