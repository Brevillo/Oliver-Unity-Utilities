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

        private const float holdTime = 0.25f;
        private const KeyCode keyCode = KeyCode.BackQuote;

        private UIDocument document;

        private static readonly System.Type[] runtimeModules = new[]
        {
            //typeof(ConsoleModule),
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

            window.window = new(document.rootVisualElement, window.references, runtimeModules);
            I = window;

            Visible = false;
        }

        private float holdTimer;
        private bool holding;

        private DebugWindow window;

        private void Update()
        {
            if (Input.GetKeyDown(keyCode))
            {
                Visible = !Visible;

                holdTimer = 0;
                holding = false;
            }

            holdTimer += Time.deltaTime;

            bool pressing = Input.GetKey(keyCode);

            if (pressing && holdTimer > holdTime)
            {
                holding = true;
            }

            if (!pressing && holding)
            {
                Visible = false;
            }
        }
    }
}
