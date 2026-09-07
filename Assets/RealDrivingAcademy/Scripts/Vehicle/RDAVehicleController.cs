using System;
using UnityEngine;
using RDA.V50.Input;

namespace RDA.V50.Vehicle
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
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
        }

        [SerializeField] private RDAInputState input;
        [SerializeField] private Axle frontAxle = new Axle();
        [SerializeField] private Axle rearAxle = new Axle();
        [SerializeField, Min(1f)] private float massKg = 1420f;
        [SerializeField, Range(1f, 50f)] private float maxSteerDeg = 34f;
        [SerializeField, Min(.5f)] private float wheelBase = 2.63f;
        [SerializeField, Min(.5f)] private float trackWidth = 1.55f;
        [SerializeField, Min(0f)] private float driveTorque = 1800f;
        [SerializeField, Min(0f)] private float brakeTorque = 3600f;
        [SerializeField, Min(0f)] private float handbrakeTorque = 5200f;

        private Rigidbody body;
        public float SpeedKph => body ? body.velocity.magnitude * 3.6f : 0f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = massKg;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void FixedUpdate()
        {
            if (!input || !ValidateRuntimeReferences()) return;
            ApplyAckermann(input.Steering);
            ApplyDrive(input.Throttle);
            ApplyBrakes(input.Brake, input.Handbrake);
            SyncAxle(frontAxle);
            SyncAxle(rearAxle);
        }

        private bool ValidateRuntimeReferences() => frontAxle.left && frontAxle.right && rearAxle.left && rearAxle.right;

        private void ApplyAckermann(float steering)
        {
            float requested = maxSteerDeg * Mathf.Clamp(steering, -1f, 1f);
            if (Mathf.Abs(requested) < .01f) { frontAxle.left.steerAngle = frontAxle.right.steerAngle = 0f; return; }
            float radius = wheelBase / Mathf.Tan(Mathf.Abs(requested) * Mathf.Deg2Rad);
            float inner = Mathf.Atan(wheelBase / Mathf.Max(.1f, radius - trackWidth * .5f)) * Mathf.Rad2Deg;
            float outer = Mathf.Atan(wheelBase / (radius + trackWidth * .5f)) * Mathf.Rad2Deg;
            bool right = requested > 0f;
            frontAxle.left.steerAngle = right ? outer : -inner;
            frontAxle.right.steerAngle = right ? inner : -outer;
        }

        private void ApplyDrive(float throttle)
        {
            float torque = Mathf.Clamp01(throttle) * driveTorque;
            ApplyDrive(frontAxle, torque);
            ApplyDrive(rearAxle, torque);
        }

        private static void ApplyDrive(Axle axle, float torque)
        {
            if (!axle.drive) return;
            axle.left.motorTorque = axle.right.motorTorque = torque * .5f;
        }

        private void ApplyBrakes(float brake, bool handbrake)
        {
            float service = Mathf.Clamp01(brake) * brakeTorque;
            frontAxle.left.brakeTorque = frontAxle.right.brakeTorque = service;
            rearAxle.left.brakeTorque = rearAxle.right.brakeTorque = service + (handbrake ? handbrakeTorque : 0f);
        }

        private static void SyncAxle(Axle axle) { Sync(axle.left, axle.leftVisual); Sync(axle.right, axle.rightVisual); }
        private static void Sync(WheelCollider collider, Transform visual)
        {
            if (!collider || !visual) return;
            collider.GetWorldPose(out var position, out var rotation);
            visual.SetPositionAndRotation(position, rotation);
        }
    }
}