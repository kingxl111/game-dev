using ReactorBreach.Data;

namespace ReactorBreach.Enemies
{
    public interface IEnemy
    {
        float CurrentHP { get; }
        bool IsNeutralized { get; }
        void TakeDamage(float damage, DamageType type);
        void ApplySlow(float multiplier, float duration);
        void SetStuck(bool stuck);
    }
}
