using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OliverBeebe.UnityUtilities.Runtime.Camera {

    /// <summary> Collects and manages camera effects so they can be combined. </summary>
    public class CameraEffectsManager {

        private readonly List<ActiveEffect> effects = new();

        /// <summary> The current offset value of all the effects. </summary>
        public Vector2 Value { get; private set; }

        /// <summary> Add a shake to the effects manager. </summary>
        public void AddShake(CameraShakeProfile shake)                          => effects.Add(shake.GetActiveEffect());
        /// <summary> Add a bounce in a certain direction to the effects manager. </summary>
        public void AddBounce(CameraBounceProfile bounce, Vector2 direction)    => effects.Add(bounce.GetActiveEffect(direction));

        /// <summary> Removes all active effects from the manager. </summary>
        public void ClearEffects() => effects.Clear();

        /// <summary> Update the active effects and calculate the current offset.
        /// <para> Should be run once per update. </para></summary>
        /// <returns> The current offset based on all active effecst. </returns>
        public Vector2 Update() {

            Value = Vector2.zero;

            foreach (var effect in effects)
                Value += effect.Evaluate();

            effects.RemoveAll(ActiveEffect.IsCompleted);

            return Value;
        }
    }

    public abstract class ActiveEffect {

        public abstract Vector2 Evaluate();
        protected abstract bool Complete { get; }

        public static bool IsCompleted(ActiveEffect effect) => effect.Complete;
    }

    [System.Serializable]
    public class CameraShakeProfile {

        #region Parameters

        [Tooltip("How large the shake will be.")]
        public float amplitude = 0.25f;

        [Tooltip("How long the shake will be.")]
        public float duration = 0.15f;

        [Tooltip("Is the shake affected by time scale?")]
        public bool unscaledTime = false;

        [Tooltip("The percent intensity of the shake over the duration.")]
        public AnimationCurve intensityCurve = AnimationCurve.Linear(0, 1, 1, 0);

        [Tooltip("Duration it takes for the camera to move between shake positions.")]
        public float timeBetweenPositions = 0.02f;

        #endregion

        public ActiveEffect GetActiveEffect() => new ActiveShake(this);

        private class ActiveShake : ActiveEffect {

            public ActiveShake(CameraShakeProfile profile) {
                this.profile = profile;
            }

            private readonly CameraShakeProfile profile;
            private Vector2 prevTargetPosition, targetPosition;
            private float timer, moveTimeRemaining;

            protected override bool Complete => timer >= profile.duration;

            public override Vector2 Evaluate() {

                float dt = profile.unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                timer += dt;

                // calculate current amplitude
                float timePercent      = timer / profile.duration,
                      intensityPercent = profile.intensityCurve.Evaluate(timePercent),
                      amplitude        = intensityPercent * profile.amplitude;

                // choose new target position
                if (moveTimeRemaining <= 0) {
                    prevTargetPosition = targetPosition;
                    targetPosition = Random.insideUnitCircle * amplitude;
                    moveTimeRemaining = profile.timeBetweenPositions;
                }

                // decrease timer and calculate position
                moveTimeRemaining -= dt;
                float movePercent = profile.timeBetweenPositions == 0 ? 1 : 1 - moveTimeRemaining / profile.timeBetweenPositions;

                return Vector2.Lerp(prevTargetPosition, targetPosition, movePercent);
            }
        }
    }

    [System.Serializable]
    public class CameraBounceProfile {

        #region Parameters

        [Tooltip("How large the bounce will be.")]
        public float amplitude = 0.25f;

        [Tooltip("How long the bounce will be.")]
        public float duration = 0.15f;

        [Tooltip("Is the bounce affected by time scale?")]
        public bool unscaledTime = false;

        [Tooltip("The percent intensity of the bounce over the duration.")]
        public AnimationCurve intensityCurve = AnimationCurve.Linear(0, 1, 1, 0);

        #endregion

        public ActiveEffect GetActiveEffect(Vector2 direction) => new ActiveBounce(this, direction);

        private class ActiveBounce : ActiveEffect {

            public ActiveBounce(CameraBounceProfile profile, Vector2 direction) {
                this.profile = profile;
                this.direction = direction;
            }

            private readonly CameraBounceProfile profile;
            private readonly Vector2 direction;
            private float timer;

            protected override bool Complete => timer >= profile.duration;

            public override Vector2 Evaluate() {

                float dt = profile.unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                timer += dt;

                // calculate current amplitude
                float timePercent      = timer / profile.duration,
                      intensityPercent = profile.intensityCurve.Evaluate(timePercent),
                      amplitude        = intensityPercent * profile.amplitude;

                return direction * amplitude;
            }
        }
    }

}

