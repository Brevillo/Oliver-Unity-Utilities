using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace OliverBeebe.UnityUtilities.Runtime.ScriptableVariables
{
    public interface IScriptableVariable
    {

    }

    public class ScriptableVariable<T> : ScriptableObject, IScriptableVariable
    {
        protected const string createAssetMenuPath = "Oliver Utilities/Scriptable Variables/";

        [SerializeField] private T value;

        public event Action<T> ValueChanged;

        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                InvokeValueChanged();
            }
        }

        public void SetValueWithoutNotify(T value)
        {
            this.value = value;
        }

        public void InvokeValueChanged()
        {
            ValueChanged?.Invoke(value);
        }
    }
}
