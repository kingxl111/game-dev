using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ReactorBreach.Systems
{
    [Serializable]
    public class SaveData
    {
        public int LastCompletedLevel;
        public float TotalPlayTime;
        public List<LevelStats> Levels = new();
    }

    [Serializable]
    public class LevelStats
    {
        public int   LevelIndex;
        public float CompletionTime;
        public int   FoamUsed;
        public int   WeldsCreated;
        public int   GravityUsed;
        public bool  Completed;
    }

    public static class SaveSystem
    {
        private static readonly string SavePath =
            Path.Combine(Application.persistentDataPath, "save.json");

        private static SaveData _cache;

        public static SaveData Load()
        {
            if (_cache != null) return _cache;

            if (File.Exists(SavePath))
            {
                try
                {
                    string json = File.ReadAllText(SavePath);
                    _cache = JsonUtility.FromJson<SaveData>(json);
                    return _cache;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveSystem] Load failed: {e.Message}");
                }
            }

            _cache = new SaveData();
            return _cache;
        }

        public static void Save(SaveData data)
        {
            _cache = data;
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Save failed: {e.Message}");
            }
        }

        public static void RecordLevelComplete(int levelIndex, LevelStats stats)
        {
            var data = Load();
            if (levelIndex > data.LastCompletedLevel)
                data.LastCompletedLevel = levelIndex;

            var existing = data.Levels.Find(s => s.LevelIndex == levelIndex);
            if (existing != null)
                data.Levels.Remove(existing);

            stats.LevelIndex = levelIndex;
            data.Levels.Add(stats);
            Save(data);
        }

        public static void DeleteSave()
        {
            _cache = null;
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
    }
}
