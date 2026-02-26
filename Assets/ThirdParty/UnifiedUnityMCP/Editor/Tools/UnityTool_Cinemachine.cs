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
        private static UnityModuleTool CreateCinemachineModule()
                {
                    return CreateBridgeOnlyModule(
                        "unity_cinemachine",
                        "Cinemachine orchestration module.",
                        "Cinemachine-specific helpers can be driven through component/property actions and package checks.",
                        "cinemachine_create_vcam",
                        "cinemachine_inspect_vcam",
                        "cinemachine_set_vcam_property",
                        "cinemachine_set_targets",
                        "cinemachine_set_component",
                        "cinemachine_add_component",
                        "cinemachine_set_lens",
                        "cinemachine_list_components",
                        "cinemachine_impulse_generate",
                        "cinemachine_get_brain_info",
                        "cinemachine_create_target_group",
                        "cinemachine_target_group_add_member",
                        "cinemachine_target_group_remove_member",
                        "cinemachine_set_spline",
                        "cinemachine_add_extension",
                        "cinemachine_remove_extension",
                        "cinemachine_set_active",
                        "cinemachine_create_mixing_camera",
                        "cinemachine_mixing_camera_set_weight",
                        "cinemachine_create_clear_shot",
                        "cinemachine_create_state_driven_camera",
                        "cinemachine_state_driven_camera_add_instruction",
                        "cinemachine_set_noise");
                }
    }
}
