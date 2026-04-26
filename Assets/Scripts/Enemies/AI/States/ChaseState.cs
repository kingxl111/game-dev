using UnityEngine;

namespace ReactorBreach.Enemies.AI.States
{
    public class ChaseState : EnemyStateBase
    {
        private float _loseTargetTimer;

        public ChaseState(EnemyStateMachine fsm) : base(fsm) { }

        public override void Enter()
        {
            _loseTargetTimer = 0f;
            FSM.Agent.isStopped = false;
        }

        public override void Update()
        {
            var player = Player.PlayerController.Instance;
            if (player == null) return;

            float dist = Vector3.Distance(FSM.Owner.transform.position, player.transform.position);

            if (dist <= FSM.Agent.stoppingDistance + 0.5f)
            {
                FSM.TransitionTo<AttackState>();
                return;
            }

            if (CanSeePlayer(out Vector3 pos))
            {
                _loseTargetTimer = 0f;
                FSM.LastKnownPlayerPosition = pos;
                FSM.Agent.SetDestination(pos);
            }
            else
            {
                _loseTargetTimer += Time.deltaTime;
                if (_loseTargetTimer >= FSM.Owner.LoseTargetTime)
                    FSM.TransitionTo<PatrolState>();
            }
        }
    }
}
