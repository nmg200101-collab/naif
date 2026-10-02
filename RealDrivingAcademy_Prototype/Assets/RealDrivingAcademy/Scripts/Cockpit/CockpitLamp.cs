using UnityEngine;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Cockpit
{
    public class CockpitLamp : MonoBehaviour
    {
        public enum LampType { Ignition, SeatBelt, LeftIndicator, RightIndicator, LowBeams, HighBeams, Hazards }
        public LampType type;
        public CarSystems systems;
        public Renderer target;
        public Color onColor = Color.green;
        public Color offColor = new Color(0.08f,0.08f,0.08f);
        public bool blinkIndicator = true;
        public float blinkHz = 1.4f;

        void Update()
        {
            if (systems == null || target == null) return;
            bool on = false;
            switch (type)
            {
                case LampType.Ignition: on = systems.ignitionOn; break;
                case LampType.SeatBelt: on = !systems.seatBeltFastened; break;
                case LampType.LeftIndicator: on = systems.leftIndicator; break;
                case LampType.RightIndicator: on = systems.rightIndicator; break;
                case LampType.LowBeams: on = systems.lowBeams; break;
                case LampType.HighBeams: on = systems.highBeams; break;
                case LampType.Hazards: on = systems.hazardLights; break;
            }
            if (blinkIndicator && (type == LampType.LeftIndicator || type == LampType.RightIndicator || type == LampType.Hazards) && on)
                on = Mathf.Repeat(Time.time * blinkHz, 1f) < 0.5f;
            var mat = target.material;
            mat.color = on ? onColor : offColor;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", on ? onColor * 2f : Color.black);
            }
        }
    }
}
