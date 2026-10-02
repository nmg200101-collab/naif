using UnityEngine;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Cockpit
{
    public class CockpitControl : MonoBehaviour
    {
        public enum ControlAction
        {
            Ignition,
            SeatBelt,
            LeftIndicator,
            RightIndicator,
            Hazards,
            LowBeams,
            HighBeams,
            Wipers,
            ShiftUp,
            ShiftDown,
            GearReverse,
            GearNeutral,
            GearDriveOrFirst,
            HandbrakeOn,
            HandbrakeOff
        }

        public ControlAction action;
        public CarSystems systems;
        public RealisticCarController car;
        public CarInputState input;

        public void Activate()
        {
            switch (action)
            {
                case ControlAction.Ignition: systems?.ToggleIgnition(); break;
                case ControlAction.SeatBelt: systems?.ToggleSeatBelt(); break;
                case ControlAction.LeftIndicator: systems?.SetLeftIndicator(); break;
                case ControlAction.RightIndicator: systems?.SetRightIndicator(); break;
                case ControlAction.Hazards: systems?.ToggleHazards(); break;
                case ControlAction.LowBeams: systems?.ToggleLowBeams(); break;
                case ControlAction.HighBeams: systems?.ToggleHighBeams(); break;
                case ControlAction.Wipers: systems?.CycleWipers(); break;
                case ControlAction.ShiftUp: car?.ShiftUp(); break;
                case ControlAction.ShiftDown: car?.ShiftDown(); break;
                case ControlAction.GearReverse: car?.SetGear(-1); break;
                case ControlAction.GearNeutral: car?.SetGear(0); break;
                case ControlAction.GearDriveOrFirst: car?.SetGear(1); break;
                case ControlAction.HandbrakeOn: input?.SetHandbrake(true); break;
                case ControlAction.HandbrakeOff: input?.SetHandbrake(false); break;
            }
        }
    }
}
