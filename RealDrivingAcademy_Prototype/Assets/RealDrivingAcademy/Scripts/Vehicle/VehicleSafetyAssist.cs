using UnityEngine;

namespace RealDrivingAcademy.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleSafetyAssist : MonoBehaviour
    {
        public RealisticCarController car;
        public float resetBelowY = -8f;
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        void Update()
        {
            if (transform.position.y < resetBelowY) ResetVehicle();
        }

        public void ResetVehicle()
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(spawnPosition + Vector3.up * 0.5f, spawnRotation);
            if (car != null) car.SetGear(0);
        }
    }
}
