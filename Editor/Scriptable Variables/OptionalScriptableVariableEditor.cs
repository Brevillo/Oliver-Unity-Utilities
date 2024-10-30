using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OliverBeebe.UnityUtilities.Runtime.ScriptableVariables;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using System;

namespace OliverBeebe.UnityUtilities.Editor.ScriptableVariables
{
    [CustomPropertyDrawer(typeof(OptionalScriptableVariable<>), true)]
    public class OptionalScriptableVariableEditor : PropertyDrawer
    {
        private string scriptableVariableType;

        private VisualElement root;
        private Button button;
        private PropertyField propertyField;
        private SerializedProperty property;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            GetScriptableVariableType();

            this.property = property;

            root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexGrow = 1;

            var assetLabel = new Label("Asset");
            assetLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            assetLabel.style.alignSelf = Align.Center;
            assetLabel.style.flexGrow = 0;
            root.Add(assetLabel);

            var asset = new PropertyField(property.FindPropertyRelative("isScriptableObject"), "");
            asset.RegisterValueChangeCallback(OnAssetChanged);
            asset.style.alignSelf = Align.Center;
            root.Add(asset);

            UpdateGUI();

            return root;
        }

        private void OnAssetChanged(SerializedPropertyChangeEvent propertyChange)
        {
            if (propertyChange.changedProperty.boolValue)
            {
                ConvertToValue();
            }

            UpdateGUI();
        }

        private void UpdateGUI()
        {
            bool scriptable = property.FindPropertyRelative("isScriptableObject").boolValue;

            if (propertyField != null && root.Contains(propertyField))
            {
                root.Remove(propertyField);
            }

            string propertyPath = scriptable
                ? "scriptableVariable"
                : "value";
            propertyField = new PropertyField(property.FindPropertyRelative(propertyPath), property.displayName);
            propertyField.RegisterValueChangeCallback(propertyChange => UpdateCreateButton());
            propertyField.style.flexGrow = 1;
            propertyField.style.marginRight = 5;
            root.Insert(0, propertyField);

            propertyField.Bind(property.serializedObject);

            UpdateCreateButton();
        }

        private void UpdateCreateButton()
        {
            if (button != null && root.Contains(button))
            {
                root.Remove(button);
            }

            bool scriptable = property.FindPropertyRelative("isScriptableObject").boolValue;

            if (scriptable && property.FindPropertyRelative("scriptableVariable").objectReferenceValue == null)
            {
                button = new Button(CreateAsset)
                {
                    text = "Create"
                };
                button.style.flexGrow = 0;
                button.focusable = false;

                root.Insert(1, button);
            }
        }

        private void GetScriptableVariableType()
        {
            var valueType = fieldInfo.FieldType.BaseType
                .GetGenericArguments()
                .First();

            scriptableVariableType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).ToArray()
                .Where(type => type.GetInterface(nameof(IScriptableVariable)) != null)
                .Select(type => (type, valueType: type.BaseType.GetGenericArguments().First()))
                .First(variable => variable.valueType == valueType).type.Name;
        }

        private void CreateAsset()
        {
            string name = $"{property.serializedObject.targetObject.name} {property.displayName}";
            string path = EditorUtility.SaveFilePanelInProject("", name, "asset", "");

            if (path == string.Empty)
            {
                return;
            }

            var asset = ScriptableObject.CreateInstance(scriptableVariableType);
            property.FindPropertyRelative("scriptableVariable").objectReferenceValue = asset;

            var setValueWithoutNotify = asset.GetType().GetMethod("SetValueWithoutNotify");
            setValueWithoutNotify.Invoke(asset, new[] { property.FindPropertyRelative("value").boxedValue });

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(asset);

            property.serializedObject.ApplyModifiedProperties();

            UpdateGUI();
        }

        private void ConvertToValue()
        {
            var variableProp = property.FindPropertyRelative("scriptableVariable");

            if (variableProp.objectReferenceValue != null)
            {
                property.FindPropertyRelative("value").boxedValue = variableProp.objectReferenceValue.GetType().GetMethod("get_Value").Invoke(variableProp.objectReferenceValue, null);
            }

            property.serializedObject.ApplyModifiedProperties();

            UpdateGUI();
        }
    }
}
