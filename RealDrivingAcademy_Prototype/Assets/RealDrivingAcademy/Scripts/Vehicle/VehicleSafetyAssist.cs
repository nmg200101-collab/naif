using UnityEngine;

namespace RealDrivingAcademy.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleSafetyAssist : MonoBehaviour
    {
        public RealisticCarController car;
        public float resetBelowY = -8f;
        public float upsideDownSecondsBeforeReset = 3f;
        public float safePoseRefreshSeconds = 0.75f;
        public float maxSpeedForSafePoseKph = 20f;

        Vector3 spawnPosition;
        Quaternion spawnRotation;
        Vector3 lastSafePosition;
        Quaternion lastSafeRotation;

        Rigidbody rb;
        CarInputState input;
        CarSystems systems;

        float upsideDownTimer;
        float safePoseTimer;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            input = GetComponent<CarInputState>();
            systems = GetComponent<CarSystems>();

            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            lastSafePosition = spawnPosition;
            lastSafeRotation = spawnRotation;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
                ResetVehicle();

            if (transform.position.y < resetBelowY)
            {
                ResetVehicle();
                return;
            }

            bool upsideDown = Vector3.Dot(transform.up, Vector3.up) < 0.2f;
            if (upsideDown)
            {
                upsideDownTimer += Time.deltaTime;
                if (upsideDownTimer >= upsideDownSecondsBeforeReset)
                {
                    ResetVehicle();
                    return;
                }
            }
            else
            {
                upsideDownTimer = 0f;
            }

            safePoseTimer += Time.deltaTime;
            if (safePoseTimer >= safePoseRefreshSeconds)
            {
                safePoseTimer = 0f;
                TryCaptureSafePose();
            }
        }

        void TryCaptureSafePose()
        {
            if (car == null) return;
            if (car.speedKph > maxSpeedForSafePoseKph) return;
            if (Vector3.Dot(transform.up, Vector3.up) < 0.75f) return;
            if (!HasGroundContact()) return;

            lastSafePosition = transform.position;
            Vector3 forwardFlat = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (forwardFlat.sqrMagnitude < 0.01f) forwardFlat = Vector3.forward;
            lastSafeRotation = Quaternion.LookRotation(forwardFlat, Vector3.up);
        }

        bool HasGroundContact()
        {
            if (car == null) return false;

            return IsGrounded(car.frontAxle != null ? car.frontAxle.leftCollider : null)
                || IsGrounded(car.frontAxle != null ? car.frontAxle.rightCollider : null)
                || IsGrounded(car.rearAxle != null ? car.rearAxle.leftCollider : null)
                || IsGrounded(car.rearAxle != null ? car.rearAxle.rightCollider : null);
        }

        static bool IsGrounded(WheelCollider wheel)
        {
            return wheel != null && wheel.isGrounded;
        }

        public void ResetVehicle()
        {
            Vector3 targetPosition = lastSafePosition == Vector3.zero ? spawnPosition : lastSafePosition;
            Quaternion targetRotation = lastSafeRotation == default ? spawnRotation : lastSafeRotation;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = targetPosition + Vector3.up * 0.55f;
            rb.rotation = targetRotation;

            if (input != null)
                input.ResetDrivingInputs(true);

            if (car != null)
                car.SetGear(0);

            if (systems != null)
                systems.CancelIndicators();

            upsideDownTimer = 0f;
        }
    }
}
