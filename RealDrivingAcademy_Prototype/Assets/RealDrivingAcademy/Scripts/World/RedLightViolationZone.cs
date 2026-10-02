using UnityEngine;
using RealDrivingAcademy.Training;

namespace RealDrivingAcademy.World
{
    [RequireComponent(typeof(Collider))]
    public class RedLightViolationZone : MonoBehaviour
    {
        public TrafficLightController trafficLight;
        public int penalty = 10;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (trafficLight == null) return;
            if (trafficLight.state != TrafficLightController.State.Red &&
                trafficLight.state != TrafficLightController.State.RedYellow) return;

            var violations = other.GetComponentInParent<DrivingViolationSystem>();
            if (violations != null)
                violations.AddViolation(ViolationType.RedLight, penalty, "تجاوزت الإشارة الحمراء.");
        }
    }
}
