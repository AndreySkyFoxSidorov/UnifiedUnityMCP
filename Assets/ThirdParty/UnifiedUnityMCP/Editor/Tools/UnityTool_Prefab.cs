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
        private static UnityModuleTool CreatePrefabModule()
                {
                    var docs = CreateDocs(
                        "prefab_create",
                        "prefab_instantiate",
                        "prefab_instantiate_batch",
                        "prefab_apply",
                        "prefab_unpack",
                        "prefab_get_overrides",
                        "prefab_revert_overrides",
                        "prefab_apply_overrides",
                        "bridge");
        
                    var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["bridge"] = CreateBridgeHandler("unity_prefab"),
                        ["prefab_instantiate"] = Forward("unity_prefab_instantiate", raw =>
                        {
                            var payload = ExtractArguments(raw);
                            if (!payload.HasKey("assetPath") && payload.HasKey("prefabPath"))
                            {
                                payload["assetPath"] = payload["prefabPath"];
                            }
        
                            return payload;
                        })
                    };
        
                    return new UnityModuleTool("unity_prefab", "Prefab creation and instantiation module.", docs, handlers, "Advanced prefab override/apply flows can be added via dedicated prefab handlers.");
                }
    }
}
