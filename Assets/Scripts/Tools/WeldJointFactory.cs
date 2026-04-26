using UnityEngine;
using ReactorBreach.Environment;
using ReactorBreach.ScriptableObjects;

namespace ReactorBreach.Tools
{
    public static class WeldJointFactory
    {
        /// <summary>
        /// Стандартное соединение через FixedJoint (хотя бы один из объектов имеет Rigidbody).
        /// </summary>
        public static WeldConnection CreateJoint(
            IWeldable a, Vector3 pointA,
            IWeldable b, Vector3 pointB,
            ToolConfig config, float duration = -1f)
        {
            // Предпочитаем прикреплять Joint к подвижному объекту
            Rigidbody rbA = a.WeldRigidbody;
            Rigidbody rbB = b.WeldRigidbody;

            FixedJoint joint;

            if (rbA != null)
            {
                joint = rbA.gameObject.AddComponent<FixedJoint>();
                joint.connectedBody  = rbB;
                joint.anchor         = rbA.transform.InverseTransformPoint(pointA);
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = rbB != null
                    ? rbB.transform.InverseTransformPoint(pointB)
                    : pointB;
            }
            else
            {
                // rbA is null, rbB must exist
                joint = rbB.gameObject.AddComponent<FixedJoint>();
                joint.connectedBody  = null;
                joint.anchor         = rbB.transform.InverseTransformPoint(pointB);
            }

            joint.breakForce  = config.WeldBreakForce;
            joint.breakTorque = config.WeldBreakForce;

            float lifetime = duration > 0f ? duration : config.WeldDuration;
            return new WeldConnection(joint, a, b, lifetime);
        }

        /// <summary>
        /// Создаёт физический мост-перекладину между двумя статическими точками.
        /// </summary>
        public static WeldConnection CreateBridge(
            Vector3 pointA, Vector3 pointB,
            IWeldable a, IWeldable b,
            ToolConfig config, float duration = -1f)
        {
            float dist   = Vector3.Distance(pointA, pointB);
            Vector3 mid  = (pointA + pointB) * 0.5f;
            Vector3 dir  = (pointB - pointA).normalized;

            var bridge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bridge.name = "WeldBridge";
            bridge.layer = ReactorBreach.Data.GameConstants.LayerWeldBridge;

            bridge.transform.position   = mid;
            bridge.transform.up         = dir;
            bridge.transform.localScale = new Vector3(0.05f, dist * 0.5f, 0.05f);

            var rb = bridge.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            // Применяем материал свечения если есть в Resources
            var weldMat = Resources.Load<Material>("WeldGlow");
            if (weldMat != null)
                bridge.GetComponent<Renderer>().material = weldMat;

            float lifetime = duration > 0f ? duration : config.WeldDuration;
            return new WeldConnection(bridge, a, b, lifetime);
        }
    }
}
