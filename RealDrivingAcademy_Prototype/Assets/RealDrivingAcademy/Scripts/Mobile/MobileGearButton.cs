using UnityEngine;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Mobile
{
    public class MobileGearButton : MonoBehaviour
    {
        public RealisticCarController car;
        public int gear;
        public void Activate() { if (car != null) car.SetGear(gear); }
    }
}
