using System;
using System.Collections.Generic;
using UnityEngine;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Training
{
    public class DrivingLessonManager : MonoBehaviour
    {
        [Serializable]
        public class LessonStep
        {
            [TextArea] public string instructionArabic;
            public StepType type;
            public float targetValue;
            public bool completed;
        }

        public enum StepType
        {
            FastenSeatBelt,
            StartEngine,
            ReleaseHandbrake,
            SelectDrive,
            UseLeftIndicator,
            ReachSpeed,
            StopVehicle
        }

        public RealisticCarController car;
        public CarInputState input;
        public CarSystems systems;
        public List<LessonStep> steps = new List<LessonStep>();
        public int currentStepIndex;

        public event Action<string> OnInstructionChanged;
        public event Action OnLessonCompleted;

        void Start()
        {
            if (steps.Count == 0)
            {
                steps.Add(new LessonStep { type = StepType.FastenSeatBelt, instructionArabic = "اربط حزام الأمان." });
                steps.Add(new LessonStep { type = StepType.StartEngine, instructionArabic = "اضغط الفرامل ثم شغّل المحرك." });
                steps.Add(new LessonStep { type = StepType.SelectDrive, instructionArabic = "ضع ناقل الحركة على D أو الغيار الأول." });
                steps.Add(new LessonStep { type = StepType.ReleaseHandbrake, instructionArabic = "فك فرامل اليد." });
                steps.Add(new LessonStep { type = StepType.UseLeftIndicator, instructionArabic = "شغّل الإشارة اليسرى قبل الانطلاق." });
                steps.Add(new LessonStep { type = StepType.ReachSpeed, instructionArabic = "انطلق بهدوء حتى سرعة 20 كم/س.", targetValue = 20f });
                steps.Add(new LessonStep { type = StepType.StopVehicle, instructionArabic = "توقف بسلاسة في منطقة التوقف.", targetValue = 1f });
            }
            RaiseInstruction();
        }

        void Update()
        {
            if (currentStepIndex >= steps.Count || car == null || input == null || systems == null) return;
            LessonStep step = steps[currentStepIndex];
            bool done = false;

            switch (step.type)
            {
                case StepType.FastenSeatBelt: done = systems.seatBeltFastened; break;
                case StepType.StartEngine: done = car.EngineRunning; break;
                case StepType.ReleaseHandbrake: done = !input.handbrake; break;
                case StepType.SelectDrive: done = car.selectedGear != 0; break;
                case StepType.UseLeftIndicator: done = systems.leftIndicator; break;
                case StepType.ReachSpeed: done = car.speedKph >= step.targetValue; break;
                case StepType.StopVehicle: done = car.speedKph <= step.targetValue; break;
            }

            if (done)
            {
                step.completed = true;
                currentStepIndex++;
                if (currentStepIndex >= steps.Count) OnLessonCompleted?.Invoke();
                else RaiseInstruction();
            }
        }

        void RaiseInstruction()
        {
            if (currentStepIndex < steps.Count)
                OnInstructionChanged?.Invoke(steps[currentStepIndex].instructionArabic);
        }
    }
}
