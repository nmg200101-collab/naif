using UnityEngine;

namespace RDA.V50.Input
{
    public sealed class RDAInputState : MonoBehaviour
    {
        public float Steering { get; private set; }
        public float Throttle { get; private set; }
        public float Brake { get; private set; }
        public float Clutch { get; private set; }
        public bool Handbrake { get; private set; }

        public void SetSteering(float value) => Steering = Mathf.Clamp(value, -1f, 1f);
        public void SetThrottle(float value) => Throttle = Mathf.Clamp01(value);
        public void SetBrake(float value) => Brake = Mathf.Clamp01(value);
        public void SetClutch(float value) => Clutch = Mathf.Clamp01(value);
        public void SetHandbrake(bool value) => Handbrake = value;

        public void ResetAll()
        {
            Steering = Throttle = Brake = Clutch = 0f;
            Handbrake = false;
        }
    }
}