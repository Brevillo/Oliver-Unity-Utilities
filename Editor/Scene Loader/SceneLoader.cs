using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.SceneManagement;
using System.Linq;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEditor.AnimatedValues;

/* todo
 * button for putting gitignore into clipboard
 * 
 * 
 * 
 * OnGUI : all open scenes from scene manager
 * SceneHelper
 *      all folder scenes,
 *      checking if every folder scene is in the build settings
 *      converting all folder scenes to editor build setting scenes
 *      converting all folder scenes to scene assets
 *      getting all the indeces of build setting scenes that aren't the active scene
 * FolderHelper
 *      checking if any folders aren't expanded
 *      checking if any folders are expanded
 * FolderAndScene
 *      getting all build setting scenes that are enabled and contained in the cached guids list
 * Folder
 *      getting all the folder scenes that are not in the build settings, and converting to editor build settings
 *      getting all the build scenes that are in the folder scenes
 *      converting all folder scenes into scene assets
 */

namespace OliverBeebe.UnityUtilities.Editor
{
    public class SceneLoader : EditorWindow
    {
        [MenuItem("Window/Oliver Utilities/Scene Loader")]
        private static void OpenSceneLoader()
        {
            CreateWindow<SceneLoader>(Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll"));
        }

        private void OnEnable()
        {
            titleContent = new("Scene Loader");
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssetDatabaseUpdated += OnAssetDatabaseUpdated;

            foreach (var animBool in AnimBools)
            {
                animBool.valueChanged.AddListener(Repaint);
            }
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            AssetDatabaseUpdated -= OnAssetDatabaseUpdated;

            foreach (var animBool in AnimBools)
            {
                animBool.valueChanged.RemoveListener(Repaint);
            }
        }

        private class SceneLoaderPostprocessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                AssetDatabaseUpdated?.Invoke();
            }
        }

        private void OnAssetDatabaseUpdated()
        {
            Cache.UpdateScenes();
        }

        private static event Action AssetDatabaseUpdated;

        private SceneLoaderCache cache;
        private SceneLoaderCache Cache => cache != null ? cache : GetCache();

        private SceneLoaderCache GetCache()
        {
            var loaded = AssetDatabase.LoadAssetAtPath<SceneLoaderCache>($"Assets/{sceneLoaderCachePath}");

            if (loaded != null)
            {
                cache = loaded;
            }
            else
            {
                GenerateCache();
            }

            cache.UpdateScenes();
            cache.UpdateAnimBools(this);

            return cache;
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

        #region Constants and Style/Content Caches

        private const string

            sceneLoaderCachesDirectory = "Editor/Scene Loader/Caches",
            sceneLoaderCachePath = sceneLoaderCachesDirectory + "/Scene Loader Cache.asset",

            gitIgnore = "# Ignore Scene Loader caches\n/" + sceneLoaderCachesDirectory;

        private const float

            settingsLabelWidth = 130,
            sceneLabelWidth = 17,
            notesLabelWidth = 60,
            maxSaturation = 0.75f,

            openSceneValue = 0.16f,
            unopenedSceneValue = 0.27f,
            folderBaseValue = 0.22f,

            animationSpeed = 4f;

        private static readonly (string title, string message, string confirm, string cancel)
            regenerateConfirmation = (
                title:   "Regenerate Scene Loader Cache",
                message: "This will reset all name, note, and color changes for the folders and scenes.",
                confirm: "Regenerate",
                cancel:  "Cancel");

        private GUIStyle
            buttonStyle,
            editableNoteStyle,
            displayingNoteStyle;

        private GUIContent

            sceneLoadingAtRuntimeHelp,

            settingsFoldout,
            selectScenesToggle,
            showBuildSettingsToggle,
            buildSettingsWarning,
            loadSceneModeLabel,
            editNamesToggle,
            editNotesToggle,
            editColorsToggle,
            doAnimationsToggle,

            regenerateCache,
            copyGitIgnore,
            copyGitIgnoreCopied,

            reloadSceneButton,
            expandFoldersButton,
            collapseFoldersButton,

            openAllScenesButton,
            closeAllScenesButton,
            loadAllScenesButton,
            unloadAllScenesButton,

            selectAllScenesButton,
            addAllScenesButton,
            removeAllScenesButton,

            openSceneButton,
            closeSceneButton,
            loadSceneButton,
            unloadSceneButton,
            selectSceneButton,
            addSceneButton,
            removeSceneButton;

        #endregion

        #region State

        private bool styleAndContentCached;

        private bool animated = true;

        private AnimBool[] animBools;
        private AnimBool[] AnimBools => animBools ??= new[]
        {
            showSceneLoaderSettings,
            selectScenes,
            editBuildSettings,
            editNotes,
            editColors,
            editNames,
            additiveLoading,
            closeAllScenes,
        };

        private readonly AnimBool showSceneLoaderSettings = new(false)
        {
            speed = animationSpeed,
        };
        private readonly AnimBool selectScenes = new(false)
        {
            speed = animationSpeed,
        };
        private readonly AnimBool editBuildSettings = new(false)
        {
            speed = animationSpeed,
        };
        private readonly AnimBool editNotes = new(false)
        {
            speed = animationSpeed,
        };
        private readonly AnimBool editColors = new(false)
        {
            speed = animationSpeed,
        };
        private readonly AnimBool editNames = new(false)
        {
            speed = animationSpeed,
        };
        private readonly AnimBool additiveLoading = new(false)
        {
            speed = animationSpeed,
        };
        private readonly AnimBool closeAllScenes = new(false)
        {
            speed = animationSpeed,
        };

        private Vector2 scrollPosition = Vector2.zero;
        private LoadSceneMode loadSceneMode = LoadSceneMode.Single;

        #endregion

        private void CacheStyleAndContent()
        {
            styleAndContentCached = true;

            buttonStyle = new(GUI.skin.button)
            {
                fixedHeight = EditorGUIUtility.singleLineHeight,
                fixedWidth = 80,
                padding = new(0, 0, 0, 0),
            };
            editableNoteStyle = new GUIStyle(GUI.skin.textArea)
            {
                wordWrap = true,
                padding = new(3, 3, 2, 2),
            };
            displayingNoteStyle = new(GUI.skin.label)
            {
                wordWrap = true,
            };

            sceneLoadingAtRuntimeHelp = new("Only enabled scenes in the build settings can be loaded at runtime.");

            settingsFoldout         = new("Scene Loader Settings");
            selectScenesToggle      = new("Select Scenes", "Show buttons for selecting scene assets in the project window.");
            showBuildSettingsToggle = new("Edit Build Settings", "Shows buttons for editing build settings.");
            buildSettingsWarning    = new("Build settings cannot be modified at runtime.");
            loadSceneModeLabel      = new("Load Scene Mode", "The load scene mode used to open and load scenes files.");
            editNamesToggle         = new("Edit Names", "Edit the displayed scene and folder names. Doesn't change file names.");
            editNotesToggle         = new("Edit Notes", "Write notes for each scene and folder.");
            editColorsToggle        = new("Edit Colors", "Choose custom colors for each scene and folder.");
            doAnimationsToggle      = new("Animate UI", "Animate various UI elements opening and closing.");

            regenerateCache         = new("Regenerate Cache", "Regenerates the cache used for the scene loader window. This will reset all names, notes, and color changes.");
            copyGitIgnore           = new("Copy Git Ignore", "Copies a gitignore to ignore scene loader caches.");
            copyGitIgnoreCopied     = new("Git Ignore Copied!");

            reloadSceneButton       = new("Reload Scene", "Reload the active scene.");
            expandFoldersButton     = new("Expand All", "Expands all scene folders.");
            collapseFoldersButton   = new("Collapse All", "Collapses all scene folders");

            openAllScenesButton     = new("Open All", "Open all scene files.");
            closeAllScenesButton    = new("Close All", "Close all open scene files.");
            loadAllScenesButton     = new("Load All", "Load all build setting scenes");
            unloadAllScenesButton   = new("Unload All", "Unload all scenes.");

            selectAllScenesButton   = new("Select All", "Select all scene assets in project window.");
            addAllScenesButton      = new("Add All", "Add all scene assets to build settings.");
            removeAllScenesButton   = new("Remove All", "Remove all scene assets from build settings.");

            openSceneButton         = new("Open", "Open scene file.");
            closeSceneButton        = new("Close", "Close scene file.");
            loadSceneButton         = new("Load", "Load scene file.");
            unloadSceneButton       = new("Unload", "Unload scene file.");
            selectSceneButton       = new("Select", "Select scene asset in project window.");
            addSceneButton          = new("Add", "Add scene asset to build settings.");
            removeSceneButton       = new("Remove", "Remove scene asset from build settings.");
        }

        /* Stupid that I have to do this, but apparently the scene manager
         * caches the build settings in its enirety at runtime. Most of the
         * time it doesn't matter, but if a user removes/adds or disables/enables
         * any scenes in the build settings, the build settings become out of
         * sync with what the scene manager is using internally.
         * 
         * Example: A scene in the build settings is disabled in the editor,
         * but a user enables it at runtime. On my end it looks fine to load,
         * but because the scene manager cached the build settings from before
         * play mode, it doesn't know that the scene is enabled now, and will
         * give an error saying it isn't in the build settings.
         * 
         * I'm assuming this is how it works based on experimentation, 
         * although I'm not entirely sure.
         */
        private void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.ExitingEditMode)
            {
                enabledBuildSceneGuids = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.guid)
                    .ToList();
            }
        }

        private List<GUID> enabledBuildSceneGuids;

        private void OnGUI()
        {
            if (!styleAndContentCached)
            {
                CacheStyleAndContent();
            }

            Cache.UpdateAnimBools(this);

            Undo.RecordObject(cache, "Scene Loader Cache Changed");

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(sceneLoadingAtRuntimeHelp.text, MessageType.Info);
            }

            EditorGUILayout.Space();

            SettingsGUI();

            EditorGUILayout.Space();

            var openScenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToList();

            SceneHelperGUI(openScenes);

            EditorGUILayout.Space();

            FolderHelperGUI();

            FolderAndScenesGUI(openScenes);

            AssetDatabase.SaveAssetIfDirty(cache);
        }

        private void SettingsGUI()
        {
            EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox));

            SetAnimBool(showSceneLoaderSettings, EditorGUILayout.Foldout(showSceneLoaderSettings.target, settingsFoldout, true, EditorStyles.foldout));

            if (EditorGUILayout.BeginFadeGroup(showSceneLoaderSettings.faded))
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.Space();

                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = settingsLabelWidth;

                loadSceneMode = (LoadSceneMode)EditorGUILayout.EnumPopup(loadSceneModeLabel, loadSceneMode);
                SetAnimBool(additiveLoading, loadSceneMode == LoadSceneMode.Additive);

                SetAnimBool(selectScenes, EditorGUILayout.Toggle(selectScenesToggle, selectScenes.target));

                SetAnimBool(editBuildSettings, EditorGUILayout.Toggle(showBuildSettingsToggle, editBuildSettings.target));

                if (EditorApplication.isPlaying)
                {
                    if (EditorGUILayout.BeginFadeGroup(editBuildSettings.faded))
                    {
                        EditorGUILayout.HelpBox(buildSettingsWarning.text, MessageType.Warning);
                    }
                    EditorGUILayout.EndFadeGroup();
                }

                SetAnimBool(editNames, EditorGUILayout.Toggle(editNamesToggle, editNames.target));

                SetAnimBool(editNotes, EditorGUILayout.Toggle(editNotesToggle, editNotes.target));

                SetAnimBool(editColors, EditorGUILayout.Toggle(editColorsToggle, editColors.target));

                EditorGUILayout.Space();

                animated = EditorGUILayout.Toggle(doAnimationsToggle, animated);

                EditorGUIUtility.labelWidth = labelWidth;

                EditorGUILayout.Space();

                if (GUI.Button(EditorGUI.IndentedRect(EditorGUILayout.GetControlRect()), regenerateCache))
                {
                    if (EditorUtility.DisplayDialog(regenerateConfirmation.title, regenerateConfirmation.message, regenerateConfirmation.confirm, regenerateConfirmation.cancel))
                    {
                        GenerateCache();
                        Cache.UpdateScenes();
                        Cache.UpdateAnimBools(this);
                    }
                }

                var gitIgnoreContent = EditorGUIUtility.systemCopyBuffer == gitIgnore
                    ? copyGitIgnoreCopied
                    : copyGitIgnore;
                if (GUI.Button(EditorGUI.IndentedRect(EditorGUILayout.GetControlRect()), gitIgnoreContent))
                {
                    EditorGUIUtility.systemCopyBuffer = gitIgnore;
                }

                EditorGUILayout.Space();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.EndVertical();
        }

        private void SceneHelperGUI(List<Scene> openScenes)
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(reloadSceneButton))
            {
                if (EditorApplication.isPlaying)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
                else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(SceneManager.GetActiveScene().path);
                }
            }

            GUILayout.FlexibleSpace();

            // build scene editing

            var allScenes = Cache.folders.SelectMany(folder => folder.scenes).ToArray();

            if (EditorGUILayout.BeginFadeGroup(editBuildSettings.faded))
            {
                EditorGUILayout.BeginVertical();

                GUI.enabled = !allScenes.All(scene => EditorBuildSettings.scenes.Any(editorScene => editorScene.guid == scene.Guid));

                if (GUILayout.Button(addAllScenesButton, buttonStyle))
                {
                    EditorBuildSettings.scenes = allScenes.Select(scene => new EditorBuildSettingsScene(scene.Guid, true)).ToArray();
                }

                GUI.enabled = EditorBuildSettings.scenes.Length != 0;

                if (GUILayout.Button(removeAllScenesButton, buttonStyle))
                {
                    EditorBuildSettings.scenes = null;
                }

                GUI.enabled = false;

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFadeGroup();

            // scene selecting

            if (EditorGUILayout.BeginFadeGroup(selectScenes.faded))
            {
                if (GUILayout.Button(selectAllScenesButton, buttonStyle))
                {
                    Selection.objects = allScenes.Select(scene => scene.asset).ToArray();
                }
            }

            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.BeginVertical();

            // scene loading

            (var openButton, var closeButton) = EditorApplication.isPlaying
                ? (loadAllScenesButton, unloadAllScenesButton)
                : (openAllScenesButton, closeAllScenesButton);

            bool multipleScenesOpen = openScenes.Count > 1;

            if (additiveLoading.target)
            {
                var activeScenePath = SceneManager.GetActiveScene().path;
                var loadableScenesIndeces = EditorBuildSettings.scenes.Where(scene => scene.path != activeScenePath).Select((scene, i) => i).ToArray();

                int loadableSceneCount = EditorApplication.isPlaying
                    ? loadableScenesIndeces.Length
                    : allScenes.Length;

                GUI.enabled = openScenes.Count < loadableSceneCount;

                if (GUILayout.Button(openButton, buttonStyle))
                {
                    if (EditorApplication.isPlaying)
                    {
                        foreach (var sceneIndex in loadableScenesIndeces)
                        {
                            SceneManager.LoadScene(sceneIndex, LoadSceneMode.Additive);
                        }
                    }
                    else
                    {
                        foreach (var scene in allScenes)
                        {
                            EditorSceneManager.OpenScene(scene.Path, OpenSceneMode.Additive);
                        }
                    }
                }

                GUI.enabled = multipleScenesOpen;

                if (GUILayout.Button(closeButton, buttonStyle))
                {
                    CloseAll();
                }
            }

            SetAnimBool(closeAllScenes, loadSceneMode == LoadSceneMode.Single && multipleScenesOpen);

            if (EditorGUILayout.BeginFadeGroup(closeAllScenes.faded))
            {
                if (GUILayout.Button(closeAllScenesButton, buttonStyle))
                {
                    CloseAll();
                }
            }

            EditorGUILayout.EndFadeGroup();

            void CloseAll()
            {
                var activeScene = SceneManager.GetActiveScene();
                var closeableScenes = openScenes.Where(scene => scene != activeScene);

                if (EditorApplication.isPlaying)
                {
                    foreach (var scene in closeableScenes)
                    {
                        SceneManager.UnloadSceneAsync(scene);
                    }
                }
                else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    foreach (var scene in closeableScenes)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            GUI.enabled = true;

            EditorGUILayout.EndVertical();

            if (!(additiveLoading.value || closeAllScenes.value))
            {
                GUILayout.Space(buttonStyle.fixedWidth);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void FolderHelperGUI()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = Cache.folders.Any(folder => !folder.editorFoldoutExpanded.target);
            if (GUILayout.Button(expandFoldersButton))
            {
                foreach (var folder in Cache.folders)
                {
                    SetAnimBool(folder.editorFoldoutExpanded, true);
                }
            }

            GUI.enabled = Cache.folders.Any(folder => folder.editorFoldoutExpanded.target);
            if (GUILayout.Button(collapseFoldersButton))
            {
                foreach (var folder in Cache.folders)
                {
                    SetAnimBool(folder.editorFoldoutExpanded, false);
                }
            }

            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        private void FolderAndScenesGUI(List<Scene> openScenes)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var buildScenes = EditorBuildSettings.scenes.ToList();
            var enabledBuildScenes = buildScenes
                .Where(scene => scene.enabled)
                .Where(scene => enabledBuildSceneGuids?.Any(buildSceneGuid => buildSceneGuid == scene.guid) ?? false)
                .ToList();

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = sceneLabelWidth;

            // draw all folders
            foreach (var folder in Cache.folders)
            {
                EditorGUILayout.BeginVertical(new GUIStyle()
                {
                    normal = new()
                    {
                        background = folder.GetTexture(CalculateColor(folder.color, folderBaseValue)),
                    }
                });

                EditorGUILayout.Space();

                FolderGUI(folder, buildScenes, enabledBuildScenes, openScenes);

                // foldout
                if (EditorGUILayout.BeginFadeGroup(folder.editorFoldoutExpanded.faded))
                {
                    EditorGUI.indentLevel++;

                    // draw all scenes
                    foreach (var scene in folder.scenes)
                    {
                        EditorGUILayout.Space();

                        SceneGUI(scene, buildScenes, enabledBuildScenes, openScenes);
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndFoldoutHeaderGroup();

                EditorGUILayout.EndFadeGroup();

                EditorGUILayout.Space();

                EditorGUILayout.EndVertical();
            }

            EditorGUIUtility.labelWidth = labelWidth;

            if (!EditorApplication.isPlaying)
            {
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            EditorGUILayout.EndScrollView();
        }

        private void FolderGUI(
            SceneLoaderCache.Folder folder,
            List<EditorBuildSettingsScene> buildScenes,
            List<EditorBuildSettingsScene> enabledScenes,
            List<Scene> openScenes)
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();

            SetAnimBool(folder.editorFoldoutExpanded, EditorGUILayout.BeginFoldoutHeaderGroup(folder.editorFoldoutExpanded.target, folder.displayName, EditorStyles.foldoutHeader));

            GUILayout.FlexibleSpace();

            if (EditorGUILayout.BeginFadeGroup(editColors.faded))
            {
                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 0;
                folder.color = EditorGUILayout.ColorField(GUIContent.none, folder.color, true, false, false, GUILayout.MaxWidth(buttonStyle.fixedWidth));
                EditorGUIUtility.labelWidth = labelWidth;
            }

            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.EndHorizontal();

            if (EditorGUILayout.BeginFadeGroup(editNames.faded))
            {
                folder.displayName = EditorGUILayout.TextField(folder.displayName, GUILayout.MaxWidth(float.MaxValue));
            }

            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.EndVertical();

            // add all scenes button
            if (EditorGUILayout.BeginFadeGroup(editBuildSettings.faded))
            {
                EditorGUILayout.BeginVertical();

                var scenesNotInBuild = folder.scenes
                    .Where(scene => !buildScenes
                        .Any(buildScene => buildScene.guid == scene.Guid))
                    .Select(scene => new EditorBuildSettingsScene(scene.Path, true))
                    .ToArray();

                GUI.enabled = !EditorApplication.isPlaying && scenesNotInBuild.Length > 0;

                if (GUILayout.Button(addAllScenesButton, buttonStyle))
                {
                    buildScenes.AddRange(scenesNotInBuild);
                }

                var scenesInBuild = buildScenes
                    .Where(buildScene => folder.scenes.Any(scene => scene.Guid == buildScene.guid))
                    .ToArray();

                GUI.enabled = !EditorApplication.isPlaying && scenesInBuild.Length > 0;

                if (GUILayout.Button(removeAllScenesButton, buttonStyle))
                {
                    buildScenes.RemoveAll(buildScene => folder.scenes.Any(scene => scene.Guid == buildScene.guid));
                }

                GUI.enabled = true;

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFadeGroup();

            // select folder asset
            if (EditorGUILayout.BeginFadeGroup(selectScenes.faded))
            {
                if (GUILayout.Button(selectAllScenesButton, buttonStyle))
                {
                    EditorUtility.FocusProjectWindow();
                    Selection.objects = folder.scenes.Select(scene => scene.asset).ToArray();
                }
            }

            EditorGUILayout.EndFadeGroup();

            // scene opening/closing/loading/unloading

            if (additiveLoading.target)
            {
                var scenePaths = folder.scenes
                    .Select(scene => scene.Path)
                    .ToArray();

                var unopenedScenePaths = scenePaths
                    .Where(path => !openScenes.Any(open => open.path == path))
                    .ToArray();

                var unopenedSceneIndeces = unopenedScenePaths
                    .Select(path => enabledScenes.FindIndex(enabled => enabled.path == path))
                    .Where(index => index != -1)
                    .ToArray();

                var myOpenScenes = openScenes
                    .Where(open => scenePaths.Any(path => path == open.path))
                    .ToArray();

                // if folder contains all open scenes, dont unload active scene
                if (myOpenScenes.Length == openScenes.Count)
                {
                    var activeScene = SceneManager.GetActiveScene();
                    myOpenScenes = myOpenScenes.Where(scene => scene != activeScene).ToArray();
                }

                (var openButton, var closeButton) = EditorApplication.isPlaying
                    ? (loadAllScenesButton, unloadAllScenesButton)
                    : (openAllScenesButton, closeAllScenesButton);

                EditorGUILayout.BeginVertical();

                GUI.enabled = EditorApplication.isPlaying
                    ? unopenedSceneIndeces.Length > 0
                    : unopenedScenePaths.Length > 0;

                if (GUILayout.Button(openButton, buttonStyle))
                {
                    // load all scenes
                    if (EditorApplication.isPlaying)
                    {
                        foreach (var sceneIndex in unopenedSceneIndeces)
                        {
                            SceneManager.LoadScene(sceneIndex, LoadSceneMode.Additive);
                        }
                    }

                    // open all scenes
                    else
                    {
                        foreach (var path in unopenedScenePaths)
                        {
                            EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                        }
                    }
                }

                GUI.enabled = myOpenScenes.Length > 0;

                if (GUILayout.Button(closeButton, buttonStyle))
                {
                    // unload all scenes
                    if (EditorApplication.isPlaying)
                    {
                        foreach (var scene in myOpenScenes)
                        {
                            SceneManager.UnloadSceneAsync(scene);
                        }
                    }

                    // close all scenes
                    else
                    {
                        foreach (var scene in myOpenScenes)
                        {
                            EditorSceneManager.CloseScene(scene, true);
                        }
                    }
                }

                GUI.enabled = true;

                EditorGUILayout.EndVertical();
            }
            else
            {
                GUILayout.Space(buttonStyle.fixedWidth);
            }

            EditorGUILayout.EndHorizontal();

            if (EditorGUILayout.BeginFadeGroup(editNotes.faded))
            {
                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = notesLabelWidth;

                folder.note = EditorGUILayout.TextArea(folder.note, editableNoteStyle);

                EditorGUIUtility.labelWidth = labelWidth;
            }

            EditorGUILayout.EndFadeGroup();

            if (folder.note != "")
            {
                if (EditorGUILayout.BeginFadeGroup(1 - editNotes.faded))
                {
                    EditorGUILayout.LabelField(folder.note, displayingNoteStyle);
                }

                EditorGUILayout.EndFadeGroup();
            }

            EditorGUILayout.EndVertical();
        }

        private void SceneGUI(
            SceneLoaderCache.Scene scene,
            List<EditorBuildSettingsScene> buildScenes,
            List<EditorBuildSettingsScene> enabledScenes,
            List<Scene> openScenes)
        {
            int openSceneIndex = openScenes.FindIndex(loaded => loaded.path == scene.Path);

            bool open = openSceneIndex != -1;
            bool closeable = openScenes.Any(openScene => openScene.path != scene.Path);

            var color = CalculateColor(scene.color, open ? openSceneValue : unopenedSceneValue);

            EditorGUILayout.BeginVertical(new GUIStyle()
            {
                normal = new()
                {
                    background = scene.GetTexture(color),
                },
            });

            EditorGUILayout.BeginHorizontal();

            // name

            EditorGUILayout.BeginVertical();

            if (EditorGUILayout.BeginFadeGroup(editNames.faded))
            {
                scene.displayName = EditorGUILayout.TextField(scene.displayName, GUILayout.MaxWidth(float.MaxValue), GUILayout.MinWidth(buttonStyle.fixedWidth));
            }

            EditorGUILayout.EndFadeGroup();

            if (EditorGUILayout.BeginFadeGroup(1 - editNames.faded))
            {
                EditorGUILayout.LabelField(scene.displayName, EditorStyles.boldLabel, GUILayout.MaxWidth(float.MaxValue));
            }

            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.EndVertical();

            // color

            if (EditorGUILayout.BeginFadeGroup(editColors.faded))
            {
                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 0;
                scene.color = EditorGUILayout.ColorField(GUIContent.none, scene.color, true, false, false, GUILayout.MaxWidth(buttonStyle.fixedWidth + EditorGUIUtility.singleLineHeight));
                EditorGUIUtility.labelWidth = labelWidth;
            }

            EditorGUILayout.EndFadeGroup();

            // add/remove scene from build settings button

            GUID sceneGuid = scene.Guid;
            bool inBuildSettings = buildScenes.Any(buildScene => buildScene.guid == sceneGuid);

            if (EditorGUILayout.BeginFadeGroup(editBuildSettings.faded))
            {
                GUI.enabled = !EditorApplication.isPlaying;

                if (!inBuildSettings && GUILayout.Button(addSceneButton, buttonStyle))
                {
                    buildScenes.Add(new(scene.Path, true));
                }

                if (inBuildSettings && GUILayout.Button(removeSceneButton, buttonStyle))
                {
                    buildScenes.RemoveAll(buildScene => buildScene.guid == sceneGuid);
                }
            }

            EditorGUILayout.EndFadeGroup();

            // select scene asset button

            GUI.enabled = true;

            if (EditorGUILayout.BeginFadeGroup(selectScenes.faded))
            {
                if (GUILayout.Button(selectSceneButton, buttonStyle))
                {
                    EditorUtility.FocusProjectWindow();
                    Selection.objects = null;
                    Selection.activeObject = scene.asset;
                }
            }

            EditorGUILayout.EndFadeGroup();

            // open/close/load/unload scene button

            var openButtonContent = EditorApplication.isPlaying
                ? open ? unloadSceneButton : loadSceneButton
                : open ? closeSceneButton : openSceneButton;

            GUI.enabled = closeable && (!EditorApplication.isPlaying || enabledScenes.Any(enabled => enabled.guid == sceneGuid) || open);

            if (GUILayout.Button(openButtonContent, buttonStyle))
            {
                if (EditorApplication.isPlaying)
                {
                    // load scene
                    if (!open)
                    {
                        SceneManager.LoadScene(scene.asset.name, loadSceneMode);
                    }

                    // unload scene
                    else if (closeable)
                    {
                        SceneManager.UnloadSceneAsync(scene.asset.name);
                    }
                }
                else
                {
                    // close scene
                    if (!open)
                    {
                        EditorSceneManager.OpenScene(scene.Path, (OpenSceneMode)loadSceneMode);
                    }

                    // open scene
                    else if (closeable)
                    {
                        EditorSceneManager.CloseScene(openScenes[openSceneIndex], true);
                    }
                }
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            // edit note

            EditorGUILayout.BeginVertical();

            if (EditorGUILayout.BeginFadeGroup(editNotes.faded))
            {
                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = notesLabelWidth;

                scene.note = EditorGUILayout.TextArea(scene.note, editableNoteStyle);

                EditorGUIUtility.labelWidth = labelWidth;
            }

            EditorGUILayout.EndFadeGroup();

            if (scene.note != "")
            {
                if (EditorGUILayout.BeginFadeGroup(1 - editNotes.faded))
                {
                    EditorGUILayout.LabelField(scene.note, displayingNoteStyle);
                }

                EditorGUILayout.EndFadeGroup();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        private Color CalculateColor(Color tint, float value)
        {
            Color.RGBToHSV(tint, out float tintHue, out float tintSaturation, out float tintValue);

            // clamp saturation
            tintSaturation = Mathf.Min(tintSaturation, maxSaturation);

            tint = Color.HSVToRGB(tintHue, tintSaturation, tintValue);

            // limit dark colors
            float colorIntensity = tintValue * tintSaturation;
            tint = Color.Lerp(Color.white, tint, colorIntensity);

            Color.RGBToHSV(tint, out tintHue, out tintSaturation, out _);
            var color = Color.HSVToRGB(tintHue, tintSaturation, value);

            return color;
        }

        private void SetAnimBool(AnimBool animBool, bool value)
        {
            if (animated)
            {
                animBool.target = value;
            }
            else
            {
                animBool.value = value;
            }
        }
    }
}
