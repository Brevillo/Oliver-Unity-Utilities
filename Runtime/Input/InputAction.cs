using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem;
using UInputAction = UnityEngine.InputSystem.InputAction;

namespace OliverBeebe.UnityUtilities.Runtime.Input
{
    [Serializable]
    public class InputAction
    {
        [SerializeField] private InputActionReference actionReference;

        public bool Pressed => Action.WasPressedThisFrame() && !forceNew;
        public bool Down => Action.WasPerformedThisFrame();
        public bool Released => Action.WasReleasedThisFrame();

        public bool Enabled => Action.enabled;

        public event Action Performed;
        public event Action Canceled;

        protected UInputAction Action => actionReference.action;

        protected bool forceNew;

        public void ForceNew()
        {
            forceNew = true;
        }

        public void UnforceNew()
        {
            forceNew = false;
        }

        public void Enable(bool enable)
        {
            if (enable) Action.Enable();
            else Action.Disable();
        }

        public virtual void Initialize()
        {
            Action.performed += OnPerformed;
            Action.canceled += OnCanceled;
        }

        protected virtual void OnPerformed(UInputAction.CallbackContext context)
        {
            forceNew = false;

            if (Enabled)
            {
                Performed?.Invoke();
            }
        }

        protected virtual void OnCanceled(UInputAction.CallbackContext context)
        {
            if (Enabled)
            {
                Canceled?.Invoke();
            }
        }
    }

    [Serializable]
    public class InputAction<TValue> : InputAction where TValue : struct
    {
        [Space]
        [Tooltip("If true, resets the value when not pressed.")]
        [SerializeField] private bool impulse = true;
        [Readonly, SerializeField] private TValue value = default;

        public TValue Value => (Enabled && forceNew) || (!Enabled && impulse)
            ? default
            : value;

        public override void Initialize()
        {
            base.Initialize();

            value = default;
        }

        protected override void OnPerformed(UInputAction.CallbackContext context)
        {
            base.OnPerformed(context);

            value = context.ReadValue<TValue>();
        }

        protected override void OnCanceled(UInputAction.CallbackContext context)
        {
            base.OnCanceled(context);

            if (impulse) value = default;
        }
    }
}
