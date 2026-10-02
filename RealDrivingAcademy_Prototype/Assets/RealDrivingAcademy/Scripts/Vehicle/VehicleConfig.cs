using UnityEngine;

namespace RealDrivingAcademy.Vehicle
{
    [CreateAssetMenu(menuName = "Real Driving Academy/Vehicle Config", fileName = "VehicleConfig")]
    public class VehicleConfig : ScriptableObject
    {
        [Header("Identity")]
        public string vehicleName = "Training Sedan";
        public bool automaticTransmission = true;

        [Header("Engine")]
        [Min(500f)] public float idleRpm = 850f;
        [Min(1000f)] public float redlineRpm = 6500f;
        [Min(1f)] public float maxEngineTorque = 260f;
        [Min(0.1f)] public float finalDriveRatio = 3.9f;
        public AnimationCurve torqueCurve = new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.35f, 1f),
            new Keyframe(0.75f, 0.86f),
            new Keyframe(1f, 0.55f));

        [Header("Transmission")]
        public float[] forwardGearRatios = { 3.55f, 2.10f, 1.36f, 1.03f, 0.84f, 0.72f };
        public float reverseGearRatio = -3.25f;
        [Range(0f, 1f)] public float drivetrainEfficiency = 0.88f;
        public float autoUpshiftRpm = 5000f;
        public float autoDownshiftRpm = 1650f;

        [Header("Chassis")]
        [Min(500f)] public float mass = 1450f;
        public Vector3 centerOfMass = new Vector3(0f, -0.45f, 0.05f);
        [Min(1f)] public float maxSteerAngle = 34f;
        [Min(1f)] public float serviceBrakeTorque = 3300f;
        [Min(1f)] public float handbrakeTorque = 5200f;
        [Min(1f)] public float maxSpeedKph = 190f;
        [Range(0f, 1f)] public float absSlipThreshold = 0.42f;
        [Range(0f, 1f)] public float tractionSlipThreshold = 0.38f;

        [Header("Assists")]
        public bool absEnabled = true;
        public bool tractionControlEnabled = true;
        [Range(0f, 1f)] public float steeringAssistAtTopSpeed = 0.35f;
    }
}
