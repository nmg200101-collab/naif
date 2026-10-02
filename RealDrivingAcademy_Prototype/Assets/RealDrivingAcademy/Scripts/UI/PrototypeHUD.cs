using UnityEngine;
using UnityEngine.UI;
using RealDrivingAcademy.Vehicle;
using RealDrivingAcademy.Training;

namespace RealDrivingAcademy.UI
{
    public class PrototypeHUD : MonoBehaviour
    {
        public RealisticCarController car;
        public DrivingViolationSystem violations;
        public DrivingLessonManager lesson;
        public Text speedText;
        public Text rpmText;
        public Text gearText;
        public Text scoreText;
        public Text lessonText;
        public Text warningText;

        void OnEnable()
        {
            if (lesson != null) lesson.OnInstructionChanged += SetLesson;
            if (violations != null) violations.OnViolation += ShowViolation;
        }

        void OnDisable()
        {
            if (lesson != null) lesson.OnInstructionChanged -= SetLesson;
            if (violations != null) violations.OnViolation -= ShowViolation;
        }

        void Update()
        {
            if (car != null)
            {
                if (speedText != null) speedText.text = Mathf.RoundToInt(car.speedKph) + " km/h";
                if (rpmText != null) rpmText.text = Mathf.RoundToInt(car.engineRpm) + " RPM";
                if (gearText != null) gearText.text = car.selectedGear < 0 ? "R" : car.selectedGear == 0 ? "N" : car.selectedGear.ToString();
            }
            if (scoreText != null && violations != null) scoreText.text = "Score: " + violations.score;
        }

        void SetLesson(string text)
        {
            if (lessonText != null) lessonText.text = text;
        }

        void ShowViolation(ViolationRecord record)
        {
            if (warningText != null) warningText.text = record.message + "  -" + record.points;
        }
    }
}
