using System;
using System.Collections.Generic;
using UnityEngine;
using ReactorBreach.ScriptableObjects;
using ReactorBreach.Data;
using ReactorBreach.Environment;

namespace ReactorBreach.Core
{
    public class ObjectiveTracker : MonoBehaviour
    {
        public event Action<string> OnObjectiveUpdated;

        private LevelObjective _objective;
        private LevelManager _levelManager;

        private int _sealedBreaches;
        private int _neutralizedEnemies;
        private bool _playerReached;

        private readonly List<BreachPoint> _breachPoints = new();
        private readonly List<Enemies.EnemyBase> _enemies = new();

        public void Initialize(LevelObjective objective, LevelManager levelManager)
        {
            _objective = objective;
            _levelManager = levelManager;

            GatherObjectives();
            UpdateUI();
        }

        private void GatherObjectives()
        {
            if (_objective.Type == ObjectiveType.Seal || _objective.Type == ObjectiveType.Combined)
            {
                var breaches = FindObjectsByType<BreachPoint>(FindObjectsSortMode.None);
                foreach (var b in breaches)
                {
                    _breachPoints.Add(b);
                    b.OnSealed += OnBreachSealed;
                }
            }

            if (_objective.Type == ObjectiveType.Purge || _objective.Type == ObjectiveType.Combined)
            {
                var enemies = FindObjectsByType<Enemies.EnemyBase>(FindObjectsSortMode.None);
                foreach (var e in enemies)
                {
                    _enemies.Add(e);
                    e.OnNeutralized += OnEnemyNeutralized;
                }
            }
        }

        private void OnBreachSealed()
        {
            _sealedBreaches++;
            UpdateUI();
            CheckCompletion();
        }

        private void OnEnemyNeutralized()
        {
            _neutralizedEnemies++;
            UpdateUI();
            CheckCompletion();
        }

        public void NotifyPlayerReached()
        {
            _playerReached = true;
            UpdateUI();
            CheckCompletion();
        }

        private void CheckCompletion()
        {
            bool complete = _objective.Type switch
            {
                ObjectiveType.Seal     => _sealedBreaches >= _breachPoints.Count,
                ObjectiveType.Purge    => _neutralizedEnemies >= _enemies.Count,
                ObjectiveType.Reach    => _playerReached,
                ObjectiveType.Combined => _sealedBreaches >= _breachPoints.Count
                                       && _neutralizedEnemies >= _enemies.Count
                                       && (_objective.BreachCount == 0 || _playerReached),
                _ => false
            };

            if (complete)
                _levelManager.NotifyLevelComplete();
        }

        private void UpdateUI()
        {
            if (_objective == null) return;

            string text = _objective.Type switch
            {
                ObjectiveType.Seal  => $"Загерметизируйте пробоины [{_sealedBreaches}/{_breachPoints.Count}]",
                ObjectiveType.Purge => $"Нейтрализуйте врагов [{_neutralizedEnemies}/{_enemies.Count}]",
                ObjectiveType.Reach => "Доберитесь до пульта управления",
                _                   => _objective.Description
            };

            OnObjectiveUpdated?.Invoke(text);
        }
    }
}
