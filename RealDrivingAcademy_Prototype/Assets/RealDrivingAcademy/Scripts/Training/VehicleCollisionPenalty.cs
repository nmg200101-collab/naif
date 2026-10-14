using UnityEngine;

namespace RealDrivingAcademy.Training
{
    public class VehicleCollisionPenalty : MonoBehaviour
    {
        public DrivingViolationSystem violations;
        public float minimumImpactSpeed = 2.5f;
        public int penalty = 5;
        public float cooldownSeconds = 2.5f;

        void OnCollisionEnter(Collision collision)
        {
            if (violations == null) return;
            if (collision.relativeVelocity.magnitude < minimumImpactSpeed) return;

            violations.AddViolation(
                ViolationType.Collision,
                penalty,
                "حدث تصادم أثناء التدريب.",
                cooldownSeconds);
        }
    }
}
