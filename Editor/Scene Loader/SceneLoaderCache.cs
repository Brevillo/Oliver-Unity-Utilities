using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace OliverBeebe.UnityUtilities.Editor
{
    public class SceneLoaderCache : ScriptableObject
    {
        public List<Folder> folders = new();

        [Serializable]
        public class Folder
        {
            public Folder(string folderPath)
            {
                this.folderPath = folderPath;
                folderName
                    = folderPath == "Assets" ? "Assets"
                    : folderPath == "Assets/Scenes" ? "Scenes"
                    : folderPath
                    .Split('/')
                    .SelectMany(name => name.Split(' '))
                    .Where(name => name != "Scenes")                        // remove "scenes"
                    .Where(name => name != "Assets")                        // remove "assets"
                    .Where(name => !Regex.Match(name, @"\d").Success)       // remove numbers
                    .Select(name => ObjectNames.NicifyVariableName(name))   // put spaces between words
                    .Aggregate((total, name) => $"{total} {name}");

                scenes = new();
            }

            public bool editorFoldoutExpanded = true;

            public string folderPath;
            public string folderName;

            public List<Scene> scenes;
        }

        [Serializable]
        public struct Scene
        {
            public Scene(string path, SceneAsset asset)
            {
                this.path = path;
                this.asset = asset;
                name = ObjectNames.NicifyVariableName(asset.name);
                guid = AssetDatabase.GUIDFromAssetPath(path);
            }

            public string path;
            public SceneAsset asset;
            public string name;
            public GUID guid;
        }

        public void UpdateScenes()
        {
            var scenes = AssetDatabase.FindAssets("t:scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .Select(path => (
                    folderPath: Path.GetDirectoryName(path),
                    scene: new Scene(path, AssetDatabase.LoadAssetAtPath<SceneAsset>(path))))
                .ToList();

            // add new scenes
            foreach (var (folderPath, scene) in scenes)
            {
                var existingFolder = folders.Find(folder => folder.folderPath == folderPath);

                if (existingFolder == null)
                {
                    existingFolder = new Folder(folderPath);
                    folders.Add(existingFolder);
                }

                if (!existingFolder.scenes.Exists(existingScene => existingScene.asset == scene.asset))
                {
                    existingFolder.scenes.Add(scene);
                }
            }

            // remove deleted scenes
            for (int folderIndex = folders.Count - 1; folderIndex >= 0; folderIndex--)
            {
                var folder = folders[folderIndex];

                folder.scenes.RemoveAll(scene => !scenes.Exists(existing => existing.scene.path == scene.path));

                if (folder.scenes.Count == 0)
                {
                    folders.RemoveAt(folderIndex);
                }
            }
        }
    }
}
