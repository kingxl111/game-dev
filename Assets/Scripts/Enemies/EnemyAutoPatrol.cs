using UnityEngine;
using ReactorBreach.Enemies.AI.States;

namespace ReactorBreach.Enemies
{
    /// <summary>
    /// Хелпер: переключает FSM в Patrol после старта, чтобы враг
    /// сразу начал двигаться, а не стоял в Idle до первой вибрации.
    /// </summary>
    [RequireComponent(typeof(EnemyStateMachine))]
    public class EnemyAutoPatrol : MonoBehaviour
    {
        [SerializeField] private float _initialDelay;

        private EnemyStateMachine _fsm;
        private float _timer;
        private bool _switched;

        private void Awake()
        {
            _fsm = GetComponent<EnemyStateMachine>();
        }

        private void Update()
        {
            if (_switched) return;
            _timer += Time.deltaTime;
            if (_timer >= _initialDelay)
            {
                _switched = true;
                _fsm.TransitionTo<PatrolState>();
            }
        }
    }
}
