/* Made by Oliver Beebe 2024 */
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OliverBeebe.UnityUtilities.Runtime {

    [System.Serializable]
    public struct Scene {

        #if UNITY_EDITOR
        [SerializeField] private SceneAsset editorSceneAsset;
        #endif
        public string name;

        public static implicit operator string(Scene scene) => scene.name;
        public static implicit operator UnityEngine.SceneManagement.Scene(Scene scene) => SceneManager.GetSceneByName(scene.name);

        #if UNITY_EDITOR

        [Tooltip("ONLY USE IN EDITOR SCRIPTS")]
        public readonly SceneAsset GetSceneAsset_EditorOnly() => editorSceneAsset;

        [CustomPropertyDrawer(typeof(Scene))]
        private class SerializeableSceneAssetPropertyDrawer : PropertyDrawer {

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                EditorGUI.BeginProperty(position, label, property);

                var sceneAsset = property.FindPropertyRelative(nameof(editorSceneAsset));
                EditorGUI.PropertyField(position, sceneAsset, label);

                if (sceneAsset.objectReferenceValue is SceneAsset scene && scene != null)
                    property.FindPropertyRelative(nameof(name)).stringValue = scene.name;

                EditorGUI.EndProperty();
            }
        }

        #endif
    }
}
