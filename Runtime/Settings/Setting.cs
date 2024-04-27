using UnityEngine;
using System;

namespace OliverBeebe.UnityUtilities.Runtime.Settings {

    public abstract class Setting<TValue> : ScriptableObject
    {
        [SerializeField] private TValue defaultValue;

        public TValue Value
        {
            get => ToValue(PlayerPrefs.GetFloat(name, ToFloat(defaultValue)));

            set
            {
                PlayerPrefs.SetFloat(name, ToFloat(value));
                InvokeValueChanged();
            }
        }

        public void InvokeValueChanged() => ValueChanged?.Invoke(Value);

        public event Action<TValue> ValueChanged;

        protected abstract TValue ToValue(float value);
        protected abstract float ToFloat(TValue value);
    }
}
