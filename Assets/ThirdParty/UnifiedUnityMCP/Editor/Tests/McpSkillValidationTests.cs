using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using NUnit.Framework;
using SimpleJSON;

namespace Mcp.Editor.Tests
{
    public class McpSkillValidationTests
    {
        [Test]
        public void AllSkillTools_ExistInActiveToolsJson()
        {
            string workspaceRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../"));
            string skillsDir = Path.Combine(workspaceRoot, ".agent", "skills");
            string activeToolsPath = Path.Combine(UnityEngine.Application.dataPath, "ThirdParty", "UnifiedUnityMCP", "Editor", "active_tools.json");

            Assert.IsTrue(Directory.Exists(skillsDir), $"Skills directory not found: {skillsDir}");
            Assert.IsTrue(File.Exists(activeToolsPath), $"active_tools.json not found: {activeToolsPath}");

            // Load active tools
            var activeToolsJson = JSON.Parse(File.ReadAllText(activeToolsPath)) as JSONObject;
            Assert.IsNotNull(activeToolsJson);
            var toolsArray = activeToolsJson["tools"].AsArray;
            Assert.IsNotNull(toolsArray);

            HashSet<string> serverTools = new HashSet<string>();
            foreach (var tool in toolsArray)
            {
                serverTools.Add(tool.Value["name"].Value);
            }

            Assert.IsTrue(serverTools.Count > 0, "No tools found in active_tools.json");

            // Validate skills
            var skillFiles = Directory.GetFiles(skillsDir, "SKILL.md", SearchOption.AllDirectories);
            List<string> mismatches = new List<string>();

            Regex toolRegex = new Regex(@"\`(?:call:)?(unity_[A-Za-z0-9_]+)[({]?\`");

            foreach (var file in skillFiles)
            {
                string content = File.ReadAllText(file);
                var matches = toolRegex.Matches(content);
                foreach (Match match in matches)
                {
                    string toolName = match.Groups[1].Value;

                    // Ignore known specific examples from skill-creator that aren't real tools
                    if (toolName == "unity_something" || toolName == "unity_editor_ui" || toolName == "unity_ui_guidelines" || toolName == "unity_unitask")
                    {
                        continue;
                    }

                    if (!serverTools.Contains(toolName))
                    {
                        mismatches.Add($"File '{Path.GetFileName(Path.GetDirectoryName(file))}/SKILL.md' uses tool '{toolName}' which is missing from server.");
                    }
                }
            }

            if (mismatches.Count > 0)
            {
                Assert.Fail("Found tool mismatches between skills and server:\n" + string.Join("\n", mismatches));
            }
        }
    }
}
