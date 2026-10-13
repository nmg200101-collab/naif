using UnityEngine;

namespace RealDrivingAcademy.Vehicle
{
    public class VehicleCameraRig : MonoBehaviour
    {
        public Camera driverCamera;
        public Camera chaseCamera;
        public AudioListener driverListener;
        public AudioListener chaseListener;
        public int mode;

        void Start()
        {
            ApplyMode();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
                ToggleCamera();
        }

        public void ToggleCamera()
        {
            mode = mode == 0 ? 1 : 0;
            ApplyMode();
        }

        void ApplyMode()
        {
            bool driver = mode == 0;

            if (driverCamera != null) driverCamera.enabled = driver;
            if (chaseCamera != null) chaseCamera.enabled = !driver;
            if (driverListener != null) driverListener.enabled = driver;
            if (chaseListener != null) chaseListener.enabled = !driver;
        }
    }
}
