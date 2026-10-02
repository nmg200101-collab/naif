using UnityEngine;

namespace RealDrivingAcademy.Vehicle
{
    public class DriverCameraController : MonoBehaviour
    {
        public Transform driverSeatAnchor;
        public float yawLimit = 75f;
        public float pitchLimit = 35f;
        public float lookSensitivity = 2f;
        public float returnSpeed = 3f;
        public bool returnToCenter = true;

        float yaw;
        float pitch;

        void LateUpdate()
        {
            if (driverSeatAnchor == null) return;

            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            yaw = Mathf.Clamp(yaw + mx * lookSensitivity, -yawLimit, yawLimit);
            pitch = Mathf.Clamp(pitch - my * lookSensitivity, -pitchLimit, pitchLimit);

            if (returnToCenter && Mathf.Abs(mx) < 0.01f && Mathf.Abs(my) < 0.01f)
            {
                yaw = Mathf.MoveTowards(yaw, 0f, returnSpeed * 20f * Time.deltaTime);
                pitch = Mathf.MoveTowards(pitch, 0f, returnSpeed * 20f * Time.deltaTime);
            }

            transform.position = driverSeatAnchor.position;
            transform.rotation = driverSeatAnchor.rotation * Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
