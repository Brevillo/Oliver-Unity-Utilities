using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System;
using System.Linq;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OliverBeebe.UnityUtilities.Runtime.DebugWindow
{
    public class SelectionModule : DefaultModule
    {
        public override string Name => "Object Selection";

        private readonly List<VisualElement> selectionComponentSections;
        private readonly Dictionary<Type, bool> componentFoldoutValues;

        private Label label;

        public SelectionModule(DebugWindowReferences references) : base(references)
        {
            selectionComponentSections = new();
            componentFoldoutValues = new()
            {
                { typeof(Transform), false },
                { typeof(GameObject), false },
            };

            #if UNITY_EDITOR
            Selection.selectionChanged += OnSelectionChange;
            #endif
        }

        protected override VisualElement CreateGUIInternal(VisualElement root)
        {
            root = base.CreateGUIInternal(root);

            label = new Label("Label");
            label.style.marginBottom = label.style.marginTop = label.style.marginLeft = label.style.marginLeft = 5;
            root.Insert(1, label);

            var selectionContent = new VisualElement();
            content.Add(selectionContent);
            content = selectionContent;

            OnSelectionChange();

            return root;
        }

        private void OnSelectionChange()
        {
            #if UNITY_EDITOR

            // clear items
            selectionComponentSections?.ForEach(item => item.parent.Remove(item));
            selectionComponentSections.Clear();

            // GameObject selection
            if (Selection.activeObject is GameObject go)
            {
                label.text = go.name;

                AddObject(go);

                foreach (var component in go.GetComponents<Component>())
                {
                    AddObject(component);
                }
            }

            // ScriptableObject selection
            else if (Selection.activeObject is Object obj)
            {
                label.text = obj.name;

                AddObject(obj);
            }

            else
            {
                label.text = "Select an Object to view reflected and serialized members.";
            }

            #endif
        }

        private static bool Serialized(FieldInfo field) => field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
        private static bool NotSerialized(FieldInfo field) => !field.IsPublic && field.GetCustomAttribute<SerializeField>() == null;
        private static bool ValidMethodSignature(MethodInfo method)
            => method.GetParameters().Length == 0
            && !method.ContainsGenericParameters
            && !method.IsSpecialName;

        private void AddObject(Object context)
        {
            var type = context.GetType();
            var section = references.componentSection.Instantiate();

            content.Add(section);
            selectionComponentSections.Add(section);

            var objectFoldout = section.Q<Foldout>("Label");
            objectFoldout.text = type.Name;

            if (componentFoldoutValues.TryGetValue(type, out bool foldoutActive))
            {
                objectFoldout.value = foldoutActive;
            }

            objectFoldout.RegisterValueChangedCallback(changeEvent => componentFoldoutValues[type] = changeEvent.newValue);

            var parent = section.Q("Content");

            var allMembersBinding
                = BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.Instance
                | BindingFlags.Static;

            // base class

            AddFields("Fields", parent, type.GetFields(allMembersBinding));
            AddMethods("Methods", parent, type.GetMethods(allMembersBinding).Where(ValidMethodSignature).ToArray());

            var spacer = new VisualElement();
            spacer.style.height = 5;
            parent.Add(spacer);

            // inherited

            var members = new List<MemberInfo>();
            for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                members.AddRange(baseType.GetMembers(allMembersBinding));
            }

            var inheritedMembers = new (string name, MemberInfo[] things)[]
            {
                ("Fields", members.OfType<FieldInfo>().ToArray()),
                ("Methods", members.OfType<MethodInfo>().Where(ValidMethodSignature).ToArray()),
            };

            void AddInheritedMembers((string name, MemberInfo[] members) parameters, VisualElement content)
            {
                if (parameters.name == "Fields")
                {
                    AddFields(parameters.name, content, parameters.members.Cast<FieldInfo>().ToArray());
                }
                else if (parameters.name == "Methods")
                {
                    AddMethods(parameters.name, content, parameters.members.Cast<MethodInfo>().ToArray());
                }
            }

            PopulateFoldout("Inherited Members", parent, inheritedMembers, AddInheritedMembers, false);

            void AddFields(string name, VisualElement parent, FieldInfo[] allFields)
            {
                var instanceFields = allFields.Where(field => !field.IsStatic);

                var fields = new (string name, bool serialized, FieldInfo[] fields)[]
                {
                    ("Serialized", true, instanceFields.Where(Serialized).ToArray()),
                    ("Not Serialized", false, instanceFields.Where(NotSerialized).ToArray()),
                    ("Static", false, allFields.Where(field => field.IsStatic).ToArray()),
                }
                    .Where(parameters => parameters.fields.Length != 0)
                    .ToArray();

                void AddFields((string name, bool serialized, FieldInfo[] fields) parameters, VisualElement parent)
                    => PopulateFoldout(parameters.name, parent, parameters.fields, (field, content) => AddField(context, content, field, parameters.serialized), scroll: true);

                PopulateFoldout(name, parent, fields, AddFields, true);
            }

            void AddMethods(string name, VisualElement parent, MethodInfo[] allMethods)
            {
                var methods = new (string name, MethodInfo[] methods)[]
                {
                    ("Instance", allMethods.Where(method => !method.IsStatic).ToArray()),
                    ("Static", allMethods.Where(method => method.IsStatic).ToArray()),
                }
                    .Where(parameters => parameters.methods.Length != 0)
                    .ToArray();

                void AddMethods((string name, MethodInfo[] methods) parameters, VisualElement parent)
                    => PopulateFoldout(parameters.name, parent, parameters.methods, (method, content) => AddMethod(context, content, method), scroll: true);

                PopulateFoldout(name, parent, methods, AddMethods, false);
            }

            static void PopulateFoldout<T>(string name, VisualElement parent, T[] things, Action<T, VisualElement> addAction, bool open = true, bool scroll = false)
            {
                if (things.Length == 0)
                {
                    return;
                }

                var foldout = new Foldout
                {
                    text = name,
                    value = open,
                };

                VisualElement content;

                if (scroll)
                {
                    var scrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
                    foldout.Q("unity-content").Add(scrollView);

                    content = scrollView.Q("unity-content-container");
                }
                else
                {
                    content = foldout.Q("unity-content");
                }

                parent.Add(foldout);


                foreach (var thing in things)
                {
                    try
                    {
                        addAction.Invoke(thing, content);
                    }
                    catch { }
                }
            }
        }
    }
}
