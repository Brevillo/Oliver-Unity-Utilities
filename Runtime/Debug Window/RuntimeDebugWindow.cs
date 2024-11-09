using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace OliverBeebe.UnityUtilities.Runtime.DebugWindow
{
    public class RuntimeDebugWindow : MonoBehaviour
    {
        [SerializeField] private DebugWindowReferences references;
        [SerializeField] private PanelSettings panelSettings;

        private bool visible;
        private static RuntimeDebugWindow I;

        public static bool Visible
        {
            get => I != null && I.visible;
            private set
            {
                if (I == null) return;
                I.SetVisible(value);
            }
        }

        private void SetVisible(bool visible)
        {
            this.visible = visible;
            document.rootVisualElement.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            if (visible)
            {
                window.Update();
            }
        }

        private const float doublePressTime = 0.25f;
        private const KeyCode keyCode = KeyCode.BackQuote;

        private UIDocument document;

        private static readonly System.Type[] runtimeModules = new[]
        {
            typeof(ConsoleModule),
            typeof(AttributedModule),

            #if UNITY_EDITOR
            typeof(SelectionModule),
            #endif
        };

        public static void Spawn()
        {
            var windowHost = new GameObject("Runtime Debug Window");
            DontDestroyOnLoad(windowHost);

            var window = windowHost.AddComponent<RuntimeDebugWindow>();
            window.enabled = true;

            var document = windowHost.AddComponent<UIDocument>();
            document.panelSettings = window.panelSettings;

            window.document = document;
            document.rootVisualElement.style.display = DisplayStyle.None;

            window.window = new(document.rootVisualElement, window.references, runtimeModules);
            I = window;
        }

        private float pressTimer;
        private bool doublePressed;

        private DebugWindow window;

        private void Update()
        {
            window.Update();

            bool pressed = Input.GetKeyDown(keyCode);

            if (pressed)
            {
                Visible = true;
                doublePressed = false;
            }
            else if (Input.GetKeyUp(keyCode) && !doublePressed)
            {
                Visible = false;
            }

            if (pressed && pressTimer < doublePressTime)
            {
                Visible = !doublePressed;
                pressTimer = Mathf.Infinity;
                doublePressed = true;
            }

            pressTimer = pressed
                ? 0
                : pressTimer + Time.deltaTime;
        }
    }
}
