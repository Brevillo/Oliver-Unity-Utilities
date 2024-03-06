using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OliverBeebe.UnityUtilities.Runtime {


    [CreateAssetMenu(fileName = "New Setting", menuName = "Oliver Utilities/Setting")]
    public class Setting : ScriptableObject {

        [SerializeField] private float defaultValue;
        [Space(15)]
        [SerializeField] private float currentValue;

        [Readonly, SerializeField] private float savedValue;

        private float value {
            get {
                if (!PlayerPrefs.HasKey(name)) PlayerPrefs.SetFloat(name, defaultValue);
                return PlayerPrefs.GetFloat(name);
            }
            set {
                PlayerPrefs.SetFloat(name, value);
                onValueChanged?.Invoke();
            }
        }

        public event System.Action onValueChanged;

        public string Name => name;

        public void SetValue(int   value) => SetValue((float)value);
        public void SetValue(bool  value) => SetValue(value ? 1f : 0f);
        public void SetValue(float value) {
            this.value = value;
            UpdateSerializedValue();
        }

        public float floatValue => value;
        public int   intValue   => (int)value;
        public bool  boolValue  => value != 0;

        public void UpdateSerializedValue  () => savedValue = currentValue = value;
        public void UpdatePlayerPrefsValue () => savedValue = value = currentValue;

        private void OnEnable() {
            UpdateSerializedValue();
        }

        private void OnValidate() {
            UpdatePlayerPrefsValue();
        }

        #region Editor
    #if UNITY_EDITOR

        [CanEditMultipleObjects, CustomEditor(typeof(Setting))]
        private class SettingsEditor : Editor {

            private static bool resetAllSubmenu1, resetAllSubmenu2, resetAllSubmenu3;

            public override void OnInspectorGUI() {

                var settings = new List<Object>(serializedObject.targetObjects).ConvertAll(o => o as Setting);

                //if (GUILayout.Button("Update Current Value"))
                //    setting.UpdateSerializedValue();

                if (GUILayout.Button("Reset Setting to Default Value")) {

                    foreach (var setting in settings) {
                        PlayerPrefs.DeleteKey(setting.name);
                        setting.UpdateSerializedValue();
                    }
                }

                base.OnInspectorGUI();

                if (GUILayout.Button("!  Clear PlayerPrefs (reset all settings) !")) {
                    resetAllSubmenu1 = !resetAllSubmenu1;
                    resetAllSubmenu2 = false;
                    resetAllSubmenu3 = false;
                }

                if (resetAllSubmenu1 && GUILayout.Button("!!  ARE YOU SURE?  !!")) resetAllSubmenu2 = true;
                if (resetAllSubmenu2 && GUILayout.Button("!!!  ARE YOU REALLY SURE?  !!!")) resetAllSubmenu3 = true;

                if (resetAllSubmenu3 && GUILayout.Button("!!!!  CLEAR PLAYERPREFS  !!!!")) {
                    resetAllSubmenu1 = resetAllSubmenu2 = resetAllSubmenu3 = false;
                    PlayerPrefs.DeleteAll();
                    AssetDatabase.FindAssets($"t:{nameof(Setting)}")
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<Object>)
                        .OfType<Setting>()
                        .ToList()
                        .ForEach(setting => setting.UpdateSerializedValue());
                }
            }
        }

    #endif
        #endregion
    }
}
