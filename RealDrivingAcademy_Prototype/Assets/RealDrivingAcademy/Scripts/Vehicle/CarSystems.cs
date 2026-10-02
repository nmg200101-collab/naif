using UnityEngine;

namespace RealDrivingAcademy.Vehicle
{
    public class CarSystems : MonoBehaviour
    {
        public bool ignitionOn;
        public bool seatBeltFastened;
        public bool lowBeams;
        public bool highBeams;
        public bool hazardLights;
        public bool leftIndicator;
        public bool rightIndicator;
        [Range(0, 2)] public int wiperSpeed;

        public AudioSource indicatorAudio;
        public AudioSource engineStartAudio;

        public void ToggleIgnition()
        {
            ignitionOn = !ignitionOn;
            if (ignitionOn && engineStartAudio != null) engineStartAudio.Play();
        }

        public void ToggleSeatBelt() => seatBeltFastened = !seatBeltFastened;
        public void ToggleLowBeams() => lowBeams = !lowBeams;
        public void ToggleHighBeams() => highBeams = !highBeams;
        public void ToggleHazards()
        {
            hazardLights = !hazardLights;
            if (hazardLights) { leftIndicator = true; rightIndicator = true; }
            else { leftIndicator = false; rightIndicator = false; }
        }

        public void SetLeftIndicator()
        {
            hazardLights = false;
            leftIndicator = !leftIndicator;
            rightIndicator = false;
            TickIndicator();
        }

        public void SetRightIndicator()
        {
            hazardLights = false;
            rightIndicator = !rightIndicator;
            leftIndicator = false;
            TickIndicator();
        }

        public void CancelIndicators()
        {
            hazardLights = false;
            leftIndicator = false;
            rightIndicator = false;
        }

        public void CycleWipers() => wiperSpeed = (wiperSpeed + 1) % 3;

        void TickIndicator()
        {
            if (indicatorAudio != null) indicatorAudio.Play();
        }
    }
}
