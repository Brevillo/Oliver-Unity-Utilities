using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System;

namespace OliverBeebe.UnityUtilities.Runtime.DebugWindow
{
    [Serializable]
    public class DebugWindow
    {
        private readonly DebugWindowReferences references;
        private readonly VisualElement root;

        private readonly Module[] modules;
        private readonly Separator[] separators;

        private AttributedModule attributed;

        private readonly Setting<float>[] moduleWidthSettings;

        private readonly struct Setting<T>
        {
            public readonly T Value
            {
                get => getValue.Invoke(key, defaultValue);
                set => setValue.Invoke(key, value);
            }

            private readonly string key;
            private readonly T defaultValue;

            private readonly Func<string, T, T> getValue;
            private readonly Action<string, T> setValue;

            public Setting(string key, T defaultValue, Func<string, T, T> getValue, Action<string, T> setValue)
            {
                this.key = key;
                this.defaultValue = defaultValue;

                this.getValue = getValue;
                this.setValue = setValue;
            }

            public static implicit operator T(Setting<T> setting) => setting.Value;

        }

        private static Setting<float> FloatSetting(string key, float defaultValue)
            => new(key, defaultValue, PlayerPrefs.GetFloat, PlayerPrefs.SetFloat);

        private static Setting<int> IntSetting(string key, int defaultValue)
            => new(key, defaultValue, PlayerPrefs.GetInt, PlayerPrefs.SetInt);

        private static Setting<string> StringSetting(string key, string defaultValue)
            => new(key, defaultValue, PlayerPrefs.GetString, PlayerPrefs.SetString);

        private class Separator
        {
            public float percent;
            public Module module;
            public VisualElement element;

            public Separator(float percent, Module module, VisualElement element)
            {
                this.percent = percent;
                this.module = module;
                this.element = element;
            }
        }

        public static void Instantiate(VisualElement rootVisualElement, DebugWindowReferences references, Type[] moduleTypes)
        {
            new DebugWindow(rootVisualElement, references, moduleTypes);
        }

        public DebugWindow(VisualElement root, DebugWindowReferences references, Type[] moduleTypes)
        {
            this.references = references;
            this.root = root;

            var baseModuleType = typeof(Module);
            modules = moduleTypes
                .Where(type => type.IsSubclassOf(typeof(Module)))
                .Select(type => type.GetConstructor(new[] { typeof(DebugWindowReferences) }).Invoke(new object[] { references }) as Module)
                .ToArray();

            attributed = modules.OfType<AttributedModule>().FirstOrDefault();

            separators = new Separator[moduleTypes.Length - 1];

            moduleWidthSettings = new Setting<float>[moduleTypes.Length];

            CreateGUI();
        }

        public void Update()
        {
            foreach (var module in modules)
            {
                module.Update();
            }
        }

        private void CreateGUI()
        {
            references.uxml.CloneTree(root);

            root.Q<Button>("Refresh").clicked += OnRefreshClicked;

            var modulesRoot = root.Q("Modules");
            modulesRoot.Clear();
            float widthPercent = 1f / modules.Length * 100;
            var moduleWidth = new Length(widthPercent, LengthUnit.Percent);

            for (int i = 0; i < modules.Length; i++)
            {
                var module = modules[i];

                if (i != 0)
                {
                    references.moduleSeparator.CloneTree(modulesRoot, out int separatorFirstElementIndex, out _);

                    var separatorElement = modulesRoot.ElementAt(separatorFirstElementIndex);
                    var separator = new Separator(i * widthPercent, module, separatorElement);
                    int separatorIndex = i - 1;

                    separatorElement.AddManipulator(new WindowDragger(Color.white, Color.HSVToRGB(0, 0, 0.1f), value => AdjustSeparators(separatorIndex, value)));

                    separators[separatorIndex] = separator;
                }

                moduleWidthSettings[i] = FloatSetting($"DebugWindowModuleWidth{i}", widthPercent);

                module.CreateGUI(modulesRoot);
                module.Root.style.width = new Length(moduleWidthSettings[i], LengthUnit.Percent);
            }
        }

        private void AdjustSeparators(int index, float value)
        {
            float prev = index == 0 ? 0 : separators[index - 1].percent;
            float next = index + 1 == separators.Length ? 100 : separators[index + 1].percent;
            float percent = root.WorldToLocal(new Vector2(value, 0)).x / root.layout.width * 100;

            separators[index].percent = Mathf.Clamp(percent, prev, next);

            RecalculateModulePositions();
        }

        private void RecalculateModulePositions()
        {
            float windowWidth = root.layout.width;

            var activeSeparators = separators.Where(separator => separator.element.visible).ToArray();
            var activeModules = modules.Where(module => module.Root.visible).ToArray();

            int activeSeparatorCount = activeSeparators.Length;
            for (int i = 0; i < activeSeparatorCount; i++)
            {
                float prev = i == 0 ? 0 : activeSeparators[i - 1].percent;
                float next = i == activeSeparatorCount - 1 ? 100 : activeSeparators[i + 1].percent;

                var sep = activeSeparators[i];
                sep.percent = Mathf.Clamp(sep.percent, prev, next);
            }

            int activeModuleCount = activeModules.Length;
            for (int i = 0; i < activeModuleCount; i++)
            {
                float prev = i == 0 ? 0 : activeSeparators[i - 1].percent;
                float next = i >= activeSeparators.Length ? 100 : activeSeparators[i].percent;
                float widthPercent = next - prev;
                activeModules[i].Root.style.width = new Length(widthPercent, LengthUnit.Percent);
                moduleWidthSettings[i].Value = widthPercent;
            }
        }

        private void OnRefreshClicked()
        {
            root.Clear();
            CreateGUI();
        }
    }
}
