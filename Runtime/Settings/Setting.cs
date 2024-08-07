using UnityEngine;
using System;
using UnityEngine.Events;

namespace OliverBeebe.UnityUtilities.Runtime.Settings
{
    public abstract class Setting<TValue> : ScriptableObject
    {
        [SerializeField] private TValue defaultValue;
        [SerializeField] private UnityEvent<TValue> valueChanged;

        public event Action<TValue> ValueChanged;

        public TValue DefaultValue => defaultValue;

        public TValue Value
        {
            get => ToValue(PlayerPrefs.GetFloat(name, ToFloat(defaultValue)));

            set
            {
                SetValueWithoutNotify(value);
                InvokeValueChanged();
            }
        }

        public void SetValueWithoutNotify(TValue value)
        {
            PlayerPrefs.SetFloat(name, ToFloat(value));
        }

        public void InvokeValueChanged()
        {
            var value = Value;

            ValueChanged?.Invoke(value);
            valueChanged.Invoke(value);
        }

        public void ResetToDefault()
        {
            PlayerPrefs.DeleteKey(name);

            InvokeValueChanged();
        }

        protected abstract TValue ToValue(float value);
        protected abstract float ToFloat(TValue value);

        private void OnValidate()
        {
            InvokeValueChanged();
        }
    }
}
