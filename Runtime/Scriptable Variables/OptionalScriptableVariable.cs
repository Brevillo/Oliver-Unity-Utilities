using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace OliverBeebe.UnityUtilities.Runtime.ScriptableVariables
{
    [Serializable]
    public class OptionalScriptableVariable<T>
    {
        [SerializeField] private T value;
        [SerializeField] private ScriptableVariable<T> scriptableVariable;
        [SerializeField] private bool isScriptableObject;

        public T Value
        {
            get => isScriptableObject ? scriptableVariable.Value : value;
            set
            {
                this.value = value;
                scriptableVariable.Value = value;
            }
        }

        public ScriptableVariable<T> Variable => scriptableVariable;
    }
}
