using UnityEngine;

namespace RealDrivingAcademy.Vehicle
{
    public class VehicleLightVisuals : MonoBehaviour
    {
        public CarSystems systems;
        public CarInputState input;
        public RealisticCarController car;

        public Renderer[] headlamps;
        public Renderer[] brakeLamps;
        public Renderer[] reverseLamps;
        public Renderer[] leftIndicators;
        public Renderer[] rightIndicators;
        public Light[] lowBeamLights;

        [Header("Colors")]
        public Color lampOff = new Color(0.08f, 0.08f, 0.08f);
        public Color headlampOn = new Color(1f, 0.96f, 0.72f);
        public Color brakeOn = new Color(1f, 0.03f, 0.01f);
        public Color reverseOn = new Color(0.9f, 0.95f, 1f);
        public Color indicatorOn = new Color(1f, 0.45f, 0.02f);

        [Header("Timing")]
        public float indicatorBlinkSeconds = 0.45f;

        MaterialPropertyBlock propertyBlock;

        void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        void Update()
        {
            bool beams = systems != null && (systems.lowBeams || systems.highBeams);
            bool brake = input != null && input.brake > 0.05f;
            bool reverse = car != null && car.selectedGear < 0;

            bool blinkPhase = indicatorBlinkSeconds <= 0.01f
                || Mathf.FloorToInt(Time.time / indicatorBlinkSeconds) % 2 == 0;

            bool left = systems != null && systems.leftIndicator && blinkPhase;
            bool right = systems != null && systems.rightIndicator && blinkPhase;

            Apply(headlamps, beams, headlampOn);
            Apply(brakeLamps, brake, brakeOn);
            Apply(reverseLamps, reverse, reverseOn);
            Apply(leftIndicators, left, indicatorOn);
            Apply(rightIndicators, right, indicatorOn);

            if (lowBeamLights != null)
            {
                foreach (var light in lowBeamLights)
                    if (light != null) light.enabled = beams;
            }
        }

        void Apply(Renderer[] renderers, bool active, Color activeColor)
        {
            if (renderers == null) return;

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                renderer.GetPropertyBlock(propertyBlock);
                Color color = active ? activeColor : lampOff;

                propertyBlock.SetColor("_Color", color);
                propertyBlock.SetColor("_BaseColor", color);
                propertyBlock.SetColor("_EmissionColor", active ? activeColor * 1.6f : Color.black);

                renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
