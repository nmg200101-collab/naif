using UnityEngine;

namespace RealDrivingAcademy.Vehicle
{
    public class CarInputState : MonoBehaviour
    {
        [Range(-1f, 1f)] public float steering;
        [Range(0f, 1f)] public float throttle;
        [Range(0f, 1f)] public float brake;
        [Range(0f, 1f)] public float clutch;
        public bool handbrake;

        public bool useKeyboardForPrototype = true;
        public float steeringResponse = 5f;
        public float pedalResponse = 8f;

        float targetSteer;
        float targetThrottle;
        float targetBrake;
        float targetClutch;

        void Update()
        {
            if (!useKeyboardForPrototype || Application.isMobilePlatform) return;

            targetSteer = Input.GetAxisRaw("Horizontal");
            targetThrottle = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f;
            targetBrake = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f;
            targetClutch = Input.GetKey(KeyCode.LeftShift) ? 1f : 0f;
            handbrake = Input.GetKey(KeyCode.Space);

            steering = Mathf.MoveTowards(steering, targetSteer, steeringResponse * Time.deltaTime);
            throttle = Mathf.MoveTowards(throttle, targetThrottle, pedalResponse * Time.deltaTime);
            brake = Mathf.MoveTowards(brake, targetBrake, pedalResponse * Time.deltaTime);
            clutch = Mathf.MoveTowards(clutch, targetClutch, pedalResponse * Time.deltaTime);
        }

        // Mobile UI hooks
        public void SetSteering(float value) => steering = Mathf.Clamp(value, -1f, 1f);
        public void SetThrottle(float value) => throttle = Mathf.Clamp01(value);
        public void SetBrake(float value) => brake = Mathf.Clamp01(value);
        public void SetClutch(float value) => clutch = Mathf.Clamp01(value);
        public void SetHandbrake(bool value) => handbrake = value;
    }
}
