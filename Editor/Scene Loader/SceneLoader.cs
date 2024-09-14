using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.SceneManagement;
using System.Linq;
using UnityEngine.SceneManagement;
using System.IO;

namespace OliverBeebe.UnityUtilities.Editor
{
    public class SceneLoader : EditorWindow
    {
        [MenuItem("Window/Oliver Utilities/Scene Loader")]
        private static void OpenSceneLoader()
        {
            CreateWindow<SceneLoader>(Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll"));
        }

        private const string sceneLoaderCachePath = "Editor/Scene Loader/Caches/Scene Loader Cache.asset";

        private SceneLoaderCache cache;
        private SceneLoaderCache Cache => cache != null ? cache : GenerateCache();

        private SceneLoaderCache GenerateCache()
        {
            cache = CreateInstance<SceneLoaderCache>();

            string directory = $"{Application.dataPath}/{Path.GetDirectoryName(sceneLoaderCachePath)}";
            Directory.CreateDirectory(directory);

            AssetDatabase.CreateAsset(cache, $"Assets/{sceneLoaderCachePath}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return cache;
        }

        private static Texture2D lightGrayTexture;

        private GUIStyle
            buttonStyle,
            addAllStyle;

        private GUIContent
            addAllScenesButton,
            removeAllScenesButton,
            openSceneButton ,
            loadSceneButton,
            selectSceneButton,
            addSceneButton,
            removeSceneButton;

        private bool initialized;

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;

            buttonStyle     = new(GUI.skin.button) { fixedWidth = 60 };
            addAllStyle     = new(GUI.skin.button) { fixedWidth = 80 };

            addAllScenesButton      = new("Add All", "Add all scene assets to build settings.");
            removeAllScenesButton   = new("Remove All", "Remove all scene assets from build settings.");
            openSceneButton         = new("Open", "Open scene file.");
            loadSceneButton         = new("Load");
            selectSceneButton       = new("Select", "Select scene asset in project window.");
            addSceneButton          = new("Add", "Add scene asset to build settings.");
            removeSceneButton       = new("Remove", "Remove scene asset from build settings.");

            float brigtness = 0.29f;
            lightGrayTexture = new(1, 1);
            lightGrayTexture.SetPixel(0, 0, new(brigtness, brigtness, brigtness, 1));
            lightGrayTexture.Apply();
        }

        private void OnGUI()
        {
            Initialize();

            Cache.UpdateScenes();

            titleContent = new("Scene Loader");

            EditorGUILayout.Space();

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("At runtime, only scenes added to the build settings can be loaded.", MessageType.Info);
                EditorGUILayout.Space();
            }

            var buildScenes = EditorBuildSettings.scenes.ToList();

            foreach (var folder in Cache.folders)
            {
                EditorGUILayout.BeginHorizontal();

                folder.editorFoldoutExpanded = EditorGUILayout.Foldout(folder.editorFoldoutExpanded, folder.folderName, true, EditorStyles.foldoutHeader);

                if (folder.editorFoldoutExpanded)
                {
                    var scenesNotInBuild = folder.scenes
                        .Where(scene => !buildScenes
                            .Any(buildScene => buildScene.guid == scene.guid))
                        .Select(scene => new EditorBuildSettingsScene(scene.path, true))
                        .ToArray();

                    GUI.enabled = !EditorApplication.isPlaying && scenesNotInBuild.Length > 0;

                    if (GUILayout.Button(addAllScenesButton, addAllStyle))
                    {
                        buildScenes.AddRange(scenesNotInBuild);
                    }

                    var scenesInBuild = buildScenes
                        .Where(buildScene => folder.scenes.Any(scene => scene.guid == buildScene.guid))
                        .ToArray();

                    GUI.enabled = !EditorApplication.isPlaying && scenesInBuild.Length > 0;

                    if (GUILayout.Button(removeAllScenesButton, addAllStyle))
                    {
                        buildScenes.RemoveAll(buildScene => folder.scenes.Any(scene => scene.guid == buildScene.guid));
                    }

                    GUI.enabled = true;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                if (folder.editorFoldoutExpanded)
                {
                    EditorGUI.indentLevel++;

                    float ogLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth *= 0.5f;

                    for (int i = 0; i < folder.scenes.Count; i++)
                    {
                        var scene = folder.scenes[i];

                        GUILayout.BeginHorizontal(new GUIStyle() { normal = new GUIStyleState() { background = lightGrayTexture } });

                        int buildSettingsSceneIndex = EditorBuildSettings.scenes.ToList().FindIndex(existing => existing.path == scene.path);
                        bool inBuildSettings = buildSettingsSceneIndex != -1;
                        EditorGUILayout.LabelField(scene.name);

                        var openButtonContent = EditorApplication.isPlaying
                            ? loadSceneButton
                            : openSceneButton;

                        GUI.enabled = !EditorApplication.isPlaying || inBuildSettings;

                        if (GUILayout.Button(openButtonContent, buttonStyle))
                        {
                            if (EditorApplication.isPlaying)
                            {
                                SceneManager.LoadScene(buildSettingsSceneIndex);
                            }
                            else
                            {
                                EditorSceneManager.OpenScene(scene.path);
                            }
                        }

                        GUI.enabled = true;

                        if (GUILayout.Button(selectSceneButton, buttonStyle))
                        {
                            EditorUtility.FocusProjectWindow();
                            Selection.activeObject = scene.asset;
                        }

                        GUI.enabled = !EditorApplication.isPlaying;

                        if (!inBuildSettings && GUILayout.Button(addSceneButton, buttonStyle))
                        {
                            buildScenes.Add(new(scene.path, true));
                        }

                        if (inBuildSettings && GUILayout.Button(removeSceneButton, buttonStyle))
                        {
                            buildScenes.RemoveAt(buildSettingsSceneIndex);
                        }

                        GUI.enabled = true;

                        GUILayout.EndHorizontal();

                        EditorGUILayout.Space();
                    }

                    EditorGUIUtility.labelWidth = ogLabelWidth;

                    EditorGUI.indentLevel--;
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
        }
    }
}
