using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
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
                displayName = name
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

                folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
                note = "";
                color = Color.white;

                editorFoldoutExpanded = new(false)
                {
                    speed = 6f,
                };
            }

            public AnimBool editorFoldoutExpanded;

            public string folderPath;
            public DefaultAsset folderAsset;
            public string name;

            public string displayName;
            public string note;
            public Color color;

            public Texture2D texture;
            public Texture2D GetTexture(Color color)
            {
                if (texture == null)
                {
                    texture = new(1, 1);
                }

                texture.SetPixel(0, 0, color);
                texture.Apply();

                return texture;
            }

            public List<Scene> scenes;
        }

        [Serializable]
        public class Scene
        {
            public Scene(SceneAsset asset)
            {
                this.asset = asset;
                displayName = name = ObjectNames.NicifyVariableName(asset.name);
                note = "";
                color = Color.white;
            }

            public SceneAsset asset;
            public string name;

            public string displayName;
            public string note;
            public Color color;

            public Texture2D texture;
            public Texture2D GetTexture(Color color)
            {
                if (texture == null)
                {
                    texture = new(1, 1);
                }

                texture.SetPixel(0, 0, color);
                texture.Apply();

                return texture;
            }

            public string Path => AssetDatabase.GetAssetPath(asset);
            public GUID Guid => AssetDatabase.GUIDFromAssetPath(Path);
        }

        public void UpdateScenes()
        {
            var scenes = AssetDatabase.FindAssets("t:scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .Select(path => (
                    folderPath: Path.GetDirectoryName(path),
                    scene: new Scene(AssetDatabase.LoadAssetAtPath<SceneAsset>(path))))
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

                folder.scenes.RemoveAll(scene => !scenes.Exists(existing => existing.scene.Path == scene.Path));

                if (folder.scenes.Count == 0)
                {
                    folders.RemoveAt(folderIndex);
                }
            }

            foreach (var folder in folders)
            {
                NaturalSort(ref folder.scenes);
            }
        }

        public static void NaturalSort(ref List<Scene> list)
        {
            int maxLen = list.Max(scene => scene.name.Length);
            list = list
                .OrderBy(scene => Regex.Replace(scene.name, @"(\d+)|(\D+)",
                    match => match.Value.PadLeft(maxLen, char.IsDigit(match.Value[0]) ? ' ' : char.MaxValue)))
                .ToList();
        }
    }
}
