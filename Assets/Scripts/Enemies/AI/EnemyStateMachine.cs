using System;
using System.Collections.Generic;
using UnityEngine;
using ReactorBreach.Enemies.AI;
using ReactorBreach.Enemies.AI.States;

namespace ReactorBreach.Enemies
{
    [RequireComponent(typeof(EnemyBase))]
    public class EnemyStateMachine : MonoBehaviour
    {
        private Dictionary<Type, IEnemyState> _states;
        private IEnemyState _currentState;

        public EnemyBase Owner  { get; private set; }
        public UnityEngine.AI.NavMeshAgent Agent { get; private set; }

        // Last known player position for Investigate/Chase
        public Vector3 LastKnownPlayerPosition { get; set; }
        public bool HasTarget { get; set; }

        private void Awake()
        {
            Owner = GetComponent<EnemyBase>();
            Agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        }

        private void Start()
        {
            InitStates();
            TransitionTo<AI.States.IdleState>();
        }

        private void InitStates()
        {
            _states = new Dictionary<Type, IEnemyState>
            {
                { typeof(AI.States.IdleState),        new AI.States.IdleState(this)        },
                { typeof(AI.States.InvestigateState), new AI.States.InvestigateState(this) },
                { typeof(AI.States.ChaseState),       new AI.States.ChaseState(this)       },
                { typeof(AI.States.AttackState),      new AI.States.AttackState(this)       },
                { typeof(AI.States.PatrolState),      new AI.States.PatrolState(this)       },
                { typeof(AI.States.StuckState),       new AI.States.StuckState(this)        },
            };
        }

        public void TransitionTo<T>() where T : IEnemyState
        {
            _currentState?.Exit();
            _currentState = _states[typeof(T)];
            _currentState.Enter();
        }

        private void Update()
        {
            _currentState?.Update();

            // Weld neutralization check
            if (Owner.WeldedTimer > 0f)
            {
                Owner.WeldedTimer += Time.deltaTime;
                Owner.CheckWeldNeutralization();
            }
        }

        public void OnVibrationDetected(Vector3 source, float intensity)
        {
            _currentState?.OnVibration(source, intensity);
        }
    }
}
