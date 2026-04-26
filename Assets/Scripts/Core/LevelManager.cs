using System;
using System.Collections;
using UnityEngine;
using ReactorBreach.ScriptableObjects;
using ReactorBreach.Data;

namespace ReactorBreach.Core
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private LevelObjective _objective;
        [SerializeField] private float _pressureDecayRate = 1f;

        public LevelObjective Objective => _objective;
        public float CurrentPressure { get; private set; } = 100f;
        public bool IsTimerActive => _objective != null && _objective.PressureTimer > 0f;

        public event Action OnLevelCompleted;
        public event Action OnLevelFailed;
        public event Action<float> OnPressureChanged;

        private ObjectiveTracker _tracker;
        private float _pressureTimer;
        private bool _finished;

        private void Awake()
        {
            _tracker = GetComponent<ObjectiveTracker>();
        }

        private void Start()
        {
            GameManager.Instance?.MarkLevelSceneAsPlayingIfNeeded();

            if (_objective == null) return;

            _pressureTimer = _objective.PressureTimer;

            if (_tracker != null)
                _tracker.Initialize(_objective, this);
        }

        private void Update()
        {
            if (_finished) return;
            if (!IsTimerActive) return;

            _pressureTimer -= Time.deltaTime * _pressureDecayRate;
            CurrentPressure = Mathf.Clamp01(_pressureTimer / _objective.PressureTimer) * 100f;
            OnPressureChanged?.Invoke(CurrentPressure);

            if (_pressureTimer <= 0f)
                Fail();
        }

        public void NotifyLevelComplete()
        {
            if (_finished) return;
            _finished = true;
            OnLevelCompleted?.Invoke();
            GameManager.Instance?.CompleteLevel();
        }

        public void Fail()
        {
            if (_finished) return;
            _finished = true;
            OnLevelFailed?.Invoke();
            GameManager.Instance?.GameOver();
        }
    }
}
