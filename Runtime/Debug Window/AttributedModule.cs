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

        private struct ObjectInfo
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
        }

        public override void Update()
        {
            var newObjects = Object.FindObjectsOfType<Object>(true);

            objectInfos.RemoveAll(objectInfo =>
            {
                if (!newObjects.Any(obj => obj == objectInfo.obj))
                {
                    foreach (var element in objectInfo.elements)
                    {
                        staticMembers.Remove(element);
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

                var attributedMembers = obj.GetType()
                    .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Select(member => (member, attribute: member.GetCustomAttribute<DebugWindowAttribute>()))
                    .Where(member => member.attribute != null)
                    .ToArray();

                VisualElement element = null;
                foreach (var (member, attribute) in attributedMembers)
                {
                    switch (member)
                    {
                        case MethodInfo method:

                            if (method.GetParameters().Length > 0) continue;

                            element = AddMethod(obj, instanceMembers, method);;

                            break;

                        case FieldInfo field:

                            element = AddField(obj, instanceMembers, field, field.IsPublic || field.GetCustomAttribute<SerializeField>() != null);

                            break;
                    }
                }

                if (element != null)
                {
                    objectInfo.elements.Add(element);
                }

                objectInfos.Add(objectInfo);
            }
        }

        protected override VisualElement CreateGUIInternal(VisualElement root)
        {
            root = base.CreateGUIInternal(root);

            staticMembers = new Foldout()
            {
                text = "Static",
            };
            root.Add(staticMembers);

            instanceMembers = new Foldout
            {
                text = "Instance",
            };
            root.Add(instanceMembers);

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
