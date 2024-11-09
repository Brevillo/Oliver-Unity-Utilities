using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace OliverBeebe.UnityUtilities.Runtime.DebugWindow
{
    public class WindowDragger : BaseManipulator
    {
        private int pointerId;
        private bool active;

        private readonly Color selectedColor;
        private readonly Color defaultColor;
        private readonly Action<float> onAdjust;

        private bool Active => active && target.HasPointerCapture(pointerId);

        private void SetColor(Color color)
        {
            target.style.backgroundColor = color;
            target.style.borderRightColor = color;
            target.style.borderLeftColor = color;
            target.style.borderTopColor = color;
            target.style.borderBottomColor = color;
        }

        public WindowDragger(Color selectedColor, Color defaultColor, Action<float> onAdjust)
        {
            pointerId = -1;
            activators.Add(new ManipulatorActivationFilter
            {
                button = MouseButton.LeftMouse,
            });

            this.selectedColor = selectedColor;
            this.defaultColor = defaultColor;

            this.onAdjust = onAdjust;
        }

        protected override void OnPointerEnter(PointerEnterEvent e)
        {
            SetColor(selectedColor);
        }

        protected override void OnPointerLeave(PointerLeaveEvent e)
        {
            if (Active)
            {
                return;
            }

            SetColor(defaultColor);
        }

        protected override void OnPointerDown(PointerDownEvent e)
        {
            if (active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (CanStartManipulation(e))
            {
                pointerId = e.pointerId;
                active = true;

                target.CapturePointer(pointerId);
                e.StopPropagation();
            }
        }

        protected override void OnPointerMove(PointerMoveEvent e)
        {
            if (!Active)
            {
                return;
            }

            onAdjust.Invoke(e.position.x);
            e.StopPropagation();
        }

        protected override void OnPointerUp(PointerUpEvent e)
        {
            if (!Active || !CanStopManipulation(e))
            {
                return;
            }

            active = false;

            SetColor(defaultColor);

            target.ReleaseMouse();
            pointerId = -1;
            e.StopPropagation();
        }
    }
}
