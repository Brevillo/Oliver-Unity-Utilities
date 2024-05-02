using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace OliverBeebe.UnityUtilities.Runtime {

    [CreateAssetMenu(fileName = "New Sound Effect", menuName = "Oliver Utilities/Sound Effect")]
    public class SoundEffect : ScriptableObject {

        #if UNITY_EDITOR
        private static readonly bool warnIfNoClips = false;
        #endif

        [field: SerializeField] public AudioClip[]      Clips           { get; private set; } = new AudioClip[1];
        [field: SerializeField] public AudioMixerGroup  Group           { get; private set; } = null;
        [field: Header("Parameters"), Range(0f, 1f)]
        [field: SerializeField] public float            Volume          { get; private set; } = 0.5f;
        [field: SerializeField] public float            MinPitch        { get; private set; } = 1;
        [field: SerializeField] public float            MaxPitch        { get; private set; } = 1;
        [field: SerializeField] public bool             Sequential      { get; private set; } = false;
        [field: SerializeField] public bool             AvoidOverlap    { get; private set; } = false;
        [field: SerializeField] public bool             Loop            { get; private set; } = false;

        /// <summary> Play the sound effect. </summary>
        public void Play(Component host) => GetInstance(host).Play();
        /// <summary> Play the sound effect. </summary>
        public void Play(GameObject host) => GetInstance(host).Play();

        /// <summary> Stop the sound effect. </summary>
        public void Stop(Component host) => GetInstance(host).Stop();
        /// <summary> Stop the sound effect. </summary>
        public void Stop(GameObject host) => GetInstance(host).Stop();

        /// <summary> Set the volume of the sound effect. Respects the asset's volume value. </summary>
        public void SetVolume(Component host, float volume) => GetInstance(host).customVolume = volume;
        /// <summary> Set the volume of the sound effect. Respects the assets volume value. </summary>
        public void SetVolume(GameObject host, float volume) => GetInstance(host).customVolume = volume;

        /// <summary> Set the pitch of the sound effect. Resepcts the asset's pitch values. </summary>
        public void SetPitch(Component host, float pitch) => GetInstance(host).customPitch = pitch;
        /// <summary> Set the pitch of the sound effect. Resepcts the asset's pitch values. </summary>
        public void SetPitch(GameObject host, float pitch) => GetInstance(host).customPitch = pitch;

        /// <summary> Is the clip playing right now? </summary>
        public bool IsPlaying(Component host) => GetInstance(host).source.isPlaying;
        /// <summary> Is the clip playing right now? </summary>
        public bool IsPlaying(GameObject host) => GetInstance(host).source.isPlaying;

        #region Internals

        private readonly Dictionary<GameObject, Instance> instances = new();

        private Instance GetInstance(Component host) => GetInstance(host.gameObject);
        private Instance GetInstance(GameObject host) {

            if (instances.TryGetValue(host, out var instance)) return instance;

            var newInstance = new Instance(this, host);
            instances.Add(host, newInstance);

            return newInstance;
        }

        private void OnValidate() {
            foreach (var instance in instances.Values) {

                if (instance.source == null) {
                    instances.Clear();
                    return;
                }

                instance.source.volume = Volume;
                instance.source.outputAudioMixerGroup = Group;
                instance.source.loop = Loop;
            }
        }

        private class Instance {

            private readonly SoundEffect asset;
            public readonly AudioSource source;

            private int clipIndex;

            private float _customPitch;
            public float customPitch {
                get => _customPitch;
                set {
                    source.pitch += value - _customPitch;
                    _customPitch = value;
                }
            }

            private float _customVolume = 1;
            public float customVolume {
                get => _customVolume * asset.Volume;
                set => source.volume = asset.Volume * (_customVolume = value); 
            }

            public Instance(SoundEffect asset, GameObject host) {

                this.asset = asset;

                source = host.AddComponent<AudioSource>();

                customVolume = 1;
                source.outputAudioMixerGroup = asset.Group;
                source.loop = asset.Loop;
                source.playOnAwake = false;

                NoClipsWarningCheck();
            }

            public void Play() {

                if (NoClipsWarningCheck()) return;

                // return if overlapping matters
                if (asset.AvoidOverlap && source.isPlaying) return;

                // get clip
                int index = asset.Sequential
                    ? clipIndex++ % asset.Clips.Length
                    : Random.Range(0, asset.Clips.Length);
                var clip = asset.Clips[index];

                if (clip == null) return;

                // play sound
                source.pitch = customPitch + Random.Range(asset.MinPitch, asset.MaxPitch);
                source.volume = customVolume;

                source.clip = clip;
                if (asset.Loop && source.isPlaying) source.Stop();
                source.Play();
            }

            public void Stop() {
                source.Stop();
            }

            private bool NoClipsWarningCheck() {

                if (asset.Clips == null || asset.Clips.Length == 0) {
    #if UNITY_EDITOR
                    if (warnIfNoClips) Debug.LogError($"<b>{asset.name} Sound Effect</b> is missing an audio clip!", asset);
    #endif
                    return true;
                }

    #if UNITY_EDITOR
                if (warnIfNoClips) {

                    List<int> nullIndeces = new();
                    for (int i = 0; i < asset.Clips.Length; i++)
                        if (asset.Clips[i] == null)
                            nullIndeces.Add(i);

                    if (nullIndeces.Count > 0) {
                        Debug.LogError($"<b>{asset.name} Sound Effect</b> is missing {(nullIndeces.Count == 1 ? $"Clip {nullIndeces[0]}!" : $"Clips {string.Join(", ", nullIndeces)}!")}", asset);
                        return true;
                    }
                }
    #endif

                return false;
            }
        }
        #endregion
    }
}
