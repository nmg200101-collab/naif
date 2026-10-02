using UnityEngine;
using UnityEngine.EventSystems;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Mobile
{
    public class HoldHandbrakeButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public CarInputState input;
        public bool latchMode = true;
        bool latched = true;

        void Start()
        {
            if (input != null) input.SetHandbrake(latched);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (input == null) return;
            if (latchMode)
            {
                latched = !latched;
                input.SetHandbrake(latched);
            }
            else input.SetHandbrake(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!latchMode && input != null) input.SetHandbrake(false);
        }
    }
}
