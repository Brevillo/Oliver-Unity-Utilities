using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.SceneManagement;
using System.Linq;
using UnityEngine.SceneManagement;

namespace OliverBeebe.UnityUtilities.Editor
{
    public class SceneLoader : EditorWindow
    {
        [MenuItem("Window/Oliver Utilities/Scene Loader")]
        private static void OpenSceneLoader()
        {
            CreateWindow<SceneLoader>(Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll"));
        }

        private SceneLoaderCache cache;
        private SceneLoaderCache Cache => cache != null ? cache : cache = CreateInstance<SceneLoaderCache>().Initialize();

        private void OnGUI()
        {
            Cache.UpdateScenes();

            titleContent = new("Scene Loader");

            EditorGUILayout.Space();

            foreach (var folder in Cache.folders)
            {
                bool foldout = EditorGUILayout.Foldout(folder.editorFoldoutExpanded, folder.folderName, true, EditorStyles.foldoutHeader);

                folder.editorFoldoutExpanded = foldout;

                if (folder.editorFoldoutExpanded)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.Space();

                    for (int i = 0; i < folder.scenes.Count; i++)
                    {
                        var scene = folder.scenes[i];

                        GUILayout.BeginHorizontal(new GUIStyle()
                        {
                            normal = i % 2 == 1
                            ? new GUIStyleState()
                                {
                                    background = Cache.lightGrayTexture,
                                }
                            : new GUIStyleState()
                        });

                        int buildSettingsSceneIndex = EditorBuildSettings.scenes.ToList().FindIndex(existing => existing.path == scene.path);
                        bool inBuildSettings = buildSettingsSceneIndex != -1;

                        EditorGUILayout.LabelField(scene.name);

                        GUIContent openButtonContent = EditorApplication.isPlaying
                            ? new("Load", inBuildSettings ? "Load scene file." : "Can't load scene because it isn't in the build settings.")
                            : new("Open", "Open scene file.");

                        GUI.enabled = !EditorApplication.isPlaying || inBuildSettings;

                        if (GUILayout.Button(openButtonContent))
                        {
                            if (Application.isPlaying)
                            {
                                SceneManager.LoadScene(buildSettingsSceneIndex);
                            }
                            else
                            {
                                EditorSceneManager.OpenScene(scene.path);
                            }
                        }

                        GUI.enabled = true;

                        if (GUILayout.Button("Select"))
                        {
                            EditorUtility.FocusProjectWindow();
                            Selection.activeObject = scene.asset;
                        }

                        GUI.enabled = !Application.isPlaying;

                        var buildScenes = EditorBuildSettings.scenes.ToList();

                        if (!inBuildSettings
                            && GUILayout.Button("Add to Build"))
                        {
                            buildScenes.Add(new(scene.path, true));
                        }

                        if (inBuildSettings
                            && GUILayout.Button("Remove from Build"))
                        {
                            buildScenes.RemoveAt(buildSettingsSceneIndex);
                        }

                        EditorBuildSettings.scenes = buildScenes.ToArray();

                        GUI.enabled = true;

                        GUILayout.EndHorizontal();
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
            }
        }
    }
}
