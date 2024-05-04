/* Made by Oliver Beebe 2023 */
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace OliverBeebe.UnityUtilities.Runtime {

    public interface ISmartCurve {
        /// <summary> Starts the curve's timer. </summary>
        public void Start();
        /// <summary> Stops the curve's timer. </summary>
        public void Stop();
        /// <summary> Clamp the timer to be within 0 and its max duration. </summary>
        public void ClampTimer();
        /// <summary> Specifies whether the curve's timer has finished. </summary>
        public bool Done { get; }
        /// <summary> Constructs a copy of this SmartCurve. </summary>
        public ISmartCurve Copy();
    }

    /// <summary> An animation curve wrapper.
    /// <list type="bullet">
    ///     <item> Internal timer with start/stop methods. </item>
    ///     <item> Serialized fields for time/value scale. </item>
    ///     <item> Evaluate for any order derivative. </item>
    /// </list>
    /// </summary>
    [System.Serializable]
    public class SmartCurve : ISmartCurve {

        public AnimationCurve curve;
        /// <summary> Time to complete the curve will be scaled by this value. </summary>
        public float timeScale = 1;
        /// <summary> The evaluated values of the curve will be scaled by this value. </summary>
        public float valueScale = 1;

        public float timer = 0;

        /// <summary> Constructs a new SmartCurve by copying the contents of the supplied SmartCurve. </summary>
        /// <param name="copy"> The SmartCurve to copy. </param>
        public SmartCurve(SmartCurve copy) {
            curve = new(copy.curve.keys);
            valueScale = copy.valueScale;
            timeScale = copy.timeScale;
            timer = copy.timer;
        }
        /// <summary> Constructs a copy of this SmartCurve. </summary>
        public ISmartCurve Copy() => new SmartCurve(this);

        /// <summary> Increments the timer and evalutes the curve at that time. </summary>
        public float Evaluate() => Evaluate(Time.deltaTime, 0);
        /// <summary> Increments the timer and evalutes the curve at that time. </summary>
        /// <param name="derivative"> The order derivative to evaulate the curve with. 
        /// <para> 1 = velocity, 2 = acceleration, etc. </para> </param>
        public float Evaluate(int derivative) => Evaluate(Time.deltaTime, derivative);
        /// <summary> Increments the timer and evalutes the curve at that time. </summary>
        /// <param name="deltaTime"> The deltaTime to increment the timer by. </param>
        public float Evaluate(float deltaTime) => Evaluate(deltaTime, 0);
        /// <summary> Increments the timer and evalutes the curve at that time. </summary>
        /// <param name="deltaTime"> The deltaTime to increment the timer by. </param>
        /// <param name="derivative"> The order derivative to evaulate the curve with. 
        /// <para> 1 = velocity, 2 = acceleration, etc. </para> </param>
        public float Evaluate(float deltaTime, int derivative) {
            if (curve == null || timeScale == 0) return 0;
            timer += deltaTime / timeScale;
            return (derivative > 0 ? Derivative(derivative) / timeScale : curve.Evaluate(timer)) * valueScale;
        }

        public float EvaluateAt(float time) => curve.Evaluate(time) * valueScale;

        public float CurrentValue => curve.Evaluate(timer) * valueScale;

        /// <summary> Starts the curve's timer. </summary>
        public void Start() => timer = 0;
        /// <summary> Stops the curve's timer. </summary>
        public void Stop() => timer = duration;
        /// <summary> Clamp the timer to be within 0 and its max duration. </summary>
        public void ClampTimer() => timer = Mathf.Clamp(timer, 0, duration);
        /// <summary> Specifies whether the curve's timer has finished. </summary>
        public bool Done => curve == null || curve.keys.Length == 0 || timer > duration;

        private float duration => curve.keys[^1].time;

        private const float delta = 0.000001f;
        /// <summary> Evaluates the curve at the timer, with the specified derivative order. </summary>
        /// <param name="order"> The order derivative to evaulate the curve with. 
        /// <para> 1 = velocity, 2 = acceleration, etc. </para> </param>
        public float Derivative(int order) => Derivative(timer, order);
        /// <summary> Evaluates the curve at the provided time, with the specified derivative order. </summary>
        /// <param name="time"> The time at which to evaulate the curve (unscaled). </param>
        /// <param name="order"> The order derivative to evaulate the curve with. 
        /// <para> 1 = velocity, 2 = acceleration, etc. </para> </param>
        public float Derivative(float time, int order) {

            if (curve == null) return 0;

            if (order < 1) return curve.Evaluate(time);

            float x1 = time - delta,
                  x2 = time + delta,
                  y1, y2;

            if (order - 1 > 0) {
                y1 = Derivative(x1, order - 1);
                y2 = Derivative(x2, order - 1);
            } else {
                y1 = curve.Evaluate(x1);
                y2 = curve.Evaluate(x2);
            }

            return (y2 - y1) / (2f * delta);
        }

        #region Editor
        #if UNITY_EDITOR

        [CustomPropertyDrawer(typeof(SmartCurve))]
        public class SmartCurvePropertyDrawer : PropertyDrawer {

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

                float labelWidth = EditorGUIUtility.labelWidth,
                      spacing = EditorGUIUtility.standardVerticalSpacing,
                      timeLabelWidth = 35,
                      valueLabelWidth = 40,
                      weirdFieldIndent = EditorGUI.indentLevel > 0 ? 10 : 0,
                      fieldWidth = (position.width - (labelWidth + spacing * 4 + timeLabelWidth + valueLabelWidth - weirdFieldIndent * 2)) / 3f;

                EditorGUI.BeginProperty(position, label, property);

                position.width = labelWidth + fieldWidth;
                EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(curve)), label);

                position.x += position.width + spacing - weirdFieldIndent;
                position.width = timeLabelWidth + fieldWidth;
                EditorGUI.PrefixLabel(position, new GUIContent("Time", "Time to complete the curve will be scaled by this value."));

                position.x += timeLabelWidth;
                position.width -= timeLabelWidth;
                EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(timeScale)), GUIContent.none);

                position.x += position.width - weirdFieldIndent + spacing * 2;
                position.width = valueLabelWidth + fieldWidth;
                EditorGUI.PrefixLabel(position, new GUIContent("Value", "The evaluated values of the curve will be scaled by this value."));

                position.x += valueLabelWidth + spacing;
                position.width -= valueLabelWidth;
                EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(valueScale)), GUIContent.none);

                EditorGUI.EndProperty();
            }
        }

        #endif
        #endregion
    }

    [System.Serializable]
    public class SmartCurve2 : SmartCurveDimensional<Vector2> {

        protected override Vector2 Construct(float[] values) => new(values[0], values[1]);
        protected override SmartCurve[] curves {
            get => new[] { x, y };
            set => (x, y) = (value[0], value[1]);
        }

        /// <summary> Constructs a new SmartCurve by copying the contents of the supplied SmartCurve. </summary>
        /// <param name="copy"> The SmartCurve to copy. </param>
        public SmartCurve2(SmartCurveDimensional<Vector2> copy) : base(copy) { }
        /// <summary> Constructs a copy of this SmartCurve. </summary>
        public override ISmartCurve Copy() => new SmartCurve2(this);

        [SerializeField, InspectorName("X"), Tooltip("X component of the smart curve.")] private SmartCurve x;
        [SerializeField, InspectorName("Y"), Tooltip("Y component of the smart curve.")] private SmartCurve y;
    }

    [System.Serializable]
    public class SmartCurve3 : SmartCurveDimensional<Vector3> {

        protected override Vector3 Construct(float[] values) => new(values[0], values[1], values[2]);
        protected override SmartCurve[] curves {
            get => new[] { x, y, z };
            set => (x, y, z) = (value[0], value[1], value[2]);
        }

        /// <summary> Constructs a new SmartCurve by copying the contents of the supplied SmartCurve. </summary>
        /// <param name="copy"> The SmartCurve to copy. </param>
        public SmartCurve3(SmartCurveDimensional<Vector3> copy) : base(copy) { }
        /// <summary> Constructs a copy of this SmartCurve. </summary>
        public override ISmartCurve Copy() => new SmartCurve3(this);

        [SerializeField, InspectorName("X"), Tooltip("X component of the smart curve.")] private SmartCurve x;
        [SerializeField, InspectorName("Y"), Tooltip("Y component of the smart curve.")] private SmartCurve y;
        [SerializeField, InspectorName("Z"), Tooltip("Z component of the smart curve.")] private SmartCurve z;
    }

    [System.Serializable]
    public class SmartCurve4 : SmartCurveDimensional<Vector4> {

        protected override Vector4 Construct(float[] values) => new(values[0], values[1], values[2], values[3]);
        protected override SmartCurve[] curves {
            get => new[] { x, y, z, w };
            set => (x, y, z, w) = (value[0], value[1], value[2], value[3]);
        }

        /// <summary> Constructs a new SmartCurve by copying the contents of the supplied SmartCurve. </summary>
        /// <param name="copy"> The SmartCurve to copy. </param>
        public SmartCurve4(SmartCurveDimensional<Vector4> copy) : base(copy) { }
        /// <summary> Constructs a copy of this SmartCurve. </summary>
        public override ISmartCurve Copy() => new SmartCurve4(this);

        [SerializeField, InspectorName("X"), Tooltip("X component of the smart curve.")] private SmartCurve x;
        [SerializeField, InspectorName("Y"), Tooltip("Y component of the smart curve.")] private SmartCurve y;
        [SerializeField, InspectorName("Z"), Tooltip("Z component of the smart curve.")] private SmartCurve z;
        [SerializeField, InspectorName("W"), Tooltip("W component of the smart curve.")] private SmartCurve w;
    }

    public abstract class SmartCurveDimensional<T> : ISmartCurve where T : struct {

        protected abstract T Construct(float[] values);
        protected abstract SmartCurve[] curves { get; set; }

        private T RunForAll(System.Func<SmartCurve, float> func) {

            var curves = this.curves;
            int dimensions = curves.Length;
            var results = new float[dimensions];

            for (int i = 0; i < dimensions; i++)
                results[i] = func.Invoke(curves[i]);

            return Construct(results);
        }

        /// <summary> Constructs a new SmartCurve by copying the contents of the supplied SmartCurve. </summary>
        /// <param name="copy"> The SmartCurve to copy. </param>
        protected SmartCurveDimensional(SmartCurveDimensional<T> copy) {

            var copyCurves = copy.curves;
            int dimensions = copyCurves.Length;
            var curves = new SmartCurve[dimensions];

            for (int i = 0; i < dimensions; i++)
                curves[i] = new(copyCurves[i]);

            this.curves = curves;
        }
        /// <summary> Constructs a copy of this SmartCurve. </summary>
        public abstract ISmartCurve Copy();

        /// <summary> Increments the timer and evalutes the curve at that time. </summary>
        public T Evaluate() => Evaluate(Time.deltaTime, 0);
        /// <summary> Increments the timer and evalutes the curve at that time. </summary>
        /// <param name="derivative"> The order derivative to evaulate the curve with. 
        /// <para> 1 = velocity, 2 = acceleration, etc. </para> </param>
        public T Evaluate(int derivative) => Evaluate(Time.deltaTime, derivative);
        /// <summary> Increments the timer and evalutes the curve at that time. </summary>
        /// <param name="deltaTime"> The deltaTime to increment the timer by. </param>
        public T Evaluate(float deltaTime) => Evaluate(deltaTime, 0);
        /// <summary> Increments the timer and evalutes the curve at that time. </summary>
        /// <param name="deltaTime"> The deltaTime to increment the timer by. </param>
        /// <param name="derivative"> The order derivative to evaulate the curve with. 
        /// <para> 1 = velocity, 2 = acceleration, etc. </para> </param>
        public T Evaluate(float deltaTime, int derivative) => RunForAll(curve => curve.Evaluate(deltaTime, derivative));

        /// <summary> Starts the curve's timer. </summary>
        public void Start() {
            foreach (var curve in curves) curve.Start();
        }
        /// <summary> Stops the curve's timer. </summary>
        public void Stop() {
            foreach (var curve in curves) curve.Stop();
        }
        /// <summary> Clamp the tmier to be within 0 and its max duration. </summary>
        public void ClampTimer() {
            foreach (var curve in curves) curve.ClampTimer();
        }
        /// <summary> Specifies whether the curve's timer has finished. </summary>
        public bool Done {
            get {
                // only true if every curve is done
                bool done = true;
                foreach (var curve in curves) done &= curve.Done;
                return done;
            }
        }

        /// <summary> Evaluates the curve at the timer, with the specified derivative order. </summary>
        /// <param name="order"> The order derivative to evaulate the curve with. 
        /// <para> 1 = velocity, 2 = acceleration, etc. </para> </param>
        public T Derivative(int order) => RunForAll(curve => curve.Derivative(order));
        /// <summary> Evaluates the curve at the provided time, with the specified derivative order. </summary>
        /// <param name="time"> The time at which to evaulate the curve (unscaled). </param>
        /// <param name="order"> The order derivative to evaulate the curve with. 
        /// <para> 1 = velocity, 2 = acceleration, etc. </para> </param>
        public T Derivative(float time, int order) => RunForAll(curve => curve.Derivative(time, order));
    }
}
