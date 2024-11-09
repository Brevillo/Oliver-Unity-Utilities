using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace OliverBeebe.UnityUtilities.Runtime.DebugWindow
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event, AllowMultiple = false)]
    public class DebugWindowAttribute : Attribute
    {
        public readonly string category;

        public DebugWindowAttribute(string category)
        {
            this.category = category;
        }

        public DebugWindowAttribute()
        {
            category = string.Empty;
        }
    }
}
