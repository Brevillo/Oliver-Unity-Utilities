using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AnimatedValues;
using UnityEngine.SceneManagement;

namespace OliverBeebe.UnityUtilities.Editor
{
    public class SceneLoaderWindowState : ScriptableObject
    {
        public AnimBool showSceneLoaderSettings   = SceneLoader.DefaultAnimBool;
        public AnimBool selectScenes              = SceneLoader.DefaultAnimBool;
        public AnimBool editBuildSettings         = SceneLoader.DefaultAnimBool;
        public AnimBool editNotes                 = SceneLoader.DefaultAnimBool;
        public AnimBool editColors                = SceneLoader.DefaultAnimBool;
        public AnimBool editNames                 = SceneLoader.DefaultAnimBool;
        public AnimBool additiveLoading           = SceneLoader.DefaultAnimBool;
        public AnimBool closeAllScenes            = SceneLoader.DefaultAnimBool;
        public AnimBool hideFolders               = SceneLoader.DefaultAnimBool;
        public bool animated = true;
        public Vector2 scrollPosition = Vector2.zero;
        public LoadSceneMode loadSceneMode = LoadSceneMode.Single;
        public SceneLoaderCache sceneCache;

        public AnimBool[] GetAnimBools() => new[]
        {
            showSceneLoaderSettings,
            selectScenes,
            editBuildSettings,
            editNotes,
            editColors,
            editNames,
            additiveLoading,
            closeAllScenes,
            hideFolders,
        };
    }
}
