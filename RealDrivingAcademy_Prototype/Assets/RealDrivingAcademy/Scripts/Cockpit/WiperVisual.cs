using UnityEngine;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Cockpit
{
    public class WiperVisual : MonoBehaviour
    {
        public CarSystems systems;
        public Transform leftWiper;
        public Transform rightWiper;
        public float sweepDegrees = 70f;
        public float speed1 = 1.6f;
        public float speed2 = 3.2f;
        float phase;
        Quaternion leftBase, rightBase;

        void Awake()
        {
            if (leftWiper != null) leftBase = leftWiper.localRotation;
            if (rightWiper != null) rightBase = rightWiper.localRotation;
        }

        void LateUpdate()
        {
            if (systems == null) return;
            if (systems.wiperSpeed == 0)
            {
                if (leftWiper != null) leftWiper.localRotation = Quaternion.Slerp(leftWiper.localRotation, leftBase, 8f * Time.deltaTime);
                if (rightWiper != null) rightWiper.localRotation = Quaternion.Slerp(rightWiper.localRotation, rightBase, 8f * Time.deltaTime);
                return;
            }
            phase += Time.deltaTime * (systems.wiperSpeed == 1 ? speed1 : speed2);
            float t = Mathf.PingPong(phase, 1f);
            float a = Mathf.Lerp(-8f, sweepDegrees, t);
            if (leftWiper != null) leftWiper.localRotation = leftBase * Quaternion.Euler(0f, 0f, a);
            if (rightWiper != null) rightWiper.localRotation = rightBase * Quaternion.Euler(0f, 0f, a);
        }
    }
}
