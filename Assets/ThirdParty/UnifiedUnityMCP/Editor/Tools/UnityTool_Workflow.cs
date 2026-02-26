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
        private static UnityModuleTool CreateWorkflowModule()
                {
                    return CreateBridgeOnlyModule(
                        "unity_workflow",
                        "Workflow history/session module.",
                        "Use unity_history actions for editor undo/redo and track higher-level sessions in your MCP client.",
                        "workflow_session_start",
                        "workflow_session_end",
                        "workflow_session_undo",
                        "workflow_session_list",
                        "workflow_session_status",
                        "workflow_task_start",
                        "workflow_task_end",
                        "workflow_snapshot_object",
                        "workflow_snapshot_created",
                        "workflow_list",
                        "workflow_undo_task",
                        "workflow_redo_task",
                        "workflow_undone_list",
                        "workflow_revert_task",
                        "workflow_delete_task");
                }
    }
}
