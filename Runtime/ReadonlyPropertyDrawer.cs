// minorly modified from https://gist.github.com/LotteMakesStuff/c0a3b404524be57574ffa5f8270268ea
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace OliverBeebe.UnityUtilities.Runtime {

    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ReadonlyAttribute))]
    public class ReadonlyPropertyDrawer : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true;
        }
    }
    #endif

    public class ReadonlyAttribute : PropertyAttribute { }
}
