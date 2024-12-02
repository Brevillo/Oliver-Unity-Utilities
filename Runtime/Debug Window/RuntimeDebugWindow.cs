using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OliverBeebe.UnityUtilities.Runtime.DebugWindow
{
    public class RuntimeDebugWindow : MonoBehaviour
    {
        public const string enabledPref = "Oliver Utilities RuntimeDebugWindow Enabled";

        #if UNITY_EDITOR

        [MenuItem("Window/Oliver Utilities/Toggle Runtime Debug Window")]
        private static void ToggleRuntimeDebugWindow()
        {
            int enabled = 1 - PlayerPrefs.GetInt(enabledPref, 0);
            PlayerPrefs.SetInt(enabledPref, enabled);

            Debug.Log($"Runtime Debug Window is {(enabled == 1 ? "ENABLED" : "DISABLED")}");
        }

        #endif

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
            typeof(ConsoleModule),
            typeof(AttributedModule),

            #if UNITY_EDITOR
            typeof(SelectionModule),
            #endif
        };

        [RuntimeInitializeOnLoadMethod]
        private static void Spawn()
        {
            if (PlayerPrefs.GetInt(enabledPref, 0) == 0)
            {
                return;
            }

            new GameObject("Runtime Debug Window").AddComponent<RuntimeDebugWindow>();
        }

        private float holdTimer;
        private bool holding;

        private DebugWindow window;

        private void Start()
        { 
            document = gameObject.AddComponent<UIDocument>();

            var references = Resources.Load<DebugWindowReferences>("Debug Window References");

            document.panelSettings = references.panelSettings;
            window = new(document.rootVisualElement, references, runtimeModules);

            DontDestroyOnLoad(this);
            I = this;

            Visible = false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(keyCode))
            {
                Visible = !Visible;

                holdTimer = 0;
                holding = false;
            }

            holdTimer += Time.unscaledDeltaTime;

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
