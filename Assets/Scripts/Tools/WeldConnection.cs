using UnityEngine;
using ReactorBreach.Environment;
using ReactorBreach.ScriptableObjects;

namespace ReactorBreach.Tools
{
    /// <summary>
    /// Данные об одном активном сварном соединении.
    /// </summary>
    public class WeldConnection
    {
        public Joint Joint { get; }
        public GameObject Bridge { get; }   // для случая Static+Static
        public float RemainingTime { get; set; }
        public bool IsBroken => Joint == null && Bridge == null;

        private readonly IWeldable _targetA;
        private readonly IWeldable _targetB;

        public WeldConnection(Joint joint, IWeldable a, IWeldable b, float lifetime)
        {
            Joint         = joint;
            _targetA      = a;
            _targetB      = b;
            RemainingTime = lifetime;
        }

        public WeldConnection(GameObject bridge, IWeldable a, IWeldable b, float lifetime)
        {
            Bridge        = bridge;
            _targetA      = a;
            _targetB      = b;
            RemainingTime = lifetime;
        }

        public void Destroy()
        {
            if (Joint != null) Object.Destroy(Joint);

            // §19.1 — перекладина не уничтожается, а становится динамической и падает.
            if (Bridge != null && Bridge.TryGetComponent<Rigidbody>(out var brb))
            {
                brb.isKinematic = false;
                Object.Destroy(Bridge, 8f);
            }

            // Не зовём OnWeldBroken на уничтоженных объектах: для Unity-объектов
            // `?.` не учитывает fake-null, внутри OnWeldBroken падает TryGetComponent.
            SafeOnWeldBroken(_targetA);
            SafeOnWeldBroken(_targetB);
        }

        private void SafeOnWeldBroken(IWeldable w)
        {
            if (w == null) return;
            if (w is MonoBehaviour mb)
            {
                if (!mb) return;
            }
            w.OnWeldBroken(this);
        }
    }
}
