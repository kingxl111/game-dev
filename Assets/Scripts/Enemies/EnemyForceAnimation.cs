using UnityEngine;

namespace ReactorBreach.Enemies
{
    public class EnemyForceAnimation : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _stateName = "walk4";
        [SerializeField] private float _speed = 1f;

        private int _stateHash;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            _stateHash = Animator.StringToHash(_stateName);
            if (_animator != null)
            {
                _animator.applyRootMotion = false;
                _animator.speed = _speed;
            }
        }

        private void OnEnable()
        {
            PlayFromStart();
        }

        private void Update()
        {
            if (_animator == null) return;

            var info = _animator.GetCurrentAnimatorStateInfo(0);
            if (_animator.IsInTransition(0) || info.shortNameHash != _stateHash)
            {
                float time = info.shortNameHash == _stateHash ? info.normalizedTime % 1f : 0f;
                _animator.Play(_stateHash, 0, time);
            }
        }

        private void PlayFromStart()
        {
            if (_animator == null) return;
            _animator.Play(_stateHash, 0, 0f);
        }
    }
}
