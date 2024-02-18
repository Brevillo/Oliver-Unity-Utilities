using System.Collections.Generic;
using UnityEngine;

namespace OliverBeebe.UnityUtilities.Runtime.Camera {

    /// <summary> Collects and manages camera effects so they can be combined. </summary>
    public class CameraEffectsManager {

        /// <summary> The current offset value of all the effects. </summary>
        public Vector2 Value { get; private set; }

        /// <summary> Add a shake to the effects manager. </summary>
        /// <returns> An active effect which can be used to remove the effect later. </returns>
        public ActiveEffect AddShake(CameraShakeProfile shake)                          => AddEffect(shake.GetActiveEffect());
        /// <summary> Add a bounce in a certain direction to the effects manager. </summary>
        /// <returns> An active effect which can be used to remove the effect later. </returns>
        public ActiveEffect AddBounce(CameraBounceProfile bounce, Vector2 direction)    => AddEffect(bounce.GetActiveEffect(direction));

        /// <summary> Remove a specific instance of an effect. </summary>
        public void RemoveEffect(ActiveEffect effect) {
            effects.Remove(effect);
        }

        /// <summary> Removes all active effects from the manager. </summary>
        public void ClearEffects() => effects.Clear();

        /// <summary> Update the active effects and calculate the current offset.
        /// <para> Should be run once per update. </para></summary>
        /// <returns> The current offset based on all active effecst. </returns>
        public Vector2 Update() {

            Value = Vector2.zero;

            foreach (var effect in effects)
                Value += effect.Evaluate();

            effects.RemoveAll(EffectComplete);

            return Value;
        }

        private readonly List<ActiveEffect> effects = new();

        private ActiveEffect AddEffect(ActiveEffect effect) {
            effects.Add(effect);
            return effect;
        }

        private bool EffectComplete(ActiveEffect effect) => effect.Complete;
    }

    /// <summary> Base class for all camera effects. </summary>
    public abstract class ActiveEffect {

        internal abstract Vector2 Evaluate();

        public abstract bool Complete { get; }
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

        internal ActiveEffect GetActiveEffect() => new ActiveShake(this);

        private class ActiveShake : ActiveEffect {

            public ActiveShake(CameraShakeProfile profile) {
                this.profile = profile;
            }

            private readonly CameraShakeProfile profile;
            private Vector2 prevTargetPosition, targetPosition;
            private float timer, moveTimeRemaining;

            public override bool Complete => timer >= profile.duration;

            internal override Vector2 Evaluate() {

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

        internal ActiveEffect GetActiveEffect(Vector2 direction) => new ActiveBounce(this, direction);

        private class ActiveBounce : ActiveEffect {

            public ActiveBounce(CameraBounceProfile profile, Vector2 direction) {
                this.profile = profile;
                this.direction = direction;
            }

            private readonly CameraBounceProfile profile;
            private readonly Vector2 direction;
            private float timer;

            public override bool Complete => timer >= profile.duration;

            internal override Vector2 Evaluate() {

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

