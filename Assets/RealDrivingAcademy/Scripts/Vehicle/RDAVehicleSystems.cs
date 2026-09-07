using UnityEngine;

namespace RDA.V50.Vehicle
{
    public sealed class RDAVehicleSystems : MonoBehaviour
    {
        public bool SeatbeltOn { get; private set; }
        public bool HeadlightsOn { get; private set; }
        public bool WipersOn { get; private set; }
        public int Indicator { get; private set; } // -1 left, 0 off, 1 right

        public void SetSeatbelt(bool value) => SeatbeltOn = value;
        public void SetHeadlights(bool value) => HeadlightsOn = value;
        public void SetWipers(bool value) => WipersOn = value;
        public void SetIndicator(int value) => Indicator = Mathf.Clamp(value, -1, 1);
        public void HazardOff() => Indicator = 0;
    }
}