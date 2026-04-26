using ReactorBreach.Core;
using ReactorBreach.Enemies;
using UnityEngine;
using UnityEngine.AI;

namespace ReactorBreach.Enemies.AI.States
{
    public class PatrolState : EnemyStateBase
    {
        private Vector3 _patrolTarget;
        private const float PatrolRadius = 8f;

        public PatrolState(EnemyStateMachine fsm) : base(fsm) { }

        public override void Enter()
        {
            FSM.Agent.isStopped = false;
            SetRandomPatrolPoint();
        }

        public override void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (CanSeePlayer(out var pos))
            {
                FSM.LastKnownPlayerPosition = pos;
                FSM.TransitionTo<ChaseState>();
                return;
            }
            if (!FSM.Agent.pathPending && FSM.Agent.remainingDistance < 0.5f)
                SetRandomPatrolPoint();
        }

        public override void OnVibration(Vector3 source, float intensity)
        {
            var g = FSM.GetComponent<EnemyVisionGatedByHint>();
            if (g != null && !g.IsUnlocked) return;

            FSM.LastKnownPlayerPosition = source;
            FSM.TransitionTo<InvestigateState>();
        }

        private void SetRandomPatrolPoint()
        {
            Vector3 origin = FSM.Owner.transform.position;
            for (int i = 0; i < 10; i++)
            {
                Vector3 candidate = origin + Random.insideUnitSphere * PatrolRadius;
                candidate.y = origin.y;
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    _patrolTarget = hit.position;
                    FSM.Agent.SetDestination(_patrolTarget);
                    return;
                }
            }
        }
    }
}
