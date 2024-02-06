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

        [Tooltip("How the shake moves the camera.")]
        public ShakeType shakeType = ShakeType.MoveToRandomPositions;

        public enum ShakeType {

            [Tooltip("Every frame the camera moves to a new position within the the amplitude radius.")]
            NewPositionWithinRadiusEachFrame,

            [Tooltip("The camera will move between random positions at a specified speed.")]
            MoveToRandomPositions,
        }

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

                Vector2 position = Vector2.zero;

                switch (profile.shakeType) {

                    case ShakeType.NewPositionWithinRadiusEachFrame:

                        // choose random position
                        position = Random.insideUnitCircle * amplitude;

                        break;

                    case ShakeType.MoveToRandomPositions:

                        // choose new target position
                        if (moveTimeRemaining <= 0) {
                            prevTargetPosition = targetPosition;
                            targetPosition = Random.insideUnitCircle * amplitude;
                            moveTimeRemaining = profile.timeBetweenPositions;
                        }

                        // decrease timer and calculate position
                        moveTimeRemaining -= dt;
                        float movePercent = 1 - moveTimeRemaining / profile.timeBetweenPositions;
                        position = Vector2.Lerp(prevTargetPosition, targetPosition, movePercent);

                        break;
                }

                return position;
            }
        }

        #region Editor
        #if UNITY_EDITOR

        [CustomPropertyDrawer(typeof(CameraShakeProfile))]
        private class CameraShakeProfilePropertyDrawer : PropertyDrawer {

            private bool foldoutActive;

            private readonly string[] properties = new[] {
                nameof(amplitude),
                nameof(duration),
                nameof(unscaledTime),
                nameof(intensityCurve),
                nameof(shakeType),
            };

            private const string conditionalProp = nameof(timeBetweenPositions);

            // found this here, search for float kIndentPerLevel: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/EditorGUI.cs
            private const float indentWidth = 15f;

            private CameraShakeProfile GetProfile(SerializedProperty property)
                => fieldInfo.GetValue(property.serializedObject.targetObject) as CameraShakeProfile;

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

                position.height = EditorGUIUtility.singleLineHeight;
                foldoutActive = EditorGUI.Foldout(new(position) { xMin = position.xMin - indentWidth}, foldoutActive, label, true);

                if (foldoutActive) {

                    EditorGUI.indentLevel++;

                    void Property(string name) {
                        position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        EditorGUI.PropertyField(position, property.FindPropertyRelative(name));
                    }

                    foreach (var prop in properties)
                        Property(prop);

                    if (GetProfile(property).shakeType == ShakeType.MoveToRandomPositions)
                        Property(conditionalProp);

                    EditorGUI.indentLevel--;
                }
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {

                int lines = foldoutActive
                    ? 7 + (GetProfile(property).shakeType == ShakeType.MoveToRandomPositions ? 1 : 0)
                    : 1;

                return EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing * (lines - 1);
            }
        }

        #endif
        #endregion
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

