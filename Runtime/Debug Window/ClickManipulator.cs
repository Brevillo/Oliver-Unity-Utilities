using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace OliverBeebe.UnityUtilities.Runtime.DebugWindow
{
    public class ClickManipulator : BaseManipulator
    {
        private readonly Action clickAction;

        public ClickManipulator(Action clickAction)
        {
            activators.Add(new()
            {
                button = MouseButton.LeftMouse,
            });

            this.clickAction = clickAction;
        }

        protected override void OnPointerDown(PointerDownEvent pointerEvent)
        {
            clickAction.Invoke();
        }
    }
}
