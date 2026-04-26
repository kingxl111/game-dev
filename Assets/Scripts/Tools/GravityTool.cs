using System.Collections;
using UnityEngine;
using ReactorBreach.Environment;
using ReactorBreach.ScriptableObjects;
using ReactorBreach.Data;

namespace ReactorBreach.Tools
{
    public class GravityTool : ToolBase
    {
        private float _cooldownTimer;

        public float CooldownTimer => _cooldownTimer;
        public float Cooldown      => Config.GravityCooldown;
        public float GravityRange  => Config.GravityRange;

        public override bool CanUse => _cooldownTimer <= 0f;

        public GravityTool(ToolConfig config, Transform mount) : base(config, mount) { }

        public override void OnPrimaryAction()
        {
            if (!TryHitGravityAffectable(out var hit, out var target)) return;
            if (target.IsAffected)
            {
                UI.HUDController.ShowWarningStatic("Гравитация: дождитесь сброса эффекта");
                return;
            }

            target.ApplyGravityEffect(Config.MassMultiplier, Config.GravityDuration);
            _cooldownTimer = Config.GravityCooldown;

            Enemies.VibrationSystem.Emit(hit.point, GameConstants.VibrationRadiusGravity);
            UI.HUDController.ShowMechanicNoteStatic("Heavy: масса ×" + Config.MassMultiplier + " (плита/ящики)");
        }

        public override void OnSecondaryAction()
        {
            if (!CanUse) return;
            if (!TryHitGravityAffectable(out var hit, out var target)) return;
            if (target.IsAffected)
            {
                UI.HUDController.ShowWarningStatic("Гравитация: дождитесь сброса эффекта");
                return;
            }

            target.ApplyLightGravityEffect(Config.LightMassDivider, Config.GravityDuration);
            _cooldownTimer = Config.GravityCooldown;

            Enemies.VibrationSystem.Emit(hit.point, GameConstants.VibrationRadiusGravity);
            UI.HUDController.ShowMechanicNoteStatic("Light: масса /" + Config.LightMassDivider);
        }

        public override void Tick(float deltaTime)
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= deltaTime;
        }

        /// <summary>
        /// Берём первый по лучу объект с IGravityAffectable (коллайдер плиты иной раз ближе к камере, чем ящик).
        /// </summary>
        private bool TryHitGravityAffectable(out RaycastHit bestHit, out IGravityAffectable target)
        {
            bestHit = default;
            target  = null;
            Camera cam = Camera.main;
            if (cam == null) return false;
            var ray  = new Ray(cam.transform.position, cam.transform.forward);
            var hits = Physics.RaycastAll(ray, Config.GravityRange);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.collider.TryGetComponent<IGravityAffectable>(out var g))
                {
                    bestHit = h;
                    target  = g;
                    return true;
                }
            }
            return false;
        }
    }
}
