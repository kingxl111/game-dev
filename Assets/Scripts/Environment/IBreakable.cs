using UnityEngine;

namespace ReactorBreach.Environment
{
    public interface IBreakable
    {
        float BreakThreshold { get; }
        void Break(Vector3 impactPoint, float force);
    }
}
