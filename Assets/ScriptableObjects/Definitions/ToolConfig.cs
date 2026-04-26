using UnityEngine;

namespace ReactorBreach.ScriptableObjects
{
    [CreateAssetMenu(menuName = "ReactorBreach/Tool Config")]
    public class ToolConfig : ScriptableObject
    {
        [Header("General")]
        public string ToolName;
        public Sprite Icon;
        public GameObject ViewModelPrefab;

        [Header("Weld Settings")]
        public float WeldRange       = 8f;
        public float WeldDuration    = 10f;
        public float WeldEnergyCost  = 25f;
        public float MaxEnergy       = 100f;
        public float EnergyRegenRate = 5f;
        public int   MaxActiveWelds  = 4;
        public float WeldBreakForce  = 50000f;

        [Header("Gravity Settings")]
        public float GravityRange     = 12f;
        public float MassMultiplier   = 30f;  // ЛКМ — ×N (плита/тяжелее)
        public float LightMassDivider = 30f;  // ПКМ — /N (легче, чтобы толкать)
        public float GravityDuration  = 4f;
        public float GravityCooldown  = 5f;
        public float MaxTargetVolume  = 1.5f;

        [Header("Foam Settings")]
        public float FoamRange               = 10f;
        public float FoamRadius              = 2f;
        public float FoamDuration            = 8f;
        public int   FoamUsesPerLevel        = 4;
        public float FoamSlowMultiplier      = 0.3f;
        public float StickVelocityThreshold  = 0.5f;
    }
}
