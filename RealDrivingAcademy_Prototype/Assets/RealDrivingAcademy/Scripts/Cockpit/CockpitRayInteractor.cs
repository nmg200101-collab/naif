using UnityEngine;

namespace RealDrivingAcademy.Cockpit
{
    public class CockpitRayInteractor : MonoBehaviour
    {
        public Camera driverCamera;
        public float maxDistance = 1.5f;
        public LayerMask interactableLayers = ~0;

        void Update()
        {
            if (driverCamera == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = driverCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayers, QueryTriggerInteraction.Collide))
                {
                    var control = hit.collider.GetComponentInParent<CockpitControl>();
                    if (control != null) control.Activate();
                }
            }
        }
    }
}
