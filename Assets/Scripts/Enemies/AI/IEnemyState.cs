using ReactorBreach.Enemies;
using UnityEngine;

namespace ReactorBreach.Enemies.AI
{
    public interface IEnemyState
    {
        void Enter();
        void Update();
        void Exit();
        void OnVibration(Vector3 source, float intensity);
    }

    public abstract class EnemyStateBase : IEnemyState
    {
        protected readonly EnemyStateMachine FSM;

        protected EnemyStateBase(EnemyStateMachine fsm)
        {
            FSM = fsm;
        }

        public virtual void Enter()  { }
        public virtual void Update() { }
        public virtual void Exit()   { }
        public virtual void OnVibration(Vector3 source, float intensity) { }

        protected bool CanSeePlayer(out Vector3 playerPos)
        {
            var player = Player.PlayerController.Instance;
            if (player == null) { playerPos = Vector3.zero; return false; }

            playerPos = player.transform.position;
            float dist = Vector3.Distance(FSM.Owner.transform.position, playerPos);
            if (dist > FSM.Owner.DetectionRange) return false;

            var visionGate = FSM.GetComponent<EnemyVisionGatedByHint>();
            if (visionGate != null && !visionGate.IsUnlocked)
                return false;

            return true;
        }
    }
}
