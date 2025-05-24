using System.Collections.Generic;
using System.Linq;
using Singleton.AudioManager;
using UnityEngine;
using UnityEngine.Audio;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#elif NAUGHTY_ATTRIBUTES
using NaughtyAttributes;
#endif

#if UNITY_EDITOR
using System.Diagnostics;
using UnityEditor;

#endif

namespace Audoty
{
    [CreateAssetMenu(fileName = "Audio Player", menuName = "Audio Player", order = 215)]
    public class AudioPlayer : ScriptableObject
    {
#if !ODIN_INSPECTOR && NAUGHTY_ATTRIBUTES
        [ReorderableList]
#endif
        [SerializeField] private List<AudioClip> _clips;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        private AudioMixerGroup _audioMixer;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        private bool _loop;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        [Tooltip("When live link is enabled, changes to the parameter will apply to existing/live audio sources.")]
        private bool _liveLinkLoop = true;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        private bool _saveLoop;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        [Space]
        [Tooltip("When true, only one instance of this AudioPlayer will be played")]
        private bool _singleton;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
        [ShowIf(nameof(_singleton))]
#endif
        [SerializeField]
        [Tooltip(
            "When true, a live singleton audio source will be interrupted to play a new clip (from the same Audio Player)")]
        private bool _allowInterrupt = true;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        private bool _saveSingelton;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [Space]
        [SerializeField]
        [Range(0, 1)]
        private float _volume = 1;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        [Tooltip("When live link is enabled, changes to the parameter will apply to existing/live audio sources.")]
        private bool _liveLinkVolume = true;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        private bool _saveVolume;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [Space]
        [SerializeField]
        private float _minDistance = 1;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        private float _maxDistance = 500;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        [Tooltip("When live link is enabled, changes to the parameter will apply to existing/live audio sources.")]
        private bool _liveLinkDistances = true;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        private bool _saveDistances;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
        [MinMaxSlider(-3, 3)]
#endif
        [Space]
        [SerializeField]
        private Vector2 _pitch = Vector2.one;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        [Tooltip("When live link is enabled, changes to the parameter will apply to existing/live audio sources.")]
        private bool _liveLinkPitch = true;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        private bool _savePitch;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [Space]
        [SerializeField]
        private float _dopplerLevel;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        [Tooltip("When live link is enabled, changes to the parameter will apply to existing/live audio sources.")]
        private bool _liveLinkDopplerLevel = true;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        private bool _saveDopplerLevel;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [Space]
        [SerializeField]
        [Tooltip("Volume fade-in time when AudioPlayer plays an audio")]
        private float _playFadeTime;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        [Tooltip(
            "When AudioPlayer gets interrupted (stopped mid playing), instead of cutting the audio, audio will fade out")]
        private float _interruptFadeTime = 0.2f;

#if ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES
        [BoxGroup("Parameters")]
#endif
        [SerializeField]
        private bool _keepEditorPlayModeChanges;

        [SerializeField, HideInInspector] private int _randomizedSaveKey;

        internal readonly Dictionary<int, AudioSource> _playingSources = new Dictionary<int, AudioSource>();

        private int _nextId;
        private AudioHandle? _singletonHandle;

#if UNITY_EDITOR
        // Keep record of keys to make sure there is no conflict
        private static readonly Dictionary<int, AudioPlayer> SaveKeys = new Dictionary<int, AudioPlayer>();
        private AudioHandle? _lastPlayedAudio;
#endif

        public IReadOnlyList<AudioClip> Clips => _clips;

        public bool Loop
        {
            get => _loop;
            set
            {
                if (_loop == value)
                    return;

                _loop = value;
                if (_keepEditorPlayModeChanges)
                    SaveParameters();
                ReconfigurePlayingAudioSources();
            }
        }

        public bool Singleton
        {
            get => _singleton;
            set
            {
                _singleton = value;
                if (_keepEditorPlayModeChanges)
                    SaveParameters();
            }
        }

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                if (_keepEditorPlayModeChanges)
                    SaveParameters();
                ReconfigurePlayingAudioSources();
            }
        }

        public float MinDistance
        {
            get => _minDistance;
            set
            {
                _minDistance = value;
                if (_keepEditorPlayModeChanges)
                    SaveParameters();
                ReconfigurePlayingAudioSources();
            }
        }

        public float MaxDistance
        {
            get => _maxDistance;
            set
            {
                _maxDistance = value;
                if (_keepEditorPlayModeChanges)
                    SaveParameters();
                ReconfigurePlayingAudioSources();
            }
        }

        public Vector2 Pitch
        {
            get => _pitch;
            set
            {
                if (_pitch == value)
                    return;
                _pitch = value;
                if (_keepEditorPlayModeChanges)
                    SaveParameters();
                ReconfigurePlayingAudioSources();
            }
        }

        public float DopplerLevel
        {
            get => _dopplerLevel;
            set => _dopplerLevel = value;
        }


        /// <summary>
        /// When true, a live singleton audio source will be interrupted to play a new clip from the same AudioPlayer
        /// </summary>
        public bool AllowInterrupt
        {
            get => _allowInterrupt;
            set => _allowInterrupt = value;
        }

        /// <summary>
        /// Volume fade-in time when AudioPlayer plays an audio
        /// </summary>
        public float PlayFadeTime
        {
            get => _playFadeTime;
            set => _playFadeTime = value;
        }

        /// <summary>
        /// When AudioPlayer gets interrupted (stopped mid playing), instead of cutting the audio, audio will fade out
        /// </summary>
        public float InterruptFadeTime
        {
            get => _interruptFadeTime;
            set => _interruptFadeTime = value;
        }

#if UNITY_EDITOR
        internal string[] ClipNames { get; private set; }
        private bool ShowStopButton => _lastPlayedAudio != null && _lastPlayedAudio.Value.IsPlaying();
#endif

        private string PersistentPrefix => _randomizedSaveKey + "_";

        private bool PersistentLoop
        {
            get => PlayerPrefs.GetInt(PersistentPrefix + "loop", _loop ? 1 : 0) == 1;
            set => PlayerPrefs.SetInt(PersistentPrefix + "loop", value ? 1 : 0);
        }

        private bool PersistentSingleton
        {
            get => PlayerPrefs.GetInt(PersistentPrefix + "singleton", _singleton ? 1 : 0) == 1;
            set => PlayerPrefs.SetInt(PersistentPrefix + "singleton", value ? 1 : 0);
        }

        private float PersistentVolume
        {
            get => PlayerPrefs.GetFloat(PersistentPrefix + "volume", _volume);
            set => PlayerPrefs.SetFloat(PersistentPrefix + "volume", value);
        }

        private float PersistentMinDistance
        {
            get => PlayerPrefs.GetFloat(PersistentPrefix + "minDistance", _minDistance);
            set => PlayerPrefs.SetFloat(PersistentPrefix + "minDistance", value);
        }

        private float PersistentMaxDistance
        {
            get => PlayerPrefs.GetFloat(PersistentPrefix + "maxDistance", _maxDistance);
            set => PlayerPrefs.SetFloat(PersistentPrefix + "maxDistance", value);
        }

        private Vector2 PersistentPitch
        {
            get
            {
                float x = PlayerPrefs.GetFloat(PersistentPrefix + "pitchX", _pitch.x);
                float y = PlayerPrefs.GetFloat(PersistentPrefix + "pitchY", _pitch.y);
                return new Vector2(x, y);
            }
            set
            {
                PlayerPrefs.SetFloat(PersistentPrefix + "pitchX", value.x);
                PlayerPrefs.SetFloat(PersistentPrefix + "pitchY", value.y);
            }
        }

        private float PersistentDopplerLevel
        {
            get => PlayerPrefs.GetFloat(PersistentPrefix + "dopplerLevel", _dopplerLevel);
            set => PlayerPrefs.SetFloat(PersistentPrefix + "dopplerLevel", value);
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            EditorApplication.playModeStateChanged += PlayModeChanged;

            void PlayModeChanged(PlayModeStateChange obj)
            {
                if (obj == PlayModeStateChange.EnteredPlayMode)
                {
                    foreach (AudioPlayer audioPlayer in SaveKeys.Values)
                    {
                        if (audioPlayer._keepEditorPlayModeChanges)
                            continue;
                        audioPlayer.SaveParameters();
                    }
                }
                else if (obj == PlayModeStateChange.EnteredEditMode)
                {
                    foreach (AudioPlayer audioPlayer in SaveKeys.Values)
                    {
                        if (audioPlayer._keepEditorPlayModeChanges)
                            continue;
                        audioPlayer.LoadParameters();
                    }
                }
            }
        }

#endif

        private void OnEnable()
        {
#if UNITY_EDITOR
            CheckSaveKeyConflict();
            ClipNames = _clips?.Select(x => x.name).ToArray();
#endif

            if (!Application.isEditor)
                LoadParameters();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            ClipNames = _clips?.Select(x => x.name).ToArray();

            int[] keys = _playingSources.Keys.ToArray();
            foreach (int id in keys)
            {
                Stop(id, 0);
            }
#endif
        }

        /// <summary>
        /// Plays a random clip Fire & Forget style
        /// </summary>
#if ODIN_INSPECTOR
        [Button("Play Random", ButtonSizes.Large)]
#elif NAUGHTY_ATTRIBUTES
        [Button("Play Random")]
#endif
#if !UNITASK && !EDITOR_COROUTINES && (ODIN_INSPECTOR || NAUGHTY_ATTRIBUTES)
        [InfoBox(
            "You need to install UniTask or Editor Coroutines package to use AudioPlayer in Edit Mode.\n" +
            "(There will be no problems in play mode)",
            InfoMessageType.Error,
            VisibleIf = "@UnityEngine.Application.isPlaying == false")]
#endif
        private void PlayForget()
        {
            Play(Random.Range(0, _clips.Count));
        }


#if !ODIN_INSPECTOR && NAUGHTY_ATTRIBUTES && UNITY_EDITOR
        // Naughty Attributes doesn't support method with parameters
        [SerializeField, BoxGroup("Specific Clip Name")] private string _specificClipParameter;
        [Button("Play Specific")]
        private void PlaySpecific()
        {
            PlayForget(_specificClipParameter);
        }
#endif
        /// <summary>
        /// Finds and Plays a clip by clip name in Fire & Forget style
        /// </summary>
        /// <param name="clipName"></param>
#if ODIN_INSPECTOR
        [Button("Play Specific", ButtonSizes.Large, ButtonStyle.Box, Expanded = true)]
#endif
        private void PlayForget(
#if ODIN_INSPECTOR
            [ValueDropdown("ClipNames")]
#endif
            string clipName)
        {
            Play(clipName);
        }

        /// <summary>
        /// Stops singleton instance if it's playing
        /// </summary>
        public void StopSingleton()
        {
            if (_singleton && _singletonHandle != null && _singletonHandle.Value.IsPlaying())
                _singletonHandle.Value.Stop();
        }

        /// <summary>
        /// Finds and plays a given clip, optionally at a position, and returns a handle which can be used to stop the clip.
        /// If clipName is not given, a random clip will be chosen.
        /// If position or tracking is provided, audio will be 3D, otherwise, audio will be played 2D 
        /// </summary>
        /// <param name="clipName">The name of clip to play</param>
        /// <param name="position">Position to play the clip at.</param>
        /// <param name="tracking">Audio player will track this transform's movement</param>
        /// <param name="delay">Delay in seconds before AudioPlayer actually plays the audio. AudioPlayers in delay are considered playing/live</param>
        /// <returns></returns>
        public AudioHandle Play(string clipName = null, Vector3? position = null, Transform tracking = null,
            float delay = 0)
        {
            if (_clips.Count == 0)
                throw new NoClipsFoundException(this);

            int index;

            if (string.IsNullOrEmpty(clipName))
            {
                index = Random.Range(0, _clips.Count);
            }
            else
            {
                index = FindIndex(clipName);

                if (index == -1)
                    throw new ClipNotFoundException(this, clipName);
            }

            return Play(index, position, tracking, delay);
        }

        /// <summary>
        /// Plays a given clip, optionally at a position, and returns a handle which can be used to stop the clip.
        /// If position or tracking is provided, audio will be 3D, otherwise, audio will be played 2D.
        /// </summary>
        /// <param name="index">The index of the clip to play</param>
        /// <param name="position">Position to play the clip at.</param>
        /// <param name="tracking">Audio player will track this transform's movement</param>
        /// <param name="delay">Delay in seconds before AudioPlayer actually plays the audio. AudioPlayers in delay are considered playing/live</param>
        /// <returns></returns>
        public AudioHandle Play(int index, Vector3? position = null, Transform tracking = null, float delay = 0)
        {
            if (_clips.Count == 0)
                throw new NoClipsFoundException(this);

#if UNITY_EDITOR
            CheckSaveKeyConflict();
#endif

            // If this instance is singleton, and there's an instance of audio that is playing, return that instance of audio
            if (_singleton && _singletonHandle != null && _singletonHandle.Value.IsPlaying())
            {
                if (!_allowInterrupt || index == _singletonHandle.Value.ClipIndex)
                    return _singletonHandle.Value;

                _singletonHandle.Value.Stop();
            }


            AudioClip clip = _clips[index];

            if (clip == null)
                throw new ClipNullException(this, index);

            int id = _nextId;
            _nextId++;
            var handle = new AudioHandle(this, id, index);

            AudioSource audioSource = AudioPool.Spawn(handle, position, tracking, _loop ? -1 : (clip.length + delay));

            ConfigureParameters(audioSource, false);
            audioSource.clip = clip;
            if (delay <= 0)
                audioSource.Play();
            else
                audioSource.PlayDelayed(delay);

            Fade.In(audioSource, audioSource.volume, _playFadeTime, delay);

            _playingSources.Add(id, audioSource);

            if (_singleton)
                _singletonHandle = handle;

#if UNITY_EDITOR
            _lastPlayedAudio = handle;
#endif

            return handle;
        }

#if UNITY_EDITOR
#if ODIN_INSPECTOR
        [Button("Stop", ButtonSizes.Large), ShowIf("ShowStopButton")]
#elif NAUGHTY_ATTRIBUTES
        [Button("Stop"), ShowIf("ShowStopButton")]
#endif
        private void StopLastPlayingClip()
        {
            if (_lastPlayedAudio != null)
                _lastPlayedAudio.Value.Stop();
        }
#endif

        public int FindIndex(string clipName)
        {
            return _clips.FindIndex(x => x.name == clipName);
        }

        internal bool Stop(int id, float fadeTime)
        {
            if (_playingSources.TryGetValue(id, out AudioSource source))
            {
                if (source != null)
                {
                    Fade.Out(source, fadeTime);
                }

                _playingSources.Remove(id);
                return true;
            }

            return false;
        }


        private void ConfigureParameters(AudioSource source, bool live)
        {
            if (!live || _liveLinkPitch)
                source.pitch = Random.Range(_pitch.x, _pitch.y);

            if (!live || _liveLinkVolume)
                source.volume = _volume;

            if (!live || _liveLinkDistances)
            {
                source.minDistance = _minDistance;
                source.maxDistance = _maxDistance;
            }

            if (!live || _liveLinkLoop)
                source.loop = _loop;

            if (!live || _liveLinkDopplerLevel)
                source.dopplerLevel = _dopplerLevel;

            if (AudioManager.Instance != null)
                source.outputAudioMixerGroup = AudioManager.Instance.GetAudioMixerGroup(_audioMixer.name);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClipNames = _clips?.Select(x => x.name).ToArray();

            CheckSaveKeyConflict();

            ReconfigurePlayingAudioSources();
        }

        private void CheckSaveKeyConflict()
        {
            while (_randomizedSaveKey == 0)
                _randomizedSaveKey = Random.Range(int.MinValue + 1, int.MaxValue - 1);

            // Remove all entries which their audio player has been destroyed
            int[] keysToRemove = SaveKeys
                .Where(x => x.Value == null)
                .Select(x => x.Key)
                .ToArray();
            foreach (int k in keysToRemove)
            {
                SaveKeys.Remove(k);
            }

            const int iterations = 1000;
            for (int i = 0; i < iterations; i++)
            {
                if (_randomizedSaveKey == 0)
                    OnValidate();

                if (SaveKeys.TryGetValue(_randomizedSaveKey, out AudioPlayer existing))
                {
                    if (existing == this)
                        return;

                    string existingName = existing != null ? existing.name : "Unknown";

                    Debug.LogError(
                        $"[Audoty] Found conflicting save keys between existing Audio Player `{existingName}` and `{name}`. Resolving the conflict by changing save key of {name}");
                    _randomizedSaveKey = Random.Range(int.MinValue + 1, int.MaxValue - 1);
                    continue;
                }

                SaveKeys.Add(_randomizedSaveKey, this);

                break;
            }
        }
#endif

        private void LoadParameters()
        {
#if UNITY_EDITOR
            CheckSaveKeyConflict();
#endif

            bool forceLoad = !_keepEditorPlayModeChanges && Application.isEditor;

            if (forceLoad || _saveLoop)
                Loop = PersistentLoop;

            if (forceLoad || _saveSingelton)
                Singleton = PersistentSingleton;

            if (forceLoad || _saveVolume)
                Volume = PersistentVolume;

            if (forceLoad || _saveDistances)
            {
                MinDistance = PersistentMinDistance;
                MaxDistance = PersistentMaxDistance;
            }

            if (forceLoad || _savePitch)
                Pitch = PersistentPitch;

            if (forceLoad || _saveDopplerLevel)
                DopplerLevel = PersistentDopplerLevel;
        }

        private void SaveParameters()
        {
#if UNITY_EDITOR
            CheckSaveKeyConflict();
#endif
            bool forceSave = !_keepEditorPlayModeChanges && Application.isEditor;

            if (forceSave || _saveLoop)
                PersistentLoop = Loop;

            if (forceSave || _saveSingelton)
                PersistentSingleton = Singleton;

            if (forceSave || _saveVolume)
                PersistentVolume = Volume;

            if (forceSave || _saveDistances)
            {
                PersistentMinDistance = MinDistance;
                PersistentMaxDistance = MaxDistance;
            }

            if (forceSave || _savePitch)
                PersistentPitch = Pitch;

            if (forceSave || _saveDopplerLevel)
                PersistentDopplerLevel = DopplerLevel;

            PlayerPrefs.Save();
        }

        private void ReconfigurePlayingAudioSources()
        {
            foreach (AudioSource source in _playingSources.Values)
            {
                if (source == null)
                    return;

                ConfigureParameters(source, true);
            }
        }
    }
}