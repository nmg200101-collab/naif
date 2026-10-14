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
            SelectDrive,
            ReleaseHandbrake,
            UseLeftIndicator,
            ReachSpeed,
            PassCheckpoint,
            StopInTargetZone,
            SecureVehicle
        }

        public RealisticCarController car;
        public CarInputState input;
        public CarSystems systems;
        public List<LessonStep> steps = new List<LessonStep>();
        public int currentStepIndex;

        public bool checkpointReached;
        public bool insideStopZone;
        public string lastCheckpointId;

        public event Action<string> OnInstructionChanged;
        public event Action<int, LessonStep> OnStepCompleted;
        public event Action OnLessonCompleted;

        public int TotalSteps => steps != null ? steps.Count : 0;
        public int CompletedSteps => Mathf.Clamp(currentStepIndex, 0, TotalSteps);
        public float Progress01 => TotalSteps == 0 ? 0f : Mathf.Clamp01((float)CompletedSteps / TotalSteps);
        public bool IsCompleted => TotalSteps > 0 && currentStepIndex >= TotalSteps;

        public string CurrentInstruction
        {
            get
            {
                if (IsCompleted) return "اكتمل درس القيادة بنجاح.";
                if (steps == null || currentStepIndex < 0 || currentStepIndex >= steps.Count) return "";
                return steps[currentStepIndex].instructionArabic;
            }
        }

        void Start()
        {
            if (steps.Count == 0)
                BuildDefaultLesson();

            RaiseInstruction();
        }

        void BuildDefaultLesson()
        {
            steps.Clear();
            steps.Add(new LessonStep { type = StepType.FastenSeatBelt, instructionArabic = "اربط حزام الأمان." });
            steps.Add(new LessonStep { type = StepType.StartEngine, instructionArabic = "شغّل المحرك." });
            steps.Add(new LessonStep { type = StepType.SelectDrive, instructionArabic = "ضع ناقل الحركة على D." });
            steps.Add(new LessonStep { type = StepType.ReleaseHandbrake, instructionArabic = "فك فرامل اليد." });
            steps.Add(new LessonStep { type = StepType.UseLeftIndicator, instructionArabic = "شغّل الإشارة اليسرى قبل الانطلاق." });
            steps.Add(new LessonStep { type = StepType.ReachSpeed, instructionArabic = "انطلق بهدوء حتى سرعة 20 كم/س.", targetValue = 20f });
            steps.Add(new LessonStep { type = StepType.PassCheckpoint, instructionArabic = "اعبر بوابة التدريب أمامك." });
            steps.Add(new LessonStep { type = StepType.StopInTargetZone, instructionArabic = "توقف داخل منطقة التوقف المحددة.", targetValue = 1.5f });
            steps.Add(new LessonStep { type = StepType.SecureVehicle, instructionArabic = "ضع القير على N وشغّل فرامل اليد لإنهاء الدرس." });
        }

        void Update()
        {
            if (IsCompleted || car == null || input == null || systems == null) return;

            LessonStep step = steps[currentStepIndex];
            bool done = false;

            switch (step.type)
            {
                case StepType.FastenSeatBelt:
                    done = systems.seatBeltFastened;
                    break;
                case StepType.StartEngine:
                    done = car.EngineRunning;
                    break;
                case StepType.SelectDrive:
                    done = car.selectedGear > 0;
                    break;
                case StepType.ReleaseHandbrake:
                    done = !input.handbrake;
                    break;
                case StepType.UseLeftIndicator:
                    done = systems.leftIndicator;
                    break;
                case StepType.ReachSpeed:
                    done = car.speedKph >= Mathf.Max(1f, step.targetValue);
                    break;
                case StepType.PassCheckpoint:
                    done = checkpointReached;
                    break;
                case StepType.StopInTargetZone:
                    done = insideStopZone && car.speedKph <= Mathf.Max(0.5f, step.targetValue);
                    break;
                case StepType.SecureVehicle:
                    done = insideStopZone && car.speedKph <= 1.5f && car.selectedGear == 0 && input.handbrake;
                    break;
            }

            if (done)
                CompleteCurrentStep();
        }

        void CompleteCurrentStep()
        {
            int completedIndex = currentStepIndex;
            LessonStep completedStep = steps[completedIndex];
            completedStep.completed = true;
            currentStepIndex++;

            OnStepCompleted?.Invoke(completedIndex, completedStep);

            if (IsCompleted)
            {
                OnInstructionChanged?.Invoke("اكتمل درس القيادة بنجاح.");
                OnLessonCompleted?.Invoke();
            }
            else
            {
                RaiseInstruction();
            }
        }

        public void MarkCheckpoint(string checkpointId)
        {
            checkpointReached = true;
            lastCheckpointId = checkpointId;
        }

        public void SetInsideStopZone(bool value)
        {
            insideStopZone = value;
        }

        public void RestartLesson()
        {
            currentStepIndex = 0;
            checkpointReached = false;
            insideStopZone = false;
            lastCheckpointId = "";

            foreach (var step in steps)
                step.completed = false;

            RaiseInstruction();
        }

        void RaiseInstruction()
        {
            OnInstructionChanged?.Invoke(CurrentInstruction);
        }
    }
}
