using UnityEngine;

namespace ReactorBreach.Enemies.AI.States
{
    public class AttackState : EnemyStateBase
    {
        private float _attackTimer;

        public AttackState(EnemyStateMachine fsm) : base(fsm) { }

        public override void Enter()
        {
            FSM.Agent.isStopped = true;
            _attackTimer = 0f;
        }

        public override void Update()
        {
            var player = Player.PlayerController.Instance;
            if (player == null) return;

            float dist = Vector3.Distance(FSM.Owner.transform.position, player.transform.position);

            if (dist > FSM.Agent.stoppingDistance + 1f)
            {
                FSM.TransitionTo<ChaseState>();
                return;
            }

            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                PerformAttack(player);
                _attackTimer = GetAttackCooldown();
            }
        }

        public override void Exit()
        {
            FSM.Agent.isStopped = false;
        }

        private void PerformAttack(Player.PlayerController player)
        {
            if (player.TryGetComponent<Player.PlayerHealth>(out var health))
                health.TakeDamage(FSM.Owner.AttackDamage);
        }

        private float GetAttackCooldown() => FSM.Owner.AttackCooldown;
    }
}
