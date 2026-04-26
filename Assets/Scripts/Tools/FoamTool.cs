using UnityEngine;
using ReactorBreach.ScriptableObjects;
using ReactorBreach.Data;

namespace ReactorBreach.Tools
{
    public class FoamTool : ToolBase
    {
        private int _remainingUses;
        public int RemainingUses => _remainingUses;
        public int MaxUses       => Config.FoamUsesPerLevel;

        public override bool CanUse => _remainingUses > 0;

        public FoamTool(ToolConfig config, Transform mount) : base(config, mount)
        {
            _remainingUses = config.FoamUsesPerLevel;
        }

        public void InitForLevel()
        {
            _remainingUses = Config.FoamUsesPerLevel;
        }

        public void AddUses(int count)
        {
            _remainingUses += count;
        }

        public override void OnPrimaryAction()
        {
            if (!TryRaycast(Config.FoamRange, out var hit)) return;
            if (!hit.collider.CompareTag(GameConstants.TagSurface)) return;

            _remainingUses--;

            var zonePrefab = Resources.Load<GameObject>("FoamZone");
            GameObject zoneGO;

            if (zonePrefab != null)
            {
                zoneGO = Object.Instantiate(zonePrefab, hit.point, Quaternion.identity);
            }
            else
            {
                zoneGO = new GameObject("FoamZone");
                zoneGO.transform.position = hit.point;
                zoneGO.AddComponent<Environment.FoamZone>();
                var col = zoneGO.AddComponent<SphereCollider>();
                col.isTrigger = true;
            }
            if (zoneGO.TryGetComponent<Environment.FoamZone>(out var zone))
            {
                zone.Init(Config.FoamRadius,
                          Config.FoamDuration,
                          Config.FoamSlowMultiplier,
                          Config.StickVelocityThreshold,
                          hit.normal);
            }

            Enemies.VibrationSystem.Emit(hit.point, GameConstants.VibrationRadiusFoam);
        }

        public override void Tick(float deltaTime) { }
    }
}
