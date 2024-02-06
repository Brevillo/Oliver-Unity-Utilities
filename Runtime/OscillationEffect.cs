/* Made by Oliver Beebe 2023  */
// Visualization https://www.desmos.com/calculator/ay4nuwxxut
using UnityEngine;
using System;
namespace OliverBeebe.UnityUtilities.Runtime {

    /// <summary> Stores an amplitude, frequency, and offset combined with a timer to make easy oscillation effects. </summary>
    [Serializable] public class Wave : Wave<float> {

        /// <summary> Constructs a new Wave by copying the contents of the supplied Wave. </summary>
        /// <param name="copy"> The Wave to copy. </param>
        public Wave(Wave<float> copy) : base(copy) { }

        public Wave() : base() => frequency = 1f;

        protected override float Get(Function function, float time) =>
            function(amplitude, frequency, offset, baseline, time);
    }

    /// <summary> Stores an amplitude, frequency, and offset combined with a timer to make easy 2D oscillation effects. </summary>
    [Serializable] public class Wave2 : Wave<Vector2> {

        /// <summary> Constructs a new Wave by copying the contents of the supplied Wave. </summary>
        /// <param name="copy"> The Wave to copy. </param>
        public Wave2(Wave<Vector2> copy) : base(copy) { }

        public Wave2() : base() => frequency = Vector2.one;

        protected override Vector2 Get(Function function, float time) => new(
            function(amplitude.x, frequency.x, offset.x, baseline.x, time),
            function(amplitude.y, frequency.y, offset.y, baseline.y, time));
    }

    /// <summary> Stores an amplitude, frequency, and offset combined with a timer to make easy 3D oscillation effects. </summary>
    [Serializable] public class Wave3 : Wave<Vector3> {

        /// <summary> Constructs a new Wave by copying the contents of the supplied Wave. </summary>
        /// <param name="copy"> The Wave to copy. </param>
        public Wave3(Wave<Vector3> copy) : base(copy) { }

        public Wave3() : base() => frequency = Vector3.one;

        protected override Vector3 Get(Function function, float time) => new(
            function(amplitude.x, frequency.x, offset.x, baseline.x, time),
            function(amplitude.y, frequency.y, offset.y, baseline.y, time),
            function(amplitude.z, frequency.z, offset.z, baseline.z, time));
    }

    /// <summary> Stores an amplitude, frequency, and offset combined with a timer to make easy 4D oscillation effects. </summary>
    [Serializable] public class Wave4 : Wave<Vector4> {

        /// <summary> Constructs a new Wave by copying the contents of the supplied Wave. </summary>
        /// <param name="copy"> The Wave to copy. </param>
        public Wave4(Wave<Vector4> copy) : base(copy) { }

        public Wave4() : base() => frequency = Vector4.one;

        protected override Vector4 Get(Function function, float time) => new(
            function(amplitude.x, frequency.x, offset.x, baseline.x, time),
            function(amplitude.y, frequency.y, offset.y, baseline.y, time),
            function(amplitude.z, frequency.z, offset.z, baseline.z, time),
            function(amplitude.w, frequency.w, offset.w, baseline.w, time));
    }

    public abstract class Wave<T> where T : struct {

        private const float TwoPI = 6.28318530718f;
        private const float TwoPISqr = 39.4784176044f;

        private static float Position    (float amplitude, float frequency, float offset, float baseline, float time) => baseline + Mathf.Sin(TwoPI * (time / frequency + offset)) * amplitude;
        private static float Velocity    (float amplitude, float frequency, float offset, float baseline, float time) => baseline + Mathf.Cos(TwoPI * (time / frequency + offset)) * amplitude * TwoPI / frequency;
        private static float Acceleration(float amplitude, float frequency, float offset, float baseline, float time) => baseline - Mathf.Sin(TwoPI * (time / frequency + offset)) * amplitude * TwoPISqr / frequency / frequency;

        [SerializeField, Tooltip("Amplitude of the effect")]   public T amplitude;
        [SerializeField, Tooltip("Time to complete a full cycle")]  public T frequency;
        [SerializeField, Tooltip("Time offset normalized to frequency (0 - 1)")] public T offset;
        [SerializeField, Tooltip("Amount to add to the final value.")] public T baseline;

        private float _timer;
        /// <summary> The internal timer of the effect. Can be set using the SetTimer method. </summary>
        public float timer => _timer;

        /// <summary> Constructs a new Wave by copying the contents of the supplied Wave. </summary>
        /// <param name="copy"> The Wave to copy. </param>
        public Wave(Wave<T> copy) {
            amplitude = copy.amplitude;
            frequency = copy.frequency;
            offset    = copy.offset;
        }

        public Wave() { }

        protected delegate float Function(float amplitude, float frequency, float offset, float baseline, float time);
        protected abstract T Get(Function function, float time);

        /// <summary> Current position of the effect based on the timer. </summary>
        public T position     => Get(Position, _timer);
        /// <summary> Current velocity of the effect based on the timer. </summary>
        public T velocity     => Get(Velocity, _timer);
        /// <summary> Current acceleration of the effect based on the timer. </summary>
        public T acceleration => Get(Acceleration, _timer);

        /// <summary> Increments the timer by Time.deltaTime. </summary>
        public void IncrementTimer() => IncrementTimer(Time.deltaTime);
        /// <summary> Increments the timer by deltaTime. </summary>
        /// <param name="deltaTime"> The time by which to inrement the timer. </param>
        public void IncrementTimer(float deltaTime) => _timer += deltaTime;
        /// <summary> Sets the timer to a specific time. </summary>
        /// <param name="time"> The time to set timer to. </param>
        public void SetTimer(float time) => _timer = time;
        /// <summary> Reset the timer to zero. </summary>
        public void Reset() => _timer = 0f;

        /// <summary> Increments the timer by Time.deltaTime and evaluates the effect at that time. </summary>
        public T Evaluate() => Evaluate(Time.deltaTime);
        /// <summary> Increments the timer by deltaTime and evaluates the effect at that time. </summary>
        /// <param name="deltaTime"> The time by which to inrement the timer. </param>
        public T Evaluate(float deltaTime) => EvaluateAt(_timer += deltaTime);
        /// <summary> Evaluates the effect at a specific time. </summary>
        /// <param name="time"> The time at which to evaluate the effect. </param>
        public T EvaluateAt(float time) => Get(Position, time);

        /// <summary> Increments the timer by Time.deltaTime and evaluates the velocity of the effect at that time. </summary>
        public T Velocity() => Velocity(Time.deltaTime);
        /// <summary> Increments the timer by deltaTime and evaluates the velocity of the effect at that time. </summary>
        /// <param name="deltaTime"> The time by which to inrement the timer. </param>
        public T Velocity(float deltaTime) => VelocityAt(_timer += deltaTime);
        /// <summary> Evaluates the velocity of the effect at a specific time. </summary>
        /// <param name="time"> The time at which to evaluate the velocity of the effect. </param>
        public T VelocityAt(float time) => Get(Velocity, time);

        /// <summary> Increments the timer by Time.deltaTime and evaluates the acceleration of the effect at that time. </summary>
        public T Acceleration() => Acceleration(Time.deltaTime);
        /// <summary> Increments the timer by deltaTime and evaluates the acceleration of the effect at that time. </summary>
        /// <param name="deltaTime"> The time by which to inrement the timer. </param>
        public T Acceleration(float deltaTime) => AccelerationAt(_timer += deltaTime);
        /// <summary> Evaluates the acceleration of the effect at a specific time. </summary>
        /// <param name="time"> The time at which to evaluate the acceleration of the effect. </param>
        public T AccelerationAt(float time) => Get(Acceleration, time);

        private class Example : MonoBehaviour {

            // this creates a serialized field for a float oscillation effect
            [SerializeField] private Wave cameraRunBob;
            [SerializeField] private Transform cameraTransform;

            private void Update() {

                // this causes the camera's position to oscillate on the y axis, while respecting other modifications to the camera's position
                cameraTransform.position += cameraRunBob.Velocity() * Time.deltaTime * Vector3.up;
            }
        }
    }
}
