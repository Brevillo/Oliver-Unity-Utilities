using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OliverBeebe.UnityUtilities.Runtime.GameServices
{
    [CreateAssetMenu(fileName = GameServiceManagerResourceName, menuName = "Oliver Utilities/" + GameServiceManagerResourceName)]
    public class GameServiceManager : ScriptableObject
    {
        [SerializeField] private List<Service> services;

        private const string GameServiceManagerResourceName = "Game Service Manager";

        private static Instance instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static Instance SpawnInstance()
        {
            instance = new GameObject($"{GameServiceManagerResourceName} Instance").AddComponent<Instance>();
            DontDestroyOnLoad(instance);

            // initialize services
            var manager = Resources.Load<GameServiceManager>(GameServiceManagerResourceName);

            if (manager == null)
            {
                Debug.LogWarning("No GameServiceManager was found in a resources folder!");
                return null;
            }

            foreach (var service in manager.services)
                service.InitializeService();

            return instance;
        }

        private class Instance : MonoBehaviour
        {
            public event Action OnStart;
            public event Action OnUpdate;
            public event Action OnLateUpdate;
            public event Action OnFixedUpdate;
            public event Action<UnityEngine.SceneManagement.Scene, UnityEngine.SceneManagement.Scene> OnSceneChange;
            public event Action OnOnApplicationQuit;

            private void Awake              () => SceneManager.activeSceneChanged += (from, to) => OnSceneChange?.Invoke(from, to);
            private void Start              () => OnStart               ?.Invoke();
            private void Update             () => OnUpdate              ?.Invoke();
            private void LateUpdate         () => OnLateUpdate          ?.Invoke();
            private void FixedUpdate        () => OnFixedUpdate         ?.Invoke();
            private void OnApplicationQuit  () => OnOnApplicationQuit   ?.Invoke();
        }

        public abstract class Service : ScriptableObject
        {
            public void InitializeService()
            {
                instance.OnStart                += Start;
                instance.OnUpdate               += Update;
                instance.OnLateUpdate           += LateUpdate;
                instance.OnFixedUpdate          += FixedUpdate;
                instance.OnSceneChange          += OnSceneChange;
                instance.OnOnApplicationQuit    += OnApplicationQuit;

                Initialize();
            }

            protected static MonoBehaviour Instance => instance != null ? instance : SpawnInstance();

            protected virtual void Initialize       () { }
            protected virtual void Start            () { }
            protected virtual void Update           () { }
            protected virtual void LateUpdate       () { }
            protected virtual void FixedUpdate      () { }
            protected virtual void OnSceneChange    (UnityEngine.SceneManagement.Scene from, UnityEngine.SceneManagement.Scene to) { }
            protected virtual void OnApplicationQuit() { }

            #region Editor
            #if UNITY_EDITOR

            // displayed in editor for easy access
            [SerializeField, HideInInspector] private GameServiceManager gameServiceManager;

            [CustomEditor(typeof(Service), true)]
            protected class ServiceEditor : Editor
            {
                private const string
                    NoManagersError       = "No Game Service Managers Found!",
                    MultipleManagersError = "Multiple Game Service Managers Found!",
                    AddToManagerButton    = "Add to Game Service Manager";

                private Service Service => target as Service;

                private GameServiceManager FindManager(out string errorMessage)
                {
                    var managerGUIDs = AssetDatabase.FindAssets($"t:{nameof(GameServiceManager)}");

                    errorMessage = "";

                    // already has manager
                    if (Service.gameServiceManager != null)
                    {
                        return Service.gameServiceManager;
                    }

                    // no managers found
                    else if (managerGUIDs.Length == 0)
                    {
                        errorMessage = NoManagersError;
                        return null;
                    }

                    // multiple managers found
                    else if (managerGUIDs.Length > 1)
                    {
                        errorMessage = MultipleManagersError;
                        return null;
                    }

                    // find manager asset
                    string managerPath = AssetDatabase.GUIDToAssetPath(managerGUIDs[0]);
                    return AssetDatabase.LoadAssetAtPath<GameServiceManager>(managerPath);
                }

                public override void OnInspectorGUI()
                {
                    base.OnInspectorGUI();

                    EditorGUILayout.Space();

                    var manager = FindManager(out var errorMessage);
                    bool onManager = manager != null && manager.services.Contains(Service);

                    // display connected manager
                    if (onManager)
                    {
                        GUI.enabled = false;
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(gameServiceManager)));
                        GUI.enabled = true;
                    }

                    // error message
                    else if (errorMessage != "")
                    {
                        EditorGUILayout.HelpBox(errorMessage, MessageType.Error, true);
                    }

                    // prompt to add service to manager
                    else if (!onManager && GUILayout.Button(AddToManagerButton))
                    {
                        manager.services.Add(Service);
                        EditorUtility.SetDirty(manager);

                        Service.gameServiceManager = manager;
                        EditorUtility.SetDirty(Service);

                        AssetDatabase.SaveAssets();
                    }
                }
            }

            #endif
            #endregion
        }
    }
}
