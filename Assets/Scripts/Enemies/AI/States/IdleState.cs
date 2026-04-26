using ReactorBreach.Core;
using ReactorBreach.Enemies;
using UnityEngine;

namespace ReactorBreach.Enemies.AI.States
{
    public class IdleState : EnemyStateBase
    {
        public IdleState(EnemyStateMachine fsm) : base(fsm) { }

        public override void Enter()
        {
            FSM.Agent.isStopped = true;
        }

        public override void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (CanSeePlayer(out var pos))
            {
                FSM.LastKnownPlayerPosition = pos;
                FSM.TransitionTo<ChaseState>();
            }
        }

        public override void OnVibration(Vector3 source, float intensity)
        {
            var g = FSM.GetComponent<EnemyVisionGatedByHint>();
            if (g != null && !g.IsUnlocked) return;

            FSM.LastKnownPlayerPosition = source;
            FSM.TransitionTo<InvestigateState>();
        }
    }
}
