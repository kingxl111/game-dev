using UnityEngine;

namespace ReactorBreach.Environment
{
    public interface IWeldable
    {
        Rigidbody WeldRigidbody { get; }
        Transform WeldTransform { get; }
        bool CanBeWelded { get; }
        void OnWelded(Tools.WeldConnection connection);
        void OnWeldBroken(Tools.WeldConnection connection);
    }
}
