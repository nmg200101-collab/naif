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

            lesson.MarkCheckpoint(checkpointId);
            consumed = true;
        }
    }
}
