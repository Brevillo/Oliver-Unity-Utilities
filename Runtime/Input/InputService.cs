using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using OliverBeebe.UnityUtilities.Runtime.GameServices;
using UInputAction = UnityEngine.InputSystem.InputAction;

namespace OliverBeebe.UnityUtilities.Runtime.Input
{
    public abstract class InputService : GameServiceManager.Service
    {
        [SerializeField] private InputActionAsset inputActions;

        private InputAction[] buttons;

        private bool usingController;
        public bool UsingController => usingController;

        public void EnableAllInputs(bool enable)
        {
            foreach (var button in buttons)
                button.Enable(enable);
        }

        protected override void Initialize()
        {
            buttons = GetType()
                .GetFields()
                .Select(field => field.GetValue(this))
                .OfType<InputAction>()
                .ToArray();

            foreach (var button in buttons)
                button.Initialize();

            inputActions.Enable();

            InputSystem.onActionChange += (obj, actionChange) => {

                if (actionChange != InputActionChange.ActionPerformed) return;

                var name = (obj as UInputAction).activeControl.device.name;
                usingController = !(name.Equals("Keyboard") || name.Equals("Mouse"));
            };
        }
    }
}
