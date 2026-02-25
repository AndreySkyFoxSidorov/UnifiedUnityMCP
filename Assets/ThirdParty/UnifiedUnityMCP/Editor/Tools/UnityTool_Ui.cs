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
        private static UnityModuleTool CreateUiModule()
                {
                    return CreateBridgeOnlyModule(
                        "unity_ui",
                        "UGUI creation and layout module.",
                        "UI workflows can be scripted through editor menu execution and component/property tools.",
                        "ui_create_canvas",
                        "ui_create_panel",
                        "ui_create_button",
                        "ui_create_text",
                        "ui_create_image",
                        "ui_create_inputfield",
                        "ui_create_slider",
                        "ui_create_toggle",
                        "ui_set_text",
                        "ui_find_all",
                        "ui_set_rect",
                        "ui_set_anchor",
                        "ui_layout_children",
                        "ui_align_selected",
                        "ui_distribute_selected",
                        "ui_create_batch");
                }
    }
}
