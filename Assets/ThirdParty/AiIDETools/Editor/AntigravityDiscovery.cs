using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CodeEditor;

namespace AntigravityEditor
{
    public interface IDiscovery
    {
        CodeEditor.Installation[] PathCallback();
    }

    public class AntigravityDiscovery : IDiscovery
    {
        List<CodeEditor.Installation> m_Installations;

        public CodeEditor.Installation[] PathCallback()
        {
            if (m_Installations == null)
            {
                m_Installations = new List<CodeEditor.Installation>();
                FindInstallationPaths();
            }

            return m_Installations.ToArray();
        }

        void FindInstallationPaths()
        {
            var installations = new List<(string Name, string Path)>();

#if UNITY_EDITOR_OSX
            AddIfExists(installations, "Antigravity", "/Applications/Antigravity.app");
            AddIfExists(installations, "Cursor", "/Applications/Cursor.app");
            AddIfExists(installations, "Windsurf", "/Applications/Windsurf.app");
            AddIfExists(installations, "Visual Studio Code", "/Applications/Visual Studio Code.app");
            AddIfExists(installations, "Visual Studio Code - Insiders", "/Applications/Visual Studio Code - Insiders.app");
#elif UNITY_EDITOR_WIN
            var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA")?.Replace("\\", "/");
            var programFiles = Environment.GetEnvironmentVariable("ProgramFiles")?.Replace("\\", "/");
            var programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)")?.Replace("\\", "/");

            // Antigravity
            AddIfExists(installations, "Antigravity", "C:/NeuralNetworks/Antigravity/Antigravity.exe");
            AddIfExists(installations, "Antigravity", $"{programFiles}/Antigravity/Antigravity.exe");
            AddIfExists(installations, "Antigravity", $"{programFilesX86}/Antigravity/Antigravity.exe");
            AddIfExists(installations, "Antigravity", $"{localAppData}/Programs/Antigravity/Antigravity.exe");

            // Cursor
            AddIfExists(installations, "Cursor", $"{localAppData}/Programs/cursor/Cursor.exe");
            AddIfExists(installations, "Cursor", $"{programFiles}/Cursor/Cursor.exe");

            // Windsurf
            AddIfExists(installations, "Windsurf", $"{localAppData}/Programs/Windsurf/Windsurf.exe");
            AddIfExists(installations, "Windsurf", $"{programFiles}/Windsurf/Windsurf.exe");

            // VSCode
            AddIfExists(installations, "Visual Studio Code", $"{programFiles}/Microsoft VS Code/Code.exe");
            AddIfExists(installations, "Visual Studio Code", $"{programFiles}/Microsoft VS Code/bin/code.cmd");
            AddIfExists(installations, "Visual Studio Code", $"{localAppData}/Programs/Microsoft VS Code/Code.exe");
            AddIfExists(installations, "Visual Studio Code", $"{localAppData}/Programs/Microsoft VS Code/bin/code.cmd");
            AddIfExists(installations, "Visual Studio Code - Insiders", $"{programFiles}/Microsoft VS Code Insiders/Code.exe");
            AddIfExists(installations, "Visual Studio Code - Insiders", $"{programFiles}/Microsoft VS Code Insiders/bin/code-insiders.cmd");
            AddIfExists(installations, "Visual Studio Code - Insiders", $"{localAppData}/Programs/Microsoft VS Code Insiders/Code.exe");
            AddIfExists(installations, "Visual Studio Code - Insiders", $"{localAppData}/Programs/Microsoft VS Code Insiders/bin/code-insiders.cmd");
#else
            // Linux
            AddIfExists(installations, "Antigravity", "/usr/bin/antigravity");
            AddIfExists(installations, "Antigravity", "/usr/local/bin/antigravity");
            AddIfExists(installations, "Cursor", "/usr/bin/cursor");
            AddIfExists(installations, "Cursor", "/snap/bin/cursor");
            AddIfExists(installations, "Windsurf", "/usr/bin/windsurf");
            AddIfExists(installations, "Windsurf", "/snap/bin/windsurf");
            AddIfExists(installations, "Visual Studio Code", "/usr/bin/code");
            AddIfExists(installations, "Visual Studio Code", "/snap/bin/code");
            AddIfExists(installations, "Visual Studio Code", "/usr/local/bin/code");
#endif

            if (installations.Count == 0) return;

            // Handle duplicates and prefix paths if multiple versions exist
            var distinctPaths = installations
                .GroupBy(i => i.Path)
                .Select(g => g.First())
                .ToList();

            if (distinctPaths.Count == 1)
            {
                m_Installations = new List<CodeEditor.Installation>
                {
                    new CodeEditor.Installation { Name = distinctPaths[0].Name, Path = distinctPaths[0].Path }
                };
                return;
            }

            m_Installations = distinctPaths.Select(i => new CodeEditor.Installation
            {
                Name = i.Name,
                Path = i.Path
            }).ToList();
        }

        void AddIfExists(List<(string Name, string Path)> installations, string name, string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            
            bool exists = false;
#if UNITY_EDITOR_OSX
            exists = Directory.Exists(path);
#else
            exists = File.Exists(path);
#endif

            if (exists)
            {
                installations.Add((name, path));
            }
        }
    }
}
