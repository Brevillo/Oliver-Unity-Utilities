/* Made by Oliver Beebe 2023 */
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OliverBeebe.UnityUtilities.Runtime {

    /// <summary> Encapsulates the functionality of a buffer and buffer timer. </summary>
    [System.Serializable]
    public class BufferTimer {

        [Tooltip("Can buffer for this many seconds."), SerializeField] private float threshold;

        private float timer = Mathf.Infinity;

        /// <summary> Maxes out the internal timer so the buffer is disabled until triggered again. </summary>
        public void Reset() => timer = Mathf.Infinity;
        /// <summary> Buffers the reset value by the lenth of the threshold, incrementing timer by Time.deltaTime. </summary>
        /// <param name="reset"> Resets buffer if true. </param>
        /// <returns> If the buffer is currently evaluating to true. </returns>
        public bool Buffer(bool reset) => Buffer(reset, Time.deltaTime);
        /// <summary> Buffers the reset value by the lenth of the threshold, incrementing timer by deltaTime. </summary>
        /// <param name="reset"> Resets buffer if true. </param>
        /// <param name="deltaTime"> deltatTime to iterate the buffer timer. Defaults to Time.deltaTime. </param>
        /// <returns> If the buffer is currently evaluating to true. </returns>
        public bool Buffer(bool reset, float deltaTime) => (timer = reset ? 0 : timer + deltaTime) <= threshold;

        public static implicit operator bool(BufferTimer buffer) => buffer.timer <= buffer.threshold;

        #region Editor
        #if UNITY_EDITOR

        [CustomPropertyDrawer(typeof(BufferTimer))]
        private class BufferTimerPropertyDrawer : PropertyDrawer {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                EditorGUI.BeginProperty(position, label, property);
                EditorGUI.PropertyField(new(position.min.x, position.min.y, position.size.x, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative("threshold"), label);
                EditorGUI.EndProperty();
            }
        }

        #endif
        #endregion

    }
}
