using System.Collections;
using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// Zona de ambiente 2D (collider trigger). Cuando el jugador entra,
    /// hace fade-in del clip; cuando sale, fade-out. Ideal para "atmósferas"
    /// asignadas a habitaciones: zumbido del baño, viento en el altillo, etc.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AmbientZone : MonoBehaviour
    {
        [SerializeField] AudioClip clip;
        [Range(0f, 1f)] [SerializeField] float targetVolume = 0.5f;
        [SerializeField] float fadeDuration = 1.2f;
        [SerializeField] bool loop = true;
        [SerializeField] string playerTag = "Player";

        AudioSource _src;
        Coroutine _fade;

        void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            _src = gameObject.AddComponent<AudioSource>();
            _src.clip = clip;
            _src.loop = loop;
            _src.spatialBlend = 0f;
            _src.volume = 0f;
            _src.playOnAwake = false;

            if (AudioManager.Instance != null)
                _src.outputAudioMixerGroup = AudioManager.Instance.ambientGroup;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            if (clip != null && !_src.isPlaying) _src.Play();
            FadeTo(targetVolume, stopAtEnd: false);
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            FadeTo(0f, stopAtEnd: true);
        }

        void FadeTo(float target, bool stopAtEnd)
        {
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeRoutine(target, stopAtEnd));
        }

        IEnumerator FadeRoutine(float target, bool stopAtEnd)
        {
            float start = _src.volume;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                _src.volume = Mathf.Lerp(start, target, t / fadeDuration);
                yield return null;
            }
            _src.volume = target;
            if (stopAtEnd) _src.Stop();
            _fade = null;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.15f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider b)        Gizmos.DrawCube(b.center, b.size);
            else if (col is SphereCollider s) Gizmos.DrawSphere(s.center, s.radius);
        }
#endif
    }
}
