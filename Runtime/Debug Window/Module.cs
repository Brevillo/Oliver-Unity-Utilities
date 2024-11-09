using UnityEngine.UIElements;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Reflection;
using System;
using Object = UnityEngine.Object;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace OliverBeebe.UnityUtilities.Runtime.DebugWindow
{
    public abstract class Module
    {
        protected readonly DebugWindowReferences references;

        public VisualElement Root => root;
        private VisualElement root;

        public abstract string Name { get; }

        public Module(DebugWindowReferences references)
        {
            this.references = references;
        }

        public void CreateGUI(VisualElement root)
        {
            this.root = CreateGUIInternal(root);
        }

        public virtual void Update()
        {

        }

        protected abstract VisualElement CreateGUIInternal(VisualElement root);

        protected static string SplitCamelCase(string input)
            => Regex.Replace(input, "([A-Z])", " $1", RegexOptions.Compiled).Trim();

        protected static VisualElement AddField(Object context, VisualElement parent, FieldInfo field, bool serialized)
        {
            #if UNITY_EDITOR
            if (serialized)
            {
                var serializedObject = new SerializedObject(context);
                var property = serializedObject.FindProperty(field.Name);
                var fieldElement = new PropertyField(property);
                fieldElement.Bind(serializedObject);

                parent.Add(fieldElement);

                return fieldElement;
            }
            #endif

            return IFieldConstructor.Get(field.FieldType).Setup(field, field.GetValue(context), context, parent);
        }

        protected static VisualElement AddMethod(Object context, VisualElement parent, MethodInfo method)
        {
            bool returnValue = method.GetParameters().Length == 0 && method.ReturnType != typeof(void);

            var fieldType = IFieldConstructor.Get(method.ReturnType);

            VisualElement valueField = default;
            var button = new Button(ClickAction)
            {
                text = SplitCamelCase(method.Name),
            };
            button.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft);

            if (returnValue)
            {
                string label = method.Name;

                valueField = fieldType.ConstructField();

                valueField.SetEnabled(false);
                valueField.style.width = new Length(50, LengthUnit.Percent);
                button.style.alignItems = Align.FlexEnd;
                valueField.visible = false;

                button.Add(valueField);
            }

            void ClickAction()
            {
                var value = method.Invoke(context, null);

                if (returnValue)
                {
                    valueField.visible = true;
                    fieldType.SetValue(value, valueField);
                }
            }

            parent.Add(button);

            return button;
        }

        private interface IFieldConstructor
        {
            public static IFieldConstructor Get(Type type) => constructors.TryGetValue(type, out var constructor)
                ? constructor.Invoke()
                : new FieldConstructor<string, TextField>();

            private static readonly Dictionary<Type, Func<IFieldConstructor>> constructors = new()
            {
                #if UNITY_EDITOR
                { typeof(Object         ), () => new FieldConstructor<float           , FloatField        >() },
                { typeof(AnimationCurve ), () => new FieldConstructor<AnimationCurve  , CurveField        >() },
                { typeof(Gradient       ), () => new FieldConstructor<Gradient        , GradientField     >() },
                { typeof(LayerMask      ), () => new FieldConstructor<int             , LayerMaskField    >() },
                #endif

                { typeof(Bounds         ), () => new FieldConstructor<Bounds          , BoundsField       >() },
                { typeof(BoundsInt      ), () => new FieldConstructor<BoundsInt       , BoundsIntField    >() },
                { typeof(Color          ), () => new FieldConstructor<Color           , ColorField        >() },
                { typeof(float          ), () => new FieldConstructor<float           , FloatField        >() },
                { typeof(Hash128        ), () => new FieldConstructor<Hash128         , Hash128Field      >() },
                { typeof(int            ), () => new FieldConstructor<int             , IntegerField      >() },
                { typeof(long           ), () => new FieldConstructor<long            , LongField         >() },
                { typeof(Rect           ), () => new FieldConstructor<Rect            , RectField         >() },
                { typeof(RectInt        ), () => new FieldConstructor<RectInt         , RectIntField      >() },
                { typeof(bool           ), () => new FieldConstructor<bool            , Toggle            >() },
                { typeof(Vector2        ), () => new FieldConstructor<Vector2         , Vector2Field      >() },
                { typeof(Vector2Int     ), () => new FieldConstructor<Vector2Int      , Vector2IntField   >() },
                { typeof(Vector3        ), () => new FieldConstructor<Vector3         , Vector3Field      >() },
                { typeof(Vector3Int     ), () => new FieldConstructor<Vector3Int      , Vector3IntField   >() },
                { typeof(Vector4        ), () => new FieldConstructor<Vector4         , Vector4Field      >() },
            };

            public VisualElement Setup(FieldInfo field, object value, Object context, VisualElement parent);
            public void SetValue(object value, VisualElement valueField);
            public VisualElement ConstructField();

            private readonly struct FieldConstructor<T, TField> : IFieldConstructor where TField : BaseField<T>, new()
            {
                public VisualElement Setup(FieldInfo field, object value, Object context, VisualElement parent)
                {
                    var fieldElement = new TField
                    {
                        label = SplitCamelCase(field.Name),
                        value = (T)value,
                    };

                    fieldElement.style.flexGrow = 1;
                    fieldElement.RegisterValueChangedCallback(changeEvent => field.SetValue(context, changeEvent.newValue));

                    if (field.IsLiteral)
                    {
                        fieldElement.SetEnabled(false);
                        fieldElement.label = $"[{(field.IsInitOnly ? "READONLY" : "CONSTANT")}] {fieldElement.label}";
                    }

                    parent.Add(fieldElement);

                    return fieldElement;
                }

                public void SetValue(object value, VisualElement valueField)
                {
                    (valueField as TField).value = (T)value;
                }

                public VisualElement ConstructField() => new TField();
            }
        }
    }

    public abstract class DefaultModule : Module
    {
        protected VisualElement content;

        protected DefaultModule(DebugWindowReferences references) : base(references)
        {
        }

        protected override VisualElement CreateGUIInternal(VisualElement root)
        {
            references.defaultModule.CloneTree(root, out int firstElement, out _);
            root = root.ElementAt(firstElement);

            content = root.Q("Content");
            root.Q<Label>("ModuleTitle").text = Name;

            return root;
        }
    }
}
