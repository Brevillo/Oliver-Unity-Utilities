using System.Collections.Generic;
using UnityEngine;
using System;

namespace OliverBeebe.UnityUtilities.Runtime
{
    /// <summary> Collects and manages camera effects so they can be combined. </summary>
    public class EffectsManager2D
    {
        private Vector2 value;

        /// <summary> The current offset value of all the effects. </summary>
        public Vector2 Value => value;

        /// <summary> Remove a specific instance of an effect. </summary>
        public void RemoveEffect(ActiveEffect effect)
        {
            effects.Remove(effect);
        }

        /// <summary> Removes all active effects from the manager. </summary>
        public void ClearEffects() => effects.Clear();

        /// <summary> Update the active effects and calculate the current offset.
        /// <para> Should be run once per update. </para></summary>
        /// <returns> The current offset based on all active effecst. </returns>
        public Vector2 Update()
        {
            value = Vector2.zero;

            foreach (var effect in effects)
                value += effect.Evaluate();

            effects.RemoveAll(EffectComplete);

            return value;
        }

        private readonly List<ActiveEffect> effects = new();

        public ActiveEffect AddEffect(ActiveEffect effect)
        {
            effects.Add(effect);
            return effect;
        }

        private static bool EffectComplete(ActiveEffect effect) => effect.Complete;
    }

    /// <summary> Base class for all camera effects. </summary>
    public abstract class ActiveEffect
    {
        internal abstract Vector2 Evaluate();

        public abstract bool Complete { get; }
    }

    [Serializable]
    public class ShakeProfile2D
    {
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

        private class ActiveShake : ActiveEffect
        {
            public ActiveShake(ShakeProfile2D profile)
            {
                this.profile = profile;
            }

            private readonly ShakeProfile2D profile;
            private Vector2 prevTargetPosition, targetPosition;

            private float timer, moveTimeRemaining;

            public override bool Complete => timer >= profile.duration;

            internal override Vector2 Evaluate()
            {
                float dt = profile.unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                timer += dt;

                // calculate current amplitude
                float timePercent = profile.duration == 0 ? 0 : timer / profile.duration,
                      intensityPercent = profile.intensityCurve.Evaluate(timePercent),
                      amplitude = intensityPercent * profile.amplitude;

                // choose new target position
                if (moveTimeRemaining <= 0)
                {
                    prevTargetPosition = targetPosition;
                    targetPosition = UnityEngine.Random.insideUnitCircle * amplitude;
                    moveTimeRemaining = profile.timeBetweenPositions;
                }

                // decrease timer and calculate position
                moveTimeRemaining -= dt;
                float movePercent = profile.timeBetweenPositions == 0 ? 1 : 1 - moveTimeRemaining / profile.timeBetweenPositions;

                return Vector2.Lerp(prevTargetPosition, targetPosition, movePercent);
            }
        }
    }

    [Serializable]
    public class BounceProfile2D
    {
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

        private class ActiveBounce : ActiveEffect
        {
            public ActiveBounce(BounceProfile2D profile, Vector2 direction)
            {
                this.profile = profile;
                this.direction = direction;
            }

            private readonly BounceProfile2D profile;
            private readonly Vector2 direction;

            private float timer;

            public override bool Complete => timer >= profile.duration;

            internal override Vector2 Evaluate()
            {

                float dt = profile.unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                timer += dt;

                // calculate current amplitude
                float timePercent = profile.duration == 0 ? 0 : timer / profile.duration,
                      intensityPercent = profile.intensityCurve.Evaluate(timePercent),
                      amplitude = intensityPercent * profile.amplitude;

                return direction * amplitude;
            }
        }
    }

}

