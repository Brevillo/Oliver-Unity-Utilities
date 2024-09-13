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
        [Serializable]
        public class Folder
        {
            public Folder(string folderPath)
            {
                FolderPath = folderPath;
                scenes = new();
            }

            public bool editorFoldoutExpanded = true;

            private string folderPath;
            private string folderName;

            public List<Scene> scenes;

            public string FolderPath
            {
                get => folderPath;
                set
                {
                    folderPath = value;
                    folderName
                        = value == "Assets" ? "Assets"
                        : value == "Assets/Scenes" ? "Scenes"
                        : value
                        .Split('/')
                        .SelectMany(name => name.Split(' '))
                        .Where(name => name != "Scenes")                        // remove "scenes"
                        .Where(name => name != "Assets")                        // remove "assets"
                        .Where(name => !Regex.Match(name, @"\d").Success)       // remove numbers
                        .Select(name => ObjectNames.NicifyVariableName(name))   // put spaces between words
                        .Aggregate((total, name) => $"{total} {name}");
                }
            }

            public string FolderName => folderName;
        }

        [Serializable]
        public struct Scene
        {
            public Scene(string path, SceneAsset asset)
            {
                this.path = path;
                this.asset = asset;
                name = ObjectNames.NicifyVariableName(asset.name);
            }

            public string path;
            public SceneAsset asset;
            public string name;
        }

        public void UpdateScenes()
        {
            folders ??= new();

            var scenes = AssetDatabase.FindAssets("t:scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => (
                    folderPath: Path.GetDirectoryName(path),
                    assetPath: path,
                    asset: AssetDatabase.LoadAssetAtPath<SceneAsset>(path)))
                .ToList();

            // add new scenes
            foreach (var (folderPath, assetPath, sceneAsset) in scenes)
            {
                var existingFolder = folders.Find(folder => folder.FolderPath == folderPath);

                if (existingFolder == null)
                {
                    existingFolder = new Folder(folderPath);
                    folders.Add(existingFolder);
                }

                if (!existingFolder.scenes.Exists(scene => scene.asset == sceneAsset))
                {
                    existingFolder.scenes.Add(new(assetPath, sceneAsset));
                }
            }

            // remove deleted scenes
            for (int folderIndex = folders.Count - 1; folderIndex >= 0; folderIndex--)
            {
                var folder = folders[folderIndex];

                folder.scenes.RemoveAll(scene => scene.asset == null);

                if (folder.scenes.Count == 0)
                {
                    folders.RemoveAt(folderIndex);
                }
            }
        }

        [SerializeField]
        private List<Folder> folders;

        [SerializeField]
        private Texture2D lightGrayTexture;

        private const float brigtness = 0.19f;

        public Texture2D LightGrayTexture
        {
            get
            {
                if (lightGrayTexture == null)
                {
                    lightGrayTexture = new(1, 1);
                    lightGrayTexture.SetPixel(0, 0, new(brigtness, brigtness, brigtness, 1));
                    lightGrayTexture.Apply();
                }

                return lightGrayTexture;
            }
        }

        public List<Folder> Folders
        {
            get
            {
                if (folders == null)
                {
                    UpdateScenes();
                }

                return folders;
            }
        }
    }
}
