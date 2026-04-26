using System.Collections.Generic;
using UnityEngine;

namespace ReactorBreach.Systems
{
    public class ObjectPool : MonoBehaviour
    {
        [System.Serializable]
        public struct PoolEntry
        {
            public string Key;
            public GameObject Prefab;
            public int InitialSize;
        }

        public static ObjectPool Instance { get; private set; }

        [SerializeField] private PoolEntry[] _entries;

        private readonly Dictionary<string, Queue<GameObject>> _pools = new();
        private readonly Dictionary<string, GameObject>        _prefabs = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (var entry in _entries)
                InitPool(entry);
        }

        private void InitPool(PoolEntry entry)
        {
            _prefabs[entry.Key] = entry.Prefab;
            _pools[entry.Key]   = new Queue<GameObject>();

            for (int i = 0; i < entry.InitialSize; i++)
            {
                var obj = Instantiate(entry.Prefab, transform);
                obj.SetActive(false);
                _pools[entry.Key].Enqueue(obj);
            }
        }

        public GameObject Get(string key, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(key, out var queue))
            {
                Debug.LogWarning($"[ObjectPool] Unknown key: {key}");
                return null;
            }

            GameObject obj;
            if (queue.Count > 0)
            {
                obj = queue.Dequeue();
            }
            else
            {
                obj = Instantiate(_prefabs[key], transform);
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        public void Return(string key, GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);

            if (_pools.TryGetValue(key, out var queue))
                queue.Enqueue(obj);
        }
    }
}
