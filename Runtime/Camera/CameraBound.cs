using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OliverBeebe.UnityUtilities.Runtime.Camera
{
    [DisallowMultipleComponent]
    public abstract class CameraBound : MonoBehaviour
    {
        [SerializeField] private float cameraScale;
        [SerializeField] private Vector2 max;
        [SerializeField] private Vector2 min;
        [Space]
        [SerializeField] private bool overrideColor;
        [SerializeField] private Color color;
        [Space]
        [SerializeField] private CameraBoundSettings settings;

        public Rect Rect
        {
            get => new()
            {
                min = min + (Vector2)transform.position,
                max = max + (Vector2)transform.position,
            };

            private set
            {
                transform.position = new Vector3(value.center.x, value.center.y, transform.position.z);
                min = value.min - (Vector2)transform.position;
                max = value.max - (Vector2)transform.position;
            }
        }

        public Vector2 Clamp(Vector2 position)
        {
            Vector2 bounds = (Rect.size - CameraSize) / 2f,
                    center = Rect.center,
                    min = center - bounds,
                    max = center + bounds;

            return new(
                Mathf.Clamp(position.x, min.x, max.x),
                Mathf.Clamp(position.y, min.y, max.y));
        }

        public Vector2 CameraSize => Settings.defaultCameraSize * cameraScale;

        private CameraBoundSettings Settings
        {
            get
            {
                if (settings != null) return settings;

                settings = ScriptableObject.CreateInstance<CameraBoundSettings>();
                settings.name = "Default Settings";

                return settings;
            }
        }

        #region Editor

        #region Gizmos

        private Color GizmoColor => overrideColor ? color : Settings.defaultColor;

        private void OnDrawGizmos()
        {
            if (Settings.alwaysShowBorder) DrawBorder();
        }

        private void OnDrawGizmosSelected()
        {
            if (!Settings.alwaysShowBorder) DrawBorder();

            if (Settings.showCameraFreeArea)
            {
                Gizmos.color = GizmoColor * new Color(1, 1, 1, 0.15f);
                Gizmos.DrawWireCube(transform.position, Rect.size - Settings.defaultCameraSize * cameraScale);
            }
        }

        private void DrawBorder()
        {
            Gizmos.color = GizmoColor;
            Gizmos.DrawWireCube(transform.position, Rect.size);
        }

        #endregion

        private void Reset()
        {
            cameraScale = Settings.defaultCameraScale;

            max = CameraSize / 2f;
            min = CameraSize / -2f;

            overrideColor = false;
            color = Color.green;
        }

        #if UNITY_EDITOR

        [CustomEditor(typeof(CameraBound), true), CanEditMultipleObjects]
        protected class CameraBoundEditor : Editor
        {
            private const float minCameraSizeMultiple = 0.01f;

            private CameraBound Bounds => target as CameraBound;

            private Vector2 Snap(Vector2 position)
            {
                Vector2 snap = Bounds.Settings.snapTo;

                return new(
                    snap.x == 0 ? position.x : Mathf.Round(position.x / snap.x) * snap.x,
                    snap.y == 0 ? position.y : Mathf.Round(position.y / snap.y) * snap.y);
            }

            public override void OnInspectorGUI()
            {
                void PropertyField(string property)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(property));
                }

                Bounds.cameraScale = Mathf.Max(minCameraSizeMultiple,
                    EditorGUILayout.FloatField(
                        serializedObject.FindProperty(nameof(cameraScale)).displayName, Bounds.cameraScale));

                var rect = Bounds.Rect;

                Vector2 originalSize = rect.size;
                Vector2 size = Vector2.Max(Bounds.CameraSize, Snap(EditorGUILayout.Vector2Field("Bound Size", originalSize)));

                rect.size = size;
                rect.position -= (size - originalSize) / 2f;

                Bounds.Rect = rect;

                PropertyField(nameof(overrideColor));

                if (Bounds.overrideColor)
                {
                    PropertyField(nameof(color));
                }

                PropertyField(nameof(settings));

                serializedObject.ApplyModifiedProperties();
            }

            protected void OnSceneGUI()
            {
                var rect = Bounds.Rect;

                Vector3 worldPos = Bounds.transform.position;
                Vector2 camSize = Bounds.CameraSize;
                float handleSize = Bounds.Settings.handleSize;

                Handles.color = Bounds.GizmoColor;

                Vector2 DrawHandle(Vector2 position, Vector2 minimum, Vector2 maximum, Vector2 anchor)
                {
                    Vector3 position3D = new(position.x, position.y, worldPos.z);

                    position = Handles.FreeMoveHandle(position3D, handleSize * HandleUtility.GetHandleSize(position3D), Vector2.one, Handles.DotHandleCap);

                    Vector2 Clamp(Vector2 position) => Vector2.Max(Vector2.Min(position, maximum), minimum);

                    return Snap(Clamp(position) - anchor) + anchor;
                }

                EditorGUI.BeginChangeCheck();

                // bottom left
                rect.min = DrawHandle(
                    position: rect.min,
                    minimum:  Vector2.negativeInfinity,
                    maximum:  rect.max - camSize,
                    anchor:   rect.max);

                // left
                rect.xMin = DrawHandle(
                    position: new(rect.xMin, rect.center.y),
                    minimum:  new(Mathf.NegativeInfinity, 0),
                    maximum:  new(rect.xMax - camSize.x, 0),
                    anchor:   new(rect.xMax, rect.center.y)).x;

                // top left
                Vector2 topLeft = DrawHandle(
                    position: new(rect.xMin, rect.yMax),
                    minimum:  new(Mathf.NegativeInfinity, rect.yMin + camSize.y),
                    maximum:  new(rect.xMax - camSize.x, Mathf.Infinity),
                    anchor:   new(rect.xMax, rect.yMin));
                (rect.xMin, rect.yMax) = (topLeft.x, topLeft.y);

                // top
                rect.yMax = DrawHandle(
                    position: new(rect.center.x, rect.yMax),
                    minimum:  new(0, rect.yMin + camSize.y),
                    maximum:  new(0, Mathf.Infinity),
                    anchor:   new(rect.center.x, rect.yMin)).y;

                // top right
                rect.max = DrawHandle(
                    position: rect.max,
                    minimum:  rect.min + camSize,
                    maximum:  Vector2.positiveInfinity,
                    anchor:   rect.min);

                // right
                rect.xMax = DrawHandle(
                    position: new(rect.xMax, rect.center.y),
                    minimum:  new(rect.xMin + camSize.x, 0),
                    maximum:  new(Mathf.Infinity, 0),
                    anchor:   new(rect.xMin, rect.center.y)).x;

                // bottom right
                Vector2 bottomRight = DrawHandle(
                    position: new(rect.xMax, rect.yMin),
                    minimum:  new(rect.xMin + camSize.x, Mathf.NegativeInfinity),
                    maximum:  new(Mathf.Infinity, rect.yMax - camSize.y),
                    anchor:   new(rect.xMin, rect.yMax));
                (rect.xMax, rect.yMin) = (bottomRight.x, bottomRight.y);

                // bottom
                rect.yMin = DrawHandle(
                    position: new(rect.center.x, rect.yMin),
                    minimum:  new(0, Mathf.NegativeInfinity),
                    maximum:  new(0, rect.yMax - camSize.y),
                    anchor:   new(rect.center.x, rect.yMax)).y;

                // center
                rect.center = DrawHandle(
                    position: rect.center,
                    minimum:  Vector2.negativeInfinity,
                    maximum:  Vector2.positiveInfinity,
                    anchor:   rect.center);

                if (EditorGUI.EndChangeCheck() || rect != Bounds.Rect)
                {
                    Undo.RecordObjects(new Object[] { Bounds, Bounds.transform }, "Camera Bound position and size adjustment.");
                    Bounds.Rect = rect;
                    EditorUtility.SetDirty(Bounds);
                }
            }
        }

        #endif
        #endregion
    }
}

