using UnityEngine;
using ReactorBreach.Player;

namespace ReactorBreach.Environment
{
    /// <summary>
    /// Триггер мгновенной смерти игрока. Используется для пропастей и
    /// зон с экстремальной опасностью.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DeathZone : MonoBehaviour
    {
        [SerializeField] private float _damage = 9999f;
        [Tooltip("World Z границы 'уступов' пола по разрыву: урон только если z строго между min и max (на полу у кромок — без урона).")]
        [SerializeField] private bool  _useWorldZBounds;
        [SerializeField] private float _worldZMin = -1e6f;
        [SerializeField] private float _worldZMax = 1e6f;

        [Header("Пропасть: только «провал»")]
        [Tooltip("Если true — смерть только при падении вниз, центр тела под уровнем моста/пола (не срабатывает на сушу).")]
        [SerializeField] private bool  _onlyWhenFellIntoPit;
        [SerializeField] private float _maxYWorldToCountAsFalling = 0.45f;
        [Tooltip("Скорость вниз: если меньше по модулю — всё ещё можно умереть, пока в воздухе (не IsGrounded).")]
        [SerializeField] private float _minDownwardSpeed = 0.15f;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var hp = other.GetComponent<PlayerHealth>()
                     ?? other.GetComponentInParent<PlayerHealth>();
            if (hp == null) return;

            if (_useWorldZBounds)
            {
                float pz = hp.transform.position.z;
                if (pz <= _worldZMin || pz >= _worldZMax) return;
            }

            if (_onlyWhenFellIntoPit)
            {
                if (!other.TryGetComponent(out PlayerController pc))
                    pc = other.GetComponentInParent<PlayerController>();
                if (pc == null) return;
                if (pc.transform.position.y > _maxYWorldToCountAsFalling) return;
                if (pc.VerticalVelocity > -_minDownwardSpeed && pc.IsGrounded) return;
            }

            hp.TakeDamage(_damage);
        }
    }
}
