using System;
using System.Collections.Generic;
using UnityEngine;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Training
{
    public enum ViolationType
    {
        NoSeatBelt,
        Speeding,
        RedLight,
        NoIndicator,
        Collision,
        WrongWay,
        StopLine,
        UnsafeParking
    }

    [Serializable]
    public class ViolationRecord
    {
        public ViolationType type;
        public int points;
        public string message;
        public float time;
    }

    public class DrivingViolationSystem : MonoBehaviour
    {
        public RealisticCarController car;
        public CarSystems systems;
        public float currentSpeedLimitKph = 50f;
        public int score = 100;
        public List<ViolationRecord> records = new List<ViolationRecord>();

        float speedingTimer;
        float noSeatBeltTimer;

        public event Action<ViolationRecord> OnViolation;

        void Update()
        {
            if (car == null || systems == null) return;

            if (car.speedKph > currentSpeedLimitKph + 5f)
            {
                speedingTimer += Time.deltaTime;
                if (speedingTimer >= 3f)
                {
                    AddViolation(ViolationType.Speeding, 2, "تجاوزت حد السرعة المسموح.");
                    speedingTimer = 0f;
                }
            }
            else speedingTimer = 0f;

            if (car.speedKph > 3f && !systems.seatBeltFastened)
            {
                noSeatBeltTimer += Time.deltaTime;
                if (noSeatBeltTimer >= 2f)
                {
                    AddViolation(ViolationType.NoSeatBelt, 5, "يجب ربط حزام الأمان قبل التحرك.");
                    noSeatBeltTimer = -999f;
                }
            }
        }

        public void AddViolation(ViolationType type, int penalty, string message)
        {
            score = Mathf.Max(0, score - penalty);
            var record = new ViolationRecord
            {
                type = type,
                points = penalty,
                message = message,
                time = Time.time
            };
            records.Add(record);
            OnViolation?.Invoke(record);
        }

        public void SetSpeedLimit(float kph) => currentSpeedLimitKph = Mathf.Max(5f, kph);
    }
}
