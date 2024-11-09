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

        public static bool Visible => I != null && I.visible;

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

        [RuntimeInitializeOnLoadMethod]
        private static void SpawnRuntimeDebugWindow()
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

            void SetVisible(bool visible)
            {
                document.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                this.visible = visible;
            }

            bool pressed = Input.GetKeyDown(keyCode);

            if (pressed)
            {
                SetVisible(true);
                doublePressed = false;
            }
            else if (Input.GetKeyUp(keyCode) && !doublePressed)
            {
                SetVisible(false);
            }

            if (pressed && pressTimer < doublePressTime)
            {
                SetVisible(!doublePressed);
                pressTimer = Mathf.Infinity;
                doublePressed = true;
            }

            pressTimer = pressed
                ? 0
                : pressTimer + Time.deltaTime;
        }
    }
}
