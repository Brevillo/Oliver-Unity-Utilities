using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System.Linq;
using System;
using Object = UnityEngine.Object;

namespace OliverBeebe.UnityUtilities.Runtime.DebugWindow
{
    public class AttributedModule : DefaultModule
    {
        public override string Name => "Attributed Members";

        private VisualElement staticMembers;
        private VisualElement instanceMembers;

        private readonly List<ObjectInfo> objectInfos;

        private readonly Dictionary<Type, Member[]> cachedReflections;

        private abstract class Member
        {
            public readonly DebugWindowAttribute attribute;

            public Member(DebugWindowAttribute attribute)
                => this.attribute = attribute;

            public abstract VisualElement AddMember(Object context, ObjectInfo objectInfo, VisualElement parent);
        }

        private class Method : Member
        {
            public readonly MethodInfo method;

            public Method(DebugWindowAttribute attribute, MethodInfo method) : base(attribute)
                => this.method = method;

            public override VisualElement AddMember(Object context, ObjectInfo objectInfo, VisualElement parent)
            {
                return AddMethod(context, parent, method);
            }
        }

        private class Field : Member
        {
            public readonly FieldInfo field;
            private readonly bool serialized;

            public Field(DebugWindowAttribute attribute, FieldInfo field) : base(attribute)
            {
                this.field = field;
                serialized = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
            }

            public override VisualElement AddMember(Object context, ObjectInfo objectInfo, VisualElement parent)
            {
                return AddField(context, parent, field, serialized);
            }
        }

        private class ObjectInfo
        {
            public Object obj;
            public List<VisualElement> elements;

            public ObjectInfo(Object obj)
            {
                this.obj = obj;
                elements = new();
            }
        }

        public AttributedModule(DebugWindowReferences references) : base(references)
        {
            objectInfos = new();
            cachedReflections = new();
        }

        public override void Update()
        {
            var newObjects = Object.FindObjectsOfType<MonoBehaviour>(true);

            objectInfos.RemoveAll(objectInfo =>
            {
                if (objectInfo.obj == null || !newObjects.Any(obj => obj == objectInfo.obj))
                {
                    foreach (var element in objectInfo.elements)
                    {
                        instanceMembers.Remove(element);
                    }

                    return true;
                }

                return false;
            });

            foreach (var obj in newObjects)
            {
                if (objectInfos.Exists(objectInfo => objectInfo.obj == obj))
                {
                    continue;
                }

                var objectInfo = new ObjectInfo(obj);
                var type = obj.GetType();

                if (!cachedReflections.TryGetValue(type, out var attributedMembers))
                {
                    attributedMembers = type
                        .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Select(member => (member, attribute: member.GetCustomAttribute<DebugWindowAttribute>()))
                        .Where(member => member.attribute != null)
                        .Where(member => member.member is not MethodInfo method || method.GetParameters().Length == 0)
                        .Select(member => member.member switch
                        {
                            MethodInfo method => new Method(member.attribute, method),
                            FieldInfo field => new Field(member.attribute, field),
                            _ => (Member)null,
                        })
                        .Where(member => member != null)
                        .ToArray();

                    cachedReflections.Add(type, attributedMembers);
                }

                foreach (var member in attributedMembers)
                {
                    var fieldParent = new VisualElement();
                    fieldParent.style.flexDirection = FlexDirection.Row;

                    var label = new Label(obj.name);
                    label.AddToClassList("Label");
                    label.style.width = new Length(25, LengthUnit.Percent);
                    fieldParent.Add(label);

                    objectInfo.elements.Add(fieldParent);
                    instanceMembers.Add(fieldParent);

                    var fieldElement = member.AddMember(obj, objectInfo, fieldParent);
                    fieldElement.style.flexGrow = 1;
                }

                objectInfos.Add(objectInfo);
            }
        }

        protected override VisualElement CreateGUIInternal(VisualElement root)
        {
            root = base.CreateGUIInternal(root);

            var staticMembersFoldout = new Foldout()
            {
                text = "Static",
            };
            staticMembersFoldout.style.flexGrow = 0;

            root.Add(staticMembersFoldout);

            var staticMembersScroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            staticMembersFoldout.Q("unity-content").Add(staticMembersScroll);
            staticMembers = staticMembersScroll.Q("unity-content-container");

            var instanceMembersFoldout = new Foldout
            {
                text = "Instance",
            };
            instanceMembersFoldout.style.flexGrow = 0;

            root.Add(instanceMembersFoldout);

            var instanceMembersScroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            instanceMembersFoldout.Q("unity-content").Add(instanceMembersScroll);
            instanceMembers = instanceMembersScroll.Q("unity-content-container");

            objectInfos.Clear();

            var debugFunctions = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Select(method => (method, attribute: method.GetCustomAttribute<DebugWindowAttribute>()))
                .Where(method => method.attribute != null)
                .ToArray();

            foreach (var (method, attribute) in debugFunctions)
            {
                AddMethod(null, staticMembers, method);
            }

            return root;
        }
    }
}
