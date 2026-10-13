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

        float keyboardSteering;
        float keyboardThrottle;
        float keyboardBrake;
        float keyboardClutch;

        float uiSteering;
        float uiThrottle;
        float uiBrake;
        float uiClutch;

        void Update()
        {
            UpdateKeyboardInput();
            MergeInputs();
        }

        void UpdateKeyboardInput()
        {
            float targetSteer = 0f;
            float targetThrottle = 0f;
            float targetBrake = 0f;
            float targetClutch = 0f;

            if (useKeyboardForPrototype && !Application.isMobilePlatform)
            {
                targetSteer = Input.GetAxisRaw("Horizontal");
                targetThrottle = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f;
                targetBrake = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f;
                targetClutch = Input.GetKey(KeyCode.LeftShift) ? 1f : 0f;

                if (Input.GetKeyDown(KeyCode.Space))
                    handbrake = !handbrake;
            }

            keyboardSteering = Mathf.MoveTowards(
                keyboardSteering,
                targetSteer,
                steeringResponse * Time.deltaTime);

            keyboardThrottle = Mathf.MoveTowards(
                keyboardThrottle,
                targetThrottle,
                pedalResponse * Time.deltaTime);

            keyboardBrake = Mathf.MoveTowards(
                keyboardBrake,
                targetBrake,
                pedalResponse * Time.deltaTime);

            keyboardClutch = Mathf.MoveTowards(
                keyboardClutch,
                targetClutch,
                pedalResponse * Time.deltaTime);
        }

        void MergeInputs()
        {
            steering = Mathf.Abs(uiSteering) > 0.001f
                ? uiSteering
                : keyboardSteering;

            throttle = Mathf.Max(uiThrottle, keyboardThrottle);
            brake = Mathf.Max(uiBrake, keyboardBrake);
            clutch = Mathf.Max(uiClutch, keyboardClutch);
        }

        // UI / mobile hooks. These no longer get overwritten by keyboard polling.
        public void SetSteering(float value)
        {
            uiSteering = Mathf.Clamp(value, -1f, 1f);
        }

        public void SetThrottle(float value)
        {
            uiThrottle = Mathf.Clamp01(value);
        }

        public void SetBrake(float value)
        {
            uiBrake = Mathf.Clamp01(value);
        }

        public void SetClutch(float value)
        {
            uiClutch = Mathf.Clamp01(value);
        }

        public void SetHandbrake(bool value)
        {
            handbrake = value;
        }

        public void ResetDrivingInputs(bool engageHandbrake = true)
        {
            keyboardSteering = 0f;
            keyboardThrottle = 0f;
            keyboardBrake = 0f;
            keyboardClutch = 0f;

            uiSteering = 0f;
            uiThrottle = 0f;
            uiBrake = 0f;
            uiClutch = 0f;

            steering = 0f;
            throttle = 0f;
            brake = 0f;
            clutch = 0f;
            handbrake = engageHandbrake;
        }
    }
}
