using UnityEngine;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Cockpit
{
    public class DashboardNeedle : MonoBehaviour
    {
        public enum Gauge { Speed, Rpm }
        public Gauge gauge;
        public RealisticCarController car;
        public Transform needle;
        public float minValue = 0f;
        public float maxValue = 220f;
        public float minAngle = 130f;
        public float maxAngle = -130f;
        public Vector3 localAxis = Vector3.forward;
        Quaternion baseRotation;

        void Awake()
        {
            if (needle != null) baseRotation = needle.localRotation;
        }

        void LateUpdate()
        {
            if (car == null || needle == null) return;
            float value = gauge == Gauge.Speed ? car.speedKph : car.engineRpm;
            float t = Mathf.InverseLerp(minValue, maxValue, value);
            float angle = Mathf.Lerp(minAngle, maxAngle, t);
            needle.localRotation = baseRotation * Quaternion.AngleAxis(angle, localAxis.normalized);
        }
    }
}
