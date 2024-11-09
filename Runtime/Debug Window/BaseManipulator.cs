using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace OliverBeebe.UnityUtilities.Runtime
{
    public class BaseManipulator : PointerManipulator
    {
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<    PointerDownEvent    >(OnPointerDown );
            target.RegisterCallback<    PointerMoveEvent    >(OnPointerMove );
            target.RegisterCallback<    PointerUpEvent      >(OnPointerUp   );
            target.RegisterCallback<    PointerEnterEvent   >(OnPointerEnter);
            target.RegisterCallback<    PointerLeaveEvent   >(OnPointerLeave);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<  PointerDownEvent    >(OnPointerDown );
            target.UnregisterCallback<  PointerMoveEvent    >(OnPointerMove );
            target.UnregisterCallback<  PointerUpEvent      >(OnPointerUp   );
            target.UnregisterCallback<  PointerEnterEvent   >(OnPointerEnter);
            target.UnregisterCallback<  PointerLeaveEvent   >(OnPointerLeave);
        }

        protected virtual void OnPointerEnter   (PointerEnterEvent  pointerEvent) { }
        protected virtual void OnPointerLeave   (PointerLeaveEvent  pointerEvent) { }
        protected virtual void OnPointerDown    (PointerDownEvent   pointerEvent) { }
        protected virtual void OnPointerMove    (PointerMoveEvent   pointerEvent) { }
        protected virtual void OnPointerUp      (PointerUpEvent     pointerEvent) { }
    }
}
