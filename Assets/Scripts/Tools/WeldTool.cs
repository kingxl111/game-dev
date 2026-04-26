using System.Collections.Generic;
using UnityEngine;
using ReactorBreach.Environment;
using ReactorBreach.ScriptableObjects;
using ReactorBreach.Data;

namespace ReactorBreach.Tools
{
    public class WeldTool : ToolBase
    {
        private float _currentEnergy;
        private readonly List<WeldConnection> _activeWelds = new();
        private IWeldable _pendingTarget;
        private Vector3 _pendingPoint;

        // VFX
        private GameObject _pendingHighlight;

        public float CurrentEnergy => _currentEnergy;
        public float MaxEnergy     => Config.MaxEnergy;
        public float WeldRange     => Config.WeldRange;
        public int   ActiveWelds   => _activeWelds.Count;

        public override bool CanUse =>
            _currentEnergy >= Config.WeldEnergyCost &&
            _activeWelds.Count < Config.MaxActiveWelds;

        public WeldTool(ToolConfig config, Transform mount) : base(config, mount)
        {
            _currentEnergy = config.MaxEnergy;
        }

        public override void OnEquip()
        {
            base.OnEquip();
            _pendingTarget = null;
            ClearHighlight();
        }

        public override void OnUnequip()
        {
            base.OnUnequip();
            _pendingTarget = null;
            ClearHighlight();
        }

        public override void OnPrimaryAction()
        {
            if (!TryRaycast(Config.WeldRange, out var hit)) return;
            if (!hit.collider.TryGetComponent<IWeldable>(out var weldable)) return;
            if (!weldable.CanBeWelded) return;

            if (_pendingTarget == null)
            {
                // First selection
                _pendingTarget = weldable;
                _pendingPoint  = hit.point;
                ShowPendingHighlight(hit.point);
            }
            else
            {
                if (weldable == _pendingTarget) return;

                if (_currentEnergy < Config.WeldEnergyCost)
                {
                    UI.HUDController.ShowWarningStatic("Недостаточно энергии");
                    return;
                }

                CreateWeld(_pendingTarget, _pendingPoint, weldable, hit.point);
                _pendingTarget = null;
                ClearHighlight();
            }
        }

        public override void OnSecondaryAction()
        {
            // Cancel pending selection
            _pendingTarget = null;
            ClearHighlight();
        }

        public override void Tick(float deltaTime)
        {
            // Energy regen
            _currentEnergy = Mathf.Min(Config.MaxEnergy,
                _currentEnergy + Config.EnergyRegenRate * deltaTime);

            // Weld duration tick
            for (int i = _activeWelds.Count - 1; i >= 0; i--)
            {
                _activeWelds[i].RemainingTime -= deltaTime;
                if (_activeWelds[i].RemainingTime <= 0f || _activeWelds[i].IsBroken)
                {
                    try
                    {
                        _activeWelds[i].Destroy();
                    }
                    finally
                    {
                        _activeWelds.RemoveAt(i);
                    }
                }
            }
        }

        // ── Private ───────────────────────────────────────────────────────

        private void CreateWeld(IWeldable a, Vector3 pointA, IWeldable b, Vector3 pointB)
        {
            _currentEnergy -= Config.WeldEnergyCost;

            bool aIsStatic = a.WeldRigidbody == null || a.WeldRigidbody.isKinematic;
            bool bIsStatic = b.WeldRigidbody == null || b.WeldRigidbody.isKinematic;

            float duration = GetEffectiveDuration();
            WeldConnection connection;

            if (aIsStatic && bIsStatic)
                connection = WeldJointFactory.CreateBridge(pointA, pointB, a, b, Config, duration);
            else
                connection = WeldJointFactory.CreateJoint(a, pointA, b, pointB, Config, duration);

            _activeWelds.Add(connection);
            a.OnWelded(connection);
            b.OnWelded(connection);

            Enemies.VibrationSystem.Emit(pointA, GameConstants.VibrationRadiusWeld);
            UI.HUDController.ShowMechanicNoteStatic("Сварка: соединение удерживается (таймер)");

            // Деал weld damage / триггеры специальных целей
            ApplyWeldEffects(a, b);
            ApplyWeldEffects(b, a);
        }

        /// <summary>
        /// Эффекты при приваривании target к other:
        /// — урон врагу (§4.5),
        /// — герметизация пробоины (§2.1, §5.4),
        /// — блокировка рта Утробы (§4.3),
        /// — фиксация щупальца Ядра (§4.4).
        /// </summary>
        private static void ApplyWeldEffects(IWeldable target, IWeldable other)
        {
            if (target is not MonoBehaviour mb) return;

            // Weld damage to enemy
            if (mb.TryGetComponent<Enemies.EnemyBase>(out var enemy))
                enemy.TakeDamage(5f, DamageType.Weld);

            // §2.1, §5.4 — герметизация пробоины: подвижный объект приварен к BreachPoint
            if (mb.TryGetComponent<Environment.BreachPoint>(out var breach))
            {
                bool otherIsMovable = other.WeldRigidbody != null && !other.WeldRigidbody.isKinematic;
                if (otherIsMovable)
                    breach.Seal();
            }

            // §4.3 — Утроба: блокировка рта (ищем Womb на родителе)
            var womb = mb.GetComponentInParent<Enemies.EnemyWomb>();
            if (womb != null && mb.CompareTag("WombMouth"))
                womb.BlockMouth();

            // §4.4 — Ядро: фиксация щупальца
            var tentacle = mb.GetComponent<Environment.CoreTentacle>();
            if (tentacle != null)
                tentacle.OnWelded();
        }

        private void ShowPendingHighlight(Vector3 point)
        {
            ClearHighlight();
            _pendingHighlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _pendingHighlight.name = "WeldHighlight";
            _pendingHighlight.transform.position   = point;
            _pendingHighlight.transform.localScale  = Vector3.one * 0.12f;
            Object.Destroy(_pendingHighlight.GetComponent<Collider>());
            var r = _pendingHighlight.GetComponent<Renderer>();
            r.material.color = Color.yellow;
        }

        private void ClearHighlight()
        {
            if (_pendingHighlight != null)
            {
                Object.Destroy(_pendingHighlight);
                _pendingHighlight = null;
            }
        }

        /// <summary>
        /// Продолжительность сварки с учётом снижающейся отдачи (§19.3 ТЗ).
        /// _activeWelds.Count — число активных сварок ДО добавления новой.
        /// Новая будет (count+1)-й: 1-я — 100%, 2-я — 70%, 3+ — 50%.
        /// </summary>
        private float GetEffectiveDuration()
        {
            return _activeWelds.Count switch
            {
                0    => Config.WeldDuration,        // 1-я сварка
                1    => Config.WeldDuration * 0.7f, // 2-я
                _    => Config.WeldDuration * 0.5f  // 3+
            };
        }
    }
}
