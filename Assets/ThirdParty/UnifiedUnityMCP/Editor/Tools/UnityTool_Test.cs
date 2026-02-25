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
        private static UnityModuleTool CreateTestModule()
                {
                    var docs = CreateDocs("test_list", "test_run", "test_get_result", "test_cancel", "bridge");
                    var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["bridge"] = CreateBridgeHandler("unity_test"),
                        ["test_run"] = Forward("unity_test_run", raw =>
                        {
                            var payload = ExtractArguments(raw);
                            if (payload.HasKey("testMode") && !payload.HasKey("mode"))
                            {
                                string mode = payload["testMode"].Value;
                                payload["mode"] = string.Equals(mode, "PlayMode", StringComparison.OrdinalIgnoreCase) ? "playmode" : "editmode";
                            }
        
                            if (!payload.HasKey("mode"))
                            {
                                payload["mode"] = "editmode";
                            }
        
                            return payload;
                        })
                    };
        
                    return new UnityModuleTool("unity_test", "Unity Test Runner module.", docs, handlers, "Async job polling/cancel can be layered on top of unity_test_run results.");
                }
    }
}
