using UnityEngine;

namespace RealDrivingAcademy.Vehicle
{
    public class VehicleVisualLayer : MonoBehaviour
    {
        public GameObject prototypeVisualRoot;
        public GameObject realisticVisualRoot;
        public Transform steeringWheel;
        public Transform[] wheelVisuals;

        [Tooltip("Keep the prototype geometry available for diagnostics, but hidden in normal play.")]
        public bool useRealisticVisuals = true;

        void Awake()
        {
            Apply();
        }

        public void UseRealisticVisuals()
        {
            useRealisticVisuals = true;
            Apply();
        }

        public void UsePrototypeVisuals()
        {
            useRealisticVisuals = false;
            Apply();
        }

        public void Apply()
        {
            if (prototypeVisualRoot != null)
                prototypeVisualRoot.SetActive(!useRealisticVisuals);

            if (realisticVisualRoot != null)
                realisticVisualRoot.SetActive(useRealisticVisuals);
        }
    }
}
