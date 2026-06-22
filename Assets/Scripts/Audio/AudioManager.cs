using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Game.Audio
{
    /// <summary>
    /// Singleton persistente. Maneja música con crossfade entre escenas y
    /// reproduce SFX 2D / 3D con ruteo por categoría a un AudioMixer (opcional).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Biblioteca de música por escena")]
        public SceneMusicLibrary musicLibrary;

        [Header("Crossfade")]
        [Tooltip("Duración del crossfade entre tracks (segundos).")]
        public float crossfadeDuration = 2f;
        [Range(0f, 1f)] public float masterMusicVolume = 0.8f;

        [Header("AudioMixer (opcional)")]
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup ambientGroup;
        public AudioMixerGroup uiGroup;
        public AudioMixerGroup voiceGroup;

        AudioSource _musicA;
        AudioSource _musicB;
        bool _aIsActive = true;
        Coroutine _musicRoutine;

        readonly Dictionary<AudioCategory, AudioSource> _oneShot2D = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _musicA = CreateMusicSource("Music_A");
            _musicB = CreateMusicSource("Music_B");
        }

        void OnEnable()  => SceneManager.sceneLoaded += HandleSceneLoaded;
        void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (musicLibrary == null) return;
            var entry = musicLibrary.GetForScene(scene.name);
            if (entry != null && entry.clip != null)
                PlayMusic(entry.clip, entry.volume, entry.loop);
            else if (musicLibrary.defaultClip != null)
                PlayMusic(musicLibrary.defaultClip, musicLibrary.defaultVolume, true);
            else
                StopMusic();
        }

        AudioSource CreateMusicSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.outputAudioMixerGroup = musicGroup;
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = 0f;
            return src;
        }

        // ---------------- MÚSICA ----------------

        public void PlayMusic(AudioClip clip, float volume = 1f, bool loop = true)
        {
            if (clip == null) { StopMusic(); return; }

            var active = _aIsActive ? _musicA : _musicB;
            if (active.clip == clip && active.isPlaying) return;

            if (_musicRoutine != null) StopCoroutine(_musicRoutine);
            _musicRoutine = StartCoroutine(CrossfadeRoutine(clip, volume, loop));
        }

        public void StopMusic()
        {
            if (_musicRoutine != null) StopCoroutine(_musicRoutine);
            _musicRoutine = StartCoroutine(FadeOutRoutine());
        }

        IEnumerator CrossfadeRoutine(AudioClip clip, float volume, bool loop)
        {
            var from = _aIsActive ? _musicA : _musicB;
            var to   = _aIsActive ? _musicB : _musicA;
            _aIsActive = !_aIsActive;

            to.clip   = clip;
            to.loop   = loop;
            to.volume = 0f;
            to.Play();

            float target = volume * masterMusicVolume;
            float startFrom = from.volume;
            float t = 0f;

            while (t < crossfadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = t / crossfadeDuration;
                from.volume = Mathf.Lerp(startFrom, 0f, k);
                to.volume   = Mathf.Lerp(0f, target, k);
                yield return null;
            }

            from.Stop();
            from.clip = null;
            to.volume = target;
            _musicRoutine = null;
        }

        IEnumerator FadeOutRoutine()
        {
            var active = _aIsActive ? _musicA : _musicB;
            float start = active.volume;
            float t = 0f;
            while (t < crossfadeDuration)
            {
                t += Time.unscaledDeltaTime;
                active.volume = Mathf.Lerp(start, 0f, t / crossfadeDuration);
                yield return null;
            }
            active.Stop();
            _musicRoutine = null;
        }

        // ---------------- SFX ----------------

        /// <summary>SFX 2D: se oye igual en cualquier parte (UI, voz interna, jumpscare).</summary>
        public void PlaySFX(SoundEvent sound)
        {
            if (sound == null || !sound.HasClip) return;
            var src = Get2DSource(sound.category);
            src.pitch = sound.PickPitch();
            src.PlayOneShot(sound.PickClip(), sound.PickVolume());
        }

        /// <summary>SFX 3D: se atenúa según la distancia del listener (pasos, golpes, props).</summary>
        public void PlaySFXAtPoint(SoundEvent sound, Vector3 position)
        {
            if (sound == null || !sound.HasClip) return;
            var clip = sound.PickClip();

            var go = new GameObject($"OneShot_{clip.name}");
            go.transform.position = position;
            var src = go.AddComponent<AudioSource>();
            src.clip   = clip;
            src.volume = sound.PickVolume();
            src.pitch  = sound.PickPitch();
            src.outputAudioMixerGroup = GroupFor(sound.category);
            src.spatialBlend = 1f;
            src.minDistance  = sound.minDistance;
            src.maxDistance  = sound.maxDistance;
            src.rolloffMode  = sound.rolloff;
            src.Play();
            Destroy(go, clip.length / Mathf.Max(0.01f, src.pitch) + 0.1f);
        }

        AudioSource Get2DSource(AudioCategory cat)
        {
            if (_oneShot2D.TryGetValue(cat, out var src) && src != null) return src;
            var go = new GameObject($"OneShot2D_{cat}");
            go.transform.SetParent(transform);
            src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            src.outputAudioMixerGroup = GroupFor(cat);
            _oneShot2D[cat] = src;
            return src;
        }

        public AudioMixerGroup GroupFor(AudioCategory cat) => cat switch
        {
            AudioCategory.Music   => musicGroup,
            AudioCategory.SFX     => sfxGroup,
            AudioCategory.Ambient => ambientGroup,
            AudioCategory.UI      => uiGroup,
            AudioCategory.Voice   => voiceGroup,
            _ => sfxGroup
        };
    }
}
