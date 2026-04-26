using UnityEngine;

namespace ReactorBreach.Enemies.AI.States
{
    public class InvestigateState : EnemyStateBase
    {
        private float _timer;
        private const float InvestigateTime = 5f;

        public InvestigateState(EnemyStateMachine fsm) : base(fsm) { }

        public override void Enter()
        {
            _timer = 0f;
            FSM.Agent.isStopped = false;
            FSM.Agent.SetDestination(FSM.LastKnownPlayerPosition);
        }

        public override void Update()
        {
            _timer += Time.deltaTime;

            if (CanSeePlayer(out _))
            {
                FSM.TransitionTo<ChaseState>();
                return;
            }

            if (_timer >= InvestigateTime)
                FSM.TransitionTo<PatrolState>();
        }

        public override void OnVibration(Vector3 source, float intensity)
        {
            FSM.LastKnownPlayerPosition = source;
            FSM.Agent.SetDestination(source);
            _timer = 0f;
        }
    }
}
