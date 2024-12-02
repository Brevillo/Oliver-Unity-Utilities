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
        
        private readonly Dictionary<Type, Member[]> cachedReflections;

        private MonoBehaviour[] previousObjects;
        
        #region Caches

        private abstract class Member
        {
            public readonly DebugWindowAttribute attribute;

            protected Member(DebugWindowAttribute attribute)
                => this.attribute = attribute;

            public abstract VisualElement AddMember(Object context, VisualElement parent);
        }

        private class Method : Member
        {
            private readonly MethodInfo method;

            public Method(DebugWindowAttribute attribute, MethodInfo method) : base(attribute)
                => this.method = method;

            public override VisualElement AddMember(Object context, VisualElement parent)
            {
                return AddMethod(context, parent, method);
            }
        }

        private class Field : Member
        {
            private readonly FieldInfo field;
            private readonly bool serialized;

            public Field(DebugWindowAttribute attribute, FieldInfo field) : base(attribute)
            {
                this.field = field;
                serialized = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
            }

            public override VisualElement AddMember(Object context, VisualElement parent)
            {
                return AddField(context, parent, field, serialized);
            }
        }

        #endregion

        public AttributedModule(DebugWindowReferences references) : base(references)
        {
            cachedReflections = new();
        }

        public override void Update()
        {
            var newObjects = Object.FindObjectsOfType<MonoBehaviour>(true);
            
            bool identical = previousObjects != null && newObjects.All(previousObjects.Contains) && previousObjects.All(newObjects.Contains);
            previousObjects = newObjects;
            
            if (identical)
            {
                return;
            }
            
            instanceMembers.Clear();
            
            Dictionary<GameObject, List<(Object obj, Member[] members)>> objects = new();
            
            foreach (var obj in newObjects)
            {
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

                if (!objects.TryGetValue(obj.gameObject, out var scripts))
                {
                    scripts = new();
                    objects.Add(obj.gameObject, scripts);
                }

                scripts.Add((obj, attributedMembers));
            }

            foreach (var (gameObject, scripts) in objects)
            {
                var allMembers = scripts.SelectMany(script => script.members.Select(member => (script.obj, member))).ToArray();
                
                switch (allMembers.Length)
                {
                    case 1:
                    {
                        var memberRoot = new VisualElement
                        {
                            style =
                            {
                                flexDirection = FlexDirection.Row,
                            },
                        };

                        var label = new Label(gameObject.name)
                        {
                            style =
                            {
                                // width = new Length(25, LengthUnit.Percent),
                            },
                        };
                        label.AddToClassList("Label");
                        memberRoot.Add(label);

                        instanceMembers.Add(memberRoot);

                        AddMember(allMembers[0], memberRoot);
                        
                        break;
                    }
                    
                    case > 1:
                    {
                        var foldout = new Foldout
                        {
                            text = gameObject.name,
                        };

                        instanceMembers.Add(foldout);
                        var scroll = new ScrollView(ScrollViewMode.Vertical);
                        foldout.Q("unity-content").Add(scroll);

                        var membersRoot = scroll.Q("unity-content-container");

                        foreach (var member in allMembers)
                        {
                            AddMember(member, membersRoot);
                        }

                        break;
                    }
                }

                void AddMember((Object obj, Member member) memberInfo, VisualElement fieldParent)
                {
                    var fieldElement = memberInfo.member.AddMember(memberInfo.obj, fieldParent);
                    fieldElement.style.flexGrow = 1;
                }
            }
        }

        protected override VisualElement CreateGUIInternal(VisualElement root)
        {
            root = base.CreateGUIInternal(root);

            var staticMembersFoldout = new Foldout
            {
                text = "Static",
                style =
                {
                    flexGrow = 0,
                },
            };

            root.Add(staticMembersFoldout);

            var staticMembersScroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            staticMembersFoldout.Q("unity-content").Add(staticMembersScroll);
            staticMembers = staticMembersScroll.Q("unity-content-container");

            var instanceMembersFoldout = new Foldout
            {
                text = "Instance",
                style =
                {
                    flexGrow = 0,
                },
            };

            root.Add(instanceMembersFoldout);

            var instanceMembersScroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            instanceMembersFoldout.Q("unity-content").Add(instanceMembersScroll);
            instanceMembers = instanceMembersScroll.Q("unity-content-container");
            
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
