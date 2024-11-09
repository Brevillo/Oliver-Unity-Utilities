using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OliverBeebe.UnityUtilities.Runtime.DebugWindow;
using UnityEditor;

namespace OliverBeebe.UnityUtilities.Editor.DebugWindow
{
    public class EditorDebugWindow : EditorWindow
    {
        [SerializeField] private DebugWindowReferences references;

        [MenuItem("Window/Oliver Utilities/Debug Window")]
        private static void CreateWindow()
        {
            var window = CreateWindow<EditorDebugWindow>();
            window.titleContent = new("Debug");
        }

        private Runtime.DebugWindow.DebugWindow window;

        private void CreateGUI()
        {
            window = new(rootVisualElement, references, new[]
            {
                typeof(ConsoleModule),
                typeof(AttributedModule),
                typeof(SelectionModule),
            });
        }

        private void Update()
        {
            window.Update();
        }
    }
}
