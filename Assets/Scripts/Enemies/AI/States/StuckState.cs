using UnityEngine;

namespace ReactorBreach.Enemies.AI.States
{
    public class StuckState : EnemyStateBase
    {
        private float _stuckPositionTimer;
        private Vector3 _lastPosition;
        private const float NeutralizationStuckTime = 10f;

        public StuckState(EnemyStateMachine fsm) : base(fsm) { }

        public override void Enter()
        {
            FSM.Agent.isStopped = true;
            _stuckPositionTimer = 0f;
            _lastPosition = FSM.Owner.transform.position;
        }

        public override void Update()
        {
            // Check if enemy can escape (weld ended + not in foam)
            if (!FSM.Owner.IsStuck && FSM.Owner.WeldedTimer <= 0f)
            {
                FSM.TransitionTo<IdleState>();
                return;
            }

            // Check stuck-in-foam neutralization
            if (Vector3.Distance(FSM.Owner.transform.position, _lastPosition) < 0.05f)
            {
                _stuckPositionTimer += Time.deltaTime;
                if (_stuckPositionTimer >= NeutralizationStuckTime)
                    FSM.Owner.ForceNeutralize();
            }
            else
            {
                _stuckPositionTimer = 0f;
                _lastPosition = FSM.Owner.transform.position;
            }
        }
    }
}
