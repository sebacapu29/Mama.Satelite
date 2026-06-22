using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// Fuente de sonido ambiental ANCLADA a un objeto del mundo.
    /// La distancia/ubicación las resuelve Unity con spatialBlend = 1 + curva logarítmica.
    /// Ejemplos: chimenea, TV encendida, ventilador, goteo en el baño, reloj de pared.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AmbientSoundEmitter : MonoBehaviour
    {
        [SerializeField] AudioClip clip;
        [Range(0f, 1f)] [SerializeField] float volume = 0.8f;

        [Header("Espacial")]
        [SerializeField] float minDistance = 1f;
        [SerializeField] float maxDistance = 15f;
        [SerializeField] AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;

        [Header("Reproducción")]
        [SerializeField] bool playOnStart = true;
        [SerializeField] bool loop = true;
        [Tooltip("Si está activo, el clip arranca en una posición aleatoria (evita sincronía entre emisores idénticos).")]
        [SerializeField] bool randomizeStartOffset = true;

        AudioSource _src;

        void Awake()
        {
            _src = GetComponent<AudioSource>();
            _src.clip = clip;
            _src.volume = volume;
            _src.loop = loop;
            _src.spatialBlend = 1f;
            _src.minDistance = minDistance;
            _src.maxDistance = maxDistance;
            _src.rolloffMode = rolloff;
            _src.playOnAwake = false;
            _src.dopplerLevel = 0f;

            if (AudioManager.Instance != null)
                _src.outputAudioMixerGroup = AudioManager.Instance.ambientGroup;
        }

        void Start()
        {
            if (!playOnStart || clip == null) return;
            if (randomizeStartOffset) _src.time = Random.Range(0f, clip.length);
            _src.Play();
        }

        public void Play()  { if (!_src.isPlaying) _src.Play(); }
        public void Stop()  { _src.Stop(); }
        public void SetVolume(float v) { _src.volume = Mathf.Clamp01(v); }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, minDistance);
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.12f);
            Gizmos.DrawWireSphere(transform.position, maxDistance);
        }
#endif
    }
}
