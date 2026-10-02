using UnityEngine;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Cockpit
{
    public class SteeringWheelVisual : MonoBehaviour
    {
        public CarInputState input;
        public Transform steeringWheel;
        public Vector3 localRotationAxis = Vector3.forward;
        public float steeringWheelDegrees = 900f;
        Quaternion baseRotation;

        void Awake()
        {
            if (steeringWheel != null) baseRotation = steeringWheel.localRotation;
        }

        void LateUpdate()
        {
            if (input == null || steeringWheel == null) return;
            float degrees = -input.steering * steeringWheelDegrees * 0.5f;
            steeringWheel.localRotation = baseRotation * Quaternion.AngleAxis(degrees, localRotationAxis.normalized);
        }
    }
}
