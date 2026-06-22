using System.Collections.Generic;
using UnityEngine;

namespace Game.Audio
{
    [CreateAssetMenu(menuName = "Mama Satelite/Audio/Scene Music Library", fileName = "SceneMusicLibrary")]
    public class SceneMusicLibrary : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            [Tooltip("Nombre exacto de la escena (ej: Floor1, Floor2, Outdoor).")]
            public string sceneName;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.7f;
            public bool loop = true;
        }

        public List<Entry> entries = new();

        [Tooltip("Música que se reproduce si la escena cargada no está en la lista.")]
        public AudioClip defaultClip;
        [Range(0f, 1f)] public float defaultVolume = 0.6f;

        public Entry GetForScene(string sceneName)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].sceneName == sceneName)
                    return entries[i];
            }
            return null;
        }
    }
}
