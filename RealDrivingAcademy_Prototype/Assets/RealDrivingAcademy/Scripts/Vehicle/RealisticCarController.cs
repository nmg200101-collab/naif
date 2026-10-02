using UnityEngine;

namespace RealDrivingAcademy.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CarInputState))]
    [RequireComponent(typeof(CarSystems))]
    public class RealisticCarController : MonoBehaviour
    {
        [System.Serializable]
        public class Axle
        {
            public WheelCollider leftCollider;
            public WheelCollider rightCollider;
            public Transform leftMesh;
            public Transform rightMesh;
            public bool steering;
            public bool powered;
            public bool handbrake;
        }

        public VehicleConfig config;
        public Axle frontAxle;
        public Axle rearAxle;

        [Header("Runtime")]
        public int selectedGear = 1; // -1 Reverse, 0 Neutral, 1..N forward
        public float engineRpm;
        public float speedKph;
        public bool engineStalled;

        Rigidbody rb;
        CarInputState input;
        CarSystems systems;

        public bool EngineRunning => systems != null && systems.ignitionOn && !engineStalled;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            input = GetComponent<CarInputState>();
            systems = GetComponent<CarSystems>();

            if (config != null)
            {
                rb.mass = config.mass;
                rb.centerOfMass = config.centerOfMass;
                engineRpm = config.idleRpm;
            }
        }

        void FixedUpdate()
        {
            if (config == null) return;

            speedKph = rb.velocity.magnitude * 3.6f;
            UpdateEngineAndTransmission();
            ApplySteering();
            ApplyDriveTorque();
            ApplyBrakes();
            UpdateWheelVisuals(frontAxle);
            UpdateWheelVisuals(rearAxle);
        }

        void UpdateEngineAndTransmission()
        {
            float drivenRpm = Mathf.Abs(GetAverageDrivenWheelRpm());
            float gearRatio = Mathf.Abs(GetCurrentGearRatio());
            float clutchEngagement = 1f - input.clutch;

            if (!systems.ignitionOn)
            {
                engineRpm = Mathf.MoveTowards(engineRpm, 0f, 2200f * Time.fixedDeltaTime);
                return;
            }

            if (engineStalled)
            {
                engineRpm = 0f;
                return;
            }

            float wheelCoupledRpm = drivenRpm * gearRatio * config.finalDriveRatio;
            float freeRevTarget = Mathf.Lerp(config.idleRpm, config.redlineRpm, input.throttle);
            float targetRpm = selectedGear == 0
                ? freeRevTarget
                : Mathf.Lerp(freeRevTarget, Mathf.Max(config.idleRpm, wheelCoupledRpm), clutchEngagement);

            engineRpm = Mathf.Lerp(engineRpm, targetRpm, 7f * Time.fixedDeltaTime);
            engineRpm = Mathf.Clamp(engineRpm, 0f, config.redlineRpm);

            if (!config.automaticTransmission && selectedGear != 0 && clutchEngagement > 0.85f && speedKph < 5f && engineRpm < 520f)
            {
                engineStalled = true;
                systems.ignitionOn = false;
            }

            if (config.automaticTransmission && selectedGear > 0)
            {
                if (engineRpm > config.autoUpshiftRpm && selectedGear < config.forwardGearRatios.Length)
                    selectedGear++;
                else if (engineRpm < config.autoDownshiftRpm && selectedGear > 1)
                    selectedGear--;
            }
        }

        void ApplySteering()
        {
            float speed01 = Mathf.Clamp01(speedKph / Mathf.Max(1f, config.maxSpeedKph));
            float assist = Mathf.Lerp(1f, config.steeringAssistAtTopSpeed, speed01);
            float steer = input.steering * config.maxSteerAngle * assist;

            if (frontAxle.steering)
            {
                frontAxle.leftCollider.steerAngle = steer;
                frontAxle.rightCollider.steerAngle = steer;
            }
            if (rearAxle.steering)
            {
                rearAxle.leftCollider.steerAngle = steer;
                rearAxle.rightCollider.steerAngle = steer;
            }
        }

        void ApplyDriveTorque()
        {
            ClearMotorTorque(frontAxle);
            ClearMotorTorque(rearAxle);
            if (!EngineRunning || selectedGear == 0 || speedKph >= config.maxSpeedKph) return;

            float gearRatio = GetCurrentGearRatio();
            float rpm01 = Mathf.Clamp01(engineRpm / config.redlineRpm);
            float torqueCurve = config.torqueCurve.Evaluate(rpm01);
            float clutchEngagement = config.automaticTransmission ? 1f : (1f - input.clutch);
            float engineTorque = config.maxEngineTorque * torqueCurve * input.throttle * clutchEngagement;
            float wheelTorque = engineTorque * gearRatio * config.finalDriveRatio * config.drivetrainEfficiency;

            int poweredWheels = CountPoweredWheels();
            if (poweredWheels == 0) return;
            float each = wheelTorque / poweredWheels;

            ApplyAxleMotorTorque(frontAxle, each);
            ApplyAxleMotorTorque(rearAxle, each);
        }

        void ApplyBrakes()
        {
            ApplyWheelBrake(frontAxle.leftCollider, input.brake, false, frontAxle.handbrake);
            ApplyWheelBrake(frontAxle.rightCollider, input.brake, false, frontAxle.handbrake);
            ApplyWheelBrake(rearAxle.leftCollider, input.brake, true, rearAxle.handbrake);
            ApplyWheelBrake(rearAxle.rightCollider, input.brake, true, rearAxle.handbrake);
        }

        void ApplyWheelBrake(WheelCollider wheel, float serviceInput, bool isRear, bool handbrakeAxle)
        {
            if (wheel == null) return;
            float brake = serviceInput * config.serviceBrakeTorque;

            if (config.absEnabled && serviceInput > 0.1f && wheel.GetGroundHit(out WheelHit hit))
            {
                if (Mathf.Abs(hit.forwardSlip) > config.absSlipThreshold)
                    brake *= 0.35f;
            }

            if (input.handbrake && handbrakeAxle)
                brake = Mathf.Max(brake, config.handbrakeTorque);

            wheel.brakeTorque = brake;
        }

        void ApplyAxleMotorTorque(Axle axle, float torque)
        {
            if (!axle.powered) return;
            axle.leftCollider.motorTorque = TractionAdjustedTorque(axle.leftCollider, torque);
            axle.rightCollider.motorTorque = TractionAdjustedTorque(axle.rightCollider, torque);
        }

        float TractionAdjustedTorque(WheelCollider wheel, float torque)
        {
            if (!config.tractionControlEnabled || !wheel.GetGroundHit(out WheelHit hit)) return torque;
            float slip = Mathf.Abs(hit.forwardSlip);
            if (slip <= config.tractionSlipThreshold) return torque;
            return torque * Mathf.Clamp01(config.tractionSlipThreshold / Mathf.Max(0.001f, slip));
        }

        void ClearMotorTorque(Axle axle)
        {
            if (axle.leftCollider != null) axle.leftCollider.motorTorque = 0f;
            if (axle.rightCollider != null) axle.rightCollider.motorTorque = 0f;
        }

        int CountPoweredWheels()
        {
            int count = 0;
            if (frontAxle.powered) count += 2;
            if (rearAxle.powered) count += 2;
            return count;
        }

        float GetAverageDrivenWheelRpm()
        {
            float total = 0f;
            int count = 0;
            if (frontAxle.powered) { total += frontAxle.leftCollider.rpm + frontAxle.rightCollider.rpm; count += 2; }
            if (rearAxle.powered) { total += rearAxle.leftCollider.rpm + rearAxle.rightCollider.rpm; count += 2; }
            return count == 0 ? 0f : total / count;
        }

        float GetCurrentGearRatio()
        {
            if (selectedGear < 0) return config.reverseGearRatio;
            if (selectedGear == 0) return 0f;
            int index = Mathf.Clamp(selectedGear - 1, 0, config.forwardGearRatios.Length - 1);
            return config.forwardGearRatios[index];
        }

        public void ShiftUp()
        {
            if (config.automaticTransmission) return;
            selectedGear = Mathf.Min(selectedGear + 1, config.forwardGearRatios.Length);
        }

        public void ShiftDown()
        {
            if (config.automaticTransmission) return;
            selectedGear = Mathf.Max(selectedGear - 1, -1);
        }

        public void SetGear(int gear)
        {
            if (config.automaticTransmission)
            {
                selectedGear = Mathf.Clamp(gear, -1, 1);
                return;
            }
            selectedGear = Mathf.Clamp(gear, -1, config.forwardGearRatios.Length);
        }

        public void RestartEngineAfterStall()
        {
            if (!engineStalled) return;
            if (!input.brake.Equals(0f) || input.clutch > 0.8f || selectedGear == 0)
            {
                engineStalled = false;
                systems.ignitionOn = true;
                engineRpm = config.idleRpm;
            }
        }

        void UpdateWheelVisuals(Axle axle)
        {
            UpdateWheel(axle.leftCollider, axle.leftMesh);
            UpdateWheel(axle.rightCollider, axle.rightMesh);
        }

        void UpdateWheel(WheelCollider collider, Transform mesh)
        {
            if (collider == null || mesh == null) return;
            collider.GetWorldPose(out Vector3 p, out Quaternion r);
            mesh.SetPositionAndRotation(p, r);
        }
    }
}
