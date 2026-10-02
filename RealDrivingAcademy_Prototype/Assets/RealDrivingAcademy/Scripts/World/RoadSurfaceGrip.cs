using UnityEngine;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.World
{
    [RequireComponent(typeof(Collider))]
    public class RoadSurfaceGrip : MonoBehaviour
    {
        public enum Surface { DryAsphalt, WetAsphalt, Gravel, Dirt }
        public Surface surface = Surface.DryAsphalt;

        [System.Serializable]
        public struct GripPreset
        {
            public float forwardStiffness;
            public float sidewaysStiffness;
        }

        public GripPreset dry = new GripPreset { forwardStiffness = 1.55f, sidewaysStiffness = 1.75f };
        public GripPreset wet = new GripPreset { forwardStiffness = 1.05f, sidewaysStiffness = 1.18f };
        public GripPreset gravel = new GripPreset { forwardStiffness = 0.78f, sidewaysStiffness = 0.82f };
        public GripPreset dirt = new GripPreset { forwardStiffness = 0.68f, sidewaysStiffness = 0.72f };

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            var car = other.GetComponentInParent<RealisticCarController>();
            if (car == null) return;
            Apply(car.frontAxle.leftCollider); Apply(car.frontAxle.rightCollider);
            Apply(car.rearAxle.leftCollider); Apply(car.rearAxle.rightCollider);
        }

        void Apply(WheelCollider wheel)
        {
            if (wheel == null) return;
            GripPreset p = GetPreset();
            var f = wheel.forwardFriction; f.stiffness = p.forwardStiffness; wheel.forwardFriction = f;
            var s = wheel.sidewaysFriction; s.stiffness = p.sidewaysStiffness; wheel.sidewaysFriction = s;
        }

        GripPreset GetPreset()
        {
            switch (surface)
            {
                case Surface.WetAsphalt: return wet;
                case Surface.Gravel: return gravel;
                case Surface.Dirt: return dirt;
                default: return dry;
            }
        }
    }
}
