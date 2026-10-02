using UnityEngine;
using UnityEngine.EventSystems;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Mobile
{
    public class TouchPedal : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public enum Pedal { Throttle, Brake, Clutch }
        public Pedal pedal;
        public CarInputState input;
        [Range(0f,1f)] public float pressedValue = 1f;

        public void OnPointerDown(PointerEventData eventData) => Set(pressedValue);
        public void OnPointerUp(PointerEventData eventData) => Set(0f);
        void OnDisable() => Set(0f);

        void Set(float value)
        {
            if (input == null) return;
            switch (pedal)
            {
                case Pedal.Throttle: input.SetThrottle(value); break;
                case Pedal.Brake: input.SetBrake(value); break;
                case Pedal.Clutch: input.SetClutch(value); break;
            }
        }
    }
}
