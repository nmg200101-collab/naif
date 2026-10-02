using UnityEngine;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Cockpit
{
    public class GearSelectorVisual : MonoBehaviour
    {
        public RealisticCarController car;
        public Transform lever;
        public Vector3 reversePosition = new Vector3(0f, 0f, -0.035f);
        public Vector3 neutralPosition = Vector3.zero;
        public Vector3 drivePosition = new Vector3(0f, 0f, 0.045f);
        public float smooth = 10f;
        Vector3 basePosition;

        void Awake() { if (lever != null) basePosition = lever.localPosition; }
        void LateUpdate()
        {
            if (car == null || lever == null) return;
            Vector3 offset = car.selectedGear < 0 ? reversePosition : car.selectedGear == 0 ? neutralPosition : drivePosition;
            lever.localPosition = Vector3.Lerp(lever.localPosition, basePosition + offset, smooth * Time.deltaTime);
        }
    }
}
