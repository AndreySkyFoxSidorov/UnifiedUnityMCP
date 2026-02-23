using System;
using System.Linq;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;

namespace Mcp.Editor.Tools
{
    public class BuildManageTool : ITool
    {
        public string Name => "unity_build_manage";
        public string Description => "Manage Scripting Defines and Trigger Builds. Actions: 'get_defines', 'set_defines', 'build_player'.";

        public JSONObject InputSchema
        {
            get
            {
                var props = new JSONObject();
                props["action"] = McpMessages.CreateStringProperty("Action: 'get_defines', 'set_defines', 'build_player'.");
                props["defines"] = McpMessages.CreateStringProperty("Semi-colon separated defines for 'set_defines' (e.g. 'DEBUG_MODE;TESTING').");
                props["buildTarget"] = McpMessages.CreateStringProperty("Target for 'build_player' (e.g. 'StandaloneWindows64', 'Android', 'iOS'). Default: current active target.");
                props["buildPath"] = McpMessages.CreateStringProperty("Output path for 'build_player' (e.g. 'Builds/Game.exe'). Default: 'Builds/App'");

                var required = new JSONArray();
                required.Add("action");

                return McpMessages.CreateToolSchema(Name, Description, props, required);
            }
        }

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            string action = arguments.GetString("action")?.ToLower();

            MainThreadDispatcher.InvokeAsync(() =>
            {
                try
                {
                    BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;

                    if (action == "get_defines")
                    {
                        var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                        string currentDefines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
                        var res = new JSONObject();
                        res["defines"] = currentDefines;
                        sendResponse(McpMessages.CreateToolResult(res.ToString()));
                        return;
                    }

                    if (action == "set_defines")
                    {
                        string defines = arguments.GetString("defines", "");
                        var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                        PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
                        var res = new JSONObject();
                        res["status"] = "Defines updated for group " + group.ToString();
                        res["defines"] = defines;
                        sendResponse(McpMessages.CreateToolResult(res.ToString()));
                        return;
                    }

                    if (action == "build_player")
                    {
                        string targetStr = arguments.GetString("buildTarget");
                        string pathStr = arguments.GetString("buildPath", "Builds/App");

                        BuildTarget bTarget = EditorUserBuildSettings.activeBuildTarget;
                        if (!string.IsNullOrEmpty(targetStr) && Enum.TryParse(targetStr, true, out BuildTarget parsedTarget))
                        {
                            bTarget = parsedTarget;
                        }

                        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                        if (scenes.Length == 0)
                        {
                            sendError("No enabled scenes in Build Settings.");
                            return;
                        }

                        var report = BuildPipeline.BuildPlayer(scenes, pathStr, bTarget, BuildOptions.None);

                        var res = new JSONObject();
                        res["result"] = report.summary.result.ToString();
                        res["totalErrors"] = report.summary.totalErrors;
                        res["totalWarnings"] = report.summary.totalWarnings;
                        res["outputPath"] = report.summary.outputPath;
                        sendResponse(McpMessages.CreateToolResult(res.ToString()));
                        return;
                    }

                    sendError($"Invalid action '{action}'.");
                }
                catch (Exception e)
                {
                    sendError($"Build tool failed: {e.Message}");
                }
            });
        }
    }
}
