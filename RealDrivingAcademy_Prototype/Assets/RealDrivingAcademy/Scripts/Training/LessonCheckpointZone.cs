using UnityEngine;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Training
{
    [RequireComponent(typeof(Collider))]
    public class LessonCheckpointZone : MonoBehaviour
    {
        public DrivingLessonManager lesson;
        public string checkpointId = "T3_Checkpoint_01";
        public bool consumeOnce = true;
        public bool requireLeftIndicator = true;
        public int missingIndicatorPenalty = 3;
        public DrivingViolationSystem violations;

        bool consumed;

        void Reset()
        {
            var c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (consumeOnce && consumed) return;

            var car = other.GetComponentInParent<RealisticCarController>();
            if (car == null || lesson == null || car != lesson.car) return;

            if (violations == null)
                violations = car.GetComponent<DrivingViolationSystem>();

            if (requireLeftIndicator &&
                lesson.systems != null &&
                !lesson.systems.leftIndicator &&
                !lesson.systems.hazardLights &&
                violations != null)
            {
                violations.AddViolation(
                    ViolationType.NoIndicator,
                    missingIndicatorPenalty,
                    "اعبر نقطة التدريب بعد تشغيل الإشارة المناسبة.",
                    2f);
            }

            lesson.MarkCheckpoint(checkpointId);
            consumed = true;
        }
    }
}
