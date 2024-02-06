using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

namespace OliverBeebe.UnityUtilities.Runtime.Input {

    public class InputManager : MonoBehaviour {

        private Button[] buttons;
        private InputActionAsset inputActions;

        public bool UsingController => usingController;

        private bool usingController;

        private void Awake() {

            buttons =
                GetType()
                .GetFields()
                .Select(field => field.GetValue(this))
                .OfType<Button>()
                .ToArray();

            if (buttons.Any(button => button.ActionReference == null)) {
                Debug.LogError($"Missing Input Actions for {GetType().Name} on {name}!");
                return;
            }

            inputActions = buttons[0].ActionReference.asset;

            foreach (var button in buttons)
                button.Init();

            InputSystem.onActionChange += (obj, actionChange) => {

                if (actionChange != InputActionChange.ActionPerformed) return;

                var name = (obj as InputAction).activeControl.device.name;
                usingController = !(name.Equals("Keyboard") || name.Equals("Mouse"));
            };
        }

        private void Update() {

            foreach (var button in buttons)
                button.Update();
        }

        private void OnEnable() {
            inputActions.Enable();
        }

        private void OnDisable() {
            inputActions.Disable();
        }

        [System.Serializable]
        public class Button {

            [SerializeField] private InputActionReference actionReference;

            public bool Enabled = true;
            public bool Pressed     => Enabled && pressed;
            public bool Down        => Enabled && down;
            public bool Released    => Enabled && released;

            public void ForceNew() {
                pressed = false;
                forceNew = true;
            }

            public void UnforceNew() {
                forceNew = false;
            }

            #region Internals

            protected InputAction Action => actionReference.action;
            public InputActionReference ActionReference => actionReference;

            public virtual void Init() {
                Action.performed += context => pressed = true;
                Action.canceled  += context => pressed = false;
            }

            [Readonly, SerializeField] protected bool pressed     = false;
            [Readonly, SerializeField] protected bool down        = false;
            [Readonly, SerializeField] protected bool released    = false;
            protected bool pressedLast = false;

            protected bool forceNew;

            public void Update() {

                down        = pressed && !pressedLast;
                released    = !pressed && pressedLast;
                pressedLast = pressed;

                if (down) forceNew = false;
            }

            #endregion
        }

        [System.Serializable]
        public class Axis : Button {

            public Vector2 Vector => Enabled && !forceNew ? _vector : Vector2.zero;

            public event System.Action<Vector2> OnUpdated;

            #region Internals

            [Tooltip("If true, sets the value to zero when not pressed.")]
            [SerializeField] private bool impulse;

            public override void Init() {
                base.Init();
                Action.performed += context => {
                    _vector = context.ReadValue<Vector2>();
                    OnUpdated?.Invoke(_vector);
                };
                if (impulse) Action.canceled += context => _vector = Vector2.zero;
            }

            [Readonly, SerializeField] private Vector2 _vector;

            #endregion
        }
    }
}