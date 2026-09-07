using System;
using UnityEngine;
using RDA.V50.Input;

namespace RDA.V50.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RDAVehicleController : MonoBehaviour
    {
        [Serializable]
        public sealed class Axle
        {
            public WheelCollider left;
            public WheelCollider right;
            public Transform leftVisual;
            public Transform rightVisual;
            public bool steer;
            public bool drive;
            public bool brake;
        }

        [Header("References")]
        [SerializeField] private RDAMobileInput input;
        [SerializeField] private Axle frontAxle = new Axle();
        [SerializeField] private Axle rearAxle = new Axle();

        [Header("Vehicle")]
        [SerializeField] private float massKg = 1420f;
        [SerializeField] private float maxSteerDeg = 34f;
        [SerializeField] private float wheelBase = 2.63f;
        [SerializeField] private float trackWidth = 1.55f;
        [SerializeField] private float maxBrakeTorque = 3600f;
        [SerializeField] private float maxHandbrakeTorque = 5200f;
        [SerializeField] private float aeroDrag = 0.34f;
        [SerializeField] private float downforce = 18f;

        [Header("Powertrain")]
        [SerializeField] private float maxEngineTorqueNm = 220f;
        [SerializeField] private float finalDrive = 3.9f;
        [SerializeField] private float idleRpm = 800f;
        [SerializeField] private float redlineRpm = 6500f;
        [SerializeField] private AnimationCurve torqueCurve = new AnimationCurve(
            new Keyframe(0f, .55f), new Keyframe(.35f, 1f), new Keyframe(.75f, .88f), new Keyframe(1f, .5f));
        [SerializeField] private float[] gearRatios = { -3.30f, 0f, 3.55f, 1.96f, 1.28f, .97f, .78f };
        [SerializeField] private int currentGear = 2;

        [Header("Assists")]
        [SerializeField] private bool abs = true;
        [SerializeField] private bool tcs = true;
        [SerializeField] private float absSlip = .32f;
        [SerializeField] private float tcsSlip = .28f;
        [SerializeField] private float engineBrakingNm = 70f;

        private Rigidbody rb;
        private float engineRpm;

        public float SpeedKph => rb ? rb.velocity.magnitude * 3.6f : 0f;
        public float EngineRpm => engineRpm;
        public int CurrentGear => currentGear;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.mass = massKg;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.centerOfMass = new Vector3(0f, -0.34f, 0.08f);
        }

        private void FixedUpdate()
        {
            if (!input) return;
            ApplyAckermann(input.steering);
            ApplyPowertrain(input.throttle, input.clutch);
            ApplyBrakes(input.brake, input.handbrake);
            ApplyAero();
            UpdateWheelVisuals(frontAxle);
            UpdateWheelVisuals(rearAxle);
        }

        private void ApplyAckermann(float steerInput)
        {
            float baseAngle = maxSteerDeg * Mathf.Clamp(steerInput, -1f, 1f);
            if (Mathf.Abs(baseAngle) < 0.01f)
            {
                frontAxle.left.steerAngle = 0f;
                frontAxle.right.steerAngle = 0f;
                return;
            }
            float turnRadius = wheelBase / Mathf.Tan(Mathf.Abs(baseAngle) * Mathf.Deg2Rad);
            float inner = Mathf.Atan(wheelBase / Mathf.Max(.1f, turnRadius - trackWidth * .5f)) * Mathf.Rad2Deg;
            float outer = Mathf.Atan(wheelBase / (turnRadius + trackWidth * .5f)) * Mathf.Rad2Deg;
            bool rightTurn = baseAngle > 0f;
            frontAxle.left.steerAngle = rightTurn ? outer : -inner;
            frontAxle.right.steerAngle = rightTurn ? inner : -outer;
        }

        private void ApplyPowertrain(float throttle, float clutch)
        {
            float ratio = gearRatios[Mathf.Clamp(currentGear, 0, gearRatios.Length - 1)];
            float drivenRpm = AverageDrivenWheelRpm() * Mathf.Abs(ratio * finalDrive);
            float clutchEngagement = 1f - Mathf.Clamp01(clutch);
            engineRpm = Mathf.Lerp(idleRpm, Mathf.Clamp(Mathf.Abs(drivenRpm), idleRpm, redlineRpm), clutchEngagement);
            float normRpm = Mathf.InverseLerp(idleRpm, redlineRpm, engineRpm);
            float torque = maxEngineTorqueNm * torqueCurve.Evaluate(normRpm) * throttle;
            float wheelTorque = torque * ratio * finalDrive * clutchEngagement;
            if (tcs && ExcessDriveSlip()) wheelTorque *= .28f;
            ApplyDriveTorque(frontAxle, wheelTorque);
            ApplyDriveTorque(rearAxle, wheelTorque);
            if (throttle < .05f && Mathf.Abs(ratio) > .01f)
            {
                float eb = engineBrakingNm * Mathf.Sign(rb.velocity.magnitude);
                ApplyDriveTorque(frontAxle, -eb);
                ApplyDriveTorque(rearAxle, -eb);
            }
        }

        private void ApplyDriveTorque(Axle axle, float torque)
        {
            if (!axle.drive) return;
            axle.left.motorTorque = torque * .5f;
            axle.right.motorTorque = torque * .5f;
        }

        private void ApplyBrakes(float brake, bool handbrake)
        {
            ApplyBrakeToWheel(frontAxle.left, brake * maxBrakeTorque);
            ApplyBrakeToWheel(frontAxle.right, brake * maxBrakeTorque);
            ApplyBrakeToWheel(rearAxle.left, brake * maxBrakeTorque + (handbrake ? maxHandbrakeTorque : 0f));
            ApplyBrakeToWheel(rearAxle.right, brake * maxBrakeTorque + (handbrake ? maxHandbrakeTorque : 0f));
        }

        private void ApplyBrakeToWheel(WheelCollider wheel, float requestedTorque)
        {
            if (!wheel) return;
            float torque = requestedTorque;
            if (abs && wheel.GetGroundHit(out WheelHit hit) && Mathf.Abs(hit.forwardSlip) > absSlip) torque *= .22f;
            wheel.brakeTorque = torque;
        }

        private bool ExcessDriveSlip()
        {
            foreach (var w in new[] { frontAxle.left, frontAxle.right, rearAxle.left, rearAxle.right })
                if (w && w.motorTorque != 0f && w.GetGroundHit(out WheelHit hit) && Mathf.Abs(hit.forwardSlip) > tcsSlip) return true;
            return false;
        }

        private float AverageDrivenWheelRpm()
        {
            float sum = 0f; int count = 0;
            foreach (var w in new[] { frontAxle.left, frontAxle.right, rearAxle.left, rearAxle.right })
            {
                if (!w) continue;
                sum += w.rpm; count++;
            }
            return count == 0 ? 0f : sum / count;
        }

        private void ApplyAero()
        {
            rb.AddForce(-rb.velocity * aeroDrag, ForceMode.Acceleration);
            rb.AddForce(-transform.up * rb.velocity.sqrMagnitude * downforce * 0.001f, ForceMode.Acceleration);
        }

        private static void UpdateWheelVisuals(Axle axle)
        {
            Sync(axle.left, axle.leftVisual);
            Sync(axle.right, axle.rightVisual);
        }

        private static void Sync(WheelCollider c, Transform t)
        {
            if (!c || !t) return;
            c.GetWorldPose(out Vector3 p, out Quaternion r);
            t.SetPositionAndRotation(p, r);
        }

        public void ShiftUp() => currentGear = Mathf.Min(currentGear + 1, gearRatios.Length - 1);
        public void ShiftDown() => currentGear = Mathf.Max(currentGear - 1, 0);
    }
}
