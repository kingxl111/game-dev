using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ReactorBreach.Player;
using ReactorBreach.Tools;

namespace ReactorBreach.UI
{
    /// <summary>
    /// Отображает иконку активного инструмента, энергию/перезарядку/запас.
    /// </summary>
    public class ToolIndicator : MonoBehaviour
    {
        [System.Serializable]
        public struct SlotUI
        {
            public Image  IconImage;
            public Slider ResourceSlider;   // энергия, кулдаун, запас
            public TextMeshProUGUI Label;
            public GameObject ActiveFrame;
        }

        [SerializeField] private SlotUI[] _slots;   // 0=Weld, 1=Gravity, 2=Foam

        private ToolManager _toolManager;

        private void Start()
        {
            _toolManager = PlayerController.Instance?.GetComponent<ToolManager>();
            if (_toolManager == null)
                _toolManager = FindFirstObjectByType<ToolManager>();

            if (_toolManager != null)
                _toolManager.OnToolSwitched += OnToolSwitched;

            OnToolSwitched(0);
        }

        private void OnDestroy()
        {
            if (_toolManager != null)
                _toolManager.OnToolSwitched -= OnToolSwitched;
        }

        private void Update()
        {
            if (_toolManager == null) return;

            UpdateSlot(0);
            UpdateSlot(1);
            UpdateSlot(2);
        }

        private void OnToolSwitched(int index)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].ActiveFrame != null)
                    _slots[i].ActiveFrame.SetActive(i == index);
            }
        }

        private void UpdateSlot(int i)
        {
            if (i >= _slots.Length) return;
            var slot = _slots[i];
            var tool = _toolManager.GetTool(i);

            if (i == 0 && tool is WeldTool weld)
            {
                if (slot.ResourceSlider != null)
                    slot.ResourceSlider.value = weld.CurrentEnergy / weld.MaxEnergy;
                if (slot.Label != null)
                    slot.Label.text = $"{weld.CurrentEnergy:F0}/{weld.MaxEnergy:F0}";
            }
            else if (i == 1 && tool is GravityTool gravity)
            {
                bool ready = gravity.CanUse;
                if (slot.ResourceSlider != null)
                    slot.ResourceSlider.value = ready ? 1f : 1f - gravity.CooldownTimer / gravity.Cooldown;
                if (slot.Label != null)
                    slot.Label.text = ready ? "READY" : $"{gravity.CooldownTimer:F1}s";
            }
            else if (i == 2 && tool is FoamTool foam)
            {
                if (slot.ResourceSlider != null)
                    slot.ResourceSlider.value = (float)foam.RemainingUses / foam.MaxUses;
                if (slot.Label != null)
                    slot.Label.text = $"{foam.RemainingUses}/{foam.MaxUses}";
            }
        }
    }
}
