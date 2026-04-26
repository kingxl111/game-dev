using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ReactorBreach.Tools;
using ReactorBreach.ScriptableObjects;

namespace ReactorBreach.Player
{
    public class ToolManager : MonoBehaviour
    {
        [SerializeField] private ToolConfig[] _toolConfigs;
        [SerializeField] private Transform _toolMount;

        private ITool[] _tools;
        private int _currentIndex;

        public ITool CurrentTool => _tools != null && _tools.Length > 0 ? _tools[_currentIndex] : null;
        public int CurrentIndex => _currentIndex;
        public int ToolCount => _tools?.Length ?? 0;

        public ITool GetTool(int index)
        {
            if (_tools == null || index < 0 || index >= _tools.Length) return null;
            return _tools[index];
        }

        public event Action<int> OnToolSwitched;

        private void Start()
        {
            if (_toolConfigs == null || _toolConfigs.Length < 3)
            {
                Debug.LogWarning("[ToolManager] Нужно 3 ToolConfig (Weld, Gravity, Foam).");
                return;
            }

            _tools = new ITool[3];
            _tools[0] = new WeldTool(_toolConfigs[0], _toolMount);
            _tools[1] = new GravityTool(_toolConfigs[1], _toolMount);
            _tools[2] = new FoamTool(_toolConfigs[2], _toolMount);

            SwitchTool(0);
        }

        public void SwitchTool(int index)
        {
            if (_tools == null) return;
            index = Mathf.Clamp(index, 0, _tools.Length - 1);

            _tools[_currentIndex]?.OnUnequip();
            _currentIndex = index;
            _tools[_currentIndex]?.OnEquip();

            OnToolSwitched?.Invoke(_currentIndex);
        }

        public void OnPrimaryAction(InputAction.CallbackContext ctx)
        {
            if (!ctx.started) return;
            if (CurrentTool == null || !CurrentTool.CanUse) return;
            CurrentTool.OnPrimaryAction();
        }

        public void OnSecondaryAction(InputAction.CallbackContext ctx)
        {
            if (!ctx.started) return;
            CurrentTool?.OnSecondaryAction();
        }

        /// <summary>
        /// Сбрасывает расходуемые инструменты при загрузке нового уровня (например, FoamTool).
        /// </summary>
        public void ResetForLevel()
        {
            if (_tools == null) return;
            foreach (var t in _tools)
                (t as FoamTool)?.InitForLevel();
        }
    }
}
