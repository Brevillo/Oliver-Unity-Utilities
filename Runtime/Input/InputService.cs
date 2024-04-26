using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using OliverBeebe.UnityUtilities.Runtime.GameServices;

namespace OliverBeebe.UnityUtilities.Runtime.Input
{
    public abstract class InputService : GameServiceManager.Service
    {
        [SerializeField] private InputActionAsset inputActions;

        private Button[] buttons;

        protected override void Awake()
        {
            buttons = GetType()
                .GetFields()
                .Select(field => field.GetValue(this))
                .OfType<Button>()
                .ToArray();

            foreach (var button in buttons)
                button.Init();

            inputActions.Enable();
        }

        protected override void Update()
        {
            foreach (var button in buttons)
                button.Update();
        }

        [System.Serializable]
        public class Button
        {
            #region Serialized Fields

            [SerializeField] private InputActionReference actionReference;
            [Space]
            [Readonly, SerializeField] private bool pressed     = false;
            [Readonly, SerializeField] private bool down        = false;
            [Readonly, SerializeField] private bool released    = false;

            #endregion

            #region Public

            public bool Pressed     => pressed;
            public bool Down        => down;
            public bool Released    => released;

            public event System.Action Performed, Canceled;

            public void ForceNew()
            {
                pressed = false;
                forceNew = true;
            }

            public void UnforceNew()
            {
                forceNew = false;
            }

            #endregion

            #region Internals

            public virtual void Init()
            {
                actionReference.action.performed += OnPerformed;
                actionReference.action.canceled  += OnCanceled;
            }

            protected virtual void OnPerformed(InputAction.CallbackContext context)
            {
                pressed = true;
                forceNew = false;
                Performed?.Invoke();
            }

            protected virtual void OnCanceled(InputAction.CallbackContext context)
            {
                pressed = false;
                Canceled?.Invoke();
            }

            private bool pressedLast;
            protected bool forceNew;

            public void Update()
            {
                down        = pressed && !pressedLast;
                released    = !pressed && pressedLast;
                pressedLast = pressed;
            }

            #endregion
        }

        [System.Serializable]
        public class Button<TValue> : Button where TValue : struct
        {
            [Space]
            [Tooltip("If true, resets the value when not pressed.")]
            [SerializeField] private bool impulse = true;
            [Readonly, SerializeField] private TValue value = default;

            public TValue Value => !forceNew ? value : default;

            public override void Init()
            {
                base.Init();
                value = default;
            }

            protected override void OnPerformed(InputAction.CallbackContext context)
            {
                base.OnPerformed(context);
                value = context.ReadValue<TValue>();
            }

            protected override void OnCanceled(InputAction.CallbackContext context)
            {
                base.OnCanceled(context);
                if (impulse) value = default;
            }
        }
    }
}
