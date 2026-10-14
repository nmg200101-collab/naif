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
        bool noSeatBeltPenalized;

        readonly Dictionary<ViolationType, float> lastViolationTimes =
            new Dictionary<ViolationType, float>();

        public event Action<ViolationRecord> OnViolation;
        public event Action<int> OnScoreChanged;

        public int TotalPenaltyPoints => Mathf.Clamp(100 - score, 0, 100);
        public int ViolationCount => records != null ? records.Count : 0;

        public string Grade
        {
            get
            {
                if (score >= 90) return "A";
                if (score >= 80) return "B";
                if (score >= 70) return "C";
                if (score >= 60) return "D";
                return "F";
            }
        }

        void Update()
        {
            if (car == null || systems == null) return;

            UpdateSpeeding();
            UpdateSeatBelt();
        }

        void UpdateSpeeding()
        {
            if (car.speedKph > currentSpeedLimitKph + 5f)
            {
                speedingTimer += Time.deltaTime;
                if (speedingTimer >= 2.5f)
                {
                    AddViolation(
                        ViolationType.Speeding,
                        2,
                        "تجاوزت حد السرعة المسموح.",
                        4f);
                    speedingTimer = 0f;
                }
            }
            else
            {
                speedingTimer = 0f;
            }
        }

        void UpdateSeatBelt()
        {
            if (noSeatBeltPenalized) return;

            if (car.speedKph > 3f && !systems.seatBeltFastened)
            {
                noSeatBeltTimer += Time.deltaTime;
                if (noSeatBeltTimer >= 2f)
                {
                    AddViolation(
                        ViolationType.NoSeatBelt,
                        5,
                        "يجب ربط حزام الأمان قبل التحرك.");

                    noSeatBeltPenalized = true;
                    noSeatBeltTimer = 0f;
                }
            }
            else
            {
                noSeatBeltTimer = 0f;
            }
        }

        public bool AddViolation(
            ViolationType type,
            int penalty,
            string message,
            float cooldownSeconds = 0f)
        {
            penalty = Mathf.Max(0, penalty);

            if (cooldownSeconds > 0f &&
                lastViolationTimes.TryGetValue(type, out float lastTime) &&
                Time.time - lastTime < cooldownSeconds)
            {
                return false;
            }

            lastViolationTimes[type] = Time.time;
            score = Mathf.Clamp(score - penalty, 0, 100);

            var record = new ViolationRecord
            {
                type = type,
                points = penalty,
                message = message,
                time = Time.time
            };

            records.Add(record);
            OnViolation?.Invoke(record);
            OnScoreChanged?.Invoke(score);
            return true;
        }

        public void SetSpeedLimit(float kph)
        {
            currentSpeedLimitKph = Mathf.Max(5f, kph);
        }

        public void ResetSessionScore()
        {
            score = 100;
            records.Clear();
            lastViolationTimes.Clear();
            speedingTimer = 0f;
            noSeatBeltTimer = 0f;
            noSeatBeltPenalized = false;
            OnScoreChanged?.Invoke(score);
        }
    }
}
