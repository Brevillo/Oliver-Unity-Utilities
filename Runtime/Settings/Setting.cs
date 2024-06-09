﻿using UnityEngine;
using System;

namespace OliverBeebe.UnityUtilities.Runtime.Settings
{
    public interface ISetting
    {
        public void InvokeValueChanged();
        public void ResetToDefault();
    }

    public abstract class Setting<TValue> : ScriptableObject, ISetting
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

        public void ResetToDefault()
        {
            PlayerPrefs.DeleteKey(name);
            InvokeValueChanged();
        }

        public event Action<TValue> ValueChanged;

        protected abstract TValue ToValue(float value);
        protected abstract float ToFloat(TValue value);
    }
}
