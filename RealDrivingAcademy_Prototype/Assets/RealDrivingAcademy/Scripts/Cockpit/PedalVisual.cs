using UnityEngine;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Cockpit
{
    public class PedalVisual : MonoBehaviour
    {
        public enum PedalType { Throttle, Brake, Clutch }
        public PedalType pedalType;
        public CarInputState input;
        public Transform pedal;
        public Vector3 localAxis = Vector3.right;
        public float travelDegrees = 18f;
        public float smooth = 12f;
        Quaternion baseRotation;
        float visualValue;

        void Awake()
        {
            if (pedal != null) baseRotation = pedal.localRotation;
        }

        void LateUpdate()
        {
            if (input == null || pedal == null) return;
            float target = pedalType == PedalType.Throttle ? input.throttle : pedalType == PedalType.Brake ? input.brake : input.clutch;
            visualValue = Mathf.Lerp(visualValue, target, smooth * Time.deltaTime);
            pedal.localRotation = baseRotation * Quaternion.AngleAxis(visualValue * travelDegrees, localAxis.normalized);
        }
    }
}
