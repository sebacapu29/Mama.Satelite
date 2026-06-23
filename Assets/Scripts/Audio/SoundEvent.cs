using UnityEngine;

namespace Game.Audio
{
    [CreateAssetMenu(menuName = "Mama Satelite/Audio/Sound Event", fileName = "SE_New")]
    public class SoundEvent : ScriptableObject
    {
        [Tooltip("Si hay varios clips, se elige uno al azar (variación natural).")]
        public AudioClip[] clips;

        public AudioCategory category = AudioCategory.SFX;

        [Range(0f, 1f)] public float volumeMin = 0.9f;
        [Range(0f, 1f)] public float volumeMax = 1f;

        [Range(0.1f, 3f)] public float pitchMin = 0.95f;
        [Range(0.1f, 3f)] public float pitchMax = 1.05f;

        [Header("Espacial (sólo si se reproduce 3D)")]
        public float minDistance = 1f;
        public float maxDistance = 20f;
        public AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;

        public bool HasClip => clips != null && clips.Length > 0 && clips[0] != null;

        public AudioClip PickClip()
        {
            if (!HasClip) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        public float PickVolume() => Random.Range(volumeMin, volumeMax);
        public float PickPitch()  => Random.Range(pitchMin, pitchMax);
    }
}
