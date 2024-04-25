// Made by Oliver Beebe 2024
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

[CreateAssetMenu(fileName = "Folder Code Line Counter", menuName = "Oliver Utilities/Editor/Folder Code Line Counter")]
public class FolderCodeLineCounter : ScriptableObject
{
    [CustomEditor(typeof(FolderCodeLineCounter))]
    private class FolderCodeLineCounterEditor : Editor
    {
        private string folderPath = "";

        public override void OnInspectorGUI()
        {
            void CleanFolderPath()
            {
                if (!folderPath.StartsWith("Assets")) folderPath = "Assets";
            }

            CleanFolderPath();
            folderPath = EditorGUILayout.TextField("Folder Path", folderPath);
            CleanFolderPath();

            try
            {
                var folders = new List<string>() { folderPath };

                var subFolders = (IEnumerable<string>) new[] { folderPath };
                while (subFolders.Any())
                {
                    subFolders = subFolders.SelectMany(AssetDatabase.GetSubFolders);
                    folders.AddRange(subFolders);
                }

                var scripts = folders
                    .SelectMany(Directory.GetFiles)
                    .Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
                    .Where(asset => asset != null)
                    .OfType<MonoScript>()
                    .Select(script => (script.name, lines: script.text.Split("\n").Length))
                    .OrderBy(e => -e.lines)
                    .ToArray();

                int totalLines = scripts.Sum(entry => entry.lines);
                int totalScripts = scripts.Count();
                string scriptsFormatted = string.Join("\n", scripts.Select((entry, i) => $"  {i + 1:000}         {entry.lines:0000}         {entry.name}"));
                string message = $"Total Lines: {totalLines}\nTotal Scripts: {totalScripts}\n\n Rank        Lines        Script\n\n{scriptsFormatted}";

                EditorGUILayout.HelpBox(message, MessageType.None, true);
            }
            catch
            {
                EditorGUILayout.HelpBox("Invalid Folder Path!", MessageType.Error, true);
            }
        }
    }
}
