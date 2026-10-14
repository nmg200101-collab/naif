using UnityEngine;

namespace RealDrivingAcademy.Vehicle
{
    public class DriverRigAnchors : MonoBehaviour
    {
        public Transform seatAnchor;
        public Transform headAnchor;
        public Transform leftHandTarget;
        public Transform rightHandTarget;
        public Transform leftFootTarget;
        public Transform rightFootTarget;

        public bool IsReady =>
            seatAnchor != null &&
            headAnchor != null &&
            leftHandTarget != null &&
            rightHandTarget != null;
    }
}
