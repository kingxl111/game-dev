using UnityEngine;
using ReactorBreach.ScriptableObjects;

namespace ReactorBreach.Tools
{
    public interface ITool
    {
        string ToolName { get; }
        Sprite Icon { get; }
        bool CanUse { get; }
        void OnEquip();
        void OnUnequip();
        void OnPrimaryAction();
        void OnSecondaryAction();
        void Tick(float deltaTime);
    }
}
