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
        private static UnityModuleTool CreateTimelineModule()
                {
                    return CreateBridgeOnlyModule(
                        "unity_timeline",
                        "Timeline authoring module.",
                        "Timeline-specific authoring can be integrated with PlayableDirector tools incrementally.",
                        "timeline_create",
                        "timeline_add_audio_track",
                        "timeline_add_animation_track");
                }
    }
}
