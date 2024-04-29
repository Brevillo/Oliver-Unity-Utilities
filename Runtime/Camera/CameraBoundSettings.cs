using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OliverBeebe.UnityUtilities.Runtime.Camera
{
    [CreateAssetMenu(menuName = "Oliver Utilities/Editor/Camera Bound Settings")]
    public class CameraBoundSettings : ScriptableObject
    {
        public Vector2 defaultCameraSize = new(16f, 9f);
        public float defaultCameraScale  = 1f;

        [Header("Gizmos")]
        public Vector2 snapTo            = new(1f, 1f);
        public bool alwaysShowBorder     = true;
        public bool showCameraFreeArea   = true;
        public Color defaultColor        = Color.green;
        public float handleSize          = 0.1f;
    }
}
