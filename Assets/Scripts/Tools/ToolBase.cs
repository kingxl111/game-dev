using UnityEngine;
using ReactorBreach.ScriptableObjects;

namespace ReactorBreach.Tools
{
    /// <summary>
    /// Абстрактный базовый класс инструмента. Не MonoBehaviour —
    /// инструменты существуют как plain C# objects, управляемые ToolManager.
    /// </summary>
    public abstract class ToolBase : ITool
    {
        protected readonly ToolConfig Config;
        protected readonly Transform ToolMount;

        private GameObject _viewModel;

        protected ToolBase(ToolConfig config, Transform toolMount)
        {
            Config    = config;
            ToolMount = toolMount;
        }

        public virtual string ToolName => Config != null ? Config.ToolName : "Unknown";
        public virtual Sprite Icon     => Config != null ? Config.Icon : null;
        public abstract bool CanUse { get; }

        public virtual void OnEquip()
        {
            if (Config?.ViewModelPrefab == null || ToolMount == null) return;
            _viewModel = Object.Instantiate(Config.ViewModelPrefab, ToolMount);
        }

        public virtual void OnUnequip()
        {
            if (_viewModel != null)
            {
                Object.Destroy(_viewModel);
                _viewModel = null;
            }
        }

        public abstract void OnPrimaryAction();

        public virtual void OnSecondaryAction() { }

        public abstract void Tick(float deltaTime);

        // ── Shared helpers ────────────────────────────────────────────────

        protected bool TryRaycast(float range, out RaycastHit hit)
        {
            Camera cam = Camera.main;
            if (cam == null) { hit = default; return false; }

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            return Physics.Raycast(ray, out hit, range);
        }

        protected void SpawnVFX(GameObject prefab, Vector3 position, Quaternion? rotation = null)
        {
            if (prefab == null) return;
            Object.Instantiate(prefab, position, rotation ?? Quaternion.identity);
        }
    }
}
