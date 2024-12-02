using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace OliverBeebe.UnityUtilities.Runtime.DebugWindow
{
    [CreateAssetMenu(menuName = "Oliver Utilities/Editor/Debug Window References")]
    public class DebugWindowReferences : ScriptableObject
    {
        public VisualTreeAsset uxml;
        public VisualTreeAsset consoleElement;
        public VisualTreeAsset componentSection;

        public VisualTreeAsset moduleSeparator;

        public VisualTreeAsset defaultModule;

        public PanelSettings panelSettings;
    }
}
