using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.SceneManagement;
using System.IO;

namespace OliverBeebe.UnityUtilities.Editor
{
    public class SceneLoader : EditorWindow
    {
        private const string sceneLoaderCachePath = "Editor/Scene Loader/Scene Loader Cache.asset";

        [MenuItem("Window/Oliver Utilities/Scene Loader")]
        private static void OpenSceneLoader()
        {
            CreateWindow<SceneLoader>(Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll"));
        }

        private void GenerateCache()
        {
            cache = CreateInstance<SceneLoaderCache>();

            string directory = $"{Application.dataPath}/{Path.GetDirectoryName(sceneLoaderCachePath)}";
            Directory.CreateDirectory(directory);

            AssetDatabase.CreateAsset(cache, $"Assets/{sceneLoaderCachePath}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private SceneLoaderCache cache;
        private SceneLoaderCache Cache
        {
            get
            {
                if (cache == null)
                {
                    GenerateCache();
                }

                return cache;
            }
        }

        private static void LoadScene(string name)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Can't load scenes with editor while in play mode!");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(name);
            }
        }

        private void OnGUI()
        {
            Cache.UpdateScenes();

            if (GUILayout.Button("Regenerate Cache"))
            {
                GenerateCache();
            }

            EditorGUILayout.Space();

            foreach (var folder in Cache.Folders)
            {
                bool foldout = EditorGUILayout.Foldout(folder.editorFoldoutExpanded, folder.FolderName, true, EditorStyles.foldoutHeader);

                folder.editorFoldoutExpanded = foldout;

                if (folder.editorFoldoutExpanded)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.Space();

                    for (int i = folder.scenes.Count - 1; i >= 0; i--)
                    {
                        var scene = folder.scenes[i];

                        GUILayout.BeginHorizontal(new GUIStyle()
                        {
                            normal = i % 2 == 1
                            ? new GUIStyleState()
                                {
                                    background = Cache.LightGrayTexture,
                                }
                            : new GUIStyleState()
                        });

                        EditorGUILayout.LabelField(scene.name);

                        if (GUILayout.Button("Open"))
                        {
                            LoadScene(scene.path);
                        }

                        if (GUILayout.Button("Select"))
                        {
                            EditorUtility.FocusProjectWindow();
                            Selection.activeObject = scene.asset;
                        }

                        GUILayout.EndHorizontal();

                        //if (i != folder.scenes.Count - 1)
                        //{
                        //    EditorGUILayout.Space();
                        //}
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
            }

            AssetDatabase.SaveAssetIfDirty(Cache);
        }
    }
}
