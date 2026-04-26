using System.Collections.Generic;
using UnityEngine;

namespace ReactorBreach.Systems
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [System.Serializable]
        public struct SoundEntry
        {
            public string     Key;
            public AudioClip  Clip;
            [Range(0f, 1f)]
            public float      Volume;
            public bool       Loop;
        }

        [SerializeField] private SoundEntry[] _sounds;
        [SerializeField] private int _sourcesPoolSize = 10;

        // Mixer channels (AudioSource per channel for 2D)
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _ambientSource;

        private readonly Queue<AudioSource>           _sourcePool  = new();
        private readonly Dictionary<string, SoundEntry> _soundMap  = new();
        private readonly List<AudioSource>            _activeSources = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (var s in _sounds)
                _soundMap[s.Key] = s;

            for (int i = 0; i < _sourcesPoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sourcePool.Enqueue(src);
            }
        }

        /// <summary>Play a 3D sound at position.</summary>
        public void PlaySFX(string key, Vector3 position)
        {
            if (!_soundMap.TryGetValue(key, out var entry)) return;
            if (entry.Clip == null) return;

            AudioSource.PlayClipAtPoint(entry.Clip, position, entry.Volume);
        }

        /// <summary>Play a 2D sound (UI, music, etc.).</summary>
        public void PlayUI(string key)
        {
            if (!_soundMap.TryGetValue(key, out var entry)) return;
            if (entry.Clip == null) return;

            var src = GetSource();
            src.spatialBlend = 0f;
            src.volume       = entry.Volume;
            src.loop         = entry.Loop;
            src.clip         = entry.Clip;
            src.Play();
            _activeSources.Add(src);
        }

        public void PlayMusic(string key)
        {
            if (_musicSource == null || !_soundMap.TryGetValue(key, out var entry)) return;
            _musicSource.clip   = entry.Clip;
            _musicSource.volume = entry.Volume;
            _musicSource.loop   = true;
            _musicSource.Play();
        }

        public void StopMusic() => _musicSource?.Stop();

        private void Update()
        {
            for (int i = _activeSources.Count - 1; i >= 0; i--)
            {
                if (!_activeSources[i].isPlaying)
                {
                    _sourcePool.Enqueue(_activeSources[i]);
                    _activeSources.RemoveAt(i);
                }
            }
        }

        private AudioSource GetSource()
        {
            return _sourcePool.Count > 0 ? _sourcePool.Dequeue() : gameObject.AddComponent<AudioSource>();
        }
    }
}
