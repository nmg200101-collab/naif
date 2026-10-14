using UnityEngine;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Training
{
    [RequireComponent(typeof(Collider))]
    public class LessonStopZone : MonoBehaviour
    {
        public DrivingLessonManager lesson;

        void Reset()
        {
            var c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            SetState(other, true);
        }

        void OnTriggerStay(Collider other)
        {
            SetState(other, true);
        }

        void OnTriggerExit(Collider other)
        {
            SetState(other, false);
        }

        void SetState(Collider other, bool inside)
        {
            var car = other.GetComponentInParent<RealisticCarController>();
            if (car == null || lesson == null || car != lesson.car) return;

            lesson.SetInsideStopZone(inside);
        }
    }
}
