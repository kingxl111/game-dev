using UnityEngine;

namespace ReactorBreach.Environment
{
    public interface IGravityAffectable
    {
        Rigidbody TargetRigidbody { get; }
        float OriginalMass { get; }
        bool IsAffected { get; }
        void ApplyGravityEffect(float multiplier, float duration);
        /// <summary>Уменьшить массу (ПКМ): mass /= divider, затем сброс по таймеру.</summary>
        void ApplyLightGravityEffect(float divider, float duration);
    }
}
