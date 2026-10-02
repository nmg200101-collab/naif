using UnityEngine;
using RealDrivingAcademy.Training;

namespace RealDrivingAcademy.World
{
    [RequireComponent(typeof(Collider))]
    public class SpeedLimitZone : MonoBehaviour
    {
        public float speedLimitKph = 50f;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            var system = other.GetComponentInParent<DrivingViolationSystem>();
            if (system != null) system.SetSpeedLimit(speedLimitKph);
        }
    }
}
